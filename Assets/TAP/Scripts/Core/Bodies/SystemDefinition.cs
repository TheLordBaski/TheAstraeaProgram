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
        /// <summary>
        /// Direction *towards* the sun in the inertial frame, for a system without a star body. With a star (a body of
        /// type "star", the root), sunlight comes from the star's actual position.
        /// </summary>
        public double[] direction = { 1, 0.15, -0.35 };
        public float intensity = 1.35f;
        public float[] color = { 1f, 0.97f, 0.92f };
    }

    [Serializable]
    public class LaunchSiteDefinition
    {
        public string body = "tellus";
        public string name = "Astraea Launch Complex";
        /// <summary>
        /// The flat area of the body's terrain the site stands on. When set, the site's position and pad altitude are
        /// the flat area's.
        /// </summary>
        public string flatArea;
        /// <summary>Geographic latitude (north positive) and longitude (east positive), degrees.</summary>
        public double latitude;
        public double longitude;
        /// <summary>Terrain height of the flattened pad area above sea level (m).</summary>
        public double padAltitude = 70;
        /// <summary>Height of the launch pad deck above padAltitude (m).</summary>
        public double padDeckHeight = 1.0;

        /// <summary>Body-fixed unit direction of the pad.</summary>
        [Newtonsoft.Json.JsonIgnore]
        public Vector3d UpBF => Geo.FromLatLon(latitude, longitude);
    }

    [Serializable]
    public class BodyDefinition
    {
        public string id;
        public string displayName;
        public string description;
        /// <summary>"star", "planet", "moon", "dwarf" or "gasgiant". A star has no terrain and lights the system.</summary>
        public string type = "planet";
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
        /// <summary>Stars: radiated power (W); sets the solar flux at any distance.</summary>
        public double luminosity;
        /// <summary>Stars: surface temperature (K), for the star's colour and glare.</summary>
        public double surfaceTemperature;
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
}
