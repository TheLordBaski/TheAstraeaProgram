using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed partial class Vessel
    {
        // ------------------------------------------------------------------ mass properties

        public double TotalMass { get; private set; }
        public Vector3 LocalCenterOfMass { get; private set; }
        /// <summary>World centre of mass from the physics pose and our mass model (valid immediately after changes).</summary>
        public Vector3 WorldCoM => Rb != null ? Rb.position + Rb.rotation * LocalCenterOfMass : transform.TransformPoint(LocalCenterOfMass);
        /// <summary>Principal moments are applied to the Rigidbody; this is the control-frame diagonal (pitch, roll, yaw axes).</summary>
        public Vector3 ControlFrameInertia { get; private set; } = Vector3.one * 1000;
        /// <summary>Available torque about control axes (N*m) from wheels, gimbals and RCS, measured last step.</summary>
        public Vector3 TorqueAuthority { get; set; } = Vector3.one * 1000;
        private Vector3 _authorityAccum;

        private Matrix3d _inertiaLocal;

        public void UpdateMassProperties()
        {
            if (Parts.Count == 0) return;
            double M = 0;
            Vector3d sum = Vector3d.zero;
            foreach (var p in Parts)
            {
                double m = Math.Max(p.Mass, 0.1);
                M += m;
                sum += (Vector3d)p.transform.localPosition * m;
            }
            Vector3d com = sum / M;
            var I = Matrix3d.Zero;
            foreach (var p in Parts)
            {
                double m = Math.Max(p.Mass, 0.1);
                Vector3d r = (Vector3d)p.transform.localPosition - com;
                I += Matrix3d.PointMass(m, r);
                double rad = Math.Max(0.1, p.Def.diameter * 0.5);
                double h = Math.Max(0.1, p.Def.height);
                var d = new Vector3d(m * (3 * rad * rad + h * h) / 12.0, m * rad * rad * 0.5, m * (3 * rad * rad + h * h) / 12.0);
                I += Matrix3d.RotatedDiagonal(p.transform.localRotation, d);
            }
            _inertiaLocal = I;
            TotalMass = M;
            LocalCenterOfMass = (Vector3)com;

            I.Eigen(out Vector3d ev, out double[,] V);
            double maxEv = Math.Max(ev.x, Math.Max(ev.y, ev.z));
            double minAllowed = Math.Max(maxEv * 0.002, 0.05);
            var diag = new Vector3((float)Math.Max(ev.x, minAllowed), (float)Math.Max(ev.y, minAllowed), (float)Math.Max(ev.z, minAllowed));
            Vector3 c0 = new Vector3((float)V[0, 0], (float)V[1, 0], (float)V[2, 0]);
            Vector3 c1 = new Vector3((float)V[0, 1], (float)V[1, 1], (float)V[2, 1]);
            Vector3 c2 = Vector3.Cross(c0, c1); // enforce right-handed basis consistent with Unity rotation
            if (Vector3.Dot(c2, new Vector3((float)V[0, 2], (float)V[1, 2], (float)V[2, 2])) < 0)
            {
                // eigenvector 2 opposite of cross -> fine, rotation uses the cross product anyway
            }
            Quaternion rot = c0.sqrMagnitude > 0.5f && c1.sqrMagnitude > 0.5f ? Quaternion.LookRotation(c2, c1) : Quaternion.identity;
            // LookRotation(forward=z, up=y) produces x = cross(y, z) which equals c0 for right-handed basis.
            Rb.mass = (float)M;
            Rb.centerOfMass = LocalCenterOfMass;
            Rb.inertiaTensor = diag;
            Rb.inertiaTensorRotation = rot;

            // Control-frame diagonal inertia (for SAS).
            Quaternion ctrlLocal = Quaternion.Inverse(transform.rotation) * ControlRotation;
            Vector3 ix = ctrlLocal * Vector3.right, iy = ctrlLocal * Vector3.up, iz = ctrlLocal * Vector3.forward;
            ControlFrameInertia = new Vector3((float)Quad(I, ix), (float)Quad(I, iy), (float)Quad(I, iz));
        }

        private static double Quad(Matrix3d I, Vector3 a)
        {
            double r = 0;
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r += a[i] * I[i, j] * a[j];
            return r;
        }

        /// <summary>Accumulate control authority (called by actuators each step), in control-frame axes.</summary>
        public void AddAuthority(Vector3 controlFrameTorque)
        {
            _authorityAccum += new Vector3(Mathf.Abs(controlFrameTorque.x), Mathf.Abs(controlFrameTorque.y), Mathf.Abs(controlFrameTorque.z));
        }

        // ------------------------------------------------------------------ flight state (true frame)

        public Vector3d TruePosition { get; private set; }  // CoM relative to frame body, inertial
        public Vector3d TrueVelocity { get; private set; }
        public Orbit Orbit { get; set; }
        public CelestialBody MainBody => Sim.Frame.Body;
        public bool KeplerLocked { get; private set; }
        public bool ForceUnlockThisStep;
        public Situation Situation { get => Record.situation; set => Record.situation = value; }

        public double Altitude { get; private set; }
        public double TerrainHeight { get; private set; }
        public double AltitudeAGL { get; private set; }
        public double VerticalSpeed { get; private set; }
        public Vector3d SurfaceVelocity { get; private set; }
        public double SurfaceSpeed => SurfaceVelocity.magnitude;
        public double OrbitalSpeed => TrueVelocity.magnitude;
        public double StaticPressure { get; private set; }
        public double AirDensity { get; private set; }
        public double AirTemperature { get; private set; }
        public double SpeedOfSound { get; private set; } = 340;
        public double Mach { get; private set; }
        public double DynamicPressure { get; private set; }
        public double GForce { get; private set; }
        public Vector3 ProperAcceleration { get; private set; }
        public Vector3 GravityAccel { get; private set; }
        public bool InAtmosphere => AirDensity > 1e-9;
        public double LastGroundContactUT = -1e9;
        public bool GroundContact => Sim != null && Sim.UT - LastGroundContactUT < 0.25;
        public bool Splashed { get; private set; }

        private Vector3 _vPre, _wPre;
        private bool _wasLocked;
        private bool _contactThisStep;
        private Vector3 _gravityAppliedAccel;
        private double _gSmoothed;

        /// <summary>Unity-space air velocity at a world point (atmosphere co-rotates with the body).</summary>
        public Vector3 AirVelocityAt(Vector3 worldPos)
        {
            Vector3d rel = Sim.Frame.ToTrue(worldPos);
            Vector3d airTrue = MainBody.FrameVelocityAt(rel);
            return (Vector3)(airTrue - Sim.Frame.Velocity);
        }

        /// <summary>Unity-space velocity of the ground frame at a world point.</summary>
        public Vector3 SurfaceFrameVelocityAt(Vector3 worldPos) => AirVelocityAt(worldPos);

        public void UpdateFlightState(double ut)
        {
            var frame = Sim.Frame;
            Vector3 comWorld = WorldCoM;
            TruePosition = frame.ToTrue(comWorld);
            if (PadHold)
                TrueVelocity = frame.Body.FrameVelocityAt(TruePosition);
            else if (KeplerLocked && Orbit != null)
                TrueVelocity = Orbit.GetVelocityAtUT(ut);
            else
                TrueVelocity = frame.ToTrueVelocity(Rb.linearVelocity);
            if (PadHold) LastGroundContactUT = ut;
            ComputeDerivedState(ut);
        }

        /// <summary>Flight state while on rails (kinematic): taken from the analytic rails state.</summary>
        public void UpdateFlightStateRails(double ut)
        {
            if (Handle == null) { UpdateFlightState(ut); return; }
            var frame = Sim.Frame;
            TruePosition = Handle.PositionRelBody(ut) + frame.BodyPosition(Handle.Body, ut);
            TrueVelocity = Handle.VelocityRelBody(ut) + frame.BodyVelocity(Handle.Body, ut);
            ComputeDerivedState(ut);
        }

        private void ComputeDerivedState(double ut)
        {
            var frame = Sim.Frame;
            var body = frame.Body;
            double r = TruePosition.magnitude;
            Altitude = r - body.Radius;
            TerrainHeight = body.SurfaceHeightAt(TruePosition, ut);
            AltitudeAGL = Altitude - TerrainHeight;
            SurfaceVelocity = TrueVelocity - body.FrameVelocityAt(TruePosition);
            VerticalSpeed = Vector3d.Dot(TrueVelocity, TruePosition / Math.Max(r, 1));
            if (body.Atmosphere != null)
            {
                StaticPressure = body.Atmosphere.Pressure(Altitude);
                AirDensity = body.Atmosphere.Density(Altitude);
                AirTemperature = body.Atmosphere.Temperature(Altitude);
                SpeedOfSound = body.Atmosphere.SpeedOfSound(Altitude);
            }
            else
            {
                StaticPressure = 0; AirDensity = 0; AirTemperature = 3; SpeedOfSound = 1;
            }
            double sp = SurfaceVelocity.magnitude;
            Mach = SpeedOfSound > 1 ? sp / SpeedOfSound : 0;
            DynamicPressure = 0.5 * AirDensity * sp * sp;
            Splashed = body.HasOcean && Altitude < 1.5 && !GroundContact && AnyPartBelowSeaLevel();
            double gmag = body.GM / (r * r);
            GravityAccel = (Vector3)(-TruePosition / r * gmag);
        }

        private bool AnyPartBelowSeaLevel()
        {
            var body = MainBody;
            foreach (var p in Parts)
            {
                double h = Sim.Frame.ToTrue(p.transform.position).magnitude - body.Radius;
                if (h < p.Def.height * 0.3) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ physics step

        public void AddForceAtPosition(Part p, Vector3 force, Vector3 worldPos)
        {
            if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z)) return;
            Rb.AddForceAtPosition(force, worldPos, ForceMode.Force);
            if (p != null)
            {
                p.ExternalForce += force;
                p.ExternalMoment += Vector3.Cross(worldPos, force);
            }
            _forceSum += force.magnitude;
        }

        public void AddTorque(Part p, Vector3 torque)
        {
            if (float.IsNaN(torque.x)) return;
            Rb.AddTorque(torque, ForceMode.Force);
            if (p != null) p.ExternalMoment += torque;
        }

        private readonly List<(Part part, Vector3 impulse, Vector3 pos)> _pendingImpulses = new List<(Part, Vector3, Vector3)>();

        /// <summary>
        /// Separation / undocking impulse. Applied as a one-step force at the next pre-step through the recorded
        /// force path, so the structural analysis sees where it acts (an unrecorded impulse would look like an
        /// unexplained acceleration and be blamed on the joints).
        /// </summary>
        public void QueueImpulse(Part p, Vector3 impulse, Vector3 worldPos)
        {
            _pendingImpulses.Add((p, impulse, worldPos));
            ForceUnlockThisStep = true;
        }

        private double _forceSum;

        public void PreStep(double dt, double ut)
        {
            if (Parts.Count == 0 || Rb == null || Rb.isKinematic) return;
            // A locked vessel moved last step with the Kepler secant velocity; restore the exact orbital velocity.
            if (_wasLocked && Orbit != null)
                Rb.linearVelocity = (Vector3)(Orbit.GetVelocityAtUT(ut) - Sim.Frame.Velocity);

            foreach (var p in Parts) { p.ExternalForce = Vector3.zero; p.ExternalMoment = Vector3.zero; p.ContactForce = Vector3.zero; p.ContactMoment = Vector3.zero; }
            _forceSum = 0;
            UpdateMassProperties();
            if (_pendingImpulses.Count > 0)
            {
                float inv = 1f / (float)dt;
                foreach (var (part, impulse, pos) in _pendingImpulses)
                    AddForceAtPosition(part != null && !part.Destroyed && part.Vessel == this ? part : null, impulse * inv, pos);
                _pendingImpulses.Clear();
            }
            KeplerLocked = false;
            UpdateFlightState(ut);

            TorqueAuthority = Vector3.Max(_authorityAccum, Vector3.one * 1f);
            _authorityAccum = Vector3.zero;

            // Controls
            if (HasControl)
            {
                Ctrl.TorqueCommand = Attitude.Update(this, dt);
                Ctrl.TranslationCommand = new Vector3(Ctrl.TransX, Ctrl.TransY, Ctrl.TransZ);
            }
            else
            {
                Ctrl.TorqueCommand = Vector3.zero;
                Ctrl.TranslationCommand = Vector3.zero;
            }

            for (int i = 0; i < Parts.Count; i++)
            {
                var mods = Parts[i].Modules;
                for (int k = 0; k < mods.Count; k++) mods[k].OnPreStep(dt);
            }

            Aerodynamics.Apply(this, ut);
            Thermal.Step(this, dt, ut);
            Buoyancy.Apply(this, ut);

            bool disturbed = _forceSum > 1e-3 || InAtmosphere || GroundContact || _contactThisStep || ForceUnlockThisStep || Splashed
                             || (Sim != null && Sim.UT < SettleUntilUT);
            ForceUnlockThisStep = false;
            if (!disturbed && Orbit != null && Sim.AllowKeplerLock)
            {
                // Coasting: follow the analytic orbit exactly (no integration drift).
                KeplerLocked = true;
                Vector3d r1 = Orbit.GetPositionAtUT(ut + dt);
                Vector3d originNext = Sim.Frame.Origin + Sim.Frame.Velocity * dt;
                Vector3 target = (Vector3)(r1 - originNext);
                Rb.linearVelocity = (target - WorldCoM) / (float)dt;
                _gravityAppliedAccel = Vector3.zero;
            }
            else
            {
                Rb.AddForce(GravityAccel * (float)TotalMass, ForceMode.Force);
                _gravityAppliedAccel = GravityAccel;
            }
            _wasLocked = KeplerLocked;
            _contactThisStep = false;
            _vPre = Rb.linearVelocity;
            _wPre = Rb.angularVelocity;
        }

        public void PostStep(double dt, double ut)
        {
            if (Parts.Count == 0 || Rb == null || Rb.isKinematic) return;
            Vector3 v = Rb.linearVelocity, w = Rb.angularVelocity;
            if (!KeplerLocked)
            {
                Vector3 aCm = (v - _vPre) / (float)dt;
                Vector3 alpha = (w - _wPre) / (float)dt;
                ProperAcceleration = aCm - _gravityAppliedAccel;
                if (DebugSeparation && ProperAcceleration.magnitude > 60f)
                    Debug.Log($"[SepDebug] {VesselName} step: vPre {_vPre:F3} v {v:F3} a {ProperAcceleration.magnitude:F0} m/s² parts {Parts.Count} mass {Rb.mass:F0} kin {Rb.isKinematic} contact {_contactThisStep}");
                if (!ImpactProtected) StructuralAnalysis.Check(this, aCm - _gravityAppliedAccel, alpha, w, dt);
                var frame = Sim.Frame;
                Vector3d r = frame.ToTrue(WorldCoM);
                Vector3d vel = frame.ToTrueVelocity(v);
                if (r.magnitude > MainBody.Radius * 0.5)
                    Orbit = new Orbit(r, vel, ut, MainBody.GM);
            }
            else
            {
                ProperAcceleration = Vector3.zero;
            }
            double g = ProperAcceleration.magnitude / MathD.G0;
            _gSmoothed += (g - _gSmoothed) * Math.Min(1.0, dt * 6);
            GForce = _gSmoothed;

            for (int i = 0; i < Parts.Count; i++)
            {
                var mods = Parts[i].Modules;
                for (int k = 0; k < mods.Count; k++) mods[k].OnPostStep(dt);
            }
            UpdateFlightState(ut);
            UpdateSituation();

            // Safety net against tunnelling: a vessel found well below the terrain surface has crashed.
            if (!IsFlag && AltitudeAGL < -30 && !ImpactProtected)
            {
                bool sank = MainBody.HasOcean && Altitude > MainBody.TerrainHeightAt(TruePosition, ut);
                string why = sank ? "sank below the sea" : $"crashed into the terrain at {SurfaceSpeed:F0} m/s";
                foreach (var p in Parts) QueueDestroy(p, why);
                Sim.RequestStructurePass(this);
            }
        }

        public void UpdateSituation()
        {
            var body = MainBody;
            if (IsFlag) { Situation = Situation.Landed; return; }
            if (Situation == Situation.Prelaunch)
            {
                if (GroundContact && SurfaceSpeed < 2 && Record.launchUT < 0) return;
            }
            if (GroundContact && SurfaceSpeed < 3) { Situation = Situation.Landed; return; }
            if (Splashed && SurfaceSpeed < 5) { Situation = Situation.Splashed; return; }
            if (body.Atmosphere != null && Altitude < body.Atmosphere.Height) { Situation = Situation.Flying; return; }
            if (Orbit == null) { Situation = Situation.SubOrbital; return; }
            double limit = body.Radius + (body.Atmosphere != null ? body.Atmosphere.Height : body.Terrain.MaxHeight);
            if (!Orbit.IsElliptic || Orbit.ApoapsisRadius > body.SOIRadius) Situation = Situation.Escaping;
            else if (Orbit.PeriapsisRadius < limit) Situation = Situation.SubOrbital;
            else Situation = Situation.Orbiting;
        }

        // ------------------------------------------------------------------ collisions

        private static readonly ContactPoint[] ContactBuffer = new ContactPoint[64];

        private void OnCollisionEnter(Collision c) => HandleCollision(c, true);
        private void OnCollisionStay(Collision c) => HandleCollision(c, false);

        private void HandleCollision(Collision c, bool enter)
        {
            if (Sim == null) return;
            _contactThisStep = true;
            bool ground = c.collider != null && c.collider.gameObject.layer == Layers.Terrain;
            if (ground) LastGroundContactUT = Sim.UT;
            int n = c.GetContacts(ContactBuffer);
            if (n == 0) return;
            Vector3 impulse = c.impulse;
            float dt = Time.fixedDeltaTime;
            Vector3 relVel = c.relativeVelocity;
            // Each point's own solver impulse puts the force where the ground actually pushes. Splitting the total
            // evenly over the reported points (often all on one side of an engine bell's rim) invented a moment of
            // weight x rim radius, which the structural check pinned on the joints of a rocket standing still.
            bool perPoint = false;
            for (int i = 0; i < n; i++) if (ContactBuffer[i].impulse.sqrMagnitude > 0f) { perPoint = true; break; }
            for (int i = 0; i < n; i++)
            {
                var cp = ContactBuffer[i];
                var part = cp.thisCollider != null ? cp.thisCollider.GetComponent<Part>() : null;
                if (part == null || part.Destroyed) continue;
                // The contact pushes this body away from the other one (the callback's normal points into this body);
                // impulses have no guaranteed sign, so orient them by the normal.
                Vector3 j = perPoint ? cp.impulse : impulse / n;
                if (Vector3.Dot(j, cp.normal) < 0) j = -j;
                Vector3 fContact = j / dt;
                part.ContactForce += fContact;
                part.ContactMoment += Vector3.Cross(cp.point, fContact);
                float vn = Mathf.Abs(Vector3.Dot(relVel, cp.normal));
                // A part feels its share of the velocity change: a 100 kg rocketeer bumping into a 10 t rocket barely
                // moves it. Ground, pad structures and other kinematic bodies count as immovable (they deal the full
                // relative speed); a vessel clamped to the pad is immovable itself, so a light body cannot hurt it.
                var otherRb = c.rigidbody;
                if (!ground && otherRb != null && !otherRb.isKinematic)
                {
                    if (Rb == null || Rb.isKinematic) vn = 0f;
                    else vn *= otherRb.mass / Mathf.Max(otherRb.mass + Rb.mass, 1e-3f);
                }
                if ((enter || vn > 3f) && !ImpactProtected && !DevCheats.NoCrashDamage)
                {
                    double tol = part.Def.impactTolerance;
                    if (vn > tol)
                    {
                        // Name the part actually touched (Collision.collider is just one collider of a compound body).
                        var otherCol = cp.otherCollider != null ? cp.otherCollider : c.collider;
                        string other = ground ? "the ground" : otherCol != null ? DescribeCollider(otherCol) : "an object";
                        QueueDestroy(part, $"impact at {vn:F1} m/s with {other} (tolerance {tol:F0} m/s)");
                        Sim.RequestStructurePass(this);
                    }
                }
            }
        }

        public void AddContactFlag() { _contactThisStep = true; }

        private static string DescribeCollider(Collider c)
        {
            var part = c.GetComponentInParent<Part>();
            if (part != null) return part.Vessel != null ? $"{part.Def.title} of {part.Vessel.VesselName}" : part.Def.title;
            return c.gameObject.name;
        }
    }
}
