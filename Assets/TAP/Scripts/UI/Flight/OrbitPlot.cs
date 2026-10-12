using System;
using TAP.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// The orbit page's drawing: the body (and its atmosphere), the orbit with periapsis to the right, apsis diamonds and the
    /// vessel. Green for a closed orbit, amber where it dips under the surface; the part inside the body is faint.
    /// </summary>
    public sealed class OrbitPlot : VisualElement
    {
        private static readonly Color Body = new Color32(12, 26, 32, 255), BodyLine = new Color32(47, 111, 122, 255);
        private static readonly Color Atmo = new Color32(30, 74, 82, 255), Green = new Color32(72, 226, 124, 255);
        private static readonly Color Amber = new Color32(255, 181, 71, 255), Inside = new Color32(106, 85, 48, 255);
        private static readonly Color Ship = new Color32(238, 243, 245, 255), Apsis = new Color32(94, 230, 242, 255);

        private double _radius, _atmo, _p, _e, _nu;
        private bool _has;

        public OrbitPlot() { generateVisualContent += Draw; pickingMode = PickingMode.Ignore; }

        /// <summary>Redraws for an orbit around a body; <paramref name="orbit"/> null shows the body with the vessel on it.</summary>
        public void Set(Orbit orbit, double bodyRadius, double atmosphere, double ut)
        {
            bool has = orbit != null && orbit.SemiLatusRectum > 0;
            double p = has ? orbit.SemiLatusRectum : 0, e = has ? orbit.Eccentricity : 0, nu = has ? orbit.TrueAnomalyAtUT(ut) : 0;
            // a still picture redraws nothing (the anomaly moves by less than a pixel)
            if (has == _has && Math.Abs(p - _p) < _p * 1e-4 && Math.Abs(e - _e) < 1e-4 && Math.Abs(nu - _nu) < 0.005 && bodyRadius == _radius) return;
            _has = has; _p = p; _e = e; _nu = nu; _radius = bodyRadius; _atmo = atmosphere;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 4 || _radius <= 0) return;
            var c = r.center;
            float half = Mathf.Min(r.width, r.height) * 0.5f - 8;
            var g = ctx.painter2D;
            double rp = _has ? _p / (1 + _e) : _radius;
            double ra = _has && _e < 1 ? _p / (1 - _e) : rp * 3;
            double view = Math.Max(_radius * 1.15, Math.Max(ra, rp) * 1.05);
            float k = (float)(half / view);
            Vector2 At(double rr, double nu) => c + new Vector2((float)(rr * Math.Cos(nu)), -(float)(rr * Math.Sin(nu))) * k;

            g.fillColor = Body;
            g.BeginPath(); g.Arc(c, (float)_radius * k, 0, 360); g.Fill();
            g.lineWidth = 1.5f;
            g.strokeColor = BodyLine;
            g.BeginPath(); g.Arc(c, (float)_radius * k, 0, 360); g.Stroke();
            if (_atmo > 0)
            {
                g.lineWidth = 1f;
                g.strokeColor = Atmo;
                g.BeginPath(); g.Arc(c, (float)(_radius + _atmo) * k, 0, 360); g.Stroke();
            }
            if (!_has)
            {
                Dot(g, c + new Vector2(0, -(float)_radius * k), 4.5f, Ship);
                return;
            }

            // the conic, in short runs coloured by whether they are above the surface
            double nuMax = _e < 1 ? Math.PI : Math.Acos(-1 / _e) * 0.97;
            const int n = 180;
            g.lineWidth = 2f;
            Vector2 prev = default;
            bool havePrev = false;
            for (int i = 0; i <= n; i++)
            {
                double nu = -nuMax + 2 * nuMax * i / n;
                double rr = _p / (1 + _e * Math.Cos(nu));
                if (rr <= 0 || rr > view * 1.4) { havePrev = false; continue; }
                var pt = At(rr, nu);
                if (havePrev)
                {
                    g.strokeColor = rr < _radius ? Inside : rp < _radius ? Amber : Green;
                    g.BeginPath(); g.MoveTo(prev); g.LineTo(pt); g.Stroke();
                }
                prev = pt;
                havePrev = true;
            }
            Diamond(g, At(rp, 0), Apsis);
            if (_e < 1) Diamond(g, At(ra, Math.PI), Apsis);
            Dot(g, At(_p / (1 + _e * Math.Cos(_nu)), _nu), 4.5f, Ship);
        }

        private static void Dot(Painter2D g, Vector2 p, float rad, Color col)
        {
            g.fillColor = col;
            g.BeginPath(); g.Arc(p, rad, 0, 360); g.Fill();
            g.lineWidth = 1f;
            g.strokeColor = Color.black;
            g.BeginPath(); g.Arc(p, rad, 0, 360); g.Stroke();
        }

        private static void Diamond(Painter2D g, Vector2 p, Color col)
        {
            g.fillColor = col;
            g.BeginPath();
            g.MoveTo(p + new Vector2(0, -5)); g.LineTo(p + new Vector2(5, 0)); g.LineTo(p + new Vector2(0, 5)); g.LineTo(p + new Vector2(-5, 0));
            g.ClosePath();
            g.Fill();
        }
    }
}
