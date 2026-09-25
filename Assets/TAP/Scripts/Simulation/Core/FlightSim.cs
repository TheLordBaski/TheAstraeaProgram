using System;
using System.Collections;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Owns universal time, the floating reference frame, all vessels and the fixed-step physics loop.
    ///
    /// Per physics step:
    ///   PRE  (FixedUpdate)      structure events, kinematic ground colliders -> UT+dt, vessel forces
    ///   PhysX step + collision callbacks
    ///   POST (WaitForFixedUpdate) UT += dt, origin advance, loads/orbits, structure events,
    ///                            SOI transitions, Krakensbane velocity rebase, floating origin rebase,
    ///                            load/unload by distance.
    /// On-rails time warp bypasses the rigid-body simulation and propagates Kepler orbits analytically.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed partial class FlightSim : MonoBehaviour
    {
        public static FlightSim Instance { get; private set; }

        public CelestialSystem System { get; private set; }
        public PartDatabase PartDb { get; private set; }
        public ReferenceFrame Frame { get; private set; }
        public double UT;
        public double PreviousUT;
        public bool Paused;
        public bool AllowKeplerLock = true;
        public FlightEffects Effects;
        public PlanetManager Planets;
        public readonly TimeWarp Warp = new TimeWarp();

        public readonly List<VesselHandle> Handles = new List<VesselHandle>();
        public readonly List<Vessel> LoadedVessels = new List<Vessel>();
        public Vessel ActiveVessel { get; private set; }
        public VesselHandle ActiveHandle => ActiveVessel != null ? ActiveVessel.Handle : null;

        public const float LoadDistance = 2200f;
        public const float UnloadDistance = 2600f;
        public const float FloatingOriginThreshold = 800f;
        public const double AtmosphereDeleteAltitudeFraction = 0.38; // unloaded vessels below this fraction of atmosphere height are lost

        public event Action<string, bool> Message;
        public event Action<string, string> CrewLost;            // crew, reason
        public event Action<string, string> CrewStatusChanged;   // crew, vessel id ("" = EVA/none)
        public event Action<Vessel> ActiveVesselChanged;
        public event Action<VesselHandle> VesselDestroyed;
        public event Action<VesselHandle> VesselRecovered;

        private bool _stepPending;
        private readonly List<(Collider a, Collider b, double until)> _ignored = new List<(Collider, Collider, double)>();
        private readonly HashSet<Vessel> _structurePass = new HashSet<Vessel>();
        private double _nextLoadCheck;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            Instance = this;
            System = CelestialSystem.Default;
            PartDb = PartDatabase.Instance;
            Physics.gravity = Vector3.zero;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.1f;
            Layers.ConfigureCollisionMatrix();
            Frame = new ReferenceFrame(System.Root);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void OnEnable() { StartCoroutine(PostStepLoop()); }

        public void Log(string msg, bool important = false)
        {
            Debug.Log("[Flight] " + msg);
            Message?.Invoke(msg, important);
        }

        // ------------------------------------------------------------------ physics loop

        private void FixedUpdate()
        {
            _stepPending = false;
            if (Paused || Warp.OnRails || ActiveVessel == null) return;
            double dt = Time.fixedDeltaTime;
            PreviousUT = UT;
            Frame.PreviousOrigin = Frame.Origin;

            // Structure changes requested since last step (staging, decouplers queued by input).
            ProcessStructureEvents();
            ProcessRequests();

            // Move kinematic ground colliders to their pose at the end of this step.
            Planets?.PreStepColliders(UT + dt, dt);
            UpdateStaticVessels(UT + dt, true);
            UpdatePadHolds(UT + dt, true);

            for (int i = 0; i < LoadedVessels.Count; i++)
            {
                var v = LoadedVessels[i];
                if (v == null || v.IsFlag || v.Rb == null) continue;
                if (v.PadHold) { v.UpdateFlightState(UT); continue; }
                if (v.Rb.isKinematic) continue;
                v.PreStep(dt, UT);
            }
            _stepPending = true;
        }

        private IEnumerator PostStepLoop()
        {
            var wait = new WaitForFixedUpdate();
            while (true)
            {
                yield return wait;
                if (_stepPending)
                {
                    _stepPending = false;
                    try { PostStep(Time.fixedDeltaTime); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
        }

        private void PostStep(double dt)
        {
            UT += dt;
            Frame.Origin += Frame.Velocity * dt;

            for (int i = 0; i < LoadedVessels.Count; i++)
            {
                var v = LoadedVessels[i];
                if (v == null || v.IsFlag || v.Rb == null || v.Rb.isKinematic) continue;
                v.PostStep(dt, UT);
            }
            ProcessStructureEvents();
            if (ActiveVessel == null) return;

            CheckSoiTransition();
            Krakensbane();
            FloatingOrigin(false);
            UpdateIgnoredCollisions();
            if (UT >= _nextLoadCheck)
            {
                _nextLoadCheck = UT + 0.5;
                UpdateLoadedSet();
            }
            CheckWarpSafety();
        }

        public void RequestStructurePass(Vessel v) { if (v != null) _structurePass.Add(v); }

        private void ProcessStructureEvents()
        {
            // Iterate over a snapshot; splitting creates new vessels.
            var snapshot = new List<Vessel>(LoadedVessels);
            foreach (var v in snapshot)
                if (v != null && v.HasPendingStructureEvents) v.ProcessStructureEvents();
            _structurePass.Clear();
        }

        /// <summary>Galilean rebase: make the active vessel's Unity velocity zero.</summary>
        private void Krakensbane()
        {
            var a = ActiveVessel;
            if (a == null || a.Rb == null) return;
            Vector3 dv;
            if (a.PadHold) dv = (Vector3)(Frame.Body.FrameVelocityAt(Frame.ToTrue(a.Rb.worldCenterOfMass)) - Frame.Velocity);
            else if (a.Rb.isKinematic) return;
            else dv = a.Rb.linearVelocity;
            if (dv.sqrMagnitude < 1e-10f) return;
            Frame.Velocity += (Vector3d)dv;
            foreach (var v in LoadedVessels)
            {
                if (v == null || v.Rb == null || v.Rb.isKinematic) continue;
                v.Rb.linearVelocity -= dv;
            }
            Effects?.ShiftVelocity(-dv);
        }

        /// <summary>Keeps the active vessel near the Unity origin.</summary>
        public void FloatingOrigin(bool force)
        {
            var a = ActiveVessel;
            if (a == null) return;
            Vector3 p = a.Rb != null ? a.Rb.worldCenterOfMass : a.transform.position;
            if (!force && p.magnitude < FloatingOriginThreshold) return;
            ShiftOrigin(p);
        }

        public void ShiftOrigin(Vector3 delta)
        {
            Frame.Origin += (Vector3d)delta;
            Frame.PreviousOrigin += (Vector3d)delta;
            foreach (var v in LoadedVessels)
            {
                if (v == null) continue;
                if (v.Rb != null)
                {
                    v.Rb.position -= delta;
                    v.transform.position -= delta;
                }
                else v.transform.position -= delta;
            }
            Planets?.OnOriginShift(delta);
            Effects?.ShiftPositions(-delta);
            Physics.SyncTransforms();
            OriginShifted?.Invoke(delta);
        }

        public event Action<Vector3> OriginShifted;

        /// <summary>Changes the reference body when the active vessel crosses an SOI boundary.</summary>
        private void CheckSoiTransition()
        {
            var a = ActiveVessel;
            if (a == null) return;
            var body = Frame.Body;
            Vector3d pos = a.TruePosition;
            CelestialBody next = null;
            if (body.Parent != null && pos.magnitude > body.SOIRadius) next = body.Parent;
            else
            {
                foreach (var c in body.Children)
                {
                    if ((pos - (c.GetPositionAtUT(UT) - body.GetPositionAtUT(UT))).magnitude < c.SOIRadius) { next = c; break; }
                }
            }
            if (next == null) return;
            ChangeFrameBody(next);
            Log($"Entering {next.Name}'s sphere of influence");
        }

        public void ChangeFrameBody(CelestialBody next)
        {
            Frame.ChangeBody(next, UT);
            foreach (var v in LoadedVessels)
            {
                if (v == null || v.Rb == null) continue;
                v.Handle.Body = next;
                v.UpdateFlightState(UT);
                if (!v.Rb.isKinematic)
                    v.Orbit = new Orbit(v.TruePosition, v.TrueVelocity, UT, next.GM);
                else if (v.Handle.Orbit != null && !v.Handle.Landed)
                    v.Handle.Orbit = v.Orbit = new Orbit(v.Handle.PositionRelBody(UT), v.Handle.VelocityRelBody(UT), UT, next.GM);
            }
            Planets?.OnFrameBodyChanged(next);
        }

        public void IgnoreCollisions(Vessel a, Vessel b, float seconds)
        {
            if (a == null || b == null) return;
            var ca = a.GetComponentsInChildren<Collider>();
            var cb = b.GetComponentsInChildren<Collider>();
            foreach (var x in ca)
                foreach (var y in cb)
                {
                    Physics.IgnoreCollision(x, y, true);
                    _ignored.Add((x, y, UT + seconds));
                }
        }

        private void UpdateIgnoredCollisions()
        {
            for (int i = _ignored.Count - 1; i >= 0; i--)
            {
                var e = _ignored[i];
                if (e.a == null || e.b == null) { _ignored.RemoveAt(i); continue; }
                if (UT >= e.until)
                {
                    Physics.IgnoreCollision(e.a, e.b, false);
                    _ignored.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ render

        /// <summary>Interpolation factor between the last two physics states for the current frame.</summary>
        public double RenderAlpha
        {
            get
            {
                if (Warp.OnRails || Paused) return 1;
                double a = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
                return MathD.Clamp01(a);
            }
        }

        /// <summary>UT corresponding to what is being rendered this frame.</summary>
        public double RenderUT => Warp.OnRails || Paused ? UT : UT - Time.fixedDeltaTime * (1 - RenderAlpha);

        private void Update()
        {
            if (!Paused && Warp.OnRails) RailsUpdate(Time.unscaledDeltaTime);
            float dt = Time.deltaTime;
            foreach (var v in LoadedVessels)
            {
                if (v == null) continue;
                foreach (var p in v.Parts)
                {
                    for (int k = 0; k < p.Modules.Count; k++) p.Modules[k].OnRenderUpdate(dt);
                    p.UpdateHeatGlow();
                }
            }
        }

        private void LateUpdate()
        {
            double rut = RenderUT;
            double alpha = RenderAlpha;
            Planets?.UpdateVisuals(rut, alpha);
            if (Warp.OnRails || Paused) { UpdateStaticVessels(UT, false); UpdatePadHolds(UT, false); }
            else UpdateStaticVesselVisuals(rut, alpha);
        }
    }
}
