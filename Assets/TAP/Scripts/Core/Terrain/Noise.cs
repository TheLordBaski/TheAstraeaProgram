using System;

namespace TAP.Core
{
    /// <summary>
    /// Deterministic 3D gradient noise (Perlin "improved noise") in double precision plus fractal helpers.
    /// Thread-safe after construction (read-only permutation table).
    /// </summary>
    public sealed class Noise3D
    {
        private readonly int[] _p = new int[512];

        public Noise3D(int seed)
        {
            var perm = new int[256];
            for (int i = 0; i < 256; i++) perm[i] = i;
            var rng = new Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }
            for (int i = 0; i < 512; i++) _p[i] = perm[i & 255];
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>Gradient noise in roughly [-1, 1].</summary>
        public double Sample(double x, double y, double z)
        {
            double fx = Math.Floor(x), fy = Math.Floor(y), fz = Math.Floor(z);
            int X = (int)((long)fx & 255), Y = (int)((long)fy & 255), Z = (int)((long)fz & 255);
            x -= fx; y -= fy; z -= fz;
            double u = Fade(x), v = Fade(y), w = Fade(z);
            int A = _p[X] + Y, AA = _p[A] + Z, AB = _p[A + 1] + Z;
            int B = _p[X + 1] + Y, BA = _p[B] + Z, BB = _p[B + 1] + Z;

            double x1 = x - 1, y1 = y - 1, z1 = z - 1;
            double l1 = Lerp(u, Grad(_p[AA], x, y, z), Grad(_p[BA], x1, y, z));
            double l2 = Lerp(u, Grad(_p[AB], x, y1, z), Grad(_p[BB], x1, y1, z));
            double l3 = Lerp(u, Grad(_p[AA + 1], x, y, z1), Grad(_p[BA + 1], x1, y, z1));
            double l4 = Lerp(u, Grad(_p[AB + 1], x, y1, z1), Grad(_p[BB + 1], x1, y1, z1));
            return Lerp(w, Lerp(v, l1, l2), Lerp(v, l3, l4));
        }

        private static double Lerp(double t, double a, double b) => a + t * (b - a);

        public double Sample(Vector3d p) => Sample(p.x, p.y, p.z);

        /// <summary>Fractal Brownian motion normalised to roughly [-1, 1].</summary>
        public double Fbm(Vector3d p, int octaves, double lacunarity = 2.0, double gain = 0.5)
        {
            double sum = 0, amp = 1, norm = 0;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Sample(x, y, z);
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                // Offset each octave to decorrelate lattice alignment.
                x += 17.13; y += 31.71; z += 7.77;
            }
            return sum / norm;
        }

        /// <summary>Ridged multifractal in [0, 1] (sharp crests).</summary>
        public double Ridged(Vector3d p, int octaves, double lacunarity = 2.1, double gain = 0.5)
        {
            double sum = 0, amp = 1, norm = 0, weight = 1;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                double n = 1.0 - Math.Abs(Sample(x, y, z));
                n *= n;
                n *= weight;
                weight = MathD.Clamp01(n * 2.0);
                sum += amp * n;
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                x += 5.31; y += 11.17; z += 23.3;
            }
            return sum / norm;
        }

        /// <summary>Cheap integer hash to [0,1).</summary>
        public static double Hash01(long x, long y, long z, int seed)
        {
            unchecked
            {
                ulong h = (ulong)(x * 73856093L) ^ (ulong)(y * 19349663L) ^ (ulong)(z * 83492791L) ^ (ulong)(seed * 2654435761L);
                h ^= h >> 33;
                h *= 0xff51afd7ed558ccdUL;
                h ^= h >> 33;
                h *= 0xc4ceb9fe1a85ec53UL;
                h ^= h >> 33;
                return (h >> 11) * (1.0 / 9007199254740992.0);
            }
        }
    }
}
