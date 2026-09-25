using System.Collections.Generic;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Androgynous docking port. Two free ports facing each other attract when close; they capture
    /// (merging both vessels into one) inside capture range, alignment and relative-speed limits.
    /// </summary>
    public sealed class DockingPortModule : PartModule
    {
        public enum PortState { Ready, Docked, Cooldown }

        public DockingPortDefinition Def;
        public PortState State = PortState.Ready;
        public int PartnerUid = -1;
        public double CooldownUntil;
        public string Status = "";

        public override string ModuleName => "DockingPort";

        public override void OnInit() { Def = Part.Def.dockingPort; }

        public Vector3 FaceWorld => Part.NodeWorldPosition(Def.nodeId);
        public Vector3 FaceNormalWorld
        {
            get
            {
                var n = Part.Def.FindNode(Def.nodeId);
                return n != null ? Part.transform.TransformDirection(n.Direction) : Part.transform.up;
            }
        }

        /// <summary>True if another part occupies the docking node (built-in connection or docked).</summary>
        public bool NodeOccupied
        {
            get
            {
                if (Part.AttachNode == Def.nodeId) return true;
                foreach (var c in Part.Children) if (c.ParentNode == Def.nodeId) return true;
                return false;
            }
        }

        public bool CanDock => State == PortState.Ready && !NodeOccupied && FlightSim.Instance.UT >= CooldownUntil;

        public override void OnPreStep(double dt)
        {
            var sim = FlightSim.Instance;
            // After undocking a port re-arms only once its timer has run out *and* no other port is within the
            // magnetic range; otherwise it would pull the vessels that just separated straight back together.
            if (State == PortState.Cooldown && sim.UT >= CooldownUntil && !OtherPortWithin(Def.captureRange * 5f)) State = PortState.Ready;
            if (State == PortState.Docked)
            {
                // Validate: partner still attached through our node?
                if (!NodeOccupied) { State = PortState.Cooldown; CooldownUntil = sim.UT + 2; PartnerUid = -1; }
                Status = "Docked";
                return;
            }
            Status = CanDock ? "Ready" : (State == PortState.Cooldown ? "Cooldown" : "Blocked");
            if (!CanDock) return;
            Vector3 face = FaceWorld, dir = FaceNormalWorld;
            foreach (var other in sim.LoadedVessels)
            {
                if (other == Vessel || other.IsEva || other.IsFlag || other.Rb == null || other.Rb.isKinematic) continue;
                if ((other.Rb.worldCenterOfMass - Vessel.Rb.worldCenterOfMass).sqrMagnitude > 250f * 250f) continue;
                foreach (var op in other.Parts)
                {
                    var om = op.GetModule<DockingPortModule>();
                    if (om == null || !om.CanDock) continue;
                    Vector3 of = om.FaceWorld, od = om.FaceNormalWorld;
                    Vector3 delta = of - face;
                    float dist = delta.magnitude;
                    if (dist > Def.captureRange * 5f) continue;
                    float angle = Vector3.Angle(dir, -od);
                    Vector3 vRel = Vessel.Rb.GetPointVelocity(face) - other.Rb.GetPointVelocity(of);
                    if (dist < Def.captureRange && angle < Def.captureAngleDeg && vRel.magnitude < Def.captureSpeed)
                    {
                        sim.RequestDock(this, om);
                        return;
                    }
                    // Magnetic pull (small, equal and opposite), only while closing slowly: it guides a gentle approach
                    // without speeding it past the capture limit.
                    float closing = Vector3.Dot(vRel, delta) / Mathf.Max(dist, 0.01f);
                    if (angle < Def.captureAngleDeg * 2.5f && dist > 0.01f && closing < 0.3f)
                    {
                        Vector3 f = delta / dist * 250f;
                        Vessel.AddForceAtPosition(Part, f, face);
                        other.AddForceAtPosition(op, -f, of);
                    }
                }
            }
        }

        private bool OtherPortWithin(float range)
        {
            Vector3 face = FaceWorld;
            foreach (var other in FlightSim.Instance.LoadedVessels)
            {
                if (other == null || other == Vessel || other.IsEva || other.IsFlag || other.Rb == null) continue;
                if ((other.Rb.worldCenterOfMass - Vessel.Rb.worldCenterOfMass).sqrMagnitude > 250f * 250f) continue;
                foreach (var op in other.Parts)
                {
                    var om = op.GetModule<DockingPortModule>();
                    if (om != null && om.State != PortState.Docked && (om.FaceWorld - face).sqrMagnitude < range * range) return true;
                }
            }
            return false;
        }

        public void Undock()
        {
            if (State != PortState.Docked) return;
            FlightSim.Instance.RequestUndock(this);
        }

        public override void Save(Dictionary<string, string> s)
        {
            s["state"] = State.ToString();
            s["partner"] = PartnerUid.ToString();
        }

        public override void Load(Dictionary<string, string> s)
        {
            if (System.Enum.TryParse(GetString(s, "state", "Ready"), out PortState st)) State = st;
            PartnerUid = (int)GetDouble(s, "partner", -1);
            // A port saved while re-arming stays so until it is clear of other ports (the timer is not saved).
        }

        public override void CollectActions(List<PartAction> actions)
        {
            if (State == PortState.Docked) actions.Add(new PartAction("Undock", Undock));
        }

        public override void CollectInfo(List<string> info) { info.Add("Docking port: " + Status); }
    }
}
