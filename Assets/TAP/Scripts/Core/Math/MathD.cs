using System;

namespace TAP.Core
{
    /// <summary>Double-precision math helpers.</summary>
    public static class MathD
    {
        public const double TwoPi = Math.PI * 2.0;
        public const double Deg2Rad = Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / Math.PI;
        /// <summary>Standard gravity used for Isp conversions (m/s^2).</summary>
        public const double G0 = 9.80665;
        /// <summary>Universal gravitational constant (m^3 kg^-1 s^-2), used only for documentation/derived masses.</summary>
        public const double G = 6.674e-11;
        public const double StefanBoltzmann = 5.670374e-8;
        public const double GasConstant = 8.314462618;

        public static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
        public static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static double InverseLerp(double a, double b, double v) => Math.Abs(b - a) < 1e-300 ? 0 : Clamp01((v - a) / (b - a));
        public static double SmoothStep(double e0, double e1, double x)
        {
            double t = Clamp01((x - e0) / (e1 - e0));
            return t * t * (3 - 2 * t);
        }

        /// <summary>Wraps an angle to [0, 2pi).</summary>
        public static double WrapTwoPi(double a)
        {
            a %= TwoPi;
            if (a < 0) a += TwoPi;
            return a;
        }

        /// <summary>Wraps an angle to (-pi, pi].</summary>
        public static double WrapPi(double a)
        {
            a = WrapTwoPi(a);
            if (a > Math.PI) a -= TwoPi;
            return a;
        }

        public static double Asinh(double x) => Math.Log(x + Math.Sqrt(x * x + 1));
        public static double Acosh(double x) => Math.Log(x + Math.Sqrt(x * x - 1));
        public static double Atanh(double x) => 0.5 * Math.Log((1 + x) / (1 - x));

        /// <summary>Stumpff function C(z).</summary>
        public static double StumpffC(double z)
        {
            if (z > 1e-6)
            {
                double s = Math.Sqrt(z);
                return (1 - Math.Cos(s)) / z;
            }
            if (z < -1e-6)
            {
                double s = Math.Sqrt(-z);
                return (Math.Cosh(s) - 1) / (-z);
            }
            // Series: 1/2 - z/24 + z^2/720 - z^3/40320
            return 0.5 - z / 24.0 + z * z / 720.0 - z * z * z / 40320.0;
        }

        /// <summary>Stumpff function S(z).</summary>
        public static double StumpffS(double z)
        {
            if (z > 1e-6)
            {
                double s = Math.Sqrt(z);
                return (s - Math.Sin(s)) / (s * s * s);
            }
            if (z < -1e-6)
            {
                double s = Math.Sqrt(-z);
                return (Math.Sinh(s) - s) / (s * s * s);
            }
            // Series: 1/6 - z/120 + z^2/5040 - z^3/362880
            return 1.0 / 6.0 - z / 120.0 + z * z / 5040.0 - z * z * z / 362880.0;
        }

        /// <summary>Formats a duration in seconds as a compact d/h/m/s string.</summary>
        public static string FormatDuration(double seconds, bool showSign = false)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--";
            string sign = seconds < 0 ? "-" : (showSign ? "+" : "");
            double s = Math.Abs(seconds);
            long total = (long)Math.Floor(s);
            long d = total / 86400; total %= 86400;
            long h = total / 3600; total %= 3600;
            long m = total / 60; long sec = total % 60;
            if (d > 0) return $"{sign}{d}d {h:00}h {m:00}m";
            if (h > 0) return $"{sign}{h}h {m:00}m {sec:00}s";
            if (m > 0) return $"{sign}{m}m {sec:00}s";
            return $"{sign}{s:0.0}s";
        }

        /// <summary>Formats a distance in metres with an adaptive unit (m, km, Mm).</summary>
        public static string FormatDistance(double meters, int decimals = 1)
        {
            if (double.IsNaN(meters)) return "--";
            if (double.IsInfinity(meters)) return "∞";
            double a = Math.Abs(meters);
            string f = "F" + decimals;
            if (a < 10000) return meters.ToString("F0") + " m";
            if (a < 1e7) return (meters / 1000.0).ToString(f) + " km";
            return (meters / 1e6).ToString(f) + " Mm";
        }

        public static string FormatSpeed(double mps) => double.IsNaN(mps) ? "--" : mps.ToString(Math.Abs(mps) < 100 ? "F1" : "F0") + " m/s";

        /// <summary>Formats universal time as "Y1 D001 00:00:00" using 6-hour days (one Tellus rotation).</summary>
        public static string FormatUT(double ut, double dayLength = 21600)
        {
            if (ut < 0) ut = 0;
            double yearLength = dayLength * 426;
            long y = (long)(ut / yearLength);
            double rem = ut - y * yearLength;
            long d = (long)(rem / dayLength);
            rem -= d * dayLength;
            long h = (long)(rem / 3600); rem -= h * 3600;
            long m = (long)(rem / 60); long s = (long)(rem - m * 60);
            return $"Y{y + 1} D{d + 1:000} {h:00}:{m:00}:{s:00}";
        }
    }
}
