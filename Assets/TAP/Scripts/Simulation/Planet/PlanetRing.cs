using System.Collections.Generic;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>The ring profile texture of a body: row 0 the colour and opacity at each radius, row 1 the ringlet strength.</summary>
    public static class RingTexture
    {
        public const int Width = 4096;
        private static readonly Dictionary<CelestialBody, Texture2D> Cache = new Dictionary<CelestialBody, Texture2D>();

        /// <summary>The (shared) profile of a body's rings, made on first use.</summary>
        public static Texture2D For(CelestialBody body)
        {
            if (Cache.TryGetValue(body, out var cached) && cached != null) return cached;
            RingProfile.Range(body.Rings, out double inner, out double outer);
            var tex = new Texture2D(Width, 2, TextureFormat.RGBA32, false, true)
            {
                name = "Rings " + body.Id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[Width * 2];
            for (int x = 0; x < Width; x++)
            {
                double radius = inner + (outer - inner) * (x + 0.5) / Width;
                double opacity = RingProfile.Sample(body.Rings, radius, out float r, out float g, out float b, out double ringlets);
                px[x] = new Color32(Byte(r), Byte(g), Byte(b), Byte(opacity));
                px[Width + x] = new Color32(Byte(ringlets), 0, 0, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Cache[body] = tex;
            return tex;
        }

        private static byte Byte(double v) => (byte)(v <= 0 ? 0 : v >= 1 ? 255 : v * 255 + 0.5);
    }

    /// <summary>
    /// The rings of a body: a flat annulus in its equatorial plane (the spin axis is +Y). Like an atmosphere shell, one copy
    /// is drawn by the flight camera while the body is near and one by the far view (or the map) otherwise.
    /// </summary>
    public sealed class PlanetRing : MonoBehaviour
    {
        public CelestialBody Body;
        /// <summary>Drawn by the flight camera: the body is near. The far view's copy stays hidden then.</summary>
        public bool Local = true;
        private Material _mat;
        private MeshRenderer _mr;
        private double _inner, _outer;

        public static PlanetRing Create(CelestialBody body, Transform parent, int layer = Layers.Default)
        {
            if (body.Rings.Count == 0) return null;
            var shader = Shader.Find("TAP/Ring");
            var mat = Resources.Load<Material>("Materials/Ring");
            if (mat == null && shader == null) return null;
            if (mat == null) mat = new Material(shader);
            var go = new GameObject("Rings " + body.Name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            var ring = go.AddComponent<PlanetRing>();
            ring.Body = body;
            RingProfile.Range(body.Rings, out ring._inner, out ring._outer);
            ring._mat = new Material(mat) { name = "Rings " + body.Id };
            ring._mat.SetTexture("_Profile", RingTexture.For(body));
            ring._mat.SetVector("_RingRadii", new Vector4((float)ring._inner, (float)ring._outer, (float)body.Radius, 0));
            go.AddComponent<MeshFilter>().sharedMesh = Annulus(ring._inner, ring._outer);
            ring._mr = go.AddComponent<MeshRenderer>();
            ring._mr.sharedMaterial = ring._mat;
            ring._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring._mr.receiveShadows = false;
            ring._mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            ring._mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            ring._mr.enabled = false;
            return ring;
        }

        /// <summary>A flat ring in metres between two radii: fine round, coarse across (the profile makes the detail).</summary>
        private static Mesh Annulus(double inner, double outer)
        {
            const int around = 360, across = 6;
            var verts = new Vector3[(around + 1) * (across + 1)];
            for (int j = 0; j <= across; j++)
            {
                float r = (float)(inner + (outer - inner) * j / across);
                for (int i = 0; i <= around; i++)
                {
                    float a = i * Mathf.PI * 2 / around;
                    verts[j * (around + 1) + i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                }
            }
            var tris = new int[around * across * 6];
            int t = 0;
            for (int j = 0; j < across; j++)
                for (int i = 0; i < around; i++)
                {
                    int a = j * (around + 1) + i, b = a + 1, c = a + around + 1, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            var mesh = new Mesh { name = "ring annulus", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3((float)outer * 2, 1000f, (float)outer * 2));
            return mesh;
        }

        /// <summary>
        /// Draws the rings around <paramref name="center"/> in a space of <paramref name="metresPerUnit"/> (1 in the flight
        /// scene, 1e5 in the far view, 1000 in the map), lit from <paramref name="sunDir"/> with <paramref name="sunColor"/>
        /// (linear, light the renderer uses) by a star of angular radius <paramref name="sunAngular"/> (rad).
        /// </summary>
        public void Place(Vector3 center, double metresPerUnit, Vector3 sunDir, Color sunColor, float sunAngular)
        {
            _mr.enabled = true;
            Vector3 normal = (Vector3)Body.RingNormal;
            transform.SetPositionAndRotation(center, Quaternion.FromToRotation(Vector3.up, normal));
            _mat.SetVector("_RingNormal", normal);
            transform.localScale = Vector3.one * (float)(1 / metresPerUnit);
            _mat.SetFloat("_DistanceScale", (float)metresPerUnit);
            _mat.SetVector("_BodyCenter", center);
            _mat.SetVector("_SunDir", sunDir);
            _mat.SetVector("_SunColor", new Vector4(sunColor.r, sunColor.g, sunColor.b, sunAngular));
        }

        public void Hide()
        {
            if (_mr != null && _mr.enabled) _mr.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
