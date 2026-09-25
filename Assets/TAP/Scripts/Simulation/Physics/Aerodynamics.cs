using System;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Per-part aerodynamic forces applied at each part's position, so the distribution of drag
    /// relative to the centre of mass produces real (in)stability:
    ///  * end-face drag with stack-node occlusion (stacked parts shield each other's faces),
    ///  * cross-flow drag on the sides (normal force at angle of attack),
    ///  * slender-body "lift" on pointed noses (destabilising when ahead of the CoM),
    ///  * flat-plate lift on fins (stabilising when behind the CoM),
    ///  * a transonic drag-rise Mach curve.
    /// </summary>
    public static class Aerodynamics
    {
        public const float SkinFrictionCf = 0.0035f;

        public static double MachFactor(double m)
        {
            if (m < 0.85) return 1.0;
            if (m < 1.0) return 1.0 + (m - 0.85) / 0.15 * 0.7;
            if (m < 1.25) return 1.7 + (m - 1.0) / 0.25 * 0.1;
            if (m < 1.6) return 1.8 - (m - 1.25) / 0.35 * 0.3;
            if (m < 2.5) return 1.5 - (m - 1.6) / 0.9 * 0.25;
            if (m < 4.0) return 1.25 - (m - 2.5) / 1.5 * 0.15;
            if (m < 10.0) return 1.1 - (m - 4.0) / 6.0 * 0.1;
            return 1.0;
        }

        /// <summary>True if a stack node is covered by an attached part at least ~90% as wide.</summary>
        public static bool NodeOccluded(Part p, string nodeId)
        {
            float d = p.Def.diameter;
            if (p.AttachNode == nodeId && p.ParentPart != null && p.ParentPart.Def.diameter >= d * 0.88f) return true;
            for (int i = 0; i < p.Children.Count; i++)
            {
                var c = p.Children[i];
                if (c.ParentNode == nodeId && c.AttachNode != "srf" && c.Def.diameter >= d * 0.88f) return true;
            }
            return false;
        }

        public static void Apply(Vessel v, double ut)
        {
            double rho = v.AirDensity;
            if (rho <= 1e-12) return;
            float machF = (float)MachFactor(v.Mach);
            Vector3 airVel = v.AirVelocityAt(v.Rb.worldCenterOfMass);
            var rb = v.Rb;
            for (int i = 0; i < v.Parts.Count; i++)
            {
                var p = v.Parts[i];
                Vector3 pos = p.transform.position;
                Vector3 vRel = rb.GetPointVelocity(pos) - airVel;
                float speed = vRel.magnitude;
                if (speed < 0.01f) continue;
                Vector3 force = PartForce(p, vRel, speed, (float)rho) * machF;
                v.AddForceAtPosition(p, force, pos);

                var fin = p.GetModule<FinModule>();
                if (fin != null)
                {
                    float span = p.Def.model != null ? p.Def.model.diameter : 0.6f;
                    Vector3 cp = p.transform.TransformPoint(new Vector3(0, 0, span * 0.45f));
                    Vector3 vr = rb.GetPointVelocity(cp) - airVel;
                    v.AddForceAtPosition(p, fin.ComputeForce(vr, rho, machF), cp);
                }
            }
        }

        /// <summary>Aerodynamic force (without Mach factor) for one part moving at vRel through still air.</summary>
        public static Vector3 PartForce(Part p, Vector3 vRel, float speed, float rho)
        {
            var def = p.Def;
            var drag = def.drag;
            if (def.fin != null)
            {
                // Fins: only a little parasitic drag here; lift handled by FinModule.
                return -vRel * (0.5f * rho * speed * 0.02f * def.fin.area);
            }
            Vector3 a = p.transform.up;
            float D = Mathf.Max(0.05f, def.diameter);
            float H = Mathf.Max(0.05f, def.height);
            float aEnd = Mathf.PI * D * D * 0.25f;
            float aSide = D * H;
            float vAx = Vector3.Dot(vRel, a);
            Vector3 vN = vRel - vAx * a;
            float vNmag = vN.magnitude;

            float cdFace;
            if (vAx > 0) cdFace = NodeOccluded(p, "top") ? 0f : drag.cdTop;
            else cdFace = NodeOccluded(p, "bottom") ? 0f : drag.cdBottom;
            if (p.Def.FindNode("top") == null && vAx > 0 && p.Def.surfaceAttach != null && p.Def.surfaceAttach.allowed == false && p.Def.model?.type == "nosecone")
                cdFace = drag.cdTop;

            Vector3 f = Vector3.zero;
            float half = 0.5f * rho;
            // End-face (pressure) drag from the axial flow component.
            f += -a * Mathf.Sign(vAx) * half * vAx * vAx * cdFace * aEnd;
            // Cross-flow drag on the side silhouette.
            if (vNmag > 1e-4f)
            {
                Vector3 nDir = vN / vNmag;
                f += -nDir * half * vNmag * vNmag * drag.cdSide * aSide;
                // Slender-body normal force on pointed forebodies.
                if (drag.bodyLift > 0)
                {
                    float sin2 = 2f * (vNmag / speed) * (Mathf.Abs(vAx) / speed);
                    f += -nDir * half * speed * speed * aEnd * drag.bodyLift * sin2;
                }
            }
            // Skin friction.
            f += -vRel.normalized * half * speed * speed * SkinFrictionCf * Mathf.PI * D * H;
            return f;
        }
    }

    /// <summary>
    /// Two-node thermal model per part (thin skin + interior). Convective heating uses
    /// q = h (T_recovery - T_skin), h = K * sqrt(rho) * v, applied to the part area facing the flow
    /// and reduced by shadowing from upstream parts (a heat shield protects what is behind it).
    /// Skins radiate, conduct into the interior, and interiors conduct across joints.
    /// </summary>
    public static class Thermal
    {
        /// <summary>Convective coefficient (physical Sutton-Graves ~0.35 scaled for this smaller world).</summary>
        public static double ConvectionK = 1.45;
        public const double SkinToInternal = 6.0;    // W/m^2/K
        public const double JointConductance = 35.0; // W/K
        public const double SpaceTemperature = 250.0; // effective radiative sink incl. sunlight (K)
        public const double PyrolysisTemp = 1150.0;  // ablator surface temperature (K)

        private static float[] _s = new float[64];
        private static Vector3[] _c = new Vector3[64];
        private static float[] _r = new float[64];
        private static int[] _order = new int[64];

        public static double SkinArea(Part p)
        {
            double D = Math.Max(0.05, p.Def.diameter), H = Math.Max(0.05, p.Def.height);
            return Math.PI * D * H + Math.PI * D * D * 0.5;
        }

        public static void Step(Vessel v, double dt, double ut)
        {
            int n = v.Parts.Count;
            if (n == 0) return;
            double rho = v.AirDensity;
            Vector3 airVel = v.AirVelocityAt(v.Rb.worldCenterOfMass);
            Vector3 vRel = v.Rb.linearVelocity - airVel;
            float speed = vRel.magnitude;
            bool flow = rho > 1e-10 && speed > 20f;
            double tAmb = v.InAtmosphere ? v.AirTemperature : SpaceTemperature;
            double tEnv = v.InAtmosphere ? Math.Max(v.AirTemperature, 180) : SpaceTemperature;
            double tRec = tAmb + 0.9 * speed * speed / 2008.0;
            double h = flow ? ConvectionK * Math.Sqrt(rho) * speed : 0;
            // weak natural/forced convection towards ambient at low speed
            double hLow = rho > 1e-6 ? 4.0 * Math.Sqrt(rho / 1.225) : 0;

            if (flow) ComputeExposure(v, vRel / speed);
            else for (int i = 0; i < n; i++) v.Parts[i].Exposure = 1f;

            for (int i = 0; i < n; i++)
            {
                var p = v.Parts[i];
                var def = p.Def;
                double aSkin = SkinArea(p);
                double cp = def.specificHeat;
                double skinMass = def.skinMassPerArea * aSkin;
                double cSkin = Math.Max(skinMass * cp, 50);
                double cInt = Math.Max((p.Mass - skinMass) * cp, 200);

                double q = 0;
                if (flow)
                {
                    Vector3 a = p.transform.up;
                    float cosT = Vector3.Dot(vRel / speed, a);
                    float D = Mathf.Max(0.05f, def.diameter), H = Mathf.Max(0.05f, def.height);
                    bool topFacing = cosT > 0;
                    bool faceOpen = !Aerodynamics.NodeOccluded(p, topFacing ? "top" : "bottom");
                    double aFront = (faceOpen ? Mathf.PI * D * D * 0.25f * Mathf.Abs(cosT) : 0) + D * H * Mathf.Sqrt(Mathf.Max(0, 1 - cosT * cosT)) * 0.6f;
                    if (def.fin != null) aFront = def.fin.area * 0.3;
                    q = h * aFront * p.Exposure * (tRec - p.SkinTemp);
                }
                q += hLow * aSkin * 0.3 * (tAmb - p.SkinTemp);
                p.HeatFlux = q;
                double tS4 = p.SkinTemp * p.SkinTemp; tS4 *= tS4;
                double tE4 = tEnv * tEnv; tE4 *= tE4;
                double qRad = def.emissivity * MathD.StefanBoltzmann * aSkin * (tS4 - tE4);
                double qSi = SkinToInternal * aSkin * (p.SkinTemp - p.InternalTemp);

                p.SkinTemp += (q - qRad - qSi) * dt / cSkin;
                p.InternalTemp += qSi * dt / cInt;

                // Ablation holds the shield surface near the pyrolysis temperature while ablator lasts.
                var hs = p.GetModule<HeatShieldModule>();
                if (hs != null)
                {
                    hs.AblationRate = 0;
                    var abl = p.GetResource("Ablator");
                    if (abl != null && abl.Amount > 0 && p.SkinTemp > PyrolysisTemp)
                    {
                        double excess = (p.SkinTemp - PyrolysisTemp) * cSkin;
                        double mass = Math.Min(abl.Amount, Math.Min(excess / hs.Def.ablationHeat, hs.Def.maxAblationRate * dt));
                        abl.Amount -= mass;
                        p.SkinTemp -= mass * hs.Def.ablationHeat / cSkin;
                        hs.AblationRate = mass / dt;
                    }
                }
                if (p.SkinTemp < 3) p.SkinTemp = 3;
                if (p.InternalTemp < 3) p.InternalTemp = 3;
            }

            // Conduction across joints (heat shields insulate what they protect).
            for (int i = 0; i < n; i++)
            {
                var p = v.Parts[i];
                var par = p.ParentPart;
                if (par == null) continue;
                double k = JointConductance;
                var hs1 = p.Def.heatShield; var hs2 = par.Def.heatShield;
                if (hs1 != null) k = hs1.insulation;
                else if (hs2 != null) k = hs2.insulation;
                double cA = Math.Max((p.Mass - p.Def.skinMassPerArea * SkinArea(p)) * p.Def.specificHeat, 200);
                double cB = Math.Max((par.Mass - par.Def.skinMassPerArea * SkinArea(par)) * par.Def.specificHeat, 200);
                double Q = k * (p.InternalTemp - par.InternalTemp) * dt;
                // stability clamp
                double maxQ = Math.Abs(p.InternalTemp - par.InternalTemp) * Math.Min(cA, cB) * 0.5;
                if (Math.Abs(Q) > maxQ) Q = Math.Sign(Q) * maxQ;
                p.InternalTemp -= Q / cA;
                par.InternalTemp += Q / cB;
            }

            // Failures
            if (v.ImpactProtected || DevCheats.IgnoreHeat) return;
            for (int i = 0; i < n; i++)
            {
                var p = v.Parts[i];
                if (p.SkinTemp > p.Def.skinMaxTemp)
                    v.QueueDestroy(p, $"overheated (skin {p.SkinTemp:F0} K > {p.Def.skinMaxTemp:F0} K)");
                else if (p.InternalTemp > p.Def.maxTemp)
                    v.QueueDestroy(p, $"overheated (internal {p.InternalTemp:F0} K > {p.Def.maxTemp:F0} K)");
            }
        }

        /// <summary>
        /// Flow shadowing: each part is projected onto the plane perpendicular to the flow; upstream parts
        /// whose projected disc covers it reduce its exposure.
        /// </summary>
        public static void ComputeExposure(Vessel v, Vector3 motionDir)
        {
            int n = v.Parts.Count;
            if (_s.Length < n) { _s = new float[n * 2]; _c = new Vector3[n * 2]; _r = new float[n * 2]; _order = new int[n * 2]; }
            for (int i = 0; i < n; i++)
            {
                var p = v.Parts[i];
                Vector3 pos = p.transform.position;
                float s = Vector3.Dot(pos, motionDir);
                _s[i] = s;
                _c[i] = pos - s * motionDir;
                float D = Mathf.Max(0.05f, p.Def.diameter), H = Mathf.Max(0.05f, p.Def.height);
                float cosT = Mathf.Abs(Vector3.Dot(p.transform.up, motionDir));
                float aProj = Mathf.PI * D * D * 0.25f * cosT + D * H * Mathf.Sqrt(Mathf.Max(0, 1 - cosT * cosT));
                _r[i] = Mathf.Sqrt(aProj / Mathf.PI);
                _order[i] = i;
            }
            Array.Sort(_order, 0, n, Comparer.Instance);
            for (int oi = 0; oi < n; oi++)
            {
                int j = _order[oi];
                float exposure = 1f;
                for (int ok = 0; ok < oi; ok++)
                {
                    int i = _order[ok];
                    if (_s[i] < _s[j] + 0.05f) continue;
                    float d = (_c[i] - _c[j]).magnitude;
                    float ri = _r[i], rj = _r[j];
                    if (d + 0.8f * rj <= ri) { exposure = 0f; break; }
                    if (d < ri + rj)
                    {
                        float overlap = Mathf.Clamp01((ri + rj - d) / (2f * rj));
                        exposure = Mathf.Min(exposure, 1f - overlap * 0.9f);
                    }
                }
                v.Parts[j].Exposure = exposure;
            }
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<int>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(int a, int b) => _s[b].CompareTo(_s[a]); // descending: most upstream first
        }
    }

    /// <summary>Simple per-part buoyancy and water drag on bodies with oceans, plus splashdown impact damage.</summary>
    public static class Buoyancy
    {
        public const float WaterDensity = 1000f;

        public static void Apply(Vessel v, double ut)
        {
            var body = v.MainBody;
            if (!body.HasOcean) return;
            if (v.Altitude > 200) return;
            var frame = v.Sim.Frame;
            Vector3 up = -v.GravityAccel.normalized;
            float g = v.GravityAccel.magnitude;
            foreach (var p in v.Parts)
            {
                Vector3 pos = p.transform.position;
                double h = frame.ToTrue(pos).magnitude - body.Radius;
                float H = Mathf.Max(0.2f, p.Def.height), D = Mathf.Max(0.2f, p.Def.diameter);
                float halfExtent = Mathf.Max(H, D) * 0.5f;
                if (h > halfExtent) continue;
                // terrain above sea level here? (e.g. beach) -> no water
                if (body.TerrainHeightAt(frame.ToTrue(pos), ut) > 0) continue;
                float frac = Mathf.Clamp01((float)((halfExtent - h) / (2 * halfExtent)));
                float vol = Mathf.PI * D * D * 0.25f * H * 0.85f;
                Vector3 vel = v.Rb.GetPointVelocity(pos) - v.AirVelocityAt(pos);
                float vDown = -Vector3.Dot(vel, up);
                if (frac > 0 && !_submerged.Contains(p) && !v.ImpactProtected && !DevCheats.NoCrashDamage)
                {
                    _submerged.Add(p);
                    if (vDown > p.Def.impactTolerance * 1.4f)
                    {
                        v.QueueDestroy(p, $"splashdown at {vDown:F1} m/s");
                        continue;
                    }
                }
                else if (frac <= 0) _submerged.Remove(p);
                Vector3 fb = up * WaterDensity * g * vol * frac;
                Vector3 fd = -vel * (0.5f * WaterDensity * vel.magnitude * 0.6f * D * H * frac);
                v.AddForceAtPosition(p, fb + fd, pos);
                v.AddContactFlag();
            }
        }

        private static readonly System.Collections.Generic.HashSet<Part> _submerged = new System.Collections.Generic.HashSet<Part>();
    }
}
