using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Computes internal joint loads for a single-rigid-body vessel.
    /// For each part i the net non-gravitational force required to give it the rigid-body acceleration is
    ///   f_i = m_i (a_cm + alpha x r_i + w x (w x r_i)) - F_ext_i
    /// (gravity cancels because it acts equally on all mass). The force a parent transmits into a child
    /// subtree is the sum of f_i over the subtree; the bending moment about the joint follows from the
    /// same sums. Loads are compared with the child's connection strength (compression is much stronger
    /// than tension/shear) and smoothed over ~80 ms to reject contact-solver spikes.
    /// </summary>
    public static class StructuralAnalysis
    {
        public const double CompressionFactor = 8.0;
        public const double SmoothingTime = 0.08;

        private static readonly Dictionary<Part, Vector3> SubF = new Dictionary<Part, Vector3>();
        private static readonly Dictionary<Part, Vector3> SubM = new Dictionary<Part, Vector3>();

        public static void Check(Vessel v, Vector3 aProperCm, Vector3 alpha, Vector3 omega, double dt)
        {
            if (v.Parts.Count < 2 || v.RootPart == null) return;
            SubF.Clear();
            SubM.Clear();
            Vector3 com = v.Rb.worldCenterOfMass;
            Accumulate(v.RootPart, com, aProperCm, alpha, omega);

            foreach (var p in v.Parts)
            {
                if (p == v.RootPart || p.ParentPart == null) continue;
                Vector3 F = SubF[p];
                Vector3 jp = p.JointWorldPosition;
                Vector3 M = SubM[p] - Vector3.Cross(jp, F);
                Vector3 u = p.JointAxisTowardsParent;
                float axial = Vector3.Dot(F, u);
                float shear = (F - axial * u).magnitude;
                float bend = (M - Vector3.Dot(M, u) * u).magnitude;
                double limF = p.Def.breakingForce, limM = p.Def.breakingTorque;
                double util;
                string what;
                double tensionU = axial > 0 ? axial / limF : -axial / (limF * CompressionFactor);
                double shearU = shear / limF;
                double bendU = bend / limM;
                if (tensionU >= shearU && tensionU >= bendU) { util = tensionU; what = axial > 0 ? "tension" : "compression"; }
                else if (shearU >= bendU) { util = shearU; what = "shear"; }
                else { util = bendU; what = "bending"; }
                double a = Math.Min(1.0, dt / SmoothingTime);
                p.SmoothedJointLoad += (util - p.SmoothedJointLoad) * a;
                p.LastLoadDescription = what;
                if (p.SmoothedJointLoad > 1.0)
                {
                    string msg = what == "bending"
                        ? $"{bend / 1000:F0} kN*m bending > {limM / 1000:F0} kN*m"
                        : $"{(what == "shear" ? shear : Math.Abs(axial)) / 1000:F0} kN {what} > {(what == "compression" ? limF * CompressionFactor : limF) / 1000:F0} kN";
                    if (Vessel.DebugSeparation)
                    {
                        var sb = new System.Text.StringBuilder($"[SepDebug] joint {p.Def.id}: F {F} M {M} jp {jp} com {com}\n");
                        var sub = new List<Part>();
                        p.CollectSubtree(sub);
                        foreach (var q in sub)
                            sb.Append($"   {q.Def.id} pos {q.transform.position:F2} m {q.Mass:F0} ext {q.ExternalForce:F0} extM {q.ExternalMoment:F0} cross(pos,ext) {Vector3.Cross(q.transform.position, q.ExternalForce):F0} contact {q.ContactForce:F0}\n");
                        Debug.Log(sb.ToString());
                    }
                    v.QueueJointFailure(p, $"structural failure at {p.Def.title} joint ({msg}; vessel {aProperCm.magnitude / 9.81f:F1} g, α {alpha.magnitude:F1} rad/s², ω {omega.magnitude:F2} rad/s)");
                }
            }
        }

        private static void Accumulate(Part p, Vector3 com, Vector3 aCm, Vector3 alpha, Vector3 omega)
        {
            Vector3 pos = p.transform.position;
            Vector3 r = pos - com;
            Vector3 a = aCm + Vector3.Cross(alpha, r) + Vector3.Cross(omega, Vector3.Cross(omega, r));
            float m = (float)p.Mass;
            Vector3 fNet = m * a - p.ExternalForce - p.ContactForce;
            Vector3 F = fNet;
            // moment of the required net force about the world origin, minus moments of external forces
            // applied at their true points (tracked in ExternalMoment).
            Vector3 M = Vector3.Cross(pos, m * a) - p.ExternalMoment - p.ContactMoment;
            for (int i = 0; i < p.Children.Count; i++)
            {
                var c = p.Children[i];
                Accumulate(c, com, aCm, alpha, omega);
                F += SubF[c];
                M += SubM[c];
            }
            SubF[p] = F;
            SubM[p] = M;
        }
    }
}
