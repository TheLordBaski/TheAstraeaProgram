using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Deployable landing leg with raycast (sphere-cast) spring-damper suspension along the part's up axis
    /// and anchored static friction at the foot. Breaks on hard touchdown or sustained overload.
    /// </summary>
    public sealed class LandingLegModule : PartModule
    {
        public LandingLegDefinition Def;
        public bool Deployed;
        public float DeployProgress;   // 0 stowed .. 1 deployed
        public bool Broken;
        public float Compression;
        public bool InContact;
        public float LastLoad;

        private Transform _pivot, _piston;
        private Quaternion _stowedRot, _deployedRot;
        private Vector3 _footLocal;       // deployed, uncompressed foot centre (part space)
        private Collider _anchorCollider;
        private Vector3 _anchorLocal;
        private bool _anchorValid;
        private bool _wasInContact;
        private double _overloadTime;
        private float _prevCompression;

        public override string ModuleName => "LandingLeg";

        public override void OnInit()
        {
            Def = Part.Def.landingLeg;
            _pivot = Part.ModelRoot != null ? Part.ModelRoot.Find("LegPivot") : null;
            _piston = _pivot != null ? _pivot.Find("Piston") : null;
            _deployedRot = Quaternion.AngleAxis(-Def.splayDeg, Vector3.right);
            _stowedRot = Quaternion.AngleAxis(-172f, Vector3.right);
            Vector3 pivotPos = _pivot != null ? _pivot.localPosition : new Vector3(0, -0.05f, 0.1f);
            _footLocal = pivotPos + _deployedRot * new Vector3(0, -Def.length, 0);
            if (Def.startDeployed) { Deployed = true; DeployProgress = 1; }
            ApplyVisual();
        }

        public void SetDeployed(bool d)
        {
            if (Broken) return;
            Deployed = d;
        }

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            if (v.Ctrl != null && !v.IsEva) Deployed = v.Ctrl.LegsDeployed;
            float target = Deployed && !Broken ? 1f : 0f;
            DeployProgress = Mathf.MoveTowards(DeployProgress, target, (float)dt / 1.6f);
            InContact = false;
            Compression = 0;
            LastLoad = 0;
            if (Broken || DeployProgress < 0.999f)
            {
                _anchorValid = false;
                _wasInContact = false;
                return;
            }

            Vector3 up = Part.transform.up;
            Vector3 footRest = Part.transform.TransformPoint(_footLocal);
            const float extra = 0.8f;
            Vector3 origin = footRest + up * (Def.travel + extra);
            float castDist = Def.travel + extra + 0.02f;
            if (!Physics.SphereCast(origin, Def.footRadius, -up, out RaycastHit hit, castDist, Layers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                _anchorValid = false;
                _wasInContact = false;
                return;
            }
            float x = (Def.travel + extra) - hit.distance;
            if (x <= 0)
            {
                _anchorValid = false;
                _wasInContact = false;
                return;
            }

            Vector3 contact = hit.point;
            Vector3 vFoot = v.Rb.GetPointVelocity(contact);
            Vector3 vGround = hit.rigidbody != null ? hit.rigidbody.GetPointVelocity(contact) : v.SurfaceFrameVelocityAt(contact);
            Vector3 rel = vFoot - vGround;
            float xdot = -Vector3.Dot(rel, up);

            if (!_wasInContact && !v.ImpactProtected && !DevCheats.NoCrashDamage && xdot > Def.impactTolerance)
            {
                Broken = true;
                FlightSim.Instance?.Log($"{Part.Def.title} broke on touchdown at {xdot:F1} m/s (limit {Def.impactTolerance:F0} m/s)", true);
                v.QueueDestroy(Part, $"landing leg struck ground at {xdot:F1} m/s");
                return;
            }

            float F = Def.spring * x + Def.damper * xdot;
            if (x > Def.travel) F += Def.spring * 12f * (x - Def.travel);
            F = Mathf.Max(0, F);
            LastLoad = F;
            if (F > Def.maxLoad && !v.ImpactProtected && !DevCheats.NoCrashDamage)
            {
                _overloadTime += dt;
                if (_overloadTime > 0.25)
                {
                    Broken = true;
                    v.QueueDestroy(Part, $"landing leg overloaded ({F / 1000:F0} kN > {Def.maxLoad / 1000:F0} kN)");
                    return;
                }
            }
            else _overloadTime = 0;

            // Anchored static friction in the ground tangent plane.
            Vector3 n = hit.normal;
            if (!_anchorValid || _anchorCollider != hit.collider)
            {
                _anchorCollider = hit.collider;
                _anchorLocal = hit.collider.transform.InverseTransformPoint(contact);
                _anchorValid = true;
            }
            Vector3 anchorWorld = _anchorCollider.transform.TransformPoint(_anchorLocal);
            Vector3 d = contact - anchorWorld;
            d -= Vector3.Dot(d, n) * n;
            Vector3 vt = rel - Vector3.Dot(rel, n) * n;
            float kf = Def.spring * 1.5f;
            float cf = Def.damper * 1.2f;
            Vector3 Ff = -(kf * d + cf * vt);
            float fMax = Def.friction * F;
            if (Ff.magnitude > fMax)
            {
                Ff = Ff.normalized * fMax;
                // slip: move the anchor so the spring force equals the friction limit
                if (d.sqrMagnitude > 1e-8f)
                {
                    Vector3 newAnchor = contact - d.normalized * Mathf.Min(d.magnitude, fMax / kf);
                    _anchorLocal = _anchorCollider.transform.InverseTransformPoint(newAnchor);
                }
            }

            v.AddForceAtPosition(Part, up * F + Ff, contact);
            v.LastGroundContactUT = FlightSim.Instance.UT;
            v.AddContactFlag();
            InContact = true;
            Compression = x;
            _wasInContact = true;
        }

        public override void OnRenderUpdate(float dt) { ApplyVisual(); }

        private void ApplyVisual()
        {
            if (_pivot != null) _pivot.localRotation = Quaternion.Slerp(_stowedRot, _deployedRot, SmoothStep(DeployProgress));
            if (_piston != null) _piston.localPosition = new Vector3(0, Mathf.Clamp(Compression, 0, Def.travel * 1.2f), 0);
        }

        private static float SmoothStep(float t) => t * t * (3 - 2 * t);

        public override void Save(Dictionary<string, string> s)
        {
            s["deployed"] = B(Deployed);
            s["progress"] = F(DeployProgress);
            s["broken"] = B(Broken);
        }

        public override void Load(Dictionary<string, string> s)
        {
            Deployed = GetBool(s, "deployed");
            DeployProgress = (float)GetDouble(s, "progress");
            Broken = GetBool(s, "broken");
        }

        public override void CollectActions(List<PartAction> actions)
        {
            if (!Broken) actions.Add(new PartAction("Toggle landing legs (G)", () => Vessel.Ctrl.LegsDeployed = !Vessel.Ctrl.LegsDeployed));
        }

        public override void CollectInfo(List<string> info)
        {
            info.Add(Broken ? "Leg BROKEN" : $"Leg {(Deployed ? "deployed" : "retracted")}, load {LastLoad / 1000:F1} kN");
        }
    }
}
