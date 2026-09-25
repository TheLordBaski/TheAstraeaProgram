using System;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>Tiny anti-aliased raster painter (distance-field primitives + 5x7 pixel font) for generated art.</summary>
    public sealed class TexturePainter
    {
        public readonly int W, H;
        public readonly Color[] Px;

        public TexturePainter(int w, int h, Color fill)
        {
            W = w; H = h;
            Px = new Color[w * h];
            for (int i = 0; i < Px.Length; i++) Px[i] = fill;
        }

        public void Blend(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || a <= 0) return;
            int i = y * W + x;
            var d = Px[i];
            float ca = c.a * Mathf.Clamp01(a);
            float outA = ca + d.a * (1 - ca);
            if (outA < 1e-6f) { Px[i] = new Color(0, 0, 0, 0); return; }
            Color o = (c * ca + d * d.a * (1 - ca)) / outA;
            o.a = outA;
            Px[i] = o;
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = c;
        }

        /// <summary>Applies a signed-distance shape over the bounding box (distance in pixels; inside negative).</summary>
        public void Shape(Func<float, float, float> sdf, Color c, float x0, float y0, float x1, float y1)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0) - 2), iy0 = Mathf.Max(0, Mathf.FloorToInt(y0) - 2);
            int ix1 = Mathf.Min(W - 1, Mathf.CeilToInt(x1) + 2), iy1 = Mathf.Min(H - 1, Mathf.CeilToInt(y1) + 2);
            for (int y = iy0; y <= iy1; y++)
                for (int x = ix0; x <= ix1; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a > 0) Blend(x, y, c, a);
                }
        }

        public void Circle(float cx, float cy, float r, Color c) =>
            Shape((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, cx - r, cy - r, cx + r, cy + r);

        public void Ring(float cx, float cy, float r, float thickness, Color c) =>
            Shape((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c, cx - r - thickness, cy - r - thickness, cx + r + thickness, cy + r + thickness);

        public void Line(float ax, float ay, float bx, float by, float thickness, Color c)
        {
            float half = thickness * 0.5f;
            Shape((x, y) =>
            {
                float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
                float h = Mathf.Clamp01((px * dx + py * dy) / Mathf.Max(dx * dx + dy * dy, 1e-6f));
                float qx = px - dx * h, qy = py - dy * h;
                return Mathf.Sqrt(qx * qx + qy * qy) - half;
            }, c, Mathf.Min(ax, bx) - half, Mathf.Min(ay, by) - half, Mathf.Max(ax, bx) + half, Mathf.Max(ay, by) + half);
        }

        public void RectFill(float x0, float y0, float x1, float y1, Color c) =>
            Shape((x, y) => Mathf.Max(Mathf.Max(x0 - x, x - x1), Mathf.Max(y0 - y, y - y1)), c, x0, y0, x1, y1);

        public void RoundRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            Shape((x, y) =>
            {
                float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                float hx = (x1 - x0) * 0.5f - r, hy = (y1 - y0) * 0.5f - r;
                float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
                float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            }, c, x0, y0, x1, y1);
        }

        public void Triangle(float ax, float ay, float bx, float by, float cx, float cy, Color c)
        {
            Shape((x, y) =>
            {
                float d1 = EdgeDist(x, y, ax, ay, bx, by);
                float d2 = EdgeDist(x, y, bx, by, cx, cy);
                float d3 = EdgeDist(x, y, cx, cy, ax, ay);
                return Mathf.Max(d1, Mathf.Max(d2, d3));
            }, c, Mathf.Min(ax, Mathf.Min(bx, cx)), Mathf.Min(ay, Mathf.Min(by, cy)), Mathf.Max(ax, Mathf.Max(bx, cx)), Mathf.Max(ay, Mathf.Max(by, cy)));
        }

        private static float EdgeDist(float x, float y, float ax, float ay, float bx, float by)
        {
            // signed distance to the line through a->b; positive on the right side (outside for CCW triangles)
            float dx = bx - ax, dy = by - ay;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            return ((x - ax) * dy - (y - ay) * dx) / Mathf.Max(len, 1e-6f);
        }

        // 5x7 font: digits, a few letters.
        private static readonly string[] Glyphs =
        {
            "0:01110100011001110101110011000101110", "1:00100011000010000100001000010001110",
            "2:01110100010000100010001000100011111", "3:11111000100010000010000011000101110",
            "4:00010001100101010010111110001000010", "5:11111100001111000001000011000101110",
            "6:00110010001000011110100011000101110", "7:11111000010001000100010000100001000",
            "8:01110100011000101110100011000101110", "9:01110100011000101111000010001001100",
            "N:10001100011100110101100111000110001", "E:11111100001000011110100001000011111",
            "S:01111100001000001110000010000111110", "W:10001100011000110101101011010101010",
            "A:01110100011000111111100011000110001", "R:11110100011000111110101001001010001",
            "T:11111001000010000100001000010000100", "G:01110100011000010111100011000101111",
            "H:10001100011000111111100011000110001", "I:01110001000010000100001000010001110",
            "L:10000100001000010000100001000011111", "U:10001100011000110001100011000101110",
            "P:11110100011000111110100001000010000", "O:01110100011000110001100011000101110",
            "-:00000000000000011111000000000000000", "+:00000001000010011111001000010000000",
            ".:00000000000000000000000000110001100", " :00000000000000000000000000000000000",
        };

        public void Text(string s, float x, float y, float scale, Color c)
        {
            float cx = x;
            foreach (char ch in s)
            {
                string bits = null;
                foreach (var g in Glyphs) if (g[0] == char.ToUpperInvariant(ch)) { bits = g.Substring(2); break; }
                if (bits != null)
                {
                    for (int row = 0; row < 7; row++)
                        for (int col = 0; col < 5; col++)
                        {
                            if (bits[row * 5 + col] != '1') continue;
                            float px = cx + col * scale, py = y + (6 - row) * scale;
                            RectFill(px, py, px + scale, py + scale, c);
                        }
                }
                cx += 6 * scale;
            }
        }

        public static float TextWidth(string s, float scale) => s.Length * 6 * scale - scale;

        public Texture2D ToTexture(bool mipmaps = true, bool linear = false)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, mipmaps, linear);
            t.SetPixels(Px);
            t.Apply();
            return t;
        }
    }
}
