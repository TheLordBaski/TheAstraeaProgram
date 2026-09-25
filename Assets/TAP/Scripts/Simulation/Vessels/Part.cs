using System;
using System.Collections.Generic;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed class PartResource
    {
        public readonly ResourceDefinition Def;
        public double Amount;
        public double Max;
        public PartResource(ResourceDefinition def, double amount, double max) { Def = def; Amount = amount; Max = max; }
        public double Mass => Amount * Def.density;
        public double Fraction => Max > 0 ? Amount / Max : 0;
    }

    /// <summary>An action shown in the part action menu (right-click).</summary>
    public struct PartAction
    {
        public string Label;
        public Action Invoke;
        public bool Enabled;
        public PartAction(string label, Action invoke, bool enabled = true) { Label = label; Invoke = invoke; Enabled = enabled; }
    }

    /// <summary>Runtime part inside a loaded vessel (child GameObject of the vessel rigidbody).</summary>
    public sealed class Part : MonoBehaviour
    {
        public PartDefinition Def;
        public Vessel Vessel;
        public int Uid;
        public Part ParentPart;
        public readonly List<Part> Children = new List<Part>();
        /// <summary>Node on this part used for the parent connection ("srf" for surface attachment).</summary>
        public string AttachNode;
        /// <summary>Node on the parent part.</summary>
        public string ParentNode;
        public int Stage = -1;
        public int SymmetryGroup = -1;
        public readonly List<PartResource> Resources = new List<PartResource>();
        public readonly List<PartModule> Modules = new List<PartModule>();
        public readonly List<string> Crew = new List<string>();
        public Dictionary<string, double> Settings = new Dictionary<string, double>();

        public double SkinTemp = 290;
        public double InternalTemp = 290;
        public bool Destroyed;

        // Per-step accumulators (world space), reset by the vessel before force computation.
        [NonSerialized] public Vector3 ExternalForce;
        [NonSerialized] public Vector3 ContactForce;
        [NonSerialized] public Vector3 ContactMoment; // about the world origin, contact forces at their contact points
        [NonSerialized] public Vector3 ExternalMoment; // about the world origin, from forces/torques applied this step
        [NonSerialized] public double HeatFlux;       // W absorbed from convection (for UI)
        [NonSerialized] public float Exposure = 1f;   // flow shadowing factor (thermal)
        [NonSerialized] public double SmoothedJointLoad; // fraction of breaking strength
        [NonSerialized] public string LastLoadDescription;

        public Transform ModelRoot { get; private set; }
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _lastGlow = -1;

        public double DryMass => Def.dryMass;

        public double Mass
        {
            get
            {
                double m = Def.dryMass;
                for (int i = 0; i < Resources.Count; i++) m += Resources[i].Mass;
                return m;
            }
        }

        public bool IsSurfaceAttached => AttachNode == "srf";
        public int SeatCount => Def.crew?.seats ?? 0;
        public bool HasFreeSeat => SeatCount > Crew.Count;

        public void InitVisuals()
        {
            ModelRoot = transform.Find("Model");
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
        }

        public T GetModule<T>() where T : PartModule
        {
            for (int i = 0; i < Modules.Count; i++) if (Modules[i] is T t) return t;
            return null;
        }

        public PartResource GetResource(string id)
        {
            for (int i = 0; i < Resources.Count; i++) if (Resources[i].Def.id == id) return Resources[i];
            return null;
        }

        /// <summary>Position of this part's centre in the vessel root's local space.</summary>
        public Vector3 LocalPositionInVessel => Vessel != null ? Vessel.transform.InverseTransformPoint(transform.position) : transform.localPosition;

        /// <summary>World position of an attach node.</summary>
        public Vector3 NodeWorldPosition(string nodeId)
        {
            if (nodeId == "srf" && Def.surfaceAttach != null) return transform.TransformPoint(Def.surfaceAttach.Position);
            var n = Def.FindNode(nodeId);
            return n != null ? transform.TransformPoint(n.Position) : transform.position;
        }

        /// <summary>World point where this part connects to its parent.</summary>
        public Vector3 JointWorldPosition => NodeWorldPosition(AttachNode);

        /// <summary>Unit vector from this part's joint towards the parent side (world).</summary>
        public Vector3 JointAxisTowardsParent
        {
            get
            {
                if (AttachNode == "srf" && Def.surfaceAttach != null)
                    return -transform.TransformDirection(Def.surfaceAttach.Direction);
                var n = Def.FindNode(AttachNode);
                return n != null ? transform.TransformDirection(n.Direction) : Vector3.up;
            }
        }

        /// <summary>Visual heat glow based on skin temperature relative to its limit.</summary>
        public void UpdateHeatGlow()
        {
            if (_renderers == null) return;
            double t = SkinTemp;
            float glow = (float)Math.Max(0, (t - 800) / Math.Max(1, Def.skinMaxTemp - 800));
            glow = Mathf.Clamp01(glow);
            if (Mathf.Abs(glow - _lastGlow) < 0.01f) return;
            _lastGlow = glow;
            Color c = glow <= 0 ? Color.black : Color.Lerp(new Color(0.6f, 0.05f, 0f), new Color(2.4f, 1.1f, 0.4f), glow) * glow * 2f;
            _mpb.SetColor("_EmissionColor", c);
            foreach (var r in _renderers)
            {
                if (r == null || r is ParticleSystemRenderer || r is LineRenderer) continue;
                r.SetPropertyBlock(glow <= 0 ? null : _mpb);
            }
        }

        public void CollectSubtree(List<Part> result)
        {
            result.Add(this);
            for (int i = 0; i < Children.Count; i++) Children[i].CollectSubtree(result);
        }

        public bool IsAncestorOf(Part other)
        {
            var p = other;
            while (p != null)
            {
                if (p == this) return true;
                p = p.ParentPart;
            }
            return false;
        }

        public override string ToString() => Def != null ? Def.title : name;
    }

    /// <summary>Base class for behaviours attached to parts (engines, chutes, legs...).</summary>
    public abstract class PartModule
    {
        public Part Part;
        public Vessel Vessel => Part.Vessel;
        public virtual string ModuleName => GetType().Name;

        /// <summary>Called once after the vessel is fully built (or rebuilt after load).</summary>
        public virtual void OnInit() { }
        /// <summary>Called when the part's stage is activated.</summary>
        public virtual void OnActivate() { }
        /// <summary>Pre-physics: compute and apply forces, consume resources.</summary>
        public virtual void OnPreStep(double dt) { }
        /// <summary>Post-physics bookkeeping.</summary>
        public virtual void OnPostStep(double dt) { }
        /// <summary>Per-frame visuals.</summary>
        public virtual void OnRenderUpdate(float dt) { }
        /// <summary>Vessel structure changed (split/merge/part lost).</summary>
        public virtual void OnVesselChanged() { }
        public virtual void OnPartDestroyed() { }
        public virtual void Save(Dictionary<string, string> state) { }
        public virtual void Load(Dictionary<string, string> state) { }
        public virtual void CollectActions(List<PartAction> actions) { }
        public virtual void CollectInfo(List<string> info) { }
        /// <summary>True if this module can be activated by staging.</summary>
        public virtual bool IsStageable => false;
        /// <summary>Short status text for the staging stack UI.</summary>
        public virtual string StageStatus => null;

        protected static bool GetBool(Dictionary<string, string> s, string key, bool def = false)
            => s != null && s.TryGetValue(key, out var v) ? v == "1" || v == "true" || v == "True" : def;
        protected static double GetDouble(Dictionary<string, string> s, string key, double def = 0)
            => s != null && s.TryGetValue(key, out var v) && double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : def;
        protected static string GetString(Dictionary<string, string> s, string key, string def = null)
            => s != null && s.TryGetValue(key, out var v) ? v : def;
        protected static string F(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        protected static string B(bool v) => v ? "1" : "0";
    }
}
