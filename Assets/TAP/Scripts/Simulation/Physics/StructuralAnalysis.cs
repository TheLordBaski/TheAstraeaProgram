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
    ///
    /// The load can be summed over either side of a joint. Measured accelerations and contact forces never
    /// balance exactly, and a sum over the side holding almost the whole vessel carries that mismatch into the
    /// joint; so each joint is evaluated from the side without ground contact, or else from the lighter side.
    /// </summary>
    public static class StructuralAnalysis
    {
        public const double CompressionFactor = 8.0;
        public const double SmoothingTime = 0.08;

        private static readonly Dictionary<Part, Vector3> SubF = new Dictionary<Part, Vector3>();
        private static readonly Dictionary<Part, Vector3> SubM = new Dictionary<Part, Vector3>();
        private static readonly Dictionary<Part, float> SubMass = new Dictionary<Part, float>();
        private static readonly Dictionary<Part, int> SubContacts = new Dictionary<Part, int>();

        public static void Check(Vessel v, Vector3 aProperCm, Vector3 alpha, Vector3 omega, double dt)
        {
            if (v.Parts.Count < 2 || v.RootPart == null) return;
            SubF.Clear();
            SubM.Clear();
            SubMass.Clear();
            SubContacts.Clear();
            Vector3 com = v.Rb.worldCenterOfMass;
            Accumulate(v.RootPart, com, aProperCm, alpha, omega);
            // Whole-vessel totals: zero if accelerations and forces agreed exactly.
            Vector3 fTotal = SubF[v.RootPart], mTotal = SubM[v.RootPart];
            float massTotal = SubMass[v.RootPart];
            int contactsTotal = SubContacts[v.RootPart];

            foreach (var p in v.Parts)
            {
                if (p == v.RootPart || p.ParentPart == null) continue;
                Vector3 F = SubF[p];
                Vector3 Msub = SubM[p];
                bool childContact = SubContacts[p] > 0, parentContact = contactsTotal - SubContacts[p] > 0;
                bool parentSide = childContact != parentContact ? childContact : SubMass[p] > massTotal - SubMass[p];
                if (parentSide)
                {
                    // What the joint must give the child subtree, seen from the parent side (Newton's third law).
                    F -= fTotal;
                    Msub -= mTotal;
                }
                Vector3 jp = p.JointWorldPosition;
                Vector3 M = Msub - Vector3.Cross(jp, F);
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
            float mass = m;
            int contacts = p.ContactForce.sqrMagnitude > 0f ? 1 : 0;
            for (int i = 0; i < p.Children.Count; i++)
            {
                var c = p.Children[i];
                Accumulate(c, com, aCm, alpha, omega);
                F += SubF[c];
                M += SubM[c];
                mass += SubMass[c];
                contacts += SubContacts[c];
            }
            SubF[p] = F;
            SubM[p] = M;
            SubMass[p] = mass;
            SubContacts[p] = contacts;
        }
    }
}
