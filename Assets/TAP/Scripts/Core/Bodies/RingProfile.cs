using System;
using System.Collections.Generic;

namespace TAP.Core
{
    /// <summary>
    /// The smooth structure of a body's rings at a radius, from its <see cref="RingDefinition"/> entries: where they overlap
    /// the light passes through each in turn, and the edges fade over each ring's soft edge. The fine ringlets come from
    /// the ring shader. The same function builds the profile texture and answers tests and shadows.
    /// </summary>
    public static class RingProfile
    {
        /// <summary>Smallest inner and largest outer radius of the rings (m); 0 and 0 without rings.</summary>
        public static void Range(IReadOnlyList<RingDefinition> rings, out double inner, out double outer)
        {
            inner = double.MaxValue;
            outer = 0;
            if (rings != null)
                foreach (var r in rings)
                {
                    inner = Math.Min(inner, r.inner);
                    outer = Math.Max(outer, r.outer);
                }
            if (outer <= 0) inner = 0;
        }

        /// <summary>
        /// Opacity at normal incidence (0 to 1) at <paramref name="radius"/> metres from the centre, with the colour of the
        /// material there and the strength of its ringlets.
        /// </summary>
        public static double Sample(IReadOnlyList<RingDefinition> rings, double radius, out float red, out float green, out float blue, out double ringlets)
        {
            double clear = 1, weight = 0, r = 0, g = 0, b = 0;
            ringlets = 0;
            red = green = blue = 0.5f;
            if (rings == null) return 0;
            foreach (var ring in rings)
            {
                if (radius <= ring.inner || radius >= ring.outer) continue;
                double soft = Math.Max(ring.softEdge * (ring.outer - ring.inner), 1e-6);
                double edge = Smooth((radius - ring.inner) / soft) * Smooth((ring.outer - radius) / soft);
                double opacity = ring.opacity * edge;
                clear *= 1 - opacity;
                weight += opacity;
                r += ring.color[0] * opacity;
                g += ring.color[1] * opacity;
                b += ring.color[2] * opacity;
                ringlets = Math.Max(ringlets, ring.ringlets * edge);
            }
            if (weight > 1e-9)
            {
                red = (float)(r / weight);
                green = (float)(g / weight);
                blue = (float)(b / weight);
            }
            return 1 - clear;
        }

        /// <summary>Opacity only.</summary>
        public static double Opacity(IReadOnlyList<RingDefinition> rings, double radius) =>
            Sample(rings, radius, out _, out _, out _, out _);

        private static double Smooth(double x)
        {
            x = x < 0 ? 0 : x > 1 ? 1 : x;
            return x * x * (3 - 2 * x);
        }
    }
}
