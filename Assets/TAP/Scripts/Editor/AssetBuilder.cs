using System;
using System.IO;
using TAP.Core;
using TAP.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAP.EditorTools
{
    /// <summary>
    /// Generates all project assets from code: project settings, textures, icons, materials and scenes.
    /// Menu: TAP/Build All Assets. Deterministic and idempotent.
    /// </summary>
    public static class AssetBuilder
    {
        public const string Root = "Assets/TAP";
        public const string Res = Root + "/Resources";

        [MenuItem("TAP/Build All Assets")]
        public static void BuildAll()
        {
            ConfigureProject();
            BuildTextures();
            BuildIcons();
            BuildMaterials();
            BuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssetBuilder] All assets built.");
        }

        // ------------------------------------------------------------------ project settings

        [MenuItem("TAP/Configure Project Settings")]
        public static void ConfigureProject()
        {
            // Layers
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            void SetLayer(int i, string name) { layers.GetArrayElementAtIndex(i).stringValue = name; }
            SetLayer(Layers.Parts, "Parts");
            SetLayer(Layers.Terrain, "Terrain");
            SetLayer(Layers.EVA, "EVA");
            SetLayer(Layers.Scaled, "Scaled");
            SetLayer(Layers.Map, "Map");
            SetLayer(Layers.Effects, "Effects");
            SetLayer(Layers.EditorParts, "EditorParts");
            SetLayer(Layers.Ghost, "Ghost");
            tagManager.ApplyModifiedProperties();

            Physics.gravity = Vector3.zero;
            Physics.defaultSolverIterations = 12;
            Physics.defaultSolverVelocityIterations = 4;
            Physics.defaultMaxDepenetrationVelocity = 3;
            Physics.bounceThreshold = 3;
            Physics.sleepThreshold = 0.001f;
            Physics.autoSyncTransforms = false;
            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.1f;

            PlayerSettings.companyName = "Astraea Works";
            PlayerSettings.productName = "The Astraea Program";
            PlayerSettings.bundleVersion = "0.9.0";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // URP pipeline asset tuning
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                asset.shadowDistance = 350f;
                asset.shadowCascadeCount = 4;
                asset.supportsHDR = true;
                asset.msaaSampleCount = 4;
                asset.supportsCameraDepthTexture = false;
                asset.supportsCameraOpaqueTexture = false;
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[AssetBuilder] Project configured.");
        }

        // ------------------------------------------------------------------ textures

        private static string SaveTexture(Texture2D tex, string relPath, bool sprite = false, bool linear = false, TextureWrapMode wrap = TextureWrapMode.Repeat, bool mip = true, int maxSize = 2048)
        {
            string path = $"{Res}/Textures/{relPath}.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
            }
            else imp.mipmapEnabled = mip;
            imp.sRGBTexture = !linear;
            imp.wrapMode = wrap;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            imp.anisoLevel = sprite ? 1 : 4;
            imp.SaveAndReimport();
            UnityEngine.Object.DestroyImmediate(tex);
            return path;
        }

        [MenuItem("TAP/Build Textures")]
        public static void BuildTextures()
        {
            SaveTexture(DetailNoise(256), "DetailNoise", linear: true);
            SaveTexture(StarField(4096, 2048), "Stars", wrap: TextureWrapMode.Clamp, mip: false, maxSize: 4096);
            SaveTexture(NavballTexture(1024, 512), "Navball", wrap: TextureWrapMode.Clamp, mip: false);
            SaveTexture(FlagTexture(256, 160), "Flag", wrap: TextureWrapMode.Clamp);
            var sys = CelestialSystem.LoadFromResources();
            foreach (var b in sys.Bodies)
                if (!b.IsStar) SaveTexture(PlanetMap(b, 1024, 512), "Map_" + b.Id, wrap: TextureWrapMode.Clamp);
            Debug.Log("[AssetBuilder] Textures built.");
        }

        private static Texture2D DetailNoise(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var rng = new System.Random(1234);
            int[] periods = { 4, 8, 16, 32, 64 };
            float[] amps = { 0.35f, 0.25f, 0.18f, 0.12f, 0.1f };
            var lattices = new float[periods.Length][];
            for (int o = 0; o < periods.Length; o++)
            {
                int p = periods[o];
                lattices[o] = new float[p * p];
                for (int i = 0; i < p * p; i++) lattices[o][i] = (float)rng.NextDouble();
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float v = 0;
                    for (int o = 0; o < periods.Length; o++)
                    {
                        int p = periods[o];
                        float fx = (float)x / size * p, fy = (float)y / size * p;
                        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                        float tx = fx - x0, ty = fy - y0;
                        tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                        float a = lattices[o][(y0 % p) * p + (x0 % p)];
                        float b = lattices[o][(y0 % p) * p + ((x0 + 1) % p)];
                        float c = lattices[o][((y0 + 1) % p) * p + (x0 % p)];
                        float d = lattices[o][((y0 + 1) % p) * p + ((x0 + 1) % p)];
                        v += Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * amps[o];
                    }
                    t.SetPixel(x, y, new Color(v, v, v, 1));
                }
            t.Apply();
            return t;
        }

        private static Texture2D StarField(int w, int h)
        {
            var p = new TexturePainter(w, h, new Color(0, 0, 0, 1));
            var rng = new System.Random(99);
            float ns = 2048f / w; // noise scale independent of the texture size
            // faint band
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float lon = (float)x / w * Mathf.PI * 2, lat = ((float)y / h - 0.5f) * Mathf.PI;
                    Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                    Vector3 bandN = new Vector3(0.3f, 0.85f, 0.43f).normalized;
                    float band = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, bandN) / 0.18f, 2));
                    float n = Mathf.PerlinNoise(x * 0.01f * ns, y * 0.02f * ns) * 0.6f + Mathf.PerlinNoise(x * 0.05f * ns, y * 0.05f * ns) * 0.4f;
                    float v = band * n * 0.05f;
                    p.Px[y * w + x] = new Color(v * 0.9f, v * 0.95f, v * 1.1f, 1);
                }
            // Point-like stars: at 4096 texels around the sky a texel covers a few screen pixels at most.
            for (int i = 0; i < 12000; i++)
            {
                double u = rng.NextDouble(), vv = rng.NextDouble();
                double lon = u * 2 * Math.PI;
                double lat = Math.Asin(2 * vv - 1);
                float x = (float)(lon / (2 * Math.PI) * w);
                float y = (float)((lat / Math.PI + 0.5) * h);
                float mag = (float)Math.Pow(rng.NextDouble(), 6);
                float b = 0.15f + mag * 1.6f;
                float temp = (float)rng.NextDouble();
                Color c = temp < 0.2f ? new Color(1f, 0.8f, 0.6f) : temp > 0.85f ? new Color(0.7f, 0.8f, 1f) : Color.white;
                float stretch = 1f / Mathf.Max(0.15f, Mathf.Cos((float)lat));
                float r = 0.3f + mag * 0.9f;
                p.Shape((px, py) =>
                {
                    float dx = (px - x) / stretch, dy = py - y;
                    return Mathf.Sqrt(dx * dx + dy * dy) - r;
                }, new Color(c.r * b, c.g * b, c.b * b, 1), x - r * stretch - 1, y - r - 1, x + r * stretch + 1, y + r + 1);
            }
            return p.ToTexture(false);
        }

        private static Texture2D NavballTexture(int w, int h)
        {
            // Night glass: a teal-black sky with a cyan graticule and a warm-black ground with dashed amber lines (the dashes
            // tell the halves apart without colour), white heading ticks and labels on a white horizon. Not the KSP blue/brown.
            var p = new TexturePainter(w, h, Color.black);
            Color skyTop = new Color32(0x05, 0x11, 0x16, 255), skyHor = new Color32(0x0f, 0x44, 0x50, 255);
            Color gndHor = new Color32(0x2a, 0x1a, 0x0c, 255), gndBot = new Color32(0x09, 0x06, 0x04, 255);
            for (int y = 0; y < h; y++)
            {
                float lat = ((y + 0.5f) / h - 0.5f) * 180f;
                Color c = lat >= 0 ? Color.Lerp(skyHor, skyTop, lat / 90f) : Color.Lerp(gndHor, gndBot, -lat / 90f);
                for (int x = 0; x < w; x++) p.Px[y * w + x] = c;
            }
            float k = w / 2048f; // sizes below are for a 2048 px wide texture
            float Y(float lat) => (lat / 180f + 0.5f) * h;
            float X(float hdg) => hdg / 360f * w;
            Color Cyan(float a) => new Color(0.4f, 0.91f, 0.96f, a);
            Color Amber(float a) => new Color(0.96f, 0.69f, 0.26f, a);
            void DashH(float y, float width, Color c) { for (float x = 0; x < w; x += 30 * k) p.Line(x, y, x + 18 * k, y, width, c); }
            void DashV(float x, float y0, float y1, float width, Color c) { for (float y = y0; y < y1; y += 30 * k) p.Line(x, y, x, Mathf.Min(y + 18 * k, y1), width, c); }
            void Label(string text, float hdg, float lat, float scale, Color c)
            {
                float sx = 1f / Mathf.Max(0.2f, Mathf.Cos(lat * Mathf.Deg2Rad));
                float tw = TexturePainter.TextWidth(text, scale, sx);
                foreach (float dx in new[] { 0f, w, -w }) p.Text(text, X(hdg) - tw / 2 + dx, Y(lat) - 3.5f * scale, scale, c, sx);
            }

            for (int lat = 10; lat < 90; lat += 10)
            {
                bool major = lat % 30 == 0;
                p.Line(0, Y(lat), w, Y(lat), (major ? 3 : 2) * k, Cyan(major ? 0.7f : 0.3f));
                DashH(Y(-lat), (major ? 3 : 2) * k, Amber(major ? 0.75f : 0.35f));
            }
            for (int lon = 0; lon < 360; lon += 10)
            {
                bool major = lon % 30 == 0;
                if (major)
                {
                    p.Line(X(lon), Y(2), X(lon), Y(88), 2 * k, Cyan(0.35f));
                    DashV(X(lon), Y(-88), Y(-2), 2 * k, Amber(0.3f));
                }
                p.Line(X(lon), Y(0), X(lon), Y(major ? 3.4f : 2f), 3 * k, Color.white);
                p.Line(X(lon + 5), Y(0), X(lon + 5), Y(1f), 2 * k, new Color(1, 1, 1, 0.8f));
            }
            p.RectFill(0, Y(86), w, h, Cyan(0.9f));
            p.RectFill(0, 0, w, Y(-86), Amber(0.9f));
            p.RectFill(0, Y(0) - 3.5f * k, w, Y(0) + 3.5f * k, Color.white);
            for (int lon = 0; lon < 360; lon += 30)
            {
                string s = lon == 0 ? "N" : lon == 90 ? "E" : lon == 180 ? "S" : lon == 270 ? "W" : lon.ToString("000");
                Label(s, lon, 6.2f, (lon % 90 == 0 ? 46 : 34) * k / 7f, Color.white);
            }
            for (int lat = 10; lat <= 80; lat += 10)
                foreach (int lon in new[] { 45, 135, 225, 315 })
                {
                    Label(lat.ToString(), lon, lat, 26 * k / 7f, Cyan(0.95f));
                    Label("-" + lat, lon, -lat, 26 * k / 7f, Amber(0.95f));
                }
            return p.ToTexture(false);
        }

        private static Texture2D FlagTexture(int w, int h)
        {
            var p = new TexturePainter(w, h, new Color(0.08f, 0.16f, 0.38f, 1));
            p.RectFill(0, 0, w, 14, new Color(0.95f, 0.55f, 0.15f));
            p.RectFill(0, h - 14, w, h, new Color(0.95f, 0.55f, 0.15f));
            // stylised star + orbit
            float cx = w * 0.3f, cy = h * 0.52f;
            p.Ring(cx, cy, 38, 4, new Color(1, 1, 1, 0.9f));
            for (int i = 0; i < 5; i++)
            {
                float a0 = Mathf.PI / 2 + i * 2 * Mathf.PI / 5;
                float a1 = a0 + Mathf.PI / 5;
                float a2 = a0 - Mathf.PI / 5;
                p.Triangle(cx + Mathf.Cos(a0) * 26, cy + Mathf.Sin(a0) * 26, cx + Mathf.Cos(a2) * 10, cy + Mathf.Sin(a2) * 10, cx + Mathf.Cos(a1) * 10, cy + Mathf.Sin(a1) * 10, Color.white);
            }
            p.Circle(cx, cy, 11, Color.white);
            p.Text("ASTRAEA", w * 0.52f, h * 0.55f, 1.6f, Color.white);
            p.Text("TELLUS", w * 0.58f, h * 0.36f, 1.8f, new Color(0.95f, 0.75f, 0.4f));
            return p.ToTexture(true);
        }

        private static Texture2D PlanetMap(CelestialBody b, int w, int h) => PlanetMaps.ToTexture(PlanetMaps.Surface(b.Terrain, w, h), w, h);

        [MenuItem("TAP/Build Icons")]
        public static void BuildIcons()
        {
            const int S = 64;
            Color W = Color.white;
            TexturePainter N() => new TexturePainter(S, S, new Color(1, 1, 1, 0));
            float c = S / 2f;

            var p = N(); p.Ring(c, c, 14, 4, W); p.Circle(c, c, 3.5f, W); p.Line(c, c + 16, c, c + 27, 4, W); p.Line(c - 16, c, c - 27, c, 4, W); p.Line(c + 16, c, c + 27, c, 4, W);
            SaveTexture(p.ToTexture(false), "Icons/nav_prograde", sprite: true);
            p = N(); p.Ring(c, c, 14, 4, W); p.Line(c - 10, c - 10, c + 10, c + 10, 3.5f, W); p.Line(c - 10, c + 10, c + 10, c - 10, 3.5f, W);
            p.Line(c, c + 16, c, c + 26, 4, W); p.Line(c - 12, c - 12, c - 20, c - 20, 4, W); p.Line(c + 12, c - 12, c + 20, c - 20, 4, W);
            SaveTexture(p.ToTexture(false), "Icons/nav_retrograde", sprite: true);
            p = N(); DrawTriangleOutline(p, c, c + 2, 20, W, true); p.Circle(c, c - 1, 3.5f, W);
            SaveTexture(p.ToTexture(false), "Icons/nav_normal", sprite: true);
            p = N(); DrawTriangleOutline(p, c, c - 2, 20, W, false); p.Circle(c, c + 1, 3.5f, W);
            p.Line(c, c - 12, c, c - 26, 3.5f, W);
            SaveTexture(p.ToTexture(false), "Icons/nav_antinormal", sprite: true);
            p = N(); p.Ring(c, c, 13, 4, W); p.Circle(c, c, 3.5f, W);
            for (int i = 0; i < 4; i++) { float a = Mathf.PI / 4 + i * Mathf.PI / 2; p.Line(c + Mathf.Cos(a) * 15, c + Mathf.Sin(a) * 15, c + Mathf.Cos(a) * 26, c + Mathf.Sin(a) * 26, 4, W); }
            SaveTexture(p.ToTexture(false), "Icons/nav_radialout", sprite: true);
            p = N(); p.Ring(c, c, 18, 4, W);
            for (int i = 0; i < 4; i++) { float a = Mathf.PI / 4 + i * Mathf.PI / 2; p.Line(c + Mathf.Cos(a) * 16, c + Mathf.Sin(a) * 16, c + Mathf.Cos(a) * 6, c + Mathf.Sin(a) * 6, 4, W); }
            SaveTexture(p.ToTexture(false), "Icons/nav_radialin", sprite: true);
            p = N(); p.Ring(c, c, 15, 5, W); p.Circle(c, c, 4, W);
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; p.Triangle(c + Mathf.Cos(a) * 29, c + Mathf.Sin(a) * 29, c + Mathf.Cos(a + 0.35f) * 18, c + Mathf.Sin(a + 0.35f) * 18, c + Mathf.Cos(a - 0.35f) * 18, c + Mathf.Sin(a - 0.35f) * 18, W); }
            SaveTexture(p.ToTexture(false), "Icons/nav_maneuver", sprite: true);
            p = N(); p.Ring(c, c, 15, 4, W); p.Circle(c, c, 4, W);
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; p.Line(c + Mathf.Cos(a) * 18, c + Mathf.Sin(a) * 18, c + Mathf.Cos(a) * 28, c + Mathf.Sin(a) * 28, 4, W); }
            SaveTexture(p.ToTexture(false), "Icons/nav_target", sprite: true);
            p = N(); p.Ring(c, c, 15, 4, W); p.Line(c - 9, c - 9, c + 9, c + 9, 3.5f, W); p.Line(c - 9, c + 9, c + 9, c - 9, 3.5f, W);
            for (int i = 0; i < 4; i++) { float a = Mathf.PI / 4 + i * Mathf.PI / 2; p.Line(c + Mathf.Cos(a) * 18, c + Mathf.Sin(a) * 18, c + Mathf.Cos(a) * 27, c + Mathf.Sin(a) * 27, 4, W); }
            SaveTexture(p.ToTexture(false), "Icons/nav_antitarget", sprite: true);
            // Vessel reference marker (fixed at navball centre)
            p = N(); p.Line(6, c, 22, c, 5, W); p.Line(42, c, 58, c, 5, W); p.Line(22, c, c, c - 10, 5, W); p.Line(c, c - 10, 42, c, 5, W); p.Circle(c, c, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/nav_level", sprite: true);

            // Map icons
            p = N(); p.Triangle(c, c + 22, c - 16, c, c, c - 22, W); p.Triangle(c, c + 22, c, c - 22, c + 16, c, W);
            p.Triangle(c, c + 14, c - 9, c, c, c - 14, new Color(0, 0, 0, 0.35f));
            SaveTexture(p.ToTexture(false), "Icons/map_vessel", sprite: true);
            p = N(); p.RoundRect(c - 10, c - 10, c + 10, c + 10, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/map_debris", sprite: true);
            p = N(); p.Circle(c, c + 10, 9, W); p.RoundRect(c - 11, c - 20, c + 11, c + 2, 5, W);
            SaveTexture(p.ToTexture(false), "Icons/map_eva", sprite: true);
            p = N(); p.Line(c - 12, c - 24, c - 12, c + 24, 4, W); p.Triangle(c - 11, c + 24, c - 11, c + 4, c + 20, c + 14, W);
            SaveTexture(p.ToTexture(false), "Icons/map_flag", sprite: true);
            p = N(); p.Triangle(c, c + 18, c - 16, c - 10, c + 16, c - 10, W);
            SaveTexture(p.ToTexture(false), "Icons/map_ap", sprite: true);
            p = N(); p.Triangle(c - 16, c + 10, c, c - 18, c + 16, c + 10, W);
            SaveTexture(p.ToTexture(false), "Icons/map_pe", sprite: true);
            p = N(); p.Ring(c, c, 14, 5, W); p.Circle(c, c, 5, W);
            SaveTexture(p.ToTexture(false), "Icons/map_node", sprite: true);
            p = N(); p.Ring(c, c, 18, 4, W); p.Ring(c, c, 8, 4, W);
            SaveTexture(p.ToTexture(false), "Icons/map_encounter", sprite: true);
            p = N(); p.Line(c - 14, c - 14, c + 14, c + 14, 6, W); p.Line(c - 14, c + 14, c + 14, c - 14, 6, W);
            SaveTexture(p.ToTexture(false), "Icons/map_impact", sprite: true);
            p = N(); p.Circle(c, c, 12, W);
            SaveTexture(p.ToTexture(false), "Icons/dot", sprite: true);
            p = N(); p.Ring(c, c, 20, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/ring", sprite: true);
            p = N(); p.Line(c - 20, c, c + 20, c, 5, W); p.Line(c, c - 20, c, c + 20, 5, W);
            SaveTexture(p.ToTexture(false), "Icons/plus", sprite: true);

            // Staging icons
            p = N(); p.RoundRect(c - 10, c + 6, c + 10, c + 24, 3, W); p.Triangle(c - 6, c + 6, c - 18, c - 24, c + 18, c - 24, W); p.Triangle(c - 6, c + 6, c + 18, c - 24, c + 6, c + 6, W);
            SaveTexture(p.ToTexture(false), "Icons/stage_engine", sprite: true);
            p = N(); p.RoundRect(c - 10, c - 22, c + 10, c + 26, 4, W); p.Triangle(c - 8, c - 22, c - 14, c - 30, c + 14, c - 30, W); p.Triangle(c - 8, c - 22, c + 14, c - 30, c + 8, c - 22, W);
            SaveTexture(p.ToTexture(false), "Icons/stage_srb", sprite: true);
            p = N(); p.RoundRect(c - 24, c + 4, c + 24, c + 12, 2, W); p.RoundRect(c - 24, c - 12, c + 24, c - 4, 2, W); p.Line(c - 6, c + 2, c + 6, c - 2, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/stage_decoupler", sprite: true);
            p = N(); p.Shape((x, y) => { float dx = x - c, dy = y - (c - 4); float r = Mathf.Sqrt(dx * dx + dy * dy); return (dy < 0 ? 999 : r - 24); }, W, 0, 0, S, S);
            p.Line(c - 22, c - 4, c, c - 26, 3, W); p.Line(c + 22, c - 4, c, c - 26, 3, W); p.Line(c, c - 4, c, c - 26, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/stage_chute", sprite: true);
            BuildHudIcons();

            // UI panel sprites (9-sliced)
            var pan = new TexturePainter(32, 32, new Color(1, 1, 1, 0));
            pan.RoundRect(0.5f, 0.5f, 31.5f, 31.5f, 7, Color.white);
            SaveTexture(pan.ToTexture(false), "UI/panel", sprite: true);
            SetBorder($"{Res}/Textures/UI/panel.png", 10);
            var outline = new TexturePainter(32, 32, new Color(1, 1, 1, 0));
            outline.Shape((x, y) =>
            {
                float cx = 16, cy = 16, hx = 15.5f - 7, hy = 15.5f - 7;
                float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
                float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
                float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - 7;
                return Mathf.Abs(d + 1f) - 1f;
            }, Color.white, 0, 0, 32, 32);
            SaveTexture(outline.ToTexture(false), "UI/panel_outline", sprite: true);
            SetBorder($"{Res}/Textures/UI/panel_outline.png", 10);
            var circ = new TexturePainter(64, 64, new Color(1, 1, 1, 0));
            circ.Circle(32, 32, 31, Color.white);
            SaveTexture(circ.ToTexture(false), "UI/circle", sprite: true);
            Debug.Log("[AssetBuilder] Icons built.");
        }

        /// <summary>Icons on the flight HUD's keys: SAS, RCS, legs, the SAS hold mode, map, help, menu and layout.</summary>
        private static void BuildHudIcons()
        {
            const int S = 64;
            Color W = Color.white;
            float c = S / 2f;
            TexturePainter N() => new TexturePainter(S, S, new Color(1, 1, 1, 0));
            // an ellipse outline, rotated by deg
            void Ellipse(TexturePainter q, float rx, float ry, float deg, float t)
            {
                float cs = Mathf.Cos(deg * Mathf.Deg2Rad), sn = Mathf.Sin(deg * Mathf.Deg2Rad);
                q.Shape((x, y) =>
                {
                    float dx = x - c, dy = y - c, u = dx * cs + dy * sn, v = -dx * sn + dy * cs;
                    float f = Mathf.Sqrt(u * u / (rx * rx) + v * v / (ry * ry));
                    return Mathf.Abs(f - 1f) * Mathf.Min(rx, ry) - t * 0.5f;
                }, W, 0, 0, S, S);
            }

            var p = N(); p.Ring(c, c, 9, 4, W); Ellipse(p, 25, 9, 25, 3.5f); p.Circle(c, c, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/key_sas", sprite: true);
            p = N(); p.RoundRect(c - 8, c - 8, c + 8, c + 8, 2, W);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                p.Line(c + ca * 10, c + sa * 10, c + ca * 18, c + sa * 18, 4, W);
                p.Circle(c + ca * 24, c + sa * 24, 4.5f, W);
            }
            SaveTexture(p.ToTexture(false), "Icons/key_rcs", sprite: true);
            p = N(); p.RoundRect(c - 12, c + 8, c + 12, c + 24, 3, W);
            p.Line(c - 8, c + 8, c - 20, c - 20, 4, W); p.Line(c + 8, c + 8, c + 20, c - 20, 4, W);
            p.Line(c - 27, c - 22, c - 13, c - 22, 4, W); p.Line(c + 13, c - 22, c + 27, c - 22, 4, W);
            SaveTexture(p.ToTexture(false), "Icons/key_legs", sprite: true);
            p = N(); p.Ring(c, c, 10, 4, W); p.Line(c - 26, c, c - 13, c, 4, W); p.Line(c + 13, c, c + 26, c, 4, W);
            SaveTexture(p.ToTexture(false), "Icons/sas_hold", sprite: true);
            p = N(); p.Circle(c, c, 11, W); Ellipse(p, 27, 10, 20, 3.5f);
            SaveTexture(p.ToTexture(false), "Icons/key_map", sprite: true);
            p = N(); p.Ring(c, c, 25, 4, W); p.Text("?", c - 7.5f, c - 10.5f, 3f, W);
            SaveTexture(p.ToTexture(false), "Icons/key_help", sprite: true);
            p = N(); for (int i = -1; i <= 1; i++) p.Line(c - 20, c + i * 13, c + 20, c + i * 13, 5, W);
            SaveTexture(p.ToTexture(false), "Icons/key_menu", sprite: true);
            p = N(); p.RoundRect(c - 24, c + 3, c - 3, c + 24, 3, W); p.RoundRect(c + 3, c + 12, c + 24, c + 24, 3, W);
            p.RoundRect(c + 3, c - 24, c + 24, c + 6, 3, W); p.RoundRect(c - 24, c - 24, c - 3, c - 3, 3, W);
            SaveTexture(p.ToTexture(false), "Icons/key_layout", sprite: true);
        }

        private static void DrawTriangleOutline(TexturePainter p, float cx, float cy, float r, Color c, bool up)
        {
            float s = up ? 1 : -1;
            Vector2 a = new Vector2(cx, cy + r * s), b = new Vector2(cx - r * 0.87f, cy - r * 0.5f * s), d = new Vector2(cx + r * 0.87f, cy - r * 0.5f * s);
            p.Line(a.x, a.y, b.x, b.y, 4, c);
            p.Line(b.x, b.y, d.x, d.y, 4, c);
            p.Line(d.x, d.y, a.x, a.y, 4, c);
        }

        private static void SetBorder(string path, int border)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.spriteBorder = new Vector4(border, border, border, border);
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ materials

        [MenuItem("TAP/Build Materials")]
        public static void BuildMaterials()
        {
            string dir = $"{Res}/Materials";
            Directory.CreateDirectory(dir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (TAP.Parts.MatSlot slot in Enum.GetValues(typeof(TAP.Parts.MatSlot)))
            {
                var pal = TAP.Parts.PartMaterials.Palette[slot];
                var m = LoadOrCreate($"{dir}/Part_{slot}.mat", lit);
                m.SetColor("_BaseColor", pal.color);
                m.SetFloat("_Metallic", pal.metallic);
                m.SetFloat("_Smoothness", pal.smoothness);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            var canopy = LoadOrCreate($"{dir}/Canopy.mat", lit);
            canopy.SetColor("_BaseColor", Color.white);
            canopy.SetFloat("_Smoothness", 0.2f);
            canopy.SetFloat("_Cull", 0f);
            canopy.doubleSidedGI = true;
            EditorUtility.SetDirty(canopy);

            var flag = LoadOrCreate($"{dir}/FlagCloth.mat", lit);
            flag.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/Flag.png"));
            flag.SetColor("_BaseColor", Color.white);
            flag.SetFloat("_Cull", 0f);
            flag.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(flag);

            var terrain = LoadOrCreate($"{dir}/Terrain.mat", Shader.Find("TAP/Terrain"));
            terrain.SetTexture("_DetailTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/DetailNoise.png"));
            EditorUtility.SetDirty(terrain);

            var sky = LoadOrCreate($"{dir}/Sky.mat", Shader.Find("TAP/Sky"));
            sky.SetTexture("_StarTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/Stars.png"));
            EditorUtility.SetDirty(sky);

            LoadOrCreate($"{dir}/AtmosphereShell.mat", Shader.Find("TAP/AtmosphereShell"));
            LoadOrCreate($"{dir}/Star.mat", Shader.Find("TAP/Star"));
            LoadOrCreate($"{dir}/Glow.mat", Shader.Find("TAP/Glow"));
            LoadOrCreate($"{dir}/Plume.mat", Shader.Find("TAP/Plume"));
            LoadOrCreate($"{dir}/OrbitLine.mat", Shader.Find("TAP/OrbitLine"));
            LoadOrCreate($"{dir}/Overlay.mat", Shader.Find("TAP/Overlay"));
            var nav = LoadOrCreate($"{dir}/NavballUI.mat", Shader.Find("TAP/NavballUI"));
            nav.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/Navball.png"));
            EditorUtility.SetDirty(nav);

            var soft = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/Icons/dot.png");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var add = LoadOrCreate($"{dir}/ParticleAdditive.mat", particles);
            ConfigureParticle(add, true);
            var smoke = LoadOrCreate($"{dir}/ParticleSmoke.mat", particles);
            ConfigureParticle(smoke, false);

            // Planet map materials (map view spheres)
            foreach (var b in CelestialSystem.LoadFromResources().Bodies)
            {
                if (b.IsStar) continue; // the map draws stars unlit (MapView)
                var pm = LoadOrCreate($"{dir}/Map_{b.Id}.mat", lit);
                var profile = Resources.Load<TAP.Simulation.PlanetVisualProfile>("PlanetVisuals/" + b.Id);
                bool valid=profile!=null && profile.Surface!=null && profile.TerrainHash==TerrainFingerprint.Hash(b.Def.terrain)
                    && (string.IsNullOrEmpty(profile.SurfaceBakeKey) || profile.SurfaceBakeKey==profile.ExpectedSurfaceKey(b));
                pm.SetTexture("_BaseMap", valid ? profile.Surface : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Res}/Textures/Map_{b.Id}.png"));
                pm.SetColor("_BaseColor", Color.white);
                pm.SetFloat("_Smoothness", 0.1f);
                EditorUtility.SetDirty(pm);
            }
            // Always-included shaders (used via Shader.Find at runtime)
            AddAlwaysIncluded("TAP/OrbitLine");
            AddAlwaysIncluded("TAP/Overlay");
            AddAlwaysIncluded("TAP/NavballUI");
            AddAlwaysIncluded("TAP/Plume");
            AddAlwaysIncluded("TAP/AtmosphereShell");
            AddAlwaysIncluded("TAP/Star");
            AddAlwaysIncluded("TAP/Glow");
            AddAlwaysIncluded("TAP/Terrain");
            AddAlwaysIncluded("TAP/Sky");
            AddAlwaysIncluded("Universal Render Pipeline/Particles/Unlit");
            AssetDatabase.SaveAssets();
            Debug.Log("[AssetBuilder] Materials built.");
        }

        private static void ConfigureParticle(Material m, bool additive)
        {
            m.SetTexture("_BaseMap", MakeSoftSprite());
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_BLENDMODE_ADD"); else m.DisableKeyword("_BLENDMODE_ADD");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
        }

        private static Texture2D MakeSoftSprite()
        {
            string path = $"{Res}/Textures/SoftParticle.png";
            if (!File.Exists(path))
            {
                var p = new TexturePainter(64, 64, new Color(1, 1, 1, 0));
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = (x + 0.5f) / 32 - 1, dy = (y + 0.5f) / 32 - 1;
                        float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                        p.Px[y * 64 + x] = new Color(1, 1, 1, a * a * (3 - 2 * a));
                    }
                SaveTexture(p.ToTexture(false), "SoftParticle", wrap: TextureWrapMode.Clamp);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            return m;
        }

        private static void AddAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) return;
            var gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------ scenes

        [MenuItem("TAP/Build Scenes")]
        public static void BuildScenes()
        {
            string dir = $"{Root}/Scenes";
            Directory.CreateDirectory(dir);
            CreateScene($"{dir}/MainMenu.unity", "TAP.App.MainMenuBootstrap, TAP.App");
            CreateScene($"{dir}/SpaceCenter.unity", "TAP.App.SpaceCenterBootstrap, TAP.App");
            CreateScene($"{dir}/Assembly.unity", "TAP.App.EditorBootstrap, TAP.App");
            CreateScene($"{dir}/Flight.unity", "TAP.App.FlightBootstrap, TAP.App");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{dir}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{dir}/SpaceCenter.unity", true),
                new EditorBuildSettingsScene($"{dir}/Assembly.unity", true),
                new EditorBuildSettingsScene($"{dir}/Flight.unity", true),
            };
            Debug.Log("[AssetBuilder] Scenes built.");
        }

        private static void CreateScene(string path, string bootstrapType)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            var t = Type.GetType(bootstrapType);
            if (t != null) go.AddComponent(t);
            else Debug.LogError("Bootstrap type not found: " + bootstrapType);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>($"{Res}/Materials/Sky.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
