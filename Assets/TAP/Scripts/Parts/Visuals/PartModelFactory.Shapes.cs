using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>
    /// The shapes of the Early Access parts (PRT-02): solar panels, antennas, a spotlight, a ladder, the launch clamp, fairing bases,
    /// science instruments, the lander can, a radial probe, separators and the separation motor. Radial parts face outward along +Z with
    /// their back against the hull at z = -0.15 × diameter (the part's surfaceAttach point). The rest pose of a deployable's pieces is the
    /// retracted one; their names are the ones in the part's `deployable.motions` (Docs/DEPLOYABLES.md).
    /// </summary>
    public static partial class PartModelFactory
    {
        /// <summary>Z of the face of a radial part that touches the hull.</summary>
        private static float Back(ModelDefinition m) => -0.15f * m.diameter;

        /// <summary>Runs <paramref name="build"/> in a frame moved to <paramref name="origin"/>.</summary>
        private static void At(MeshBuilder mb, Vector3 origin, Action build)
        {
            var saved = mb.Transform;
            mb.Transform = saved * Matrix4x4.Translate(origin);
            build();
            mb.Transform = saved;
        }

        /// <summary>Runs <paramref name="build"/> with the builder's +Y axis turned to point along +Z (a lathe then makes a cylinder that sticks out of the hull).</summary>
        private static void AlongZ(MeshBuilder mb, Vector3 origin, Action build)
        {
            var saved = mb.Transform;
            mb.Transform = saved * Matrix4x4.TRS(origin, Quaternion.Euler(90, 0, 0), Vector3.one);
            build();
            mb.Transform = saved;
        }

        // ------------------------------------------------------------------ solar panels

        /// <summary>
        /// One wing of a folding panel. Its hinge is at the origin and its long axis is +Y: a short arm, then the plate with cells on both faces,
        /// starting half a width up so that, swung out, it clears the body.
        /// </summary>
        private static Mesh Wing(string name, float w, float len, out int[] slots)
        {
            var b = new MeshBuilder();
            float off = w / 2, y = off + len / 2;
            b.Box(S(MatSlot.MetalDark), new Vector3(0, off / 2, 0), new Vector3(0.03f, off, 0.012f));
            b.Box(S(MatSlot.Metal), new Vector3(0, y, 0), new Vector3(w, len, 0.012f));
            foreach (float z in new[] { 0.0062f, -0.0062f })
            {
                b.Box(S(MatSlot.Blue), new Vector3(0, y, z), new Vector3(w * 0.9f, len * 0.92f, 0.004f));
                for (int i = 1; i < 3; i++)
                    b.Box(S(MatSlot.Dark), new Vector3(-w * 0.45f + w * 0.9f * i / 3f, y, z * 1.1f), new Vector3(0.006f, len * 0.92f, 0.003f));
            }
            return b.ToMesh(name, out slots);
        }

        private static void SolarPanel(MeshBuilder mb, MeshBuilder col, ModelDefinition m, CachedModel cm, PartDefinition def)
        {
            float w = m.diameter, h = m.height, zb = Back(m);
            if (m.style == "fold")
            {
                // A body at the foot with the hinge on its top edge; the wings stand folded above it, one behind the other.
                float ww = w * 0.92f, bodyH = h * 0.24f, bodyD = 0.07f, hingeY = -h / 2 + bodyH, hingeZ = zb + bodyD + 0.008f, len = h - bodyH - ww / 2;
                mb.Box(S(MatSlot.Dark), new Vector3(0, -h / 2 + bodyH / 2, zb + bodyD / 2), new Vector3(w, bodyH, bodyD));
                mb.Box(S(MatSlot.Metal), new Vector3(0, hingeY, zb + bodyD / 2), new Vector3(w * 0.9f, 0.02f, bodyD * 0.9f));
                mb.Box(S(MatSlot.Yellow), new Vector3(0, -h / 2 + bodyH * 0.45f, zb + bodyD + 0.001f), new Vector3(w * 0.5f, bodyH * 0.18f, 0.002f));
                var a = Wing(def.id + "_wingA", ww, len, out int[] sa);
                var b = Wing(def.id + "_wingB", ww, len, out int[] sb);
                cm.subParts.Add(("WingA", a, sa, new Vector3(0, hingeY, hingeZ), Quaternion.identity));
                cm.subParts.Add(("WingB", b, sb, new Vector3(0, hingeY, hingeZ + 0.016f), Quaternion.identity));
                col.Box(0, new Vector3(0, 0, zb + 0.05f), new Vector3(w, h, 0.1f));
                return;
            }
            // fixed: a mount, a frame and a sheet of cells with dividing lines
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0, zb + 0.02f), new Vector3(0.14f, 0.14f, 0.04f));
            mb.Box(S(MatSlot.Metal), new Vector3(0, 0, zb + 0.05f), new Vector3(w, h, 0.02f));
            mb.Box(S(MatSlot.Blue), new Vector3(0, 0, zb + 0.062f), new Vector3(w * 0.92f, h * 0.92f, 0.004f));
            for (int i = 1; i < 4; i++)
            {
                mb.Box(S(MatSlot.Dark), new Vector3(-w * 0.46f + w * 0.92f * i / 4f, 0, zb + 0.0645f), new Vector3(0.006f, h * 0.92f, 0.002f));
                mb.Box(S(MatSlot.Dark), new Vector3(0, -h * 0.46f + h * 0.92f * i / 4f, zb + 0.0645f), new Vector3(w * 0.92f, 0.006f, 0.002f));
            }
            col.Box(0, new Vector3(0, 0, zb + 0.04f), new Vector3(w, h, 0.08f));
        }

        // ------------------------------------------------------------------ antennas

        private static void Antenna(MeshBuilder mb, MeshBuilder col, ModelDefinition m, CachedModel cm, PartDefinition def)
        {
            float d = m.diameter, h = m.height, zb = Back(m);
            if (m.style == "dish")
            {
                // A housing at the foot, a boom that slides up out of it, and a dish on top that tips from facing down to facing out.
                float R = d * 0.3f; // the folded bowl hangs round the boom, so the boom stands one bowl radius off the hull
                float baseH = h * 0.36f, y0 = -h / 2, y1 = y0 + baseH, zc = zb + R + 0.01f;
                mb.Box(S(MatSlot.Dark), new Vector3(0, (y0 + y1) / 2, (zb + zc) / 2 + 0.015f), new Vector3(d * 0.4f, baseH, zc - zb + 0.03f));
                At(mb, new Vector3(0, 0, zc), () => Cyl(mb, S(MatSlot.Metal), 0.045f, y1 - 0.01f, y1 + 0.02f, 16));
                var boom = new MeshBuilder();
                boom.Lathe(S(MatSlot.Metal), new[] { new Vector2(0.03f, -0.2f), new Vector2(0.03f, 0.16f) }, 16, true);
                boom.Lathe(S(MatSlot.Dark), new[] { new Vector2(0.04f, 0.13f), new Vector2(0.04f, 0.16f) }, 16, true);
                boom.Disc(S(MatSlot.Dark), 0.04f, 0.16f, true, 16);
                var boomMesh = boom.ToMesh(def.id + "_boom", out int[] bslots);
                cm.subParts.Add(("Boom", boomMesh, bslots, new Vector3(0, y1, zc), Quaternion.identity));

                float depth = d * 0.17f, yRim = -0.07f;
                var outer = new List<Vector2>();
                for (int i = 0; i <= 8; i++)
                {
                    float r = R * (1 - i / 8f);
                    outer.Add(new Vector2(r + 0.001f, yRim + depth * (1 - (r / R) * (r / R))));
                }
                var dish = new MeshBuilder();
                dish.Lathe(S(MatSlot.Metal), outer, 28, true);
                var inner = new List<Vector2>();
                for (int i = outer.Count - 1; i >= 0; i--) inner.Add(new Vector2(outer[i].x * 0.97f, outer[i].y - 0.004f));
                dish.Lathe(S(MatSlot.White), inner, 28, true);
                dish.Disc(S(MatSlot.Metal), R, yRim, false, 28, R * 0.97f);
                dish.CylinderBetween(S(MatSlot.MetalDark), new Vector3(0, yRim + depth, 0), new Vector3(0, yRim - 0.05f, 0), 0.008f, 6, false);
                dish.Sphere(S(MatSlot.Gold), new Vector3(0, yRim - 0.05f, 0), 0.02f, 8, 5);
                var dishMesh = dish.ToMesh(def.id + "_dish", out int[] dslots);
                cm.subParts.Add(("Boom/Dish", dishMesh, dslots, new Vector3(0, 0.16f, 0), Quaternion.identity));
                col.Box(0, new Vector3(0, 0, zc), new Vector3(R * 2, h, R * 2 + 0.03f));
                return;
            }
            // whip: a slim mast on a small base, topped by a red ball
            float zm = zb + 0.035f;
            mb.Box(S(MatSlot.Dark), new Vector3(0, -h / 2 + 0.04f, zb + 0.03f), new Vector3(0.09f, 0.08f, 0.06f));
            mb.CylinderBetween(S(MatSlot.Metal), new Vector3(0, -h / 2 + 0.08f, zm), new Vector3(0, h / 2 - 0.02f, zm), 0.008f, 6);
            mb.CylinderBetween(S(MatSlot.MetalDark), new Vector3(0, -h / 2 + 0.08f, zm), new Vector3(0, -h / 2 + 0.2f, zm), 0.014f, 8);
            mb.Sphere(S(MatSlot.Red), new Vector3(0, h / 2 - 0.01f, zm), 0.018f, 8, 5);
            col.Box(0, new Vector3(0, 0, zb + 0.04f), new Vector3(0.1f, h, 0.08f));
        }

        // ------------------------------------------------------------------ light, ladder, clamp

        private static void Spotlight(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float d = m.diameter, zb = Back(m), r = d * 0.36f, len = m.height * 0.55f;
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0, zb + 0.015f), new Vector3(d * 0.8f, d * 0.8f, 0.03f));
            AlongZ(mb, new Vector3(0, 0, zb + 0.03f), () =>
            {
                Cyl(mb, S(MatSlot.MetalDark), r, 0, len * 0.7f, 16);
                Frustum(mb, S(MatSlot.Metal), r, len * 0.7f, r * 1.25f, len, 16);
                mb.Disc(S(MatSlot.Glass), r * 1.15f, len, true, 16, r * 0.86f);
                mb.Disc(S(MatSlot.Yellow), r * 0.86f, len - 0.004f, true, 16);
            });
            col.Box(0, new Vector3(0, 0, zb + 0.07f), new Vector3(d, d, 0.14f));
        }

        private static void Ladder(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float h = m.height, half = Mathf.Max(m.diameter, 0.2f), zb = Back(m), zr = zb + 0.07f;
            foreach (int s in new[] { -1, 1 })
            {
                mb.CylinderBetween(S(MatSlot.Metal), new Vector3(s * half, -h / 2, zr), new Vector3(s * half, h / 2, zr), 0.014f, 8);
                foreach (float y in new[] { -h / 2 + 0.06f, 0f, h / 2 - 0.06f })
                    mb.Box(S(MatSlot.Dark), new Vector3(s * half, y, (zb + zr) / 2), new Vector3(0.03f, 0.03f, zr - zb));
            }
            int rungs = Mathf.Max(3, Mathf.FloorToInt(h / 0.22f));
            for (int i = 0; i < rungs; i++)
            {
                float y = -h / 2 + 0.11f + (h - 0.22f) * i / Mathf.Max(1, rungs - 1);
                mb.CylinderBetween(S(MatSlot.MetalDark), new Vector3(-half, y, zr), new Vector3(half, y, zr), 0.01f, 6);
            }
            col.Box(0, new Vector3(0, 0, (zb + zr) / 2 + 0.01f), new Vector3(half * 2 + 0.06f, h, zr - zb + 0.04f));
        }

        private static void Clamp(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float w = m.diameter, h = m.height, zb = Back(m), zt = 0.28f;
            // foot on the pad, a tower, and two arms that reach the rocket with a yellow gripper at each hand
            mb.Box(S(MatSlot.Concrete), new Vector3(0, -h / 2 + 0.04f, 0.2f), new Vector3(w, 0.08f, 0.8f));
            mb.Box(S(MatSlot.Grey), new Vector3(0, 0.0f, zt), new Vector3(0.14f, h - 0.08f, 0.14f));
            foreach (float y in new[] { h * 0.28f, -h * 0.12f })
            {
                mb.Box(S(MatSlot.Grey), new Vector3(0, y, (zt + zb) / 2), new Vector3(0.1f, 0.08f, zt - zb));
                mb.Box(S(MatSlot.Yellow), new Vector3(0, y, zb + 0.03f), new Vector3(0.22f, 0.12f, 0.06f));
            }
            mb.CylinderBetween(S(MatSlot.MetalDark), new Vector3(0, -h / 2 + 0.08f, 0.6f), new Vector3(0, h * 0.2f, zt + 0.05f), 0.02f, 6);
            mb.Box(S(MatSlot.Red), new Vector3(0, h / 2 - 0.05f, zt), new Vector3(0.16f, 0.1f, 0.16f));
            col.Box(0, new Vector3(0, 0, 0.2f), new Vector3(w, h, 0.8f));
        }

        // ------------------------------------------------------------------ stack and radial parts

        private static void FairingBase(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            Cyl(mb, S(MatSlot.Dark), r, y0, y0 + h * 0.3f);
            for (int i = 0; i < 12; i++)
            {
                var q = Quaternion.AngleAxis(i * 30, Vector3.up);
                mb.OrientedBox(S(MatSlot.Metal), q * new Vector3(0, y0 + h * 0.15f, r), q, new Vector3(0.05f, h * 0.12f, 0.02f));
            }
            Cyl(mb, S(MatSlot.White), r * 0.985f, y0 + h * 0.3f, y1 - h * 0.14f);
            Cyl(mb, S(MatSlot.Yellow), r * 0.99f, y1 - h * 0.14f, y1 - h * 0.06f);
            Frustum(mb, S(MatSlot.Grey), r * 0.985f, y1 - h * 0.06f, r * 0.9f, y1);
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Grey), r * 0.9f, y1, true, Seg);
            ColliderLathe(col, r, y0, r * 0.9f, y1);
        }

        private static void Separator(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2, gap = h * 0.04f;
            Cyl(mb, S(MatSlot.Dark), r, y0, -gap);
            Cyl(mb, S(MatSlot.Dark), r, gap, y1);
            Cyl(mb, S(MatSlot.Black), r * 0.985f, -gap, gap);
            foreach (int s in new[] { -1, 1 })
                Cyl(mb, S(MatSlot.Red), r * 1.004f, s > 0 ? gap * 2.5f : -h * 0.22f, s > 0 ? h * 0.22f : -gap * 2.5f);
            int bolts = Mathf.Max(8, Mathf.RoundToInt(r * 14));
            for (int i = 0; i < bolts; i++)
            {
                var q = Quaternion.AngleAxis(i * 360f / bolts, Vector3.up);
                foreach (int s in new[] { -1, 1 })
                    mb.OrientedBox(S(MatSlot.Metal), q * new Vector3(0, s * h * 0.36f, r), q, new Vector3(0.035f, h * 0.08f, 0.015f));
            }
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Metal), r, y1, true, Seg);
            ColliderLathe(col, r, y0, r, y1);
        }

        private static void SeparationMotor(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2, noz = h * 0.3f, zc = Back(m) + r;
            // a bracket on the hull, the casing with a red band and a rounded head, and a small bell
            mb.Box(S(MatSlot.Dark), new Vector3(0, h * 0.1f, (Back(m) + zc) / 2), new Vector3(r * 1.5f, h * 0.45f, zc - Back(m)));
            At(mb, new Vector3(0, 0, zc), () =>
            {
                float yb = y0 + noz;
                Cyl(mb, S(MatSlot.White), r, yb, y1 - 0.05f, 16);
                Cyl(mb, S(MatSlot.Red), r * 1.01f, yb + (y1 - yb) * 0.3f, yb + (y1 - yb) * 0.5f, 16);
                Frustum(mb, S(MatSlot.Grey), r, y1 - 0.05f, r * 0.6f, y1, 16);
                mb.Disc(S(MatSlot.Metal), r * 0.6f, y1, true, 16);
                var bell = BellProfile(r * 0.35f, r * 0.8f, noz, 5);
                var saved = mb.Transform;
                mb.Transform = saved * Matrix4x4.Translate(new Vector3(0, yb, 0));
                mb.Lathe(S(MatSlot.MetalDark), bell, 16, true);
                mb.Transform = saved;
            });
            col.Box(0, new Vector3(0, 0, zc - r * 0.5f), new Vector3(r * 2, h, r * 3));
        }

        private static void RadialProbe(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float w = m.diameter, h = m.height, zb = Back(m), d = 0.1f;
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0, zb + d / 2), new Vector3(w, h, d));
            mb.Box(S(MatSlot.Gold), new Vector3(0, -h * 0.08f, zb + d + 0.002f), new Vector3(w * 0.8f, h * 0.55f, 0.004f));
            mb.Box(S(MatSlot.Metal), new Vector3(0, h * 0.3f, zb + d + 0.004f), new Vector3(w * 0.5f, h * 0.1f, 0.008f));
            mb.Sphere(S(MatSlot.Red), new Vector3(w * 0.33f, h * 0.3f, zb + d + 0.012f), 0.014f, 8, 5);
            mb.CylinderBetween(S(MatSlot.Metal), new Vector3(-w * 0.35f, h / 2, zb + d / 2), new Vector3(-w * 0.35f, h / 2 + 0.2f, zb + d / 2), 0.008f, 6);
            mb.Sphere(S(MatSlot.Red), new Vector3(-w * 0.35f, h / 2 + 0.2f, zb + d / 2), 0.014f, 8, 5);
            col.Box(0, new Vector3(0, 0, zb + d / 2), new Vector3(w, h, d));
        }

        private static void LanderCan(MeshBuilder mb, MeshBuilder col, ModelDefinition m, PartDefinition def)
        {
            float rb = m.BottomD * 0.5f, rt = m.TopD * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            float ySkirt = y0 + h * 0.26f, yRoof = y1 - h * 0.18f;
            float rSkirt = Mathf.Lerp(rb, rt, 0.26f), rRoof = Mathf.Lerp(rb, rt, (yRoof - y0) / h);
            // a gold-foil skirt, a white hull and a low rounded roof with a docking collar: no heat shield, so no dark base
            Frustum(mb, S(MatSlot.Gold), rb, y0, rSkirt, ySkirt);
            Frustum(mb, S(MatSlot.White), rSkirt, ySkirt, rRoof, yRoof);
            var roof = new List<Vector2>();
            for (int i = 0; i <= 5; i++)
            {
                float a = i * Mathf.PI / 2 / 5;
                roof.Add(new Vector2(rRoof * Mathf.Cos(a) * 0.98f + 0.001f, yRoof + (y1 - yRoof) * Mathf.Sin(a)));
            }
            mb.Lathe(S(MatSlot.White), roof, Seg, true);
            Cyl(mb, S(MatSlot.Metal), rRoof * 0.45f, y1 - 0.01f, y1 + 0.02f, 16);
            mb.Disc(S(MatSlot.Metal), rRoof * 0.45f, y1 + 0.02f, true, 16);
            Cyl(mb, S(MatSlot.Dark), rb, y0, y0 + 0.03f);
            mb.Disc(S(MatSlot.Dark), rb, y0, false, Seg);
            if (def.crew != null)
            {
                // a large front window and one each side
                Vector3 hp = def.crew.HatchPosition, hn = def.crew.HatchNormal;
                for (int k = -1; k <= 1; k++)
                {
                    Quaternion ry = Quaternion.AngleAxis(k * 62f, Vector3.up);
                    Vector3 wp = ry * (hp + Vector3.up * (h * 0.18f)), wn = ry * hn;
                    Quaternion q = Quaternion.LookRotation(wn, Vector3.up);
                    float s = k == 0 ? 0.3f : 0.24f;
                    mb.OrientedBox(S(MatSlot.Grey), wp, q, new Vector3(s, s * 0.9f, 0.04f));
                    mb.OrientedBox(S(MatSlot.Glass), wp + wn * 0.021f, q, new Vector3(s * 0.8f, s * 0.7f, 0.01f));
                }
                Quaternion hq = Quaternion.LookRotation(hn, Vector3.up);
                mb.OrientedBox(S(MatSlot.Dark), hp + Vector3.down * (h * 0.12f), hq, new Vector3(0.22f, 0.3f, 0.03f));
            }
            for (int i = 0; i < 3; i++)
            {
                var q = Quaternion.AngleAxis(30 + i * 120, Vector3.up);
                mb.OrientedBox(S(MatSlot.Grey), q * new Vector3(0, ySkirt, rSkirt), q, new Vector3(0.1f, 0.08f, 0.06f));
            }
            ColliderLathe(col, rb, y0, rt, y1);
        }

        // ------------------------------------------------------------------ science instruments

        private static void Instrument(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float d = m.diameter, h = m.height, zb = Back(m);
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0, zb + 0.015f), new Vector3(d * 0.8f, h * 0.8f, 0.03f));
            switch (m.style)
            {
                case "boom": // a thermometer on a short boom that reaches out into the flow
                    mb.Box(S(MatSlot.MetalDark), new Vector3(0, 0, zb + 0.06f), new Vector3(d * 0.4f, d * 0.4f, 0.06f));
                    mb.CylinderBetween(S(MatSlot.Metal), new Vector3(0, 0, zb + 0.09f), new Vector3(0, 0, zb + 0.34f), 0.012f, 8);
                    mb.Sphere(S(MatSlot.Gold), new Vector3(0, 0, zb + 0.35f), 0.028f, 10, 6);
                    mb.CylinderBetween(S(MatSlot.Red), new Vector3(0, 0, zb + 0.15f), new Vector3(0, 0, zb + 0.18f), 0.02f, 8);
                    col.Box(0, new Vector3(0, 0, zb + 0.19f), new Vector3(d * 0.5f, d * 0.5f, 0.38f));
                    break;
                case "box": // a pressure gauge: a white case with a grille and an inlet
                    mb.Box(S(MatSlot.White), new Vector3(0, 0, zb + 0.075f), new Vector3(d * 0.8f, h * 0.7f, 0.09f));
                    for (int i = 0; i < 4; i++)
                        mb.Box(S(MatSlot.Dark), new Vector3(0, h * 0.2f - i * h * 0.07f, zb + 0.122f), new Vector3(d * 0.6f, h * 0.03f, 0.004f));
                    AlongZ(mb, new Vector3(0, -h * 0.2f, zb + 0.12f), () => Cyl(mb, S(MatSlot.Metal), 0.03f, 0, 0.07f, 12));
                    mb.Sphere(S(MatSlot.Red), new Vector3(d * 0.28f, h * 0.28f, zb + 0.124f), 0.012f, 8, 5);
                    col.Box(0, new Vector3(0, 0, zb + 0.1f), new Vector3(d * 0.8f, h * 0.8f, 0.2f));
                    break;
                case "canister": // a culture canister held by two straps, with a yellow band and a red cap
                    At(mb, new Vector3(0, 0, zb + 0.13f), () =>
                    {
                        float r = d * 0.38f, y0 = -h * 0.42f, y1 = h * 0.42f;
                        Cyl(mb, S(MatSlot.White), r, y0, y1 - 0.05f, 20);
                        Cyl(mb, S(MatSlot.Yellow), r * 1.01f, -0.03f, 0.03f, 20);
                        Cyl(mb, S(MatSlot.Red), r, y1 - 0.05f, y1, 20);
                        mb.Disc(S(MatSlot.Red), r, y1, true, 20);
                        mb.Disc(S(MatSlot.Metal), r, y0, false, 20);
                        foreach (float y in new[] { y0 + 0.07f, y1 - 0.12f }) Cyl(mb, S(MatSlot.Dark), r * 1.02f, y - 0.015f, y + 0.015f, 20);
                    });
                    col.Box(0, new Vector3(0, 0, zb + 0.13f), new Vector3(d * 0.8f, h * 0.9f, 0.26f));
                    break;
                default: // rack: a frame with three sample plates
                    foreach (int s in new[] { -1, 1 })
                        mb.Box(S(MatSlot.Metal), new Vector3(s * d * 0.38f, 0, zb + 0.07f), new Vector3(0.02f, h * 0.8f, 0.08f));
                    mb.Box(S(MatSlot.Metal), new Vector3(0, h * 0.4f, zb + 0.07f), new Vector3(d * 0.8f, 0.02f, 0.08f));
                    mb.Box(S(MatSlot.Metal), new Vector3(0, -h * 0.4f, zb + 0.07f), new Vector3(d * 0.8f, 0.02f, 0.08f));
                    var plates = new[] { MatSlot.Gold, MatSlot.Grey, MatSlot.Shield };
                    for (int i = 0; i < 3; i++)
                        mb.Box(S(plates[i]), new Vector3(0, h * 0.24f - i * h * 0.24f, zb + 0.075f), new Vector3(d * 0.66f, h * 0.16f, 0.02f));
                    col.Box(0, new Vector3(0, 0, zb + 0.07f), new Vector3(d * 0.8f, h * 0.84f, 0.14f));
                    break;
            }
            col.Box(0, new Vector3(0, 0, zb + 0.015f), new Vector3(d * 0.8f, h * 0.8f, 0.03f));
        }
    }
}
