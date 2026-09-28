using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAP.Game
{
    /// <summary>
    /// The far view (FND-03): everything beyond the flight camera's reach. A second camera, the base of the flight
    /// camera's stack, draws the sky, every body, the star with its corona, atmospheres seen from afar and points of
    /// light for bodies too small to make out; the flight camera then draws the vessels and the terrain of nearby bodies
    /// over it. The far scene is shrunk by <see cref="Scale"/> about the eye, which leaves the picture unchanged, and
    /// every position is taken relative to the camera in doubles, so nothing jitters however far away it is.
    /// </summary>
    [DefaultExecutionOrder(1000)] // after the flight camera has moved this frame
    public sealed class ScaledSpace : MonoBehaviour
    {
        /// <summary>Metres per unit of the far view.</summary>
        public const double Scale = 1e5;
        /// <summary>
        /// A body's own terrain takes over within this many radii of its centre, and hands back 10% further out. Out
        /// there its quadtree still shows only the six root chunks the far view draws, so the switch doesn't show.
        /// </summary>
        public const double LocalRange = 6;
        /// <summary>Bodies smaller than this on screen (radius in pixels) turn into points of light.</summary>
        public const float PointRadiusPx = 1.5f;

        public FlightSim Sim;
        public Camera FlightCam;
        public Camera Cam;
        public SkyController Sky;
        public PlanetManager Planets;
        /// <summary>What the far view drew this frame, per body (the star included), for labels and checks.</summary>
        public readonly List<FarBody> Bodies = new List<FarBody>();
        /// <summary>How much of the star's disc no body covers (0 to 1), for the lens flare.</summary>
        public float StarVisible = 1f;
        /// <summary>CPU time of the last update, in milliseconds.</summary>
        public double UpdateMs;

        public sealed class FarBody
        {
            public CelestialBody Body;
            public Transform Root;
            public Material Material;       // the terrain of a planet or moon; the disc of the star
            public AtmosphereShell Shell;
            public Transform Glow;          // the star's corona, or a body's point of light
            public Material GlowMaterial;
            public bool Local;              // the flight camera draws its terrain
            public bool Drawn;              // the far view draws its surface this frame
            public Vector3d Relative;       // true position relative to the camera (m)
            public double AngularRadius;    // rad
            public float PixelRadius;
            public float PointAlpha;        // 0: a resolved disc ... 1: only a point of light
            public bool Occluded;           // its centre is behind a nearer body
            public Vector3 Screen;          // screen position (z > 0 in front of the camera)
            internal int Chunks;
            internal readonly ConcurrentQueue<ChunkData> Done = new ConcurrentQueue<ChunkData>();
        }

        private Transform _root;
        private FarBody _star;
        private LensFlareComponentSRP _flare;
        private readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

        public static ScaledSpace Create(FlightSim sim, FlightCamera flight, SkyController sky, PlanetManager planets)
        {
            var go = new GameObject("ScaledSpace");
            var s = go.AddComponent<ScaledSpace>();
            s.Sim = sim;
            s.FlightCam = flight.Cam;
            s.Sky = sky;
            s.Planets = planets;
            s._root = go.transform;

            var camGo = new GameObject("FarCamera");
            camGo.transform.SetParent(go.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 1 << Layers.Scaled;
            cam.fieldOfView = flight.Cam.fieldOfView;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.depth = flight.Cam.depth - 1;
            s.Cam = cam;
            // The far camera is the base of the stack: it clears and draws first. It sets the stack's anti-aliasing;
            // post-processing (bloom, tone mapping) runs once, after the flight camera, over the whole picture.
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var flightData = flight.Cam.GetUniversalAdditionalCameraData();
            flightData.renderType = CameraRenderType.Overlay;
            flightData.renderPostProcessing = true;
            data.cameraStack.Add(flight.Cam);
            flight.Cam.cullingMask &= ~(1 << Layers.Scaled);

            if (sky.Dome != null) sky.Dome.SetParent(go.transform, false);
            foreach (var b in sim.System.Bodies)
                s.Bodies.Add(b.IsStar ? s.CreateStar(b) : s.CreateBody(b));
            s.CreateFlare();
            return s;
        }

        // ------------------------------------------------------------------ building

        private FarBody CreateBody(CelestialBody b)
        {
            var fb = new FarBody { Body = b, Local = true };
            var go = new GameObject("Far " + b.Name) { layer = Layers.Scaled };
            go.transform.SetParent(_root, false);
            go.SetActive(false);
            fb.Root = go.transform;
            fb.Material = NewMaterial("Terrain", "Far terrain " + b.Name);
            fb.Material.SetFloat("_DistanceScale", (float)Scale);
            fb.Material.SetFloat("_SkyAmbient", 0f);
            fb.Material.SetColor("_SpaceAmbient", PlanetTerrain.SpaceAmbient);
            if (b.Terrain != null)
            {
                // The six root chunks of the body's own quadtree, from the same generator.
                var terr = b.Terrain;
                double radius = b.Radius;
                bool ocean = b.HasOcean;
                for (int face = 0; face < 6; face++)
                {
                    int f = face;
                    TerrainWorkers.Enqueue(() => fb.Done.Enqueue(TerrainChunkGenerator.Generate(terr, radius, f, -1, -1, 2, 32, ocean, false)));
                }
            }
            if (b.Atmosphere != null) fb.Shell = AtmosphereShell.Create(b, _root, Layers.Scaled);
            fb.GlowMaterial = NewGlow("Point of light " + b.Name);
            fb.GlowMaterial.SetFloat("_Gauss", 1f);
            fb.Glow = Billboard("Point " + b.Name, fb.GlowMaterial);
            return fb;
        }

        private FarBody CreateStar(CelestialBody b)
        {
            var fb = new FarBody { Body = b };
            var go = new GameObject("Far " + b.Name) { layer = Layers.Scaled };
            go.transform.SetParent(_root, false);
            fb.Root = go.transform;
            var mb = new MeshBuilder();
            mb.Sphere(0, Vector3.zero, 1f, 64, 32);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMergedMesh("star");
            var mr = go.AddComponent<MeshRenderer>();
            fb.Material = NewMaterial("Star", "Star " + b.Name);
            var c = Sim.System.Def.sun.color;
            fb.Material.SetColor("_Color", new Color(c[0], c[1], c[2]));
            Quiet(mr, fb.Material);
            fb.GlowMaterial = NewGlow("Corona " + b.Name);
            fb.GlowMaterial.SetFloat("_Near", 1.6f);
            fb.GlowMaterial.SetFloat("_NearFalloff", 7f);
            fb.GlowMaterial.SetFloat("_Far", 0.35f);
            fb.GlowMaterial.SetFloat("_FarPower", 2.2f);
            fb.Glow = Billboard("Corona " + b.Name, fb.GlowMaterial);
            _star = fb;
            return fb;
        }

        private static Material NewGlow(string name) => NewMaterial("Glow", name);

        /// <summary>A copy of a material from Resources/Materials (which keeps its shader in the build).</summary>
        private static Material NewMaterial(string resource, string name)
        {
            var m = Resources.Load<Material>("Materials/" + resource);
            return new Material(m != null ? m : new Material(Shader.Find("TAP/" + resource))) { name = name };
        }

        private Transform Billboard(string name, Material mat)
        {
            var go = new GameObject(name) { layer = Layers.Scaled };
            go.transform.SetParent(_root, false);
            var mesh = new Mesh { name = "billboard" };
            mesh.SetVertices(new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2, 2, 0.01f));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Quiet(go.AddComponent<MeshRenderer>(), mat);
            go.SetActive(false);
            return go.transform;
        }

        private static void Quiet(MeshRenderer mr, Material mat)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void BuildChunks(FarBody fb)
        {
            while (fb.Done.TryDequeue(out var d))
            {
                var mesh = new Mesh { name = $"far_{fb.Body.Id}_{fb.Chunks}" };
                mesh.SetVertices(d.Positions);
                mesh.SetNormals(d.Normals);
                mesh.SetColors(d.Colors);
                mesh.SetUVs(0, d.UV0);
                mesh.SetUVs(1, d.UV1);
                mesh.SetTriangles(d.Triangles, 0, false);
                mesh.bounds = new Bounds((d.BoundsMin + d.BoundsMax) * 0.5f, d.BoundsMax - d.BoundsMin);
                var go = new GameObject("chunk " + fb.Chunks) { layer = Layers.Scaled };
                go.transform.SetParent(fb.Root, false);
                go.transform.localPosition = (Vector3)d.CenterBF; // metres; the root scales them down
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                Quiet(go.AddComponent<MeshRenderer>(), fb.Material);
                fb.Chunks++;
            }
        }

        // ------------------------------------------------------------------ every frame

        private void LateUpdate()
        {
            if (Sim == null || FlightCam == null) return;
            // The map view switches the flight camera off; the whole stack goes with it.
            if (Cam.enabled != FlightCam.enabled) Cam.enabled = FlightCam.enabled;
            if (!Cam.enabled) return;
            _watch.Restart();

            double ut = Sim.RenderUT;
            var frame = Sim.Frame;
            Vector3d camTrue = frame.RenderOrigin(Sim.RenderAlpha) + (Vector3d)FlightCam.transform.position;
            var ct = Cam.transform;
            ct.SetPositionAndRotation(Vector3.zero, FlightCam.transform.rotation);
            Cam.fieldOfView = FlightCam.fieldOfView;
            // One pixel at the centre of the screen, in radians.
            double pixelAngle = 2 * Math.Tan(Cam.fieldOfView * 0.5 * MathD.Deg2Rad) / Math.Max(1, Cam.pixelHeight);

            double nearest = double.MaxValue, farthest = 0;
            foreach (var fb in Bodies)
            {
                var b = fb.Body;
                fb.Relative = frame.BodyPosition(b, ut) - camTrue;
                double d = fb.Relative.magnitude;
                fb.AngularRadius = d > b.Radius ? Math.Asin(b.Radius / d) : Math.PI / 2;
                fb.PixelRadius = (float)(Math.Tan(fb.AngularRadius) / pixelAngle);
                Vector3 pos = (Vector3)(fb.Relative / Scale);
                fb.Screen = Cam.WorldToScreenPoint(pos);
                if (b.IsStar) PlaceStar(fb, pos, d);
                else PlaceBody(fb, pos, d, ut, pixelAngle);
                if (fb.Drawn || fb.PointAlpha > 0)
                {
                    double outer = b.Radius + Math.Max(b.Terrain != null ? b.Terrain.MaxHeight : 0, b.Atmosphere != null ? b.Atmosphere.Height : 0);
                    nearest = Math.Min(nearest, d - outer);
                    farthest = Math.Max(farthest, d + b.Radius);
                }
            }
            MarkOccluded();

            // Depth range: from half-way to the nearest surface to well beyond the farthest body (the corona reaches
            // out from the star); the reversed floating-point depth buffer keeps even 10¹² : 1 precise.
            if (farthest <= 0) { nearest = 1e3; farthest = 1e7; }
            double near = Math.Max(Math.Max(nearest * 0.5, farthest * 1e-10), 1.0) / Scale;
            double far = farthest * 3 / Scale;
            Cam.nearClipPlane = (float)near;
            Cam.farClipPlane = (float)Math.Max(far, near * 100);
            if (Sky != null && Sky.Dome != null)
            {
                // The dome draws first without depth: anywhere between the clip planes will do.
                Sky.Dome.localPosition = Vector3.zero;
                Sky.Dome.localScale = Vector3.one * (float)Math.Sqrt(Cam.nearClipPlane * (double)Cam.farClipPlane);
            }
            UpdateFlare();
            _watch.Stop();
            UpdateMs = _watch.Elapsed.TotalMilliseconds;
        }

        private void PlaceBody(FarBody fb, Vector3 pos, double d, double ut, double pixelAngle)
        {
            var b = fb.Body;
            BuildChunks(fb);
            bool local = fb.Local ? d < LocalRange * b.Radius * 1.1 : d < LocalRange * b.Radius;
            if (local != fb.Local)
            {
                fb.Local = local;
                Planets.SetLocal(b, local);
            }
            // Near a body its own terrain covers the far one, which only fills in until the terrain has loaded.
            bool terrainDone = Planets.Terrains.TryGetValue(b, out var terrain) && terrain.Complete;
            fb.Drawn = fb.Chunks > 0 && (!local || !terrainDone);
            if (fb.Root.gameObject.activeSelf != fb.Drawn) fb.Root.gameObject.SetActive(fb.Drawn);
            Vector3d sunDir = Sim.System.SunDirectionFrom(b, Vector3d.zero, ut);
            if (fb.Drawn)
            {
                fb.Root.localPosition = pos;
                fb.Root.localRotation = b.RotationAtUT(ut).ToQuaternion();
                fb.Root.localScale = Vector3.one * (float)(1 / Scale);
                fb.Material.SetVector("_BodySunDir", new Vector4((float)sunDir.x, (float)sunDir.y, (float)sunDir.z, 1));
                // Already linear, like the scene's light: SetColor would treat it as sRGB and brighten it past 1.
                fb.Material.SetVector("_BodySunColor", Sky != null ? (Vector4)Sky.SunColor : Vector4.one);
                // Seen through the air around the camera, a body is dimmed and takes on the sky's colour.
                float atm = Sky != null ? (float)Sky.AtmosphereFactor : 0f;
                fb.Material.SetColor("_HazeColor", Sky != null ? Sky.SkyColor * 0.55f : Color.black);
                fb.Material.SetFloat("_Transmittance", 1f - 0.35f * atm);
            }
            if (fb.Shell != null)
            {
                if (!local) fb.Shell.Place(pos, Scale, d, (Vector3)sunDir);
                else fb.Shell.Hide();
            }

            // A point of light where the body is too small to make out; it fades as the disc grows.
            fb.PointAlpha = local ? 0f : Mathf.Clamp01((2 * PointRadiusPx - fb.PixelRadius) / PointRadiusPx);
            bool dot = fb.PointAlpha > 0.001f;
            if (fb.Glow.gameObject.activeSelf != dot) fb.Glow.gameObject.SetActive(dot);
            if (!dot) return;
            // Just in front of the body's surface, so its own tiny mesh doesn't hide it; nearer bodies still do.
            double inFront = Math.Max(d - b.Radius, d * 0.5) * 0.999;
            Vector3 dir = (Vector3)(fb.Relative / d);
            double dotRadius = PointRadiusPx * pixelAngle;
            const float extent = 3f;
            float half = (float)(inFront / Scale * Math.Tan(dotRadius * extent));
            fb.Glow.localPosition = dir * (float)(inFront / Scale);
            fb.Glow.localRotation = Quaternion.LookRotation(dir);
            fb.Glow.localScale = new Vector3(half, half, half);
            // Lit fraction of the disc as seen from here.
            double phase = 0.5 * (1 + Vector3d.Dot(sunDir, -fb.Relative / d));
            // A pale tint of the body's colour: points of light look nearly white.
            var mc = b.Def.mapColor;
            var colour = new Color(mc[0], mc[1], mc[2]);
            colour = Color.Lerp(Color.white, colour / Mathf.Max(colour.maxColorComponent, 1e-3f), 0.35f);
            float bright = PointBrightness(b.Radius, d, pixelAngle, phase) * fb.PointAlpha * (Sky != null ? Sky.StarVisibility : 1f);
            fb.GlowMaterial.SetVector("_Color", (Vector4)(colour.linear * bright)); // brightness in linear units
            fb.GlowMaterial.SetFloat("_Radius", (float)dotRadius);
            fb.GlowMaterial.SetFloat("_Extent", extent);
        }

        private void PlaceStar(FarBody fb, Vector3 pos, double d)
        {
            var b = fb.Body;
            fb.Drawn = true;
            fb.Root.localPosition = pos;
            fb.Root.localRotation = Quaternion.identity;
            fb.Root.localScale = Vector3.one * (float)(b.Radius / Scale);
            Color tint = Sky != null ? Sky.SunTint : Color.white;
            fb.Material.SetColor("_Tint", tint);
            // The corona: a camera-facing quad through the star's centre, behind its disc, out to 16 radii (at most
            // ~70° when very close).
            double reach = Math.Min(fb.AngularRadius * 16, 1.2);
            Vector3 dir = pos.normalized;
            float half = (float)(d / Scale * Math.Tan(reach));
            fb.Glow.gameObject.SetActive(true);
            fb.Glow.localPosition = pos;
            fb.Glow.localRotation = Quaternion.LookRotation(dir);
            fb.Glow.localScale = new Vector3(half, half, half);
            fb.GlowMaterial.SetFloat("_Radius", (float)fb.AngularRadius);
            fb.GlowMaterial.SetFloat("_Extent", (float)(reach / fb.AngularRadius));
            var c = Sim.System.Def.sun.color;
            fb.GlowMaterial.SetVector("_Color", (Vector4)(new Color(c[0], c[1], c[2]) * tint).linear); // linear, as the glow expects
        }

        /// <summary>Marks bodies whose centre is behind a nearer one, and how much of the star's disc is uncovered.</summary>
        private void MarkOccluded()
        {
            StarVisible = 1f;
            foreach (var a in Bodies)
            {
                a.Occluded = false;
                double da = a.Relative.magnitude;
                foreach (var o in Bodies)
                {
                    if (o == a || o.Relative.magnitude >= da) continue;
                    double sep = Vector3d.AngleRad(a.Relative, o.Relative);
                    if (sep < o.AngularRadius) a.Occluded = true;
                    if (a == _star) StarVisible *= 1f - (float)CoveredFraction(a.AngularRadius, o.AngularRadius, sep);
                }
            }
        }

        // ------------------------------------------------------------------ lens flare

        private void CreateFlare()
        {
            if (Sky == null || Sky.Sun == null) return;
            var data = ScriptableObject.CreateInstance<LensFlareDataSRP>();
            data.name = "Sun flare";
            // edgeOffset 1 fades a shape from its centre to its rim (0 gives a hard edge); fallOff 1 is a linear fade,
            // lower values concentrate the light in the middle.
            LensFlareDataElementSRP Element(SRPLensFlareType type, float position, float size, Color tint, float intensity, float edge, float fallOff)
            {
                return new LensFlareDataElementSRP
                {
                    flareType = type,
                    position = position,
                    uniformScale = size,
                    tint = tint,
                    localIntensity = intensity,
                    edgeOffset = edge,
                    fallOff = fallOff,
                    sdfRoundness = 0.35f,
                    modulateByLightColor = true,
                    blendMode = SRPLensFlareBlendMode.Additive,
                    sideCount = 6,
                };
            }
            data.elements = new[]
            {
                Element(SRPLensFlareType.Circle, 0f, 1.5f, new Color(1f, 0.93f, 0.82f), 0.45f, 1f, 0.35f),   // glare on the sun
                Element(SRPLensFlareType.Circle, 0.45f, 0.16f, new Color(0.55f, 0.75f, 1f), 0.10f, 0.7f, 1f), // ghosts towards the centre
                Element(SRPLensFlareType.Polygon, 0.8f, 0.3f, new Color(0.6f, 1f, 0.75f), 0.05f, 0.9f, 1f),
                Element(SRPLensFlareType.Circle, 1.3f, 0.5f, new Color(1f, 0.65f, 0.45f), 0.04f, 0.8f, 1f),
                Element(SRPLensFlareType.Ring, 1.75f, 1.1f, new Color(0.6f, 0.75f, 1f), 0.03f, 0.5f, 1f),
            };
            _flare = Sky.Sun.gameObject.AddComponent<LensFlareComponentSRP>();
            _flare.lensFlareData = data;
            _flare.useOcclusion = true;      // vessels and nearby terrain, from the depth buffer
            _flare.occlusionRadius = 0.02f;
            _flare.sampleCount = 16;
            _flare.allowOffScreen = false;
            _flare.attenuationByLightShape = false;
            _flare.intensity = 1f;
        }

        private void UpdateFlare()
        {
            if (_flare == null) return;
            // Bodies in front of the star (planets far away, the local horizon as a sphere) cover it analytically: the
            // flight camera's depth buffer only holds the vessels and nearby terrain.
            float dim = Sky != null ? Mathf.Clamp01(Sky.SunTint.maxColorComponent) : 1f;
            _flare.intensity = StarVisible * dim;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// The far view's position (units, camera at the origin) of a point <paramref name="relative"/> metres from the
        /// camera.
        /// </summary>
        public static Vector3 ToFar(Vector3d relative) => (Vector3)(relative / Scale);

        /// <summary>
        /// Fraction of a disc of angular radius <paramref name="r"/> covered by one of radius <paramref name="rOther"/>
        /// whose centre lies <paramref name="sep"/> away (radians, small-angle area).
        /// </summary>
        public static double CoveredFraction(double r, double rOther, double sep)
        {
            if (r <= 0) return 0;
            if (sep >= r + rOther) return 0;
            if (sep <= rOther - r) return 1;
            if (sep <= r - rOther) return (rOther * rOther) / (r * r);
            // Area of the lens where the two circles overlap.
            double r2 = r * r, o2 = rOther * rOther, d2 = sep * sep;
            double a = r2 * Math.Acos(MathD.Clamp((d2 + r2 - o2) / (2 * sep * r), -1, 1));
            double b = o2 * Math.Acos(MathD.Clamp((d2 + o2 - r2) / (2 * sep * rOther), -1, 1));
            double c = 0.5 * Math.Sqrt(Math.Max(0, (-sep + r + rOther) * (sep + r - rOther) * (sep - r + rOther) * (sep + r + rOther)));
            return MathD.Clamp01((a + b - c) / (Math.PI * r2));
        }

        /// <summary>
        /// How bright a body's point of light is: more light for a large, near, fully lit body (on a logarithmic scale,
        /// as the eye sees it), and never so faint that it can't be found.
        /// </summary>
        public static float PointBrightness(double radius, double distance, double pixelAngle, double litFraction)
        {
            double rPx = radius / Math.Max(distance, radius) / pixelAngle;
            double light = Math.PI * rPx * rPx * 0.3 * Math.Max(litFraction, 0.05);
            // About as bright as the lit disc (~1) where the disc hands over at 1.5 px.
            return (float)MathD.Clamp(1.0 + 0.12 * Math.Log10(Math.Max(light, 1e-12)), 0.45, 1.5);
        }

        /// <summary>
        /// The largest distance on screen (pixels) between where the far view draws a body and where the flight camera
        /// sees that body's true direction, over the bodies ahead of the camera: 0 when the two cameras agree.
        /// </summary>
        public float MeasureAlignment(out string worst)
        {
            float max = 0;
            worst = "none in view";
            var ft = FlightCam.transform;
            foreach (var fb in Bodies)
            {
                Vector3 dir = (Vector3)fb.Relative.normalized;
                if (Vector3.Dot(dir, ft.forward) < 0.3f) continue;
                Vector3 seen = FlightCam.WorldToScreenPoint(ft.position + dir * 100f);
                Vector3 drawn = Cam.WorldToScreenPoint(fb.Drawn ? fb.Root.position : ToFar(fb.Relative));
                float e = Vector2.Distance(seen, drawn);
                if (e >= max) { max = e; worst = fb.Body.Name; }
            }
            return max;
        }

        private void OnDestroy()
        {
            if (_flare != null) Destroy(_flare);
        }
    }
}
