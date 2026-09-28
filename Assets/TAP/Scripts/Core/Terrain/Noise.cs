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
            var rng = new SeededRandom(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }
            for (int i = 0; i < 512; i++) _p[i] = perm[i & 255];
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        // Perlin's 16 gradients (12 cube edges, four repeated) as a table: the same sums as his bit tests, without branches.
        private static readonly double[] GX = { 1, -1, 1, -1, 1, -1, 1, -1, 0, 0, 0, 0, 1, 0, -1, 0 };
        private static readonly double[] GY = { 1, 1, -1, -1, 0, 0, 0, 0, 1, -1, 1, -1, 1, -1, 1, -1 };
        private static readonly double[] GZ = { 0, 0, 0, 0, 1, 1, -1, -1, 1, 1, -1, -1, 0, 1, 0, -1 };

        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            return GX[h] * x + GY[h] * y + GZ[h] * z;
        }

        /// <summary>Gradient noise in roughly [-1, 1].</summary>
        public double Sample(double x, double y, double z)
        {
            // Floor without a call (exact for the coordinates noise sees).
            long ix = (long)x, iy = (long)y, iz = (long)z;
            if (x < ix) ix--;
            if (y < iy) iy--;
            if (z < iz) iz--;
            int X = (int)(ix & 255), Y = (int)(iy & 255), Z = (int)(iz & 255);
            x -= ix; y -= iy; z -= iz;
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

        /// <summary>Billow noise in roughly [-1, 1]: folded octaves, rounded crests and sharp creases (clouds, dunes, lumps).</summary>
        public double Billow(Vector3d p, int octaves, double lacunarity = 2.0, double gain = 0.5)
        {
            double sum = 0, amp = 1, norm = 0;
            double x = p.x, y = p.y, z = p.z;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * (2.0 * Math.Abs(Sample(x, y, z)) - 1.0);
                norm += amp;
                amp *= gain;
                x *= lacunarity; y *= lacunarity; z *= lacunarity;
                x += 13.37; y += 5.29; z += 19.61;
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

    /// <summary>
    /// The seeded generator of System.Random (Knuth's subtractive method, as in .NET Framework, Mono and .NET's
    /// seeded compatibility mode), kept here so the noise permutations can never change with the runtime.
    /// </summary>
    public sealed class SeededRandom
    {
        private const int MBig = int.MaxValue;
        private const int MSeed = 161803398;
        private readonly int[] _seedArray = new int[56];
        private int _inext, _inextp;

        public SeededRandom(int seed)
        {
            int subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
            int mj = MSeed - subtraction;
            _seedArray[55] = mj;
            int mk = 1;
            for (int i = 1; i < 55; i++)
            {
                int ii = 21 * i % 55;
                _seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBig;
                mj = _seedArray[ii];
            }
            for (int k = 1; k < 5; k++)
                for (int i = 1; i < 56; i++)
                {
                    _seedArray[i] -= _seedArray[1 + (i + 30) % 55];
                    if (_seedArray[i] < 0) _seedArray[i] += MBig;
                }
            _inext = 0;
            _inextp = 21;
        }

        private int InternalSample()
        {
            int locINext = _inext, locINextp = _inextp;
            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;
            int retVal = _seedArray[locINext] - _seedArray[locINextp];
            if (retVal == MBig) retVal--;
            if (retVal < 0) retVal += MBig;
            _seedArray[locINext] = retVal;
            _inext = locINext;
            _inextp = locINextp;
            return retVal;
        }

        /// <summary>A number in [0, 1).</summary>
        public double NextDouble() => InternalSample() * (1.0 / MBig);

        /// <summary>An integer in [0, maxValue).</summary>
        public int Next(int maxValue) => (int)(NextDouble() * maxValue);
    }
}
