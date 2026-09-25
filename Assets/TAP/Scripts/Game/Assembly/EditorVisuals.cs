using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;
using UnityEngine.Rendering;

namespace TAP.Game
{
    /// <summary>Materials, the assembly building (hangar) and the CoM / CoT / CoP markers.</summary>
    public static class EditorVisuals
    {
        private static Material _overlayTemplate;

        /// <summary>Translucent unlit marker material; alwaysOnTop draws through parts.</summary>
        public static Material OverlayMaterial(Color c, bool alwaysOnTop)
        {
            if (_overlayTemplate == null)
            {
                _overlayTemplate = Resources.Load<Material>("Materials/Overlay");
                if (_overlayTemplate == null)
                {
                    var sh = Shader.Find("TAP/Overlay");
                    _overlayTemplate = sh != null ? new Material(sh) : PartMaterials.CreateLit("OverlayFallback", Color.white, 0, 0, false);
                }
            }
            var m = new Material(_overlayTemplate) { name = "Overlay" };
            m.SetColor("_Color", c);
            m.SetFloat("_ZTest", alwaysOnTop ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            m.renderQueue = alwaysOnTop ? 3200 : 3100;
            return m;
        }

        // ------------------------------------------------------------------ hangar

        /// <summary>Builds the assembly building: floor, launch-prep platform, walls with steel frames, lights.</summary>
        public static GameObject BuildHangar()
        {
            var root = new GameObject("AssemblyBuilding");
            const float W = 80f, D = 80f, Hgt = 64f;

            // Floor (tiled concrete with painted grid)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(floor.GetComponent<Collider>());
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localRotation = Quaternion.Euler(90, 0, 0);
            floor.transform.localScale = new Vector3(W, D, 1);
            var floorMat = PartMaterials.CreateLit("HangarFloor", Color.white, 0f, 0.35f, false);
            floorMat.SetTexture("_BaseMap", FloorTexture());
            floorMat.SetTextureScale("_BaseMap", new Vector2(W / 8f, D / 8f));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Build platform + service tower (shadow casters) and the shell (walls, roof: no shadows,
            // otherwise the roof would shade the whole floor from the key light).
            var mb = new MeshBuilder();
            var shell = new MeshBuilder();
            int concrete = (int)MatSlot.Concrete, dark = (int)MatSlot.Dark, yellow = (int)MatSlot.Yellow, grey = (int)MatSlot.Grey,
                metal = (int)MatSlot.MetalDark, white = (int)MatSlot.White, blue = (int)MatSlot.Blue;
            // platform: raised disc with a hazard ring
            mb.Lathe(grey, new[] { new Vector2(7.2f, 0f), new Vector2(7.2f, 0.18f) }, 64);
            mb.Disc(concrete, 7.2f, 0.18f, true, 64, 6.6f);
            mb.Disc(dark, 6.6f, 0.181f, true, 64);
            for (int k = 0; k < 24; k++)
            {
                if (k % 2 == 1) continue;
                float a0 = k * 15f;
                var q = Quaternion.AngleAxis(a0 + 7.5f, Vector3.up);
                mb.OrientedBox(yellow, q * new Vector3(0, 0.185f, 6.9f), q, new Vector3(1.6f, 0.012f, 0.5f));
            }
            // walls (inner faces visible from the inside)
            float t = 0.6f;
            shell.Box(white, new Vector3(0, Hgt / 2, -D / 2 - t / 2), new Vector3(W, Hgt, t));
            shell.Box(white, new Vector3(0, Hgt / 2, D / 2 + t / 2), new Vector3(W, Hgt, t));
            shell.Box(white, new Vector3(-W / 2 - t / 2, Hgt / 2, 0), new Vector3(t, Hgt, D));
            shell.Box(white, new Vector3(W / 2 + t / 2, Hgt / 2, 0), new Vector3(t, Hgt, D));
            shell.Box(grey, new Vector3(0, Hgt + t / 2, 0), new Vector3(W, t, D));
            // wainscot band and floor trim
            foreach (var (c, s) in new[]
                     {
                         (new Vector3(0, 1.5f, -D / 2 + 0.05f), new Vector3(W, 3f, 0.1f)),
                         (new Vector3(0, 1.5f, D / 2 - 0.05f), new Vector3(W, 3f, 0.1f)),
                         (new Vector3(-W / 2 + 0.05f, 1.5f, 0), new Vector3(0.1f, 3f, D)),
                         (new Vector3(W / 2 - 0.05f, 1.5f, 0), new Vector3(0.1f, 3f, D)),
                     })
                shell.Box(blue, c, s);
            // steel columns and girders on every wall
            for (int side = 0; side < 4; side++)
            {
                Quaternion q = Quaternion.AngleAxis(side * 90f, Vector3.up);
                for (float x = -W / 2 + 10; x <= W / 2 - 10 + 0.01f; x += 10f)
                {
                    shell.OrientedBox(metal, q * new Vector3(x, Hgt / 2, -D / 2 + 0.5f), q, new Vector3(0.9f, Hgt, 0.9f));
                }
                for (float y = 16; y < Hgt; y += 16)
                    shell.OrientedBox(metal, q * new Vector3(0, y, -D / 2 + 0.5f), q, new Vector3(W, 0.7f, 0.8f));
            }
            // roll-out door on the east wall (+X): frame and hazard stripes
            {
                Quaternion q = Quaternion.AngleAxis(-90f, Vector3.up);
                Vector3 c = new Vector3(W / 2 - 0.2f, 0, 0);
                shell.OrientedBox(grey, c + new Vector3(0, 22f, 0), q, new Vector3(24f, 44f, 0.15f));
                shell.OrientedBox(yellow, c + new Vector3(-0.1f, 44.5f, 0), q, new Vector3(25f, 1f, 0.2f));
                shell.OrientedBox(yellow, c + new Vector3(-0.1f, 22f, 12.5f), q, new Vector3(1f, 45f, 0.2f));
                shell.OrientedBox(yellow, c + new Vector3(-0.1f, 22f, -12.5f), q, new Vector3(1f, 45f, 0.2f));
                for (float y = 4; y < 44; y += 8)
                    shell.OrientedBox(dark, c + new Vector3(-0.12f, y, 0), q, new Vector3(23.5f, 0.12f, 0.1f));
            }
            // service tower beside the platform
            Vector3 tp = new Vector3(-9.5f, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 1.2f, z = (i < 2 ? -1 : 1) * 1.2f;
                mb.CylinderBetween(metal, tp + new Vector3(x, 0, z), tp + new Vector3(x, 36f, z), 0.12f, 8);
            }
            for (float y = 2; y < 36; y += 3)
            {
                mb.Box(yellow, tp + new Vector3(0, y, -1.2f), new Vector3(2.5f, 0.12f, 0.12f));
                mb.Box(yellow, tp + new Vector3(0, y, 1.2f), new Vector3(2.5f, 0.12f, 0.12f));
                mb.Box(yellow, tp + new Vector3(-1.2f, y, 0), new Vector3(0.12f, 0.12f, 2.5f));
                mb.Box(yellow, tp + new Vector3(1.2f, y, 0), new Vector3(0.12f, 0.12f, 2.5f));
            }
            var props = new GameObject("Platform");
            props.transform.SetParent(root.transform, false);
            PartModelFactory.AddRenderer(props, mb.ToMesh("HangarProps", out int[] propSlots), propSlots);
            var walls = new GameObject("Shell");
            walls.transform.SetParent(root.transform, false);
            PartModelFactory.AddRenderer(walls, shell.ToMesh("HangarShell", out int[] shellSlots), shellSlots);
            walls.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Lighting: soft sky-like ambient, a key light from high above the door, fill lights.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.62f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.37f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.21f);
            RenderSettings.fog = false;
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(root.transform, false);
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.9f);
            key.intensity = 1.25f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.7f;
            key.transform.rotation = Quaternion.Euler(58f, -35f, 0f);
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(root.transform, false);
            fill.type = LightType.Directional;
            fill.color = new Color(0.7f, 0.8f, 1f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var lamp = new GameObject("Lamp" + i).AddComponent<Light>();
                lamp.transform.SetParent(root.transform, false);
                lamp.type = LightType.Point;
                lamp.range = 45f;
                lamp.intensity = 2.5f;
                lamp.color = new Color(1f, 0.93f, 0.82f);
                lamp.transform.position = new Vector3(i < 2 ? -18 : 18, 40f, i % 2 == 0 ? -18 : 18);
            }
            return root;
        }

