using System;
using TAP.Core;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAP.Game
{
    /// <summary>
    /// Drives the sun light, procedural sky, fog and ambient lighting from the camera's position:
    /// blue sky fading to black space with altitude, sunset tints, eclipse by planets, and haze.
    /// </summary>
    public sealed class SkyController : MonoBehaviour
    {
        public FlightSim Sim;
        public Camera Cam;
        public Light Sun;
        public Material SkyMaterial;
        public float SunIntensity = 1.35f;

        public double CameraAltitude;
        public double AtmosphereFactor;
        public bool InShadow;
        private UniversalAdditionalLightData _sunData;
        private Transform _dome;

        public static SkyController Create(FlightSim sim, Camera cam)
        {
            var go = new GameObject("Sky & Sun");
            var sc = go.AddComponent<SkyController>();
            sc.Sim = sim;
            sc.Cam = cam;
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(go.transform, false);
            sc.Sun = sunGo.AddComponent<Light>();
            sc.Sun.type = LightType.Directional;
            sc.Sun.shadows = LightShadows.Soft;
            sc.Sun.shadowStrength = 0.9f;
            sc.Sun.shadowBias = 0.05f;
            sc.Sun.shadowNormalBias = 0.3f;
            var sd = sim.System.Def.sun;
            sc.Sun.color = new Color(sd.color[0], sd.color[1], sd.color[2]);
            sc.SunIntensity = sd.intensity;
            var mat = Resources.Load<Material>("Materials/Sky");
            sc.SkyMaterial = mat != null ? new Material(mat) : null;
            if (sc.SkyMaterial != null)
            {
                RenderSettings.skybox = sc.SkyMaterial; // environment reference; the flight view draws the dome below
                // The sky is drawn first, as a dome around the camera (background queue, no depth test), not by the
                // skybox pass after the opaque geometry: with the depth range a camera next to a small vessel needs,
                // terrain several hundred kilometres away lands on the far-plane depth and that pass painted the sky
                // over it (the planet vanished above ~500 km).
                sc.SkyMaterial.SetFloat("_ZTest", (float)CompareFunction.Always);
                sc.SkyMaterial.renderQueue = (int)RenderQueue.Background;
                var dome = new GameObject("SkyDome");
                dome.transform.SetParent(go.transform, false);
                var mb = new TAP.Parts.MeshBuilder();
                mb.Sphere(0, Vector3.zero, 1f, 48, 24);
                dome.AddComponent<MeshFilter>().sharedMesh = mb.ToMergedMesh("sky_dome");
                var mr = dome.AddComponent<MeshRenderer>();
                mr.sharedMaterial = sc.SkyMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                dome.transform.localScale = Vector3.one * 1000f;
                sc._dome = dome.transform;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                RenderPipelineManager.beginCameraRendering += sc.OnBeginCamera;
            }
            RenderSettings.sun = sc.Sun;
            sc._sunData = sc.Sun.GetUniversalAdditionalLightData();
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.fogMode = FogMode.Exponential;
            return sc;
        }

        private void OnBeginCamera(ScriptableRenderContext ctx, Camera c)
        {
            if (c == Cam && _dome != null) _dome.position = c.transform.position;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        }

        private void LateUpdate()
        {
            if (Sim == null || Cam == null) return;
            double rut = Sim.RenderUT;
            Vector3d sunDir = Sim.System.SunDirection;
            Sun.transform.rotation = Quaternion.LookRotation(-(Vector3)sunDir);

            // Camera position relative to the frame body (true).
            var frame = Sim.Frame;
            Vector3d camTrue = frame.RenderOrigin(Sim.RenderAlpha) + (Vector3d)Cam.transform.position;
            // Closest body for atmosphere / horizon purposes.
            CelestialBody near = frame.Body;
            double best = double.MaxValue;
            foreach (var b in Sim.System.Bodies)
            {
                double d = (camTrue - frame.BodyPosition(b, rut)).magnitude - b.Radius;
                if (d < best) { best = d; near = b; }
            }
            Vector3d rel = camTrue - frame.BodyPosition(near, rut);
            double r = rel.magnitude;
            CameraAltitude = r - near.Radius;
            Vector3 up = (Vector3)(rel / r);

            // Eclipse: is the camera in any body's shadow?
            InShadow = false;
            foreach (var b in Sim.System.Bodies)
            {
                Vector3d bc = frame.BodyPosition(b, rut);
                Vector3d toCam = camTrue - bc;
                double along = Vector3d.Dot(toCam, sunDir);
                if (along > 0) continue; // on the sun side
                double perp = (toCam - sunDir * along).magnitude;
                if (perp < b.Radius * 0.995) { InShadow = true; break; }
            }

            double atm = 0;
            Color skyC = Color.black, horC = Color.black;
            // The map is a view from space: planets are always sunlit there, whatever the vessel's own situation
            // (no eclipse, no dimming of the sun below the vessel's horizon, no haze).
            bool map = UiState.MapActive;
            if (map) InShadow = false;
            if (near.Atmosphere != null && !map)
            {
                double rho = near.Atmosphere.Density(Math.Max(0, CameraAltitude));
                atm = Math.Pow(MathD.Clamp01(rho / near.Atmosphere.SeaLevelDensity), 0.3);
                if (CameraAltitude > near.Atmosphere.Height) atm = 0;
                var sc = near.Def.atmosphere.skyColor; var hc = near.Def.atmosphere.horizonColor;
                skyC = new Color(sc[0], sc[1], sc[2]);
                horC = new Color(hc[0], hc[1], hc[2]);
            }
            AtmosphereFactor = atm;
            float sunElev = Vector3.Dot((Vector3)sunDir, up);
            float dip = (float)Math.Acos(Math.Min(1.0, near.Radius / Math.Max(r, near.Radius)));
            float dayAtCam = Mathf.Clamp01((sunElev + dip) * 6f + 0.5f);
            float sunVisible = InShadow ? 0f : 1f;
            // Atmospheric extinction near the horizon reddens/dims the sun.
            float ext = atm > 0 ? Mathf.Lerp(1f, Mathf.Clamp01(sunElev * 4f + 0.35f), (float)atm) : 1f;
            Sun.intensity = SunIntensity * ext;
            // Eclipse: in a planet's shadow the nearby scene (vessels, launch site, crew, flags) goes dark, but the planets
            // stay sunlit: the terrain shader takes the sun directly and its night side is dark by its own shading.
            // (Switching the whole sun off here blacked out every planet in view, day sides included.)
            if (_sunData != null) _sunData.renderingLayers = InShadow ? (RenderingLayerMask)0u : (RenderingLayerMask)0xFFFFFFFFu;
            // Sunset reddening only through an atmosphere (never in space or on the map).
            float white = atm > 0 ? Mathf.Clamp01(sunElev * 3f + 0.2f + (1 - (float)atm)) : 1f;
            Sun.color = Color.Lerp(new Color(1f, 0.62f, 0.38f), new Color(1f, 0.97f, 0.92f), white);

            if (SkyMaterial != null)
            {
                SkyMaterial.SetColor("_SkyColor", skyC);
                SkyMaterial.SetColor("_HorizonColor", horC);
                SkyMaterial.SetFloat("_Atmosphere", (float)atm);
                SkyMaterial.SetVector("_SunDir", (Vector3)sunDir);
                SkyMaterial.SetVector("_UpDir", up);
                SkyMaterial.SetFloat("_HorizonDip", dip);
                SkyMaterial.SetFloat("_StarBrightness", 1.2f);
            }

            // Fog (haze) inside atmospheres.
            if (atm > 0.02)
            {
                RenderSettings.fog = true;
                double rho = near.Atmosphere.Density(Math.Max(0, CameraAltitude));
                RenderSettings.fogDensity = (float)(1.6e-5 * rho / near.Atmosphere.SeaLevelDensity + 1e-7);
                RenderSettings.fogColor = horC * (0.25f + 0.75f * dayAtCam);
            }
            else RenderSettings.fog = false;

            // Ambient light: sky contribution by day + a small floor so night sides stay readable.
            float day = dayAtCam * sunVisible;
            Color amb = Color.Lerp(new Color(0.05f, 0.055f, 0.07f), skyC * 0.55f + new Color(0.12f, 0.12f, 0.12f), day * (float)(0.3 + 0.7 * atm));
            if (atm <= 0.01) amb = new Color(0.06f, 0.065f, 0.08f) + new Color(0.12f, 0.12f, 0.13f) * sunVisible;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(amb);
            RenderSettings.ambientProbe = sh;
        }
    }
}
