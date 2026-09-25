using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Parachute: Stowed -> Armed (waiting for pressure) -> SemiDeployed (reefed) -> Deployed (below deploy
    /// altitude above terrain). Opening is gradual; the canopy tears if its load exceeds maxLoad and burns
    /// if it is exposed to reentry heat above its temperature limit.
    /// </summary>
    public sealed class ParachuteModule : PartModule
    {
        public enum ChuteState { Stowed, Armed, SemiDeployed, Deployed, Cut, Failed }

        public ParachuteDefinition Def;
        public ChuteState State = ChuteState.Stowed;
        public double CurrentArea;   // current Cd*A (m^2)
        public double CanopyLoad;    // N
        public double CanopyTemp = 290;
        public string FailReason;

        private Transform _anchor;
        private GameObject _canopy;
        private Vector3 _canopyDir = Vector3.up;
        private float _visualOpen;
        private static readonly Dictionary<string, Mesh> CanopyMeshes = new Dictionary<string, Mesh>();

        public override string ModuleName => "Parachute";
        public override bool IsStageable => true;
        public bool IsOpen => State == ChuteState.SemiDeployed || State == ChuteState.Deployed;

        public override void OnInit()
        {
            Def = Part.Def.parachute;
            _anchor = Part.transform.Find("CanopyAnchor");
        }

        public Vector3 AnchorWorld => _anchor != null ? _anchor.position : Part.transform.position;

        public enum Safety { Safe, Risky, Unsafe, NoAir }

        /// <summary>
        /// Would opening now be survivable? Uses the same canopy load and heating rules as the simulation:
        /// load at the next opening step (semi / full area) against the rated load, recovery temperature against the canopy limit.
        /// </summary>
        public Safety DeploySafety(out string reason)
        {
            var v = Vessel;
            reason = null;
            if (v == null || !v.InAtmosphere || v.AirDensity <= 0) { reason = "no air: the canopy will wait for thicker air"; return Safety.NoAir; }
            double speed = v.SurfaceSpeed;
            double q = 0.5 * v.AirDensity * speed * speed;
            double area = State == ChuteState.SemiDeployed || v.AltitudeAGL <= Def.deployAltitude ? Def.deployedArea : Def.semiDeployedArea;
            double load = q * area;
            double tRec = v.AirTemperature + 0.9 * speed * speed / 2008.0;
            double loadFrac = load / Def.maxLoad, heatFrac = tRec / Def.maxCanopyTemp;
            double worst = Math.Max(loadFrac, heatFrac);
            reason = $"canopy load {load / 1000:F0} / {Def.maxLoad / 1000:F0} kN, flow {tRec:F0} / {Def.maxCanopyTemp:F0} K";
            if (worst >= 1.0) return Safety.Unsafe;
            if (worst >= 0.65) return Safety.Risky;
            return Safety.Safe;
        }

        public override void OnActivate()
        {
            if (State == ChuteState.Stowed)
            {
                State = ChuteState.Armed;
                FlightSim.Instance?.Log($"{Part.Def.title} armed");
            }
        }

        public void Cut()
        {
            if (IsOpen)
            {
                State = ChuteState.Cut;
                CurrentArea = 0;
                FlightSim.Instance?.Log($"{Part.Def.title} cut");
            }
        }

        private void Fail(string reason)
        {
            State = ChuteState.Failed;
            FailReason = reason;
            CurrentArea = 0;
            FlightSim.Instance?.Log($"{Part.Def.title} FAILED: {reason}", true);
            FlightSim.Instance?.Effects?.SpawnCanopyShreds(AnchorWorld, Def.canopyDiameter, Def.canopyColor);
        }

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            CanopyLoad = 0;
            if (State == ChuteState.Stowed || State == ChuteState.Cut || State == ChuteState.Failed) { CurrentArea = 0; return; }

            if (State == ChuteState.Armed)
            {
                CurrentArea = 0;
                if (v.StaticPressure >= Def.minPressure && v.InAtmosphere)
                {
                    State = ChuteState.SemiDeployed;
                    FlightSim.Instance?.Log($"{Part.Def.title} semi-deployed");
                }
                return;
            }

            double targetArea;
            double openTime;
            if (State == ChuteState.SemiDeployed)
            {
                targetArea = Def.semiDeployedArea;
                openTime = Def.semiDeployTime;
                if (v.AltitudeAGL <= Def.deployAltitude)
                {
                    State = ChuteState.Deployed;
                    FlightSim.Instance?.Log($"{Part.Def.title} fully deployed");
                }
            }
            else
            {
                targetArea = Def.deployedArea;
                openTime = Def.deployTime;
            }
            double rate = (Def.deployedArea) / Math.Max(openTime, 0.1);
            if (State == ChuteState.SemiDeployed) rate = Def.semiDeployedArea / Math.Max(openTime, 0.1);
            CurrentArea = Math.Min(targetArea, CurrentArea + rate * dt);

            // Aerodynamic load on the canopy.
            Vector3 anchor = AnchorWorld;
            Vector3 vRel = v.Rb.GetPointVelocity(anchor) - v.AirVelocityAt(anchor);
            float speed = vRel.magnitude;
            if (speed < 0.05f || v.AirDensity <= 0) return;
            double q = 0.5 * v.AirDensity * speed * speed;
            CanopyLoad = q * CurrentArea;
            if (CanopyLoad > Def.maxLoad)
            {
                if (!DevCheats.UnbreakableJoints) Fail($"torn by aerodynamic load ({CanopyLoad / 1000:F0} kN > {Def.maxLoad / 1000:F0} kN at {speed:F0} m/s)");
                return;
            }
            // Canopy heating: approaches recovery temperature of the flow.
            double tRec = v.AirTemperature + 0.9 * speed * speed / 2008.0;
            CanopyTemp += (tRec - CanopyTemp) * Math.Min(1.0, dt / 1.5);
            if (CanopyTemp > Def.maxCanopyTemp)
            {
                if (!DevCheats.IgnoreHeat) Fail($"burned by reentry heat ({CanopyTemp:F0} K > {Def.maxCanopyTemp:F0} K)");
                return;
            }
            Vector3 dir = -vRel / speed;
            v.AddForceAtPosition(Part, dir * (float)CanopyLoad, anchor);
            _canopyDir = Vector3.Slerp(_canopyDir, dir, (float)Math.Min(1, dt * 3));
        }

        public override void OnPostStep(double dt)
        {
            // Automatically cut the chute once the vessel has come to rest on the ground or water.
            if (IsOpen && (Vessel.GroundContact || Vessel.Splashed) && Vessel.SurfaceSpeed < 1.0 && State == ChuteState.Deployed)
            {
                _restTime += dt;
                if (_restTime > 3) Cut();
            }
            else _restTime = 0;
        }
        private double _restTime;

        public override void OnRenderUpdate(float dt)
        {
            float target = 0;
            if (State == ChuteState.SemiDeployed) target = (float)(CurrentArea / Math.Max(Def.deployedArea, 1));
            if (State == ChuteState.Deployed) target = (float)(CurrentArea / Math.Max(Def.deployedArea, 1));
            if (State == ChuteState.SemiDeployed) target = Mathf.Max(target, 0.05f);
            _visualOpen = Mathf.MoveTowards(_visualOpen, target, dt * 1.5f);
            if (_visualOpen <= 0.001f)
            {
                if (_canopy != null) _canopy.SetActive(false);
                return;
            }
            EnsureCanopy();
            _canopy.SetActive(true);
            _canopy.transform.position = AnchorWorld;
            Vector3 up = _canopyDir.sqrMagnitude > 0.1f ? _canopyDir.normalized : Part.transform.up;
            _canopy.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            float radial = State == ChuteState.SemiDeployed ? 0.12f + 0.1f * _visualOpen : Mathf.Lerp(0.15f, 1f, _visualOpen);
            float length = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_visualOpen * 3));
            _canopy.transform.localScale = new Vector3(radial, length, radial);
        }

        private void EnsureCanopy()
        {
            if (_canopy != null) return;
            _canopy = new GameObject("Canopy");
            _canopy.transform.SetParent(Part.transform, true);
            var mf = _canopy.AddComponent<MeshFilter>();
            mf.sharedMesh = GetCanopyMesh(Def);
            var mr = _canopy.AddComponent<MeshRenderer>();
            mr.sharedMaterial = PartMaterials.Canopy;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", new Color(Def.canopyColor[0], Def.canopyColor[1], Def.canopyColor[2]));
            mr.SetPropertyBlock(mpb);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        private static Mesh GetCanopyMesh(ParachuteDefinition d)
        {
            string key = d.canopyDiameter.ToString("F2");
            if (CanopyMeshes.TryGetValue(key, out var m)) return m;
            float R = d.canopyDiameter * 0.5f;
            float lineLen = R * 1.7f;
            var mb = new MeshBuilder();
            // Dome: rim at y = lineLen, apex at lineLen + 0.45R (outer surface faces outward/up)
            var prof = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                float ang = t * Mathf.PI * 0.5f;
                prof.Add(new Vector2(R * Mathf.Cos(ang) + 0.01f, lineLen + 0.45f * R * Mathf.Sin(ang)));
            }
            mb.Lathe(0, prof, 24, true);
            // inner surface (reverse) so it is visible from below
            var inner = new List<Vector2>();
            for (int i = prof.Count - 1; i >= 0; i--) inner.Add(new Vector2(prof[i].x * 0.995f, prof[i].y - 0.01f));
            mb.Lathe(0, inner, 24, true);
            // suspension lines
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                mb.CylinderBetween(0, Vector3.zero, new Vector3(Mathf.Cos(a) * R, lineLen, Mathf.Sin(a) * R), 0.012f, 3, false);
            }
            m = mb.ToMergedMesh("canopy_" + key);
            m.RecalculateNormals();
            CanopyMeshes[key] = m;
            return m;
        }

        public override void OnPartDestroyed()
        {
            if (_canopy != null) UnityEngine.Object.Destroy(_canopy);
        }

        public override void Save(Dictionary<string, string> s)
        {
            s["state"] = State.ToString();
            s["area"] = F(CurrentArea);
            s["canopyTemp"] = F(CanopyTemp);
        }

        public override void Load(Dictionary<string, string> s)
        {
            if (Enum.TryParse(GetString(s, "state", "Stowed"), out ChuteState st)) State = st;
            CurrentArea = GetDouble(s, "area");
            CanopyTemp = GetDouble(s, "canopyTemp", 290);
        }

        public override void CollectActions(List<PartAction> actions)
        {
            if (State == ChuteState.Stowed) actions.Add(new PartAction("Deploy parachute", OnActivate));
            if (IsOpen) actions.Add(new PartAction("Cut parachute", Cut));
        }

        public override void CollectInfo(List<string> info)
        {
            info.Add($"Chute: {State}" + (State == ChuteState.Failed ? $" ({FailReason})" : ""));
            if (IsOpen) info.Add($"Load {CanopyLoad / 1000:F1}/{Def.maxLoad / 1000:F0} kN, canopy {CanopyTemp:F0} K");
        }

        public override string StageStatus => State == ChuteState.Stowed ? null : State.ToString();
    }
}
