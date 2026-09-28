using System;

namespace TAP.Core
{
    /// <summary>
    /// Exponential, logarithm and trigonometry in plain IEEE double arithmetic (+ − × ÷, square root, floor), ported
    /// from fdlibm. System.Math hands these to the platform's C library, whose results differ in the last bit between
    /// Windows and Linux; these give the same bits everywhere. The terrain uses them, so heights and biomes are
    /// identical on every platform.
    /// </summary>
    public static class DetMath
    {
        public const double Pi = 3.14159265358979311600e+00;
        public const double HalfPi = 1.57079632679489655800e+00;

        // ------------------------------------------------------------------ exp, log, pow

        private const double Ln2Hi = 6.93147180369123816490e-01;
        private const double Ln2Lo = 1.90821492927058770002e-10;
        private const double InvLn2 = 1.44269504088896338700e+00;
        private const double P1 = 1.66666666666666019037e-01;
        private const double P2 = -2.77777777770155933842e-03;
        private const double P3 = 6.61375632143793436117e-05;
        private const double P4 = -1.65339022054652515390e-06;
        private const double P5 = 4.13813679705723846039e-08;

        public static double Exp(double x)
        {
            if (double.IsNaN(x)) return x;
            if (x > 709.782712893383973096) return double.PositiveInfinity;
            if (x < -745.13321910194110842) return 0;
            if (x > -3.725290298461914e-9 && x < 3.725290298461914e-9) return 1 + x; // |x| < 2^-28
            // x = k·ln2 + r with |r| ≤ ln2/2.
            double kd = Math.Floor(x * InvLn2 + 0.5);
            double hi = x - kd * Ln2Hi; // exact: kd·Ln2Hi has trailing zeros
            double lo = kd * Ln2Lo;
            double r = hi - lo;
            double t = r * r;
            double c = r - t * (P1 + t * (P2 + t * (P3 + t * (P4 + t * P5))));
            double y = 1 - ((lo - (r * c) / (2 - c)) - hi);
            return ScaleB(y, (int)kd);
        }

        /// <summary>y · 2^k.</summary>
        private static double ScaleB(double y, int k)
        {
            if (k > 1023) { y *= Pow2(1023); k -= 1023; if (k > 1023) k = 1023; }
            else if (k < -1022) { y *= Pow2(-1022); k += 1022; if (k < -1022) k = -1022; }
            return y * Pow2(k);
        }

        private static double Pow2(int k) => BitConverter.Int64BitsToDouble((long)(k + 1023) << 52);

        private const double Lg1 = 6.666666666666735130e-01;
        private const double Lg2 = 3.999999999940941908e-01;
        private const double Lg3 = 2.857142874366239149e-01;
        private const double Lg4 = 2.222219843214978396e-01;
        private const double Lg5 = 1.818357216161805012e-01;
        private const double Lg6 = 1.531383769920937332e-01;
        private const double Lg7 = 1.479819860511658591e-01;

        /// <summary>Natural logarithm.</summary>
        public static double Log(double x)
        {
            if (double.IsNaN(x) || x < 0) return double.NaN;
            if (x == 0) return double.NegativeInfinity;
            if (double.IsPositiveInfinity(x)) return x;
            long bits = BitConverter.DoubleToInt64Bits(x);
            int hx = (int)(bits >> 32);
            uint lx = (uint)bits;
            int k = 0;
            if (hx < 0x00100000)
            {
                // Subnormal: scale up by 2^54.
                k -= 54;
                x *= 1.80143985094819840000e+16;
                bits = BitConverter.DoubleToInt64Bits(x);
                hx = (int)(bits >> 32);
                lx = (uint)bits;
            }
            k += (hx >> 20) - 1023;
            hx &= 0x000fffff;
            int i = (hx + 0x95f64) & 0x100000;
            // Normalise x or x/2 into [sqrt(2)/2, sqrt(2)).
            x = BitConverter.Int64BitsToDouble(((long)(hx | (i ^ 0x3ff00000)) << 32) | lx);
            k += i >> 20;
            double f = x - 1.0;
            double dk;
            if ((0x000fffff & (2 + hx)) < 3)
            {
                // |f| < 2^-20
                if (f == 0)
                {
                    if (k == 0) return 0;
                    dk = k;
                    return dk * Ln2Hi + dk * Ln2Lo;
                }
                double R0 = f * f * (0.5 - 0.33333333333333333 * f);
                if (k == 0) return f - R0;
                dk = k;
                return dk * Ln2Hi - ((R0 - dk * Ln2Lo) - f);
            }
            double s = f / (2.0 + f);
            dk = k;
            double z = s * s;
            int ii = hx - 0x6147a;
            double w = z * z;
            int j = 0x6b851 - hx;
            double t1 = w * (Lg2 + w * (Lg4 + w * Lg6));
            double t2 = z * (Lg1 + w * (Lg3 + w * (Lg5 + w * Lg7)));
            ii |= j;
            double R = t2 + t1;
            if (ii > 0)
            {
                double hfsq = 0.5 * f * f;
                if (k == 0) return f - (hfsq - s * (hfsq + R));
                return dk * Ln2Hi - ((hfsq - (s * (hfsq + R) + dk * Ln2Lo)) - f);
            }
            if (k == 0) return f - s * (f - R);
            return dk * Ln2Hi - ((s * (f - R) - dk * Ln2Lo) - f);
        }

