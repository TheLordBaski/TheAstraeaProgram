using System.Collections.Generic;
using TAP.Core;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>A sampled trajectory arc in body-relative map units, rendered with constant screen width.</summary>
    public sealed class OrbitLine
    {
        public GameObject Go;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public readonly List<Vector3d> PointsRel = new List<Vector3d>();   // metres, relative to body
        public readonly List<double> PointsUT = new List<double>();
        public CelestialBody Body;
        public OrbitPatch Patch;
        public Color Color;
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Vector3> _n = new List<Vector3>();
        private readonly List<Vector4> _uv = new List<Vector4>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<int> _i = new List<int>();

        public OrbitLine(Transform parent, Material mat, int layer)
        {
            Go = new GameObject("OrbitLine");
            Go.layer = layer;
            Go.transform.SetParent(parent, false);
            Mesh = new Mesh { name = "orbitline" };
            Mesh.MarkDynamic();
            Go.AddComponent<MeshFilter>().sharedMesh = Mesh;
            Renderer = Go.AddComponent<MeshRenderer>();
            Renderer.sharedMaterial = mat;
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
        }

        public void SetVisible(bool v) { if (Go.activeSelf != v) Go.SetActive(v); }

        /// <summary>Samples a patch into body-relative points.</summary>
        public void SamplePatch(OrbitPatch p, int samples, double maxRadius)
        {
            Patch = p;
            Body = p.Body;
            PointsRel.Clear();
            PointsUT.Clear();
            var o = p.Orbit;
            if (o.IsRadial)
            {
                for (int i = 0; i <= samples; i++)
                {
                    double t = p.StartUT + (p.EndUT - p.StartUT) * i / samples;
                    PointsRel.Add(o.GetPositionAtUT(t));
                    PointsUT.Add(t);
                }
                return;
            }
            double nu0 = o.TrueAnomalyAtUT(p.StartUT);
            double dnu;
            bool fullLoop = o.IsElliptic && (p.EndType == PatchEnd.None || p.EndUT - p.StartUT >= o.Period * 0.999);
            if (fullLoop) dnu = MathD.TwoPi;
            else
            {
                double nu1 = o.TrueAnomalyAtUT(p.EndUT);
                if (o.IsElliptic) dnu = MathD.WrapTwoPi(nu1 - nu0);
                else dnu = nu1 - nu0;
                if (!o.IsElliptic)
                {
                    // Clamp hyperbolic arcs to a sensible display radius.
                    double lim = o.MaxTrueAnomaly - 1e-3;
                    double nuMaxR = o.TrueAnomalyAtRadius(maxRadius);
                    if (!double.IsNaN(nuMaxR)) lim = System.Math.Min(lim, nuMaxR);
                    double end = Clamp(nu0 + dnu, -lim, lim);
                    dnu = end - nu0;
                }
            }
            for (int i = 0; i <= samples; i++)
            {
                double nu = nu0 + dnu * i / samples;
                var pos = o.PositionAtTrueAnomaly(nu);
                if (!pos.IsFinite()) continue;
                PointsRel.Add(pos);
                PointsUT.Add(o.IsElliptic ? (fullLoop ? o.UTAtTrueAnomaly(nu, p.StartUT) : o.UTAtTrueAnomaly(nu, p.StartUT)) : o.UTAtTrueAnomaly(nu, p.StartUT));
            }
        }

        private static double Clamp(double v, double a, double b) => v < a ? a : (v > b ? b : v);

        /// <summary>Rebuilds the line mesh in map units relative to the line object's origin.</summary>
        public void BuildMesh(double scale, Color color, double fadeStartFraction = 1.0)
        {
            Color = color;
            _v.Clear(); _n.Clear(); _uv.Clear(); _c.Clear(); _i.Clear();
            int n = PointsRel.Count;
            float along = 0;
            for (int k = 0; k < n - 1; k++)
            {
                Vector3 a = (Vector3)(PointsRel[k] / scale), b = (Vector3)(PointsRel[k + 1] / scale);
                float seg = (b - a).magnitude;
                float alphaA = 1f, alphaB = 1f;
                if (fadeStartFraction < 1.0)
                {
                    float fa = (float)k / (n - 1), fb = (float)(k + 1) / (n - 1);
                    alphaA = Mathf.Clamp01(1f - (fa - (float)fadeStartFraction) / (1f - (float)fadeStartFraction));
                    alphaB = Mathf.Clamp01(1f - (fb - (float)fadeStartFraction) / (1f - (float)fadeStartFraction));
                }
                int baseIdx = _v.Count;
                _v.Add(a); _n.Add(b); _uv.Add(new Vector4(-1, 0, along, alphaA)); _c.Add(color);
                _v.Add(a); _n.Add(b); _uv.Add(new Vector4(1, 0, along, alphaA)); _c.Add(color);
                _v.Add(b); _n.Add(a); _uv.Add(new Vector4(-1, 1, along + seg, alphaB)); _c.Add(color);
                _v.Add(b); _n.Add(a); _uv.Add(new Vector4(1, 1, along + seg, alphaB)); _c.Add(color);
                _i.Add(baseIdx); _i.Add(baseIdx + 1); _i.Add(baseIdx + 3);
                _i.Add(baseIdx); _i.Add(baseIdx + 3); _i.Add(baseIdx + 2);
                along += seg;
            }
            Mesh.Clear();
            if (_v.Count > 65000) Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Mesh.SetVertices(_v);
            Mesh.SetNormals(_n);
            Mesh.SetUVs(0, _uv);
            Mesh.SetColors(_c);
            Mesh.SetTriangles(_i, 0, false);
            Mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e7f);
        }

        public void Destroy()
        {
            if (Go != null) Object.Destroy(Go);
            if (Mesh != null) Object.Destroy(Mesh);
        }
    }
}
