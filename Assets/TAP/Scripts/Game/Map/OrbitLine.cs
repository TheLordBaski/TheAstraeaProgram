using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// A sampled trajectory arc in body-relative map units, rendered with constant screen width. The mesh holds the
    /// points relative to an <see cref="Anchor"/> near the map's focus and gathers extra samples there
    /// (<see cref="KeepExactNear"/>), so even an orbit around the star, 10¹⁰ m across, runs exactly through the focus at
    /// the closest zoom: neither float rounding nor the chords between samples show.
    /// </summary>
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
        /// <summary>The mesh's origin: metres relative to the body. The line object sits there on the map.</summary>
        public Vector3d Anchor;
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Vector3> _n = new List<Vector3>();
        private readonly List<Vector4> _uv = new List<Vector4>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<int> _i = new List<int>();
        private readonly List<double> _nus = new List<double>();
        private int _samples;
        private double _maxRadius;
        private double _scale = 1;
        private double _fade = 1;
        private double _refineNu = double.NaN, _refineStep;

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
            _samples = samples;
            _maxRadius = maxRadius;
            _refineNu = double.NaN;
            Sample();
        }

        private void Sample()
        {
            var p = Patch;
            PointsRel.Clear();
            PointsUT.Clear();
            var o = p.Orbit;
            if (o.IsRadial)
            {
                for (int i = 0; i <= _samples; i++)
                {
                    double t = p.StartUT + (p.EndUT - p.StartUT) * i / _samples;
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
                    double nuMaxR = o.TrueAnomalyAtRadius(_maxRadius);
                    if (!double.IsNaN(nuMaxR)) lim = Math.Min(lim, nuMaxR);
                    double end = Clamp(nu0 + dnu, -lim, lim);
                    dnu = end - nu0;
                }
            }
            _nus.Clear();
            for (int i = 0; i <= _samples; i++) _nus.Add(nu0 + dnu * i / _samples);
            if (!double.IsNaN(_refineNu) && dnu > 0)
            {
                // Around the focus: the finest step next to it, doubling outwards until it meets the even spacing.
                double t = o.IsElliptic ? MathD.WrapTwoPi(_refineNu - nu0) : _refineNu - nu0;
                double coarse = dnu / _samples;
                if (t > 0 && t < dnu)
                {
                    _nus.Add(nu0 + t);
                    for (double h = _refineStep; h < coarse; h *= 2)
                    {
                        if (t - h > 0) _nus.Add(nu0 + t - h);
                        if (t + h < dnu) _nus.Add(nu0 + t + h);
                    }
                    _nus.Sort();
                }
            }
            foreach (double nu in _nus)
            {
                var pos = o.PositionAtTrueAnomaly(nu);
                if (!pos.IsFinite()) continue;
                PointsRel.Add(pos);
                PointsUT.Add(o.UTAtTrueAnomaly(nu, p.StartUT));
            }
        }

        private static double Clamp(double v, double a, double b) => v < a ? a : (v > b ? b : v);

        /// <summary>
        /// Keeps the line exact where the map camera looks, <paramref name="view"/> metres from the focus
        /// (<paramref name="focusRel"/>, relative to the body). Where the line passes the focus, the mesh is anchored on
        /// it and samples gather around it so that no chord cuts more than a thousandth of the view inside the arc.
        /// Returns true when the mesh was rebuilt.
        /// </summary>
        public bool KeepExactNear(Vector3d focusRel, double view)
        {
            if (Patch == null || Patch.Orbit.IsRadial || PointsRel.Count < 2 || view <= 0) return false;
            var o = Patch.Orbit;
            double nuF = o.TrueAnomalyOfPosition(focusRel);
            Vector3d onLine = o.PositionAtTrueAnomaly(nuF);
            bool near = onLine.IsFinite() && (onLine - focusRel).magnitude < 50 * view;
            // A chord spanning Δν cuts r·Δν²/8 inside the arc.
            double step = near ? Math.Sqrt(8e-3 * view / Math.Max(onLine.magnitude, 1)) : 0;
            bool refine = near && step * _samples < MathD.TwoPi;
            bool resample;
            if (refine)
                resample = double.IsNaN(_refineNu) || Math.Abs(MathD.WrapPi(nuF - _refineNu)) > 2 * _refineStep
                           || step < _refineStep * 0.5 || step > _refineStep * 2;
            else resample = !double.IsNaN(_refineNu);
            // Float vertices keep ~7 digits of their distance from the anchor: that must stay far below the view.
            bool reanchor = near && (resample || (Anchor - focusRel).magnitude > 1e3 * view);
            if (!resample && !reanchor) return false;
            if (resample)
            {
                _refineNu = refine ? nuF : double.NaN;
                _refineStep = step;
                Sample();
            }
            if (reanchor) Anchor = onLine;
            BuildMesh(_scale, Color, _fade);
            return true;
        }

        /// <summary>Rebuilds the line mesh in map units relative to the anchor (the line object's origin).</summary>
        public void BuildMesh(double scale, Color color, double fadeStartFraction = 1.0)
        {
            Color = color;
            _scale = scale;
            _fade = fadeStartFraction;
            _v.Clear(); _n.Clear(); _uv.Clear(); _c.Clear(); _i.Clear();
            int n = PointsRel.Count;
            float along = 0;
            for (int k = 0; k < n - 1; k++)
            {
                Vector3 a = (Vector3)((PointsRel[k] - Anchor) / scale), b = (Vector3)((PointsRel[k + 1] - Anchor) / scale);
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
            // Bounds that hold every point, however large the orbit (a planet's orbit around the star spans 10¹⁰ m):
            // a fixed box culled the home planet's own orbit line whenever the star was out of view.
            var bounds = new Bounds(_v.Count > 0 ? _v[0] : Vector3.zero, Vector3.zero);
            foreach (var p in _v) bounds.Encapsulate(p);
            bounds.Expand(Mathf.Max(1f, bounds.size.magnitude * 1e-3f));
            Mesh.bounds = bounds;
        }

        public void Destroy()
        {
            if (Go != null) UnityEngine.Object.Destroy(Go);
            if (Mesh != null) UnityEngine.Object.Destroy(Mesh);
        }
    }
}