        /// <summary>x^y for x ≥ 0 (as exp(y·ln x)); negative x gives NaN.</summary>
        public static double Pow(double x, double y)
        {
            if (y == 0) return 1;
            if (x == 1) return 1;
            if (x == 0) return y > 0 ? 0 : double.PositiveInfinity;
            if (x < 0 || double.IsNaN(x) || double.IsNaN(y)) return double.NaN;
            return Exp(y * Log(x));
        }

        // ------------------------------------------------------------------ atan, atan2, asin, acos

        private static readonly double[] AtanHi = { 4.63647609000806093515e-01, 7.85398163397448278999e-01, 9.82793723247329054082e-01, 1.57079632679489655800e+00 };
        private static readonly double[] AtanLo = { 2.26987774529616870924e-17, 3.06161699786838301793e-17, 1.39033110312309984516e-17, 6.12323399573676603587e-17 };
        private const double AT0 = 3.33333333333329318027e-01, AT1 = -1.99999999998764832476e-01, AT2 = 1.42857142725034663711e-01;
        private const double AT3 = -1.11111104054623557880e-01, AT4 = 9.09088713343650656196e-02, AT5 = -7.69187620504482999495e-02;
        private const double AT6 = 6.66107313738753120669e-02, AT7 = -5.83357013379057348645e-02, AT8 = 4.97687799461593236017e-02;
        private const double AT9 = -3.65315727442169155270e-02, AT10 = 1.62858201153657823623e-02;

        public static double Atan(double x)
        {
            if (double.IsNaN(x)) return x;
            double ax = Math.Abs(x);
            if (ax >= 7.378697629483821e19) return x > 0 ? AtanHi[3] + AtanLo[3] : -AtanHi[3] - AtanLo[3]; // |x| ≥ 2^66
            int id;
            if (ax < 0.4375)
            {
                if (ax < 1.862645149230957e-9) return x; // |x| < 2^-29
                id = -1;
            }
            else
            {
                if (ax < 1.1875)
                {
                    if (ax < 0.6875) { id = 0; ax = (2.0 * ax - 1.0) / (2.0 + ax); }
                    else { id = 1; ax = (ax - 1.0) / (ax + 1.0); }
                }
                else
                {
                    if (ax < 2.4375) { id = 2; ax = (ax - 1.5) / (1.0 + 1.5 * ax); }
                    else { id = 3; ax = -1.0 / ax; }
                }
            }
            double v = id < 0 ? x : ax;
            double z = v * v;
            double w = z * z;
            double s1 = z * (AT0 + w * (AT2 + w * (AT4 + w * (AT6 + w * (AT8 + w * AT10)))));
            double s2 = w * (AT1 + w * (AT3 + w * (AT5 + w * (AT7 + w * AT9))));
            if (id < 0) return v - v * (s1 + s2);
            double r = AtanHi[id] - ((v * (s1 + s2) - AtanLo[id]) - v);
            return x < 0 ? -r : r;
        }

        public static double Atan2(double y, double x)
        {
            if (double.IsNaN(x) || double.IsNaN(y)) return double.NaN;
            if (x == 0)
            {
                if (y == 0) return 0;
                return y > 0 ? HalfPi : -HalfPi;
            }
            if (y == 0) return x > 0 ? 0 : (BitConverter.DoubleToInt64Bits(y) < 0 ? -Pi : Pi);
            double a = Atan(Math.Abs(y / x));
            if (x < 0) a = Pi - a;
            return y < 0 ? -a : a;
        }

        public static double Asin(double x)
        {
            if (x >= 1) return x == 1 ? HalfPi : double.NaN;
            if (x <= -1) return x == -1 ? -HalfPi : double.NaN;
            return Atan2(x, Math.Sqrt((1 - x) * (1 + x)));
        }

        public static double Acos(double x)
        {
            if (x >= 1) return x == 1 ? 0 : double.NaN;
            if (x <= -1) return x == -1 ? Pi : double.NaN;
            return Atan2(Math.Sqrt((1 - x) * (1 + x)), x);
        }