        private static Texture2D FloorTexture()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "HangarFloor", wrapMode = TextureWrapMode.Repeat };
            var px = new Color[N * N];
            var rng = new System.Random(7);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float n = (float)rng.NextDouble() * 0.035f;
                    Color c = new Color(0.44f + n, 0.45f + n, 0.47f + n);
                    bool seam = x < 2 || y < 2;
                    bool center = (x > N / 2 - 1 && x < N / 2 + 1) || (y > N / 2 - 1 && y < N / 2 + 1);
                    if (seam) c = new Color(0.3f, 0.31f, 0.33f);
                    else if (center) c = new Color(0.38f, 0.39f, 0.41f);
                    px[y * N + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            tex.anisoLevel = 8;
            return tex;
        }

        // ------------------------------------------------------------------ markers

        /// <summary>CoM (yellow ball), CoT (purple ball + exhaust arrow), CoP (cyan ball + restoring-force arrow).</summary>
        public sealed class CraftMarkers
        {
            private readonly GameObject _root;
            private readonly Transform _com, _cot, _cop, _cotArrow, _copArrow;

            public CraftMarkers()
            {
                _root = new GameObject("CraftMarkers");
                _com = Ball("CoM", new Color(1f, 0.85f, 0.15f, 0.9f), 0.55f);
                _cot = Ball("CoT", new Color(0.85f, 0.35f, 1f, 0.9f), 0.45f);
                _cop = Ball("CoP", new Color(0.25f, 0.85f, 1f, 0.9f), 0.45f);
                _cotArrow = Arrow("CoT arrow", new Color(0.85f, 0.35f, 1f, 0.85f));
                _copArrow = Arrow("CoP arrow", new Color(0.25f, 0.85f, 1f, 0.85f));
            }

