using System;
using System.Collections.Generic;

namespace TAP.Core
{
    // Plain data classes deserialized from Resources/Data/system.json (Newtonsoft).
    // All units SI (m, s, kg, Pa, K); angles in degrees in the data files.

    [Serializable]
    public class SystemDefinition
    {
        public string name;
        /// <summary>UT at which a new game starts (s).</summary>
        public double startUT;
        public SunDefinition sun = new SunDefinition();
        public LaunchSiteDefinition launchSite = new LaunchSiteDefinition();
        public List<BodyDefinition> bodies = new List<BodyDefinition>();
    }

    [Serializable]
    public class SunDefinition
    {
        /// <summary>Direction *towards* the sun in the inertial frame.</summary>
        public double[] direction = { 1, 0.15, -0.35 };
        public float intensity = 1.35f;
        public float[] color = { 1f, 0.97f, 0.92f };
    }

    [Serializable]
    public class LaunchSiteDefinition
    {
        public string body = "tellus";
        public string name = "Astraea Launch Complex";
        public double latitude;
        public double longitude;
        /// <summary>Terrain height of the flattened pad area above sea level (m).</summary>
        public double padAltitude = 70;
        /// <summary>Radius of the perfectly flat area (m).</summary>
        public double flatRadius = 700;
        /// <summary>Distance over which the flat area blends into natural terrain (m).</summary>
        public double blendRadius = 2500;
        /// <summary>Height of the launch pad deck above padAltitude (m).</summary>
        public double padDeckHeight = 1.0;
    }

    [Serializable]
    public class BodyDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string parent;
        public double radius;
        /// <summary>Standard gravitational parameter GM (m^3/s^2).</summary>
        public double gm;
        /// <summary>Sidereal rotation period (s). Ignored when tidallyLocked.</summary>
        public double rotationPeriod;
        public bool tidallyLocked;
        public double initialRotationDeg;
        public OrbitDefinition orbit;
        public AtmosphereDefinition atmosphere;
        public TerrainDefinition terrain = new TerrainDefinition();
        public bool hasOcean;
        /// <summary>Altitudes (above datum) below which each rails-warp level is disallowed.</summary>
        public double[] warpAltitudeLimits;
        /// <summary>Scaled-space / map colour.</summary>
        public float[] mapColor = { 0.5f, 0.5f, 0.5f };
    }

    [Serializable]
    public class OrbitDefinition
    {
        public double semiMajorAxis;
        public double eccentricity;
        public double inclinationDeg;
        public double lanDeg;
        public double argPeDeg;
        public double meanAnomalyAtEpochDeg;
        public double epoch;
    }

    [Serializable]
    public class AtmosphereDefinition
    {
        public double height = 70000;
        public double seaLevelPressure = 101325;
        public double scaleHeight = 5600;
        public double molarMass = 0.0289644;
        public double adiabaticIndex = 1.4;
        /// <summary>[[altitude m, temperature K], ...] piecewise-linear.</summary>
        public double[][] temperatureCurve;
        public float[] skyColor = { 0.35f, 0.6f, 1f };
        public float[] horizonColor = { 0.75f, 0.85f, 1f };
    }

    [Serializable]
    public class TerrainDefinition
    {
        /// <summary>Generator id: "tellus" (continents/oceans) or "luma" (cratered).</summary>
        public string generator = "flat";
        public int seed = 1;
        /// <summary>Approximate maximum terrain height (m) — used for warp/rails safety and LOD bounds.</summary>
        public double maxHeight = 5000;
        public double minHeight = -3000;
        public double continentScale = 1.0;
        public double mountainHeight = 3000;
        public double craterDepthScale = 1.0;
    }
}
