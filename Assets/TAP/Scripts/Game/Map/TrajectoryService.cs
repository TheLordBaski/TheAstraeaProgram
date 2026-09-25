using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Periodically predicts the active vessel's trajectory from its real current state:
    /// the coasting path (no nodes) and the planned path through all maneuver nodes, following
    /// SOI transitions. Also computes closest approach to the selected target.
    /// </summary>
    public sealed class TrajectoryService : MonoBehaviour
    {
        public FlightSim Sim;
        public readonly List<OrbitPatch> Current = new List<OrbitPatch>();
        public readonly List<OrbitPatch> Planned = new List<OrbitPatch>();
        public bool HasNodes;
        public bool HasClosest;
        public double ClosestDistance, ClosestUT, ClosestRelSpeed;
        public int Version;
        public float Interval = 0.12f;
        private float _timer;
        private int _lastNodeVersion = -1;
        private Vessel _lastVessel;

        public static TrajectoryService Instance { get; private set; }

        private void Awake() { Instance = this; }

        public void ForceUpdate() { _timer = Interval; }

        private void Update()
        {
            if (Sim == null) return;
            var v = Sim.ActiveVessel;
            _timer += Time.unscaledDeltaTime;
            bool nodesChanged = v != null && v.ManeuverVersion != _lastNodeVersion;
            if (_timer < Interval && !nodesChanged && v == _lastVessel) return;
            _timer = 0;
            _lastVessel = v;
            if (v != null) _lastNodeVersion = v.ManeuverVersion;
            Recompute(v);
        }

        public void Recompute(Vessel v)
        {
            Current.Clear();
            Planned.Clear();
            HasNodes = false;
            HasClosest = false;
            Version++;
            if (v == null) return;
            var h = v.Handle;
            if (h == null) return;
            Orbit orbit = h.CurrentOrbit;
            var body = h.Body;
            if (orbit == null || body == null) return;
            if (v.Situation == Situation.Landed || v.Situation == Situation.Prelaunch || v.Situation == Situation.Splashed)
            {
                if (v.SurfaceSpeed < 5) return;
            }
            double ut = Sim.UT;
            try
            {
                Current.AddRange(PatchedConics.Predict(orbit, body, ut, null, 5));
                var nodes = v.ManeuverNodes;
                if (nodes.Count > 0)
                {
                    HasNodes = true;
                    var specs = new List<NodeSpec>();
                    foreach (var n in nodes) specs.Add(new NodeSpec(n.ut, n.prograde, n.normal, n.radial));
                    specs.Sort((a, b) => a.UT.CompareTo(b.UT));
                    Planned.AddRange(PatchedConics.Predict(orbit, body, ut, specs, 4 + specs.Count * 2));
                }
                ComputeClosest(v);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Trajectory prediction failed: " + e.Message);
            }
        }

        private void ComputeClosest(Vessel v)
        {
            var id = v.TargetId;
            if (string.IsNullOrEmpty(id)) return;
            Func<double, Vector3d> pos = null, vel = null;
            if (id.StartsWith("body:"))
            {
                var b = Sim.System.Get(id.Substring(5));
                if (b == null) return;
                pos = t => b.GetPositionAtUT(t);
                vel = t => b.GetVelocityAtUT(t);
            }
            else if (id.StartsWith("vessel:"))
            {
                var th = Sim.FindHandle(id.Substring(7));
                if (th == null) return;
                var o = th.CurrentOrbit;
                var tb = th.Body;
                if (o == null)
                {
                    pos = t => th.AbsolutePosition(t);
                    vel = t => th.AbsoluteVelocity(t);
                }
                else
                {
                    pos = t => tb.GetPositionAtUT(t) + o.GetPositionAtUT(t);
                    vel = t => tb.GetVelocityAtUT(t) + o.GetVelocityAtUT(t);
                }
            }
            var patches = Planned.Count > 0 ? Planned : Current;
            HasClosest = PatchedConics.ClosestApproach(patches, pos, vel, out ClosestDistance, out ClosestUT, out ClosestRelSpeed);
        }

        /// <summary>First patch of the displayed trajectory that orbits a different body than the first.</summary>
        public OrbitPatch FirstEncounter(bool planned)
        {
            var list = planned && Planned.Count > 0 ? Planned : Current;
            if (list.Count == 0) return null;
            var b0 = list[0].Body;
            foreach (var p in list) if (p.Body != b0) return p;
            return null;
        }
    }
}