            private Transform Ball(string name, Color c, float size)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(s.GetComponent<Collider>());
                s.name = name;
                s.layer = Layers.Ghost;
                s.transform.SetParent(_root.transform, false);
                s.transform.localScale = Vector3.one * size;
                var r = s.GetComponent<Renderer>();
                r.sharedMaterial = OverlayMaterial(c, true);
                r.shadowCastingMode = ShadowCastingMode.Off;
                return s.transform;
            }

            private Transform Arrow(string name, Color c)
            {
                var mb = new MeshBuilder();
                mb.CylinderBetween(0, Vector3.zero, new Vector3(0, 1.4f, 0), 0.06f, 10);
                mb.Lathe(0, new[] { new Vector2(0.18f, 1.4f), new Vector2(0.0f, 1.9f) }, 12);
                mb.Disc(0, 0.18f, 1.4f, false, 12);
                var go = new GameObject(name);
                go.layer = Layers.Ghost;
                go.transform.SetParent(_root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mb.ToMergedMesh(name);
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = OverlayMaterial(c, true);
                r.shadowCastingMode = ShadowCastingMode.Off;
                return go.transform;
            }

            public void Update(DesignStats s, Transform craftRoot, bool show)
            {
                bool any = show && s != null && s.PartCount > 0;
                _root.SetActive(any);
                if (!any) return;
                Vector3 o = craftRoot.position;
                _com.position = o + s.CoM;
                _cot.gameObject.SetActive(s.HasThrust);
                _cotArrow.gameObject.SetActive(s.HasThrust);
                if (s.HasThrust)
                {
                    _cot.position = o + s.CoT;
                    _cotArrow.position = o + s.CoT;
                    _cotArrow.rotation = Quaternion.FromToRotation(Vector3.up, -s.ThrustDir);
                }
                _cop.gameObject.SetActive(s.HasCoP);
                _copArrow.gameObject.SetActive(s.HasCoP);
                if (s.HasCoP)
                {
                    _cop.position = o + s.CoP;
                    _copArrow.position = o + s.CoP;
                    _copArrow.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.left);
                }
            }
        }
    }
}