        /// <summary>Angle between two vectors (rad), accurate for tiny and near-opposite angles alike.</summary>
        public static double Angle(Vector3d a, Vector3d b) => Atan2(Vector3d.Cross(a, b).magnitude, Vector3d.Dot(a, b));

        // ------------------------------------------------------------------ sin, cos

        private const double S1 = -1.66666666666666324348e-01, S2 = 8.33333333332248946124e-03, S3 = -1.98412698298579493134e-04;
        private const double S4 = 2.75573137070700676789e-06, S5 = -2.50507602534068634195e-08, S6 = 1.58969099521155010221e-10;
        private const double C1 = 4.16666666666666019037e-02, C2 = -1.38888888888741095749e-03, C3 = 2.48015872894767294178e-05;
        private const double C4 = -2.75573143513906633035e-07, C5 = 2.08757232129817482790e-09, C6 = -1.13596475577881948265e-11;

        private static double KernelSin(double x, double y, bool tail)
        {
            double z = x * x;
            double v = z * x;
            double r = S2 + z * (S3 + z * (S4 + z * (S5 + z * S6)));
            if (!tail) return x + v * (S1 + z * r);
            return x - ((z * (0.5 * y - v * r) - y) - v * S1);
        }

        private static double KernelCos(double x, double y)
        {
            double ax = Math.Abs(x);
            double z = x * x;
            double r = z * (C1 + z * (C2 + z * (C3 + z * (C4 + z * (C5 + z * C6)))));
            if (ax < 0.3) return 1.0 - (0.5 * z - (z * r - x * y));
            double qx;
            if (ax > 0.78125) qx = 0.28125;
            else
            {
                // x/4 with the low word cleared
                long hi = (BitConverter.DoubleToInt64Bits(ax) >> 32) - 0x00200000;
                qx = BitConverter.Int64BitsToDouble(hi << 32);
            }
            double hz = 0.5 * z - qx;
            double a = 1.0 - qx;
            return a - (hz - (z * r - x * y));
        }

        private const double InvPio2 = 6.36619772367581382433e-01;
        private const double Pio2_1 = 1.57079632673412561417e+00, Pio2_1t = 6.07710050650619224932e-11;
        private const double Pio2_2 = 6.07710050630396597660e-11, Pio2_2t = 2.02226624879595063154e-21;
        private const double Pio2_3 = 2.02226624871116645580e-21, Pio2_3t = 8.47842766036889956997e-32;

        private static int Exponent(double x) => (int)((BitConverter.DoubleToInt64Bits(x) >> 52) & 0x7ff);

        /// <summary>x = n·π/2 + (y0 + y1), |y0 + y1| ≤ π/4, for |x| up to 2^19·π/2 (larger arguments are not needed here).</summary>
        private static int RemPio2(double x, out double y0, out double y1)
        {
            double t = Math.Abs(x);
            int n = (int)(t * InvPio2 + 0.5);
            double fn = n;
            double r = t - fn * Pio2_1;
            double w = fn * Pio2_1t;
            int j = Exponent(t);
            y0 = r - w;
            if (j - Exponent(y0) > 16)
            {
                // Cancellation: a second (and maybe third) round with more bits of π/2.
                t = r;
                w = fn * Pio2_2;
                r = t - w;
                w = fn * Pio2_2t - ((t - r) - w);
                y0 = r - w;
                if (j - Exponent(y0) > 49)
                {
                    t = r;
                    w = fn * Pio2_3;
                    r = t - w;
                    w = fn * Pio2_3t - ((t - r) - w);
                    y0 = r - w;
                }
            }
            y1 = (r - y0) - w;
            if (x < 0) { y0 = -y0; y1 = -y1; return -n; }
            return n;
        }

        public static double Sin(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return double.NaN;
            if (Math.Abs(x) <= 0.7853981633974483) return Math.Abs(x) < 7.450580596923828e-9 ? x : KernelSin(x, 0, false);
            int n = RemPio2(x, out double y0, out double y1);
            switch (n & 3)
            {
                case 0: return KernelSin(y0, y1, true);
                case 1: return KernelCos(y0, y1);
                case 2: return -KernelSin(y0, y1, true);
                default: return -KernelCos(y0, y1);
            }
        }

        public static double Cos(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return double.NaN;
            if (Math.Abs(x) <= 0.7853981633974483) return Math.Abs(x) < 7.450580596923828e-9 ? 1.0 : KernelCos(x, 0);
            int n = RemPio2(x, out double y0, out double y1);
            switch (n & 3)
            {
                case 0: return KernelCos(y0, y1);
                case 1: return -KernelSin(y0, y1, true);
                case 2: return -KernelCos(y0, y1);
                default: return KernelSin(y0, y1, true);
            }
        }
    }
}
