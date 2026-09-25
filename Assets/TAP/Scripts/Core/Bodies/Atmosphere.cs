using System;

namespace TAP.Core
{
    /// <summary>
    /// Exponential-pressure atmosphere with a piecewise-linear temperature profile.
    /// Density is derived from the ideal gas law so drag, heating and Mach number are consistent.
    /// Above <see cref="Height"/> the atmosphere is treated as vacuum (with a smooth taper over the top 8%).
    /// </summary>
    public sealed class Atmosphere
    {
        public readonly double Height;
        public readonly double SeaLevelPressure;
        public readonly double ScaleHeight;
        public readonly double MolarMass;
        public readonly double AdiabaticIndex;
        public readonly double SeaLevelDensity;
        private readonly double[] _tAlt;
        private readonly double[] _tK;

        public Atmosphere(AtmosphereDefinition def)
        {
            Height = def.height;
            SeaLevelPressure = def.seaLevelPressure;
            ScaleHeight = def.scaleHeight;
            MolarMass = def.molarMass;
            AdiabaticIndex = def.adiabaticIndex;
            if (def.temperatureCurve != null && def.temperatureCurve.Length > 0)
            {
                _tAlt = new double[def.temperatureCurve.Length];
                _tK = new double[def.temperatureCurve.Length];
                for (int i = 0; i < def.temperatureCurve.Length; i++)
                {
                    _tAlt[i] = def.temperatureCurve[i][0];
                    _tK[i] = def.temperatureCurve[i][1];
                }
            }
            else
            {
                _tAlt = new[] { 0.0, Height };
                _tK = new[] { 288.15, 220.0 };
            }
            SeaLevelDensity = Density(0);
        }

        /// <summary>Static pressure (Pa) at altitude above the datum.</summary>
        public double Pressure(double altitude)
        {
            if (altitude >= Height) return 0;
            if (altitude < -5000) altitude = -5000;
            double p = SeaLevelPressure * Math.Exp(-altitude / ScaleHeight);
            double taperStart = Height * 0.92;
            if (altitude > taperStart)
                p *= 1.0 - MathD.SmoothStep(taperStart, Height, altitude);
            return p;
        }

        public double Temperature(double altitude)
        {
            if (altitude <= _tAlt[0]) return _tK[0];
            int last = _tAlt.Length - 1;
            if (altitude >= _tAlt[last]) return _tK[last];
            for (int i = 1; i <= last; i++)
            {
                if (altitude <= _tAlt[i])
                {
                    double t = (altitude - _tAlt[i - 1]) / (_tAlt[i] - _tAlt[i - 1]);
                    return _tK[i - 1] + (_tK[i] - _tK[i - 1]) * t;
                }
            }
            return _tK[last];
        }

        /// <summary>Air density (kg/m^3).</summary>
        public double Density(double altitude)
        {
            double p = Pressure(altitude);
            if (p <= 0) return 0;
            return p * MolarMass / (MathD.GasConstant * Temperature(altitude));
        }

        public double SpeedOfSound(double altitude)
        {
            return Math.Sqrt(AdiabaticIndex * MathD.GasConstant * Temperature(altitude) / MolarMass);
        }

        /// <summary>Altitude at which static pressure equals p (inverse of Pressure, ignoring taper).</summary>
        public double AltitudeForPressure(double p)
        {
            if (p <= 0) return Height;
            return -ScaleHeight * Math.Log(p / SeaLevelPressure);
        }
    }
}
