using System.Collections.Generic;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>
    /// Builds part GameObjects (visual meshes, convex colliders and named anchor transforms)
    /// from part definitions. Meshes are generated procedurally and cached per part id.
    ///
    /// Named children created for part modules:
    ///   "Model"            visual root
    ///   "Bell"             engine nozzle (gimbals about its throat)
    ///   "Nozzle"           engine nozzle exit (child of Bell)
    ///   "LegPivot"/"Piston"/"Foot"   landing leg rig
    ///   "CanopyAnchor"     parachute attachment point
    ///   "RcsNozzle{i}"     RCS nozzle exits (forward = exhaust direction)
    ///   "Hatch"            crew hatch (forward = outward normal)
    /// </summary>
    public static class PartModelFactory
    {
        private class CachedModel
        {
            public Mesh visual;
            public int[] slots;
            public Mesh collider;
            public List<(string name, Mesh mesh, int[] slots, Vector3 pos, Quaternion rot)> subParts = new List<(string, Mesh, int[], Vector3, Quaternion)>();
        }

        private static readonly Dictionary<string, CachedModel> Cache = new Dictionary<string, CachedModel>();

        public const int Seg = 32;
        public const int ColSeg = 12;

        /// <summary>Creates a new part GameObject (not parented) with visuals and colliders.</summary>
        public static GameObject Build(PartDefinition def, int colliderLayer)
        {
            var root = new GameObject(def.id);
            BuildInto(root, def, colliderLayer);
            return root;
        }

        public static void BuildInto(GameObject root, PartDefinition def, int colliderLayer)
        {
            var cm = GetModel(def);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            AddRenderer(model, cm.visual, cm.slots);

            // Sub parts (animated pieces) and anchors.
            BuildRig(root.transform, model.transform, def, cm);

            if (cm.collider != null)
            {
                var col = root.AddComponent<MeshCollider>();
                col.sharedMesh = cm.collider;
                col.convex = true;
            }
            root.layer = colliderLayer;
        }

        public static void AddRenderer(GameObject go, Mesh mesh, int[] slots)
        {
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = PartMaterials.ForSlots(slots);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        private static CachedModel GetModel(PartDefinition def)
        {
            if (Cache.TryGetValue(def.id, out var cm)) return cm;
            cm = new CachedModel();
            var mb = new MeshBuilder();
            var col = new MeshBuilder();
            var m = def.model ?? new ModelDefinition();
            switch (m.type)
            {
                case "tank": Tank(mb, col, m); break;
                case "adapter": Adapter(mb, col, m); break;
                case "capsule": Capsule(mb, col, m, def); break;
                case "probe": Probe(mb, col, m); break;
                case "engine": Engine(mb, col, m, cm, def); break;
                case "srb": Srb(mb, col, m); break;
                case "decoupler": Decoupler(mb, col, m); break;
                case "radialdecoupler": RadialDecoupler(mb, col, m); break;
                case "leg": Leg(mb, col, m, cm, def); break;
                case "chute": Chute(mb, col, m); break;
                case "radialchute": RadialChute(mb, col, m); break;
                case "heatshield": HeatShield(mb, col, m); break;
                case "battery": Battery(mb, col, m); break;
                case "disc": Disc(mb, col, m); break;
                case "rcs": Rcs(mb, col, m, def); break;
                case "radialtank": RadialTank(mb, col, m); break;
                case "dockingport": DockingPort(mb, col, m); break;
                case "nosecone": NoseCone(mb, col, m); break;
                case "fin": Fin(mb, col, m); break;
                case "eva": Astronaut(mb, col); break;
                case "flag": Flag(mb, col, cm); break;
                default: Tank(mb, col, m); break;
            }
            cm.visual = mb.ToMesh(def.id + "_vis", out cm.slots);
            cm.collider = col.Vertices.Count > 0 ? col.ToMergedMesh(def.id + "_col") : null;
            Cache[def.id] = cm;
            return cm;
        }

        // ------------------------------------------------------------------ rigs

        private static void BuildRig(Transform root, Transform model, PartDefinition def, CachedModel cm)
        {
            foreach (var sp in cm.subParts)
            {
                // Names may be paths ("LegPivot/Piston"): parent under the already-created prefix.
                Transform parent = model;
                string leaf = sp.name;
                int slash = sp.name.LastIndexOf('/');
                if (slash > 0)
                {
                    parent = model.Find(sp.name.Substring(0, slash));
                    if (parent == null) parent = model;
                    leaf = sp.name.Substring(slash + 1);
                }
                var go = new GameObject(leaf);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = sp.pos;
                go.transform.localRotation = sp.rot;
                if (sp.mesh != null)
                {
                    if (sp.slots != null) AddRenderer(go, sp.mesh, sp.slots);
                    else
                    {
                        go.AddComponent<MeshFilter>().sharedMesh = sp.mesh;
                        var mr = go.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = PartMaterials.FlagCloth;
                    }
                }
            }

            if (def.engine != null)
            {
                var bell = model.Find("Bell");
                var parent = bell != null ? bell : model;
                var nozzle = new GameObject("Nozzle").transform;
                nozzle.SetParent(parent, false);
                Vector3 np = def.engine.NozzlePosition;
                nozzle.position = root.TransformPoint(np);
                nozzle.rotation = root.rotation * Quaternion.FromToRotation(Vector3.forward, -def.engine.ThrustDirection);
            }
            if (def.landingLeg != null)
            {
                var pivot = model.Find("LegPivot");
                if (pivot != null)
                {
                    var piston = pivot.Find("Piston");
                    if (piston != null)
                    {
                        var foot = piston.Find("Foot");
                        if (foot == null)
                        {
                            foot = new GameObject("Foot").transform;
                            foot.SetParent(piston, false);
                        }
                    }
                }
            }
            if (def.parachute != null)
            {
                var anchor = new GameObject("CanopyAnchor").transform;
                anchor.SetParent(root, false);
                float h = def.model != null ? def.model.height : def.height;
                anchor.localPosition = def.model?.type == "radialchute" ? new Vector3(0, h * 0.5f, 0.1f) : new Vector3(0, h * 0.5f, 0);
            }
            if (def.rcs?.nozzles != null)
            {
                for (int i = 0; i < def.rcs.nozzles.Length; i++)
                {
                    var n = def.rcs.nozzles[i];
                    var t = new GameObject("RcsNozzle" + i).transform;
                    t.SetParent(root, false);
                    t.localPosition = new Vector3(n[0], n[1], n[2]);
                    t.localRotation = Quaternion.LookRotation(new Vector3(n[3], n[4], n[5]));
                }
            }
            if (def.crew != null)
            {
                var hatch = new GameObject("Hatch").transform;
                hatch.SetParent(root, false);
                hatch.localPosition = def.crew.HatchPosition;
                hatch.localRotation = Quaternion.LookRotation(def.crew.HatchNormal, Vector3.up);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void Cyl(MeshBuilder mb, int slot, float r, float y0, float y1, int seg = Seg)
        {
            mb.Lathe(slot, new[] { new Vector2(r, y0), new Vector2(r, y1) }, seg, true);
        }

        private static void Frustum(MeshBuilder mb, int slot, float r0, float y0, float r1, float y1, int seg = Seg)
        {
            mb.Lathe(slot, new[] { new Vector2(r0, y0), new Vector2(r1, y1) }, seg, false);
        }

        private static void ColliderLathe(MeshBuilder col, float r0, float y0, float r1, float y1)
        {
            col.Lathe(0, new[] { new Vector2(r0, y0), new Vector2(r1, y1) }, ColSeg, false);
            col.Disc(0, r0, y0, false, ColSeg);
            col.Disc(0, r1, y1, true, ColSeg);
        }

        private static int S(MatSlot s) => (int)s;

        // ------------------------------------------------------------------ models

        private static void Tank(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            float bevel = Mathf.Min(0.04f * m.diameter, 0.06f);
            float ring = Mathf.Min(0.08f, h * 0.06f);
            int body = S(MatSlot.White), rim = S(MatSlot.Dark), cap = S(MatSlot.Metal);
            int band = -1;
            if (m.style == "orange") { body = S(MatSlot.Orange); rim = S(MatSlot.White); }
            if (m.style == "mono") { band = S(MatSlot.Yellow); }
            // bevelled ends
            Frustum(mb, cap, r - bevel, y0, r, y0 + bevel);
            Cyl(mb, rim, r, y0 + bevel, y0 + bevel + ring);
            if (band >= 0)
            {
                float mid = (y0 + y1) / 2;
                Cyl(mb, body, r, y0 + bevel + ring, mid - h * 0.12f);
                Cyl(mb, band, r * 1.002f, mid - h * 0.12f, mid + h * 0.12f);
                Cyl(mb, body, r, mid + h * 0.12f, y1 - bevel - ring);
            }
            else
            {
                Cyl(mb, body, r, y0 + bevel + ring, y1 - bevel - ring);
            }
            Cyl(mb, rim, r, y1 - bevel - ring, y1 - bevel);
            Frustum(mb, cap, r, y1 - bevel, r - bevel, y1);
            mb.Disc(cap, r - bevel, y0, false, Seg);
            mb.Disc(cap, r - bevel, y1, true, Seg);
            // panel lines on long tanks
            if (h > 3f)
            {
                int lines = Mathf.FloorToInt(h / 2.2f);
                for (int i = 1; i <= lines; i++)
                {
                    float y = y0 + h * i / (lines + 1);
                    Cyl(mb, rim, r * 1.004f, y - 0.02f, y + 0.02f);
                }
            }
            ColliderLathe(col, r, y0, r, y1);
        }

        private static void Adapter(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float rb = m.BottomD * 0.5f, rt = m.TopD * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            Cyl(mb, S(MatSlot.Dark), rb, y0, y0 + 0.08f);
            Frustum(mb, S(MatSlot.White), rb, y0 + 0.08f, rt, y1 - 0.08f);
            Cyl(mb, S(MatSlot.Dark), rt, y1 - 0.08f, y1);
            mb.Disc(S(MatSlot.Metal), rb, y0, false, Seg);
            mb.Disc(S(MatSlot.Metal), rt, y1, true, Seg);
            ColliderLathe(col, rb, y0, rt, y1);
        }

        private static void Capsule(MeshBuilder mb, MeshBuilder col, ModelDefinition m, PartDefinition def)
        {
            float rb = m.BottomD * 0.5f, rt = m.TopD * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            float collarH = h * 0.1f;
            float yBody1 = y1 - collarH;
            float band = h * 0.12f;
            // body: dark heat band at the base, light hull above
            float rBand = Mathf.Lerp(rb, rt, band / (yBody1 - y0));
            Frustum(mb, S(MatSlot.Dark), rb, y0, rBand, y0 + band);
            Frustum(mb, S(MatSlot.White), rBand, y0 + band, rt, yBody1);
            // top collar + docking ring
            Cyl(mb, S(MatSlot.Grey), rt * 0.94f, yBody1, y1 - 0.03f);
            Cyl(mb, S(MatSlot.Metal), rt * 0.8f, y1 - 0.03f, y1);
            mb.Disc(S(MatSlot.Grey), rt, yBody1, true, Seg, rt * 0.94f);
            mb.Disc(S(MatSlot.Metal), rt * 0.8f, y1, true, Seg);
            mb.Disc(S(MatSlot.Dark), rb, y0, false, Seg);
            // windows and hatch
            if (def.crew != null)
            {
                Vector3 hp = def.crew.HatchPosition;
                Vector3 hn = def.crew.HatchNormal;
                Quaternion q = Quaternion.LookRotation(hn, Vector3.up);
                float s = Mathf.Clamp(rb * 0.7f, 0.35f, 0.8f);
                mb.OrientedBox(S(MatSlot.Grey), hp, q, new Vector3(s * 0.75f, s * 0.95f, 0.04f));
                mb.OrientedBox(S(MatSlot.Dark), hp + hn * 0.021f, q, new Vector3(s * 0.55f, s * 0.75f, 0.01f));
                mb.OrientedBox(S(MatSlot.Glass), hp + hn * 0.028f + q * new Vector3(0, s * 0.2f, 0), q, new Vector3(s * 0.25f, s * 0.2f, 0.01f));
                // side windows rotated +-55 degrees around Y
                for (int side = -1; side <= 1; side += 2)
                {
                    Quaternion ry = Quaternion.AngleAxis(side * 58f, Vector3.up);
                    Vector3 wp = ry * (hp + Vector3.up * (h * 0.12f));
                    Vector3 wn = ry * hn;
                    Quaternion wq = Quaternion.LookRotation(wn, Vector3.up);
                    mb.OrientedBox(S(MatSlot.Dark), wp, wq, new Vector3(s * 0.4f, s * 0.36f, 0.03f));
                    mb.OrientedBox(S(MatSlot.Glass), wp + wn * 0.016f, wq, new Vector3(s * 0.32f, s * 0.28f, 0.01f));
                }
            }
            // RCS nubs (decorative)
            for (int i = 0; i < 4; i++)
            {
                Quaternion r = Quaternion.AngleAxis(45 + i * 90, Vector3.up);
                float yy = yBody1 - h * 0.15f;
                float rr = Mathf.Lerp(rb, rt, (yy - y0) / (yBody1 - y0));
                mb.OrientedBox(S(MatSlot.Grey), r * new Vector3(0, yy, rr), r, new Vector3(0.08f, 0.08f, 0.05f));
            }
            ColliderLathe(col, rb, y0, rt, y1);
        }

        private static void Probe(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            Cyl(mb, S(MatSlot.Dark), r, y0, y0 + h * 0.25f);
            Cyl(mb, S(MatSlot.Gold), r * 0.97f, y0 + h * 0.25f, y1 - h * 0.25f, 16);
            Cyl(mb, S(MatSlot.Dark), r, y1 - h * 0.25f, y1);
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Metal), r, y1, true, Seg);
            mb.CylinderBetween(S(MatSlot.Metal), new Vector3(r * 0.95f, 0, 0), new Vector3(r * 1.35f, 0.35f, 0), 0.015f, 6);
            mb.Sphere(S(MatSlot.Red), new Vector3(r * 1.35f, 0.35f, 0), 0.03f, 8, 5);
            ColliderLathe(col, r, y0, r, y1);
        }

        private static List<Vector2> BellProfile(float rThroat, float rExit, float length, int n = 10)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float r = rThroat + (rExit - rThroat) * Mathf.Pow(t, 0.6f);
                p.Add(new Vector2(r, -length * t));
            }
            p.Reverse(); // bottom to top
            return p;
        }

        private static void Engine(MeshBuilder mb, MeshBuilder col, ModelDefinition m, CachedModel cm, PartDefinition def)
        {
            float r = m.diameter * 0.5f, H = m.mountHeight + m.bellLength, yTop = H / 2;
            float yThroat = yTop - m.mountHeight;
            bool gold = m.style == "engine_gold";
            int mountMat = gold ? S(MatSlot.Gold) : S(MatSlot.Metal);
            // Mount: top plate, frustum housing, thrust structure
            Cyl(mb, S(MatSlot.Dark), r, yTop - 0.06f, yTop);
            mb.Disc(S(MatSlot.Dark), r, yTop, true, Seg);
            Frustum(mb, mountMat, r * 0.92f, yTop - 0.06f, r * 0.55f, yThroat + m.mountHeight * 0.1f);
            mb.Disc(S(MatSlot.MetalDark), r * 0.92f, yTop - 0.06f, false, Seg, r * 0.55f);
            Cyl(mb, S(MatSlot.MetalDark), m.bellTop * 0.55f, yThroat, yThroat + m.mountHeight * 0.12f, 16);
            // pipes
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2 + Mathf.PI / 4;
                var pa = new Vector3(Mathf.Cos(a) * r * 0.62f, yTop - 0.08f, Mathf.Sin(a) * r * 0.62f);
                var pb = new Vector3(Mathf.Cos(a) * m.bellTop * 0.55f, yThroat + 0.02f, Mathf.Sin(a) * m.bellTop * 0.55f);
                mb.CylinderBetween(S(MatSlot.MetalDark), pa, pb, Mathf.Max(0.015f, r * 0.035f), 6, false);
            }
            // Bell (separate gimballing sub-object, pivot at throat)
            var bell = new MeshBuilder();
            var outer = BellProfile(m.bellTop * 0.5f, m.bellBottom * 0.5f, m.bellLength);
            bell.Lathe(S(MatSlot.MetalDark), outer, Seg, true);
            // inner surface (reverse profile so normals face inward)
            var inner = new List<Vector2>();
            for (int i = outer.Count - 1; i >= 0; i--) inner.Add(new Vector2(outer[i].x * 0.97f, outer[i].y));
            bell.Lathe(S(MatSlot.Black), inner, Seg, true);
            // lip ring
            bell.Disc(S(MatSlot.Metal), m.bellBottom * 0.5f, -m.bellLength, false, Seg, m.bellBottom * 0.5f * 0.97f);
            var bellMesh = bell.ToMesh(def.id + "_bell", out int[] bslots);
            cm.subParts.Add(("Bell", bellMesh, bslots, new Vector3(0, yThroat, 0), Quaternion.identity));
            // collider: mount + bell cone
            ColliderLathe(col, m.bellBottom * 0.5f, -H / 2, r, yTop);
        }

        private static void Srb(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, H = m.height, y0 = -H / 2, y1 = H / 2;
            float noz = m.bellLength;
            float yb = y0 + noz; // bottom of casing
            // casing with bands
            Frustum(mb, S(MatSlot.Dark), r * 0.8f, yb, r, yb + 0.12f);
            Cyl(mb, S(MatSlot.White), r, yb + 0.12f, yb + (y1 - yb) * 0.18f);
            Cyl(mb, S(MatSlot.Red), r * 1.003f, yb + (y1 - yb) * 0.18f, yb + (y1 - yb) * 0.24f);
            Cyl(mb, S(MatSlot.White), r, yb + (y1 - yb) * 0.24f, y1 - 0.35f);
            Cyl(mb, S(MatSlot.Red), r * 1.003f, y1 - 0.35f, y1 - 0.2f);
            Frustum(mb, S(MatSlot.Grey), r, y1 - 0.2f, r * 0.9f, y1);
            mb.Disc(S(MatSlot.Metal), r * 0.9f, y1, true, Seg);
            mb.Disc(S(MatSlot.Dark), r * 0.8f, yb, false, Seg);
            // nozzle
            var outer = BellProfile(m.bellTop * 0.5f, m.bellBottom * 0.5f, noz, 6);
            var saved = mb.Transform;
            mb.Transform = Matrix4x4.Translate(new Vector3(0, yb, 0));
            mb.Lathe(S(MatSlot.MetalDark), outer, 24, true);
            var inner = new List<Vector2>();
            for (int i = outer.Count - 1; i >= 0; i--) inner.Add(new Vector2(outer[i].x * 0.95f, outer[i].y));
            mb.Lathe(S(MatSlot.Black), inner, 24, true);
            mb.Transform = saved;
            ColliderLathe(col, m.bellBottom * 0.5f, y0, r, y1);
        }

        private static void Decoupler(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            Cyl(mb, S(MatSlot.Dark), r, y0, y0 + h * 0.35f);
            Cyl(mb, S(MatSlot.Yellow), r * 1.004f, y0 + h * 0.35f, y1 - h * 0.35f);
            Cyl(mb, S(MatSlot.Dark), r, y1 - h * 0.35f, y1);
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Metal), r, y1, true, Seg);
            ColliderLathe(col, r, y0, r, y1);
        }

        private static void RadialDecoupler(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float w = m.diameter, h = m.height;
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0, 0), new Vector3(w, h, 0.2f));
            mb.Box(S(MatSlot.Yellow), new Vector3(0, 0, 0.101f), new Vector3(w * 0.7f, h * 0.25f, 0.002f));
            mb.Box(S(MatSlot.Metal), new Vector3(0, h * 0.35f, 0.06f), new Vector3(w * 0.5f, 0.06f, 0.1f));
            col.Box(0, Vector3.zero, new Vector3(w, h, 0.2f));
        }

        private static void Leg(MeshBuilder mb, MeshBuilder col, ModelDefinition m, CachedModel cm, PartDefinition def)
        {
            var leg = def.landingLeg;
            float L = leg.length;
            float w = m.diameter;
            // Mount bracket
            mb.Box(S(MatSlot.Grey), new Vector3(0, 0.1f, 0), new Vector3(w, 0.4f, 0.2f));
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0.1f, 0.101f), new Vector3(w * 0.6f, 0.3f, 0.004f));
            // Leg pivot rig (rotates between stowed and deployed); in the rig frame the strut points down (-Y).
            var strut = new MeshBuilder();
            strut.CylinderBetween(S(MatSlot.Metal), Vector3.zero, new Vector3(0, -L * 0.62f, 0), w * 0.16f, 12);
            strut.Box(S(MatSlot.Dark), new Vector3(0, -0.02f, 0), new Vector3(w * 0.5f, 0.12f, 0.12f));
            var strutMesh = strut.ToMesh(def.id + "_strut", out int[] sslots);
            Vector3 pivotPos = new Vector3(0, -0.05f, 0.1f);
            cm.subParts.Add(("LegPivot", strutMesh, sslots, pivotPos, Quaternion.identity));

            var piston = new MeshBuilder();
            piston.CylinderBetween(S(MatSlot.MetalDark), new Vector3(0, -L * 0.55f, 0), new Vector3(0, -L, 0), w * 0.1f, 10);
            // foot pad
            piston.Transform = Matrix4x4.Translate(new Vector3(0, -L, 0));
            piston.Lathe(S(MatSlot.Grey), new[] { new Vector2(leg.footRadius, -0.03f), new Vector2(leg.footRadius * 0.9f, 0.03f) }, 14, false);
            piston.Disc(S(MatSlot.Dark), leg.footRadius, -0.03f, false, 14);
            piston.Disc(S(MatSlot.Grey), leg.footRadius * 0.9f, 0.03f, true, 14);
            var pistonMesh = piston.ToMesh(def.id + "_piston", out int[] pslots);
            cm.subParts.Add(("LegPivot/Piston", pistonMesh, pslots, Vector3.zero, Quaternion.identity));
            // collider covers the bracket only; ground contact handled by the suspension raycast
            col.Box(0, new Vector3(0, 0.1f, 0), new Vector3(w, 0.4f, 0.2f));
        }

        private static void Chute(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            int cap = m.style == "drogue" ? S(MatSlot.Red) : S(MatSlot.Orange);
            Cyl(mb, S(MatSlot.Grey), r, y0, y0 + h * 0.5f);
            var prof = new List<Vector2>();
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                prof.Add(new Vector2(r * Mathf.Cos(t * Mathf.PI * 0.5f) + 0.001f, y0 + h * 0.5f + h * 0.5f * Mathf.Sin(t * Mathf.PI * 0.5f)));
            }
            mb.Lathe(cap, prof, Seg, true);
            mb.Disc(S(MatSlot.Dark), r, y0, false, Seg);
            ColliderLathe(col, r, y0, r * 0.6f, y1);
        }

        private static void RadialChute(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float w = m.diameter, h = m.height;
            mb.Box(S(MatSlot.Grey), new Vector3(0, 0, 0.02f), new Vector3(w * 0.9f, h, 0.2f));
            mb.Box(S(MatSlot.Orange), new Vector3(0, h * 0.3f, 0.123f), new Vector3(w * 0.8f, h * 0.35f, 0.01f));
            col.Box(0, new Vector3(0, 0, 0.02f), new Vector3(w * 0.9f, h, 0.2f));
        }

        private static void HeatShield(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            float rimY = y0 + h * 0.35f;
            // convex ablative bottom
            var prof = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                float rr = r * t;
                float yy = y0 + (rimY - y0) * (t * t);
                prof.Add(new Vector2(rr + 0.001f, yy));
            }
            mb.Lathe(S(MatSlot.Shield), prof, Seg, true);
            Cyl(mb, S(MatSlot.Shield), r, rimY, y1 - h * 0.25f);
            Frustum(mb, S(MatSlot.MetalDark), r, y1 - h * 0.25f, r * 0.93f, y1);
            mb.Disc(S(MatSlot.Grey), r * 0.93f, y1, true, Seg);
            ColliderLathe(col, r * 0.9f, y0, r, y1);
        }

        private static void Battery(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float w = m.diameter, h = m.height;
            mb.Box(S(MatSlot.Dark), Vector3.zero, new Vector3(w, h, 0.12f));
            mb.Box(S(MatSlot.Yellow), new Vector3(0, h * 0.25f, 0.061f), new Vector3(w * 0.8f, h * 0.12f, 0.002f));
            col.Box(0, Vector3.zero, new Vector3(w, h, 0.12f));
        }

        private static void Disc(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            if (m.style == "wheel")
            {
                Cyl(mb, S(MatSlot.Metal), r, y0, y0 + h * 0.3f);
                Cyl(mb, S(MatSlot.Dark), r * 0.96f, y0 + h * 0.3f, y1 - h * 0.3f);
                Cyl(mb, S(MatSlot.Metal), r, y1 - h * 0.3f, y1);
            }
            else
            {
                Cyl(mb, S(MatSlot.Dark), r, y0, y1);
                Cyl(mb, S(MatSlot.Yellow), r * 1.004f, -h * 0.12f, h * 0.12f);
            }
            mb.Disc(S(MatSlot.Grey), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Grey), r, y1, true, Seg);
            ColliderLathe(col, r, y0, r, y1);
        }

        private static void Rcs(MeshBuilder mb, MeshBuilder col, ModelDefinition m, PartDefinition def)
        {
            mb.Box(S(MatSlot.Grey), new Vector3(0, 0, -0.03f), new Vector3(0.26f, 0.26f, 0.06f));
            mb.Box(S(MatSlot.White), new Vector3(0, 0, 0.06f), new Vector3(0.16f, 0.16f, 0.12f));
            if (def.rcs?.nozzles != null)
                foreach (var n in def.rcs.nozzles)
                {
                    var p = new Vector3(n[0], n[1], n[2]);
                    var d = new Vector3(n[3], n[4], n[5]).normalized;
                    mb.CylinderBetween(S(MatSlot.MetalDark), p - d * 0.1f, p, 0.02f, 8, true, 0.045f);
                }
            col.Box(0, new Vector3(0, 0, 0.03f), new Vector3(0.3f, 0.3f, 0.18f));
        }

        private static void RadialTank(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height;
            float body = h - 2 * r;
            var saved = mb.Transform;
            var prof = new List<Vector2>();
            for (int i = 0; i <= 6; i++) { float t = -Mathf.PI / 2 + i * Mathf.PI / 12; prof.Add(new Vector2(r * Mathf.Cos(t) + 1e-4f, -body / 2 + r * Mathf.Sin(t))); }
            prof.Add(new Vector2(r, body / 2));
            for (int i = 1; i <= 6; i++) { float t = i * Mathf.PI / 12; prof.Add(new Vector2(r * Mathf.Cos(t) + 1e-4f, body / 2 + r * Mathf.Sin(t))); }
            mb.Lathe(S(MatSlot.White), prof, 20, true);
            Cyl(mb, S(MatSlot.Yellow), r * 1.01f, -body * 0.15f, body * 0.15f, 20);
            mb.Transform = saved;
            col.Box(0, Vector3.zero, new Vector3(2 * r, h, 2 * r));
        }

        private static void DockingPort(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2, y1 = h / 2;
            Cyl(mb, S(MatSlot.Dark), r, y0, y0 + h * 0.4f);
            Frustum(mb, S(MatSlot.Grey), r, y0 + h * 0.4f, r * 0.72f, y1 - h * 0.25f);
            Cyl(mb, S(MatSlot.Metal), r * 0.72f, y1 - h * 0.25f, y1);
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            mb.Disc(S(MatSlot.Dark), r * 0.72f, y1, true, Seg, r * 0.5f);
            mb.Disc(S(MatSlot.Black), r * 0.5f, y1 - 0.01f, true, Seg);
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Quaternion.AngleAxis(i * 120, Vector3.up);
                mb.OrientedBox(S(MatSlot.Yellow), q * new Vector3(0, y1 + 0.04f, r * 0.6f), q, new Vector3(0.12f, 0.08f, 0.04f));
            }
            ColliderLathe(col, r, y0, r * 0.72f, y1);
        }

        private static void NoseCone(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float r = m.diameter * 0.5f, h = m.height, y0 = -h / 2;
            var prof = new List<Vector2>();
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                prof.Add(new Vector2(r * Mathf.Pow(1 - t * t, 0.62f) + 0.001f, y0 + h * t));
            }
            var body = prof.GetRange(0, n - 2);
            var tip = prof.GetRange(n - 3, 4);
            mb.Lathe(S(MatSlot.White), body, Seg, true);
            mb.Lathe(S(MatSlot.Dark), tip, Seg, true);
            mb.Disc(S(MatSlot.Metal), r, y0, false, Seg);
            // collider: stepped frustums approximating the ogive
            col.Lathe(0, new[] { new Vector2(r, y0), new Vector2(r * 0.75f, y0 + h * 0.5f), new Vector2(0.02f, y0 + h) }, ColSeg, false);
            col.Disc(0, r, y0, false, ColSeg);
        }

        private static void Fin(MeshBuilder mb, MeshBuilder col, ModelDefinition m)
        {
            float span = m.diameter, chord = m.height;
            var poly = new List<Vector2>
            {
                new Vector2(0, -chord / 2), new Vector2(span, -chord / 2), new Vector2(span, -chord * 0.1f), new Vector2(0, chord / 2),
            };
            var saved = mb.Transform;
            mb.Transform = Matrix4x4.Rotate(Quaternion.AngleAxis(-90, Vector3.up));
            mb.ExtrudePolygon(S(MatSlot.White), poly, 0.04f, S(MatSlot.Dark));
            mb.Transform = saved;
            col.Transform = Matrix4x4.Rotate(Quaternion.AngleAxis(-90, Vector3.up));
            col.ExtrudePolygon(0, poly, 0.05f);
            col.Transform = Matrix4x4.identity;
        }

        private static void Astronaut(MeshBuilder mb, MeshBuilder col)
        {
            // Origin at the centre of the collider capsule (height 1.3). Feet at y = -0.65.
            // Legs
            for (int s = -1; s <= 1; s += 2)
            {
                mb.CylinderBetween(S(MatSlot.Suit), new Vector3(0.1f * s, -0.62f, 0), new Vector3(0.1f * s, -0.15f, 0), 0.075f, 10);
                mb.Box(S(MatSlot.Dark), new Vector3(0.1f * s, -0.61f, 0.03f), new Vector3(0.13f, 0.08f, 0.22f));
            }
            // Torso
            mb.CylinderBetween(S(MatSlot.Suit), new Vector3(0, -0.2f, 0), new Vector3(0, 0.3f, 0), 0.19f, 14, true, 0.2f);
            mb.Box(S(MatSlot.Orange), new Vector3(0, 0.05f, 0.18f), new Vector3(0.22f, 0.16f, 0.04f));
            // Arms
            for (int s = -1; s <= 1; s += 2)
            {
                mb.CylinderBetween(S(MatSlot.Suit), new Vector3(0.23f * s, 0.26f, 0), new Vector3(0.28f * s, -0.12f, 0.05f), 0.06f, 8);
                mb.Sphere(S(MatSlot.Dark), new Vector3(0.28f * s, -0.15f, 0.05f), 0.055f, 8, 5);
            }
            // Backpack
            mb.Box(S(MatSlot.Grey), new Vector3(0, 0.08f, -0.24f), new Vector3(0.34f, 0.42f, 0.16f));
            mb.Box(S(MatSlot.Dark), new Vector3(0, 0.2f, -0.325f), new Vector3(0.24f, 0.1f, 0.02f));
            // Helmet
            mb.Sphere(S(MatSlot.Suit), new Vector3(0, 0.48f, 0), 0.2f, 18, 12);
            mb.Transform = Matrix4x4.TRS(new Vector3(0, 0.49f, 0.06f), Quaternion.identity, new Vector3(1f, 0.8f, 1f));
            mb.Sphere(S(MatSlot.Visor), Vector3.zero, 0.165f, 18, 12);
            mb.Transform = Matrix4x4.identity;
            col.Box(0, Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)); // replaced by a CapsuleCollider at runtime
        }

        private static void Flag(MeshBuilder mb, MeshBuilder col, CachedModel cm)
        {
            // Pole from ground (y=0) to 2.1 m, cloth added as a separate sub-object.
            mb.CylinderBetween(S(MatSlot.Metal), new Vector3(0, -0.3f, 0), new Vector3(0, 2.1f, 0), 0.025f, 8);
            mb.CylinderBetween(S(MatSlot.Metal), new Vector3(0, 2.05f, 0), new Vector3(0.9f, 2.05f, 0), 0.015f, 6);
            mb.Sphere(S(MatSlot.Gold), new Vector3(0, 2.12f, 0), 0.04f, 8, 5);
            var cloth = new MeshBuilder();
            int a = cloth.AddVertex(new Vector3(0.02f, 1.45f, 0), Vector3.back, new Vector2(0, 0));
            int b = cloth.AddVertex(new Vector3(0.9f, 1.45f, 0), Vector3.back, new Vector2(1, 0));
            int c = cloth.AddVertex(new Vector3(0.9f, 2.03f, 0), Vector3.back, new Vector2(1, 1));
            int d = cloth.AddVertex(new Vector3(0.02f, 2.03f, 0), Vector3.back, new Vector2(0, 1));
            cloth.AddQuad(0, a, d, c, b);
            int a2 = cloth.AddVertex(new Vector3(0.02f, 1.45f, 0), Vector3.forward, new Vector2(1, 0));
            int b2 = cloth.AddVertex(new Vector3(0.9f, 1.45f, 0), Vector3.forward, new Vector2(0, 0));
            int c2 = cloth.AddVertex(new Vector3(0.9f, 2.03f, 0), Vector3.forward, new Vector2(0, 1));
            int d2 = cloth.AddVertex(new Vector3(0.02f, 2.03f, 0), Vector3.forward, new Vector2(1, 1));
            cloth.AddQuad(0, a2, b2, c2, d2);
            var clothMesh = cloth.ToMesh("flag_cloth", out _);
            cm.subParts.Add(("Cloth", clothMesh, null, Vector3.zero, Quaternion.identity));
            col.Box(0, new Vector3(0, 0.9f, 0), new Vector3(0.1f, 1.8f, 0.1f));
        }
    }
}
