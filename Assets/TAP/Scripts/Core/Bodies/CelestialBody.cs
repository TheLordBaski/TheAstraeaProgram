using System;
using System.Collections.Generic;

namespace TAP.Core
{
    /// <summary>
    /// Runtime celestial body. Positions returned by <see cref="GetPositionAtUT"/> are relative to the
    /// root body (the system's star) in the shared inertial frame. Body rotation is about +Y.
    /// </summary>
    public sealed class CelestialBody
    {
        public readonly BodyDefinition Def;
        public readonly string Id;
        public string Name { get; private set; }
        public readonly double Radius;
        public readonly double GM;
        public CelestialBody Parent { get; private set; }
        public readonly List<CelestialBody> Children = new List<CelestialBody>();
        public Orbit Orbit { get; private set; }
        public double SOIRadius { get; private set; } = double.PositiveInfinity;
        public double RotationPeriod { get; private set; }
        public readonly double InitialRotation; // rad
        public Atmosphere Atmosphere { get; private set; }
        public TerrainGenerator Terrain { get; private set; }
        public readonly bool HasOcean;
        public double[] WarpAltitudeLimits { get; private set; }
        /// <summary>A star: no terrain or surface, lights the system (<see cref="Luminosity"/>).</summary>
        public readonly bool IsStar;
        /// <summary>Radiated power of a star (W); 0 for other bodies.</summary>
        public double Luminosity { get; private set; }

        /// <summary>
        /// The parameters a body takes while the game runs (F8 hot reload, <see cref="ApplyLive"/>). The others (size,
        /// mass, orbit, spin, terrain) shape everything already flying and take effect when the game starts again.
        /// </summary>
        public static readonly string[] LiveFields =
            { "displayName", "description", "mapColor", "warpAltitudeLimits", "atmosphere", "luminosity", "surfaceTemperature" };

        public bool HasAtmosphere => Atmosphere != null;
        public double Mass => GM / MathD.G;
        public double SurfaceGravity => GM / (Radius * Radius);
        public double AngularSpeed => MathD.TwoPi / RotationPeriod;
        public Vector3d AngularVelocity => Vector3d.up * AngularSpeed;

        /// <summary>
        /// A day seen from the surface: one turn relative to the star (s). Longer than the sidereal
        /// <see cref="RotationPeriod"/> for prograde spin, because the body (or the planet it circles) moves around the
        /// star meanwhile. Equals the rotation period for a star or in a system without one.
        /// </summary>
        public double SolarDay
        {
            get
            {
                var b = this;
                while (b.Parent != null && !b.Parent.IsStar) b = b.Parent;
                if (IsStar || b.Parent == null || b.Orbit == null || !b.Orbit.IsElliptic) return RotationPeriod;
                // Spin and orbits share the +Y axis: a prograde orbit moves the star across the sky against the spin.
                double rate = Math.Abs(1 / RotationPeriod - b.Orbit.W.y / b.Orbit.Period);
                return rate > 1e-15 ? 1 / rate : double.PositiveInfinity;
            }
        }

        public CelestialBody(BodyDefinition def)
        {
            Def = def;
            Id = def.id;
            Radius = def.radius;
            GM = def.gm;
            RotationPeriod = def.rotationPeriod > 0 ? def.rotationPeriod : 86400;
            InitialRotation = def.initialRotationDeg * MathD.Deg2Rad;
            HasOcean = def.hasOcean;
            IsStar = def.type == "star";
            ReadLiveFields();
        }

        private void ReadLiveFields()
        {
            Name = string.IsNullOrEmpty(Def.displayName) ? Def.id : Def.displayName;
            Atmosphere = Def.atmosphere != null && Def.atmosphere.height > 0 ? new Atmosphere(Def.atmosphere) : null;
            WarpAltitudeLimits = Def.warpAltitudeLimits;
            Luminosity = IsStar ? Def.luminosity : 0;
        }

        /// <summary>
        /// Takes the new values of the <see cref="LiveFields"/> (F8 hot reload): the definition is updated in place and the
        /// atmosphere rebuilt, so drag, heating and engines use it from their next step.
        /// </summary>
        public void ApplyLive(BodyDefinition next)
        {
            Def.displayName = next.displayName;
            Def.description = next.description;
            Def.mapColor = next.mapColor;
            Def.warpAltitudeLimits = next.warpAltitudeLimits;
            Def.luminosity = next.luminosity;
            Def.surfaceTemperature = next.surfaceTemperature;
            if (Def.atmosphere != null && next.atmosphere != null) ContentUpdate.CopyInto(Def.atmosphere, next.atmosphere);
            else Def.atmosphere = next.atmosphere;
            ReadLiveFields();
        }

        internal void Link(CelestialBody parent)
        {
            Parent = parent;
            if (parent != null)
            {
                parent.Children.Add(this);
                var o = Def.orbit;
                Orbit = Orbit.FromElements(o.semiMajorAxis, o.eccentricity, o.inclinationDeg * MathD.Deg2Rad,
                    o.lanDeg * MathD.Deg2Rad, o.argPeDeg * MathD.Deg2Rad, o.meanAnomalyAtEpochDeg * MathD.Deg2Rad,
                    o.epoch, parent.GM);
                // Laplace sphere of influence.
                SOIRadius = o.semiMajorAxis * Math.Pow(GM / parent.GM, 0.4);
                if (Def.tidallyLocked) RotationPeriod = Orbit.Period;
            }
            Terrain = IsStar ? null : TerrainGenerator.Create(Def);
        }

        // ------------------------------------------------------------------ position

        /// <summary>Position relative to the root body, the star (inertial).</summary>
        public Vector3d GetPositionAtUT(double ut)
        {
            if (Parent == null) return Vector3d.zero;
            return Parent.GetPositionAtUT(ut) + Orbit.GetPositionAtUT(ut);
        }

        public Vector3d GetVelocityAtUT(double ut)
        {
            if (Parent == null) return Vector3d.zero;
            return Parent.GetVelocityAtUT(ut) + Orbit.GetVelocityAtUT(ut);
        }

        /// <summary>Position of this body relative to another body (inertial).</summary>
        public Vector3d GetPositionRelativeTo(CelestialBody other, double ut) => GetPositionAtUT(ut) - other.GetPositionAtUT(ut);
        public Vector3d GetVelocityRelativeTo(CelestialBody other, double ut) => GetVelocityAtUT(ut) - other.GetVelocityAtUT(ut);

        // ------------------------------------------------------------------ rotation

        public double RotationAngleAtUT(double ut)
        {
            double turns = ut / RotationPeriod;
            turns -= Math.Floor(turns);
            return MathD.WrapTwoPi(InitialRotation + turns * MathD.TwoPi);
        }

        /// <summary>Rotation that maps body-fixed vectors to the inertial frame.</summary>
        public QuaternionD RotationAtUT(double ut) => QuaternionD.AngleAxisRad(RotationAngleAtUT(ut), Vector3d.up);

        public Vector3d BodyFixedToInertial(Vector3d bodyFixed, double ut) => RotationAtUT(ut) * bodyFixed;
        public Vector3d InertialToBodyFixed(Vector3d inertial, double ut) => RotationAtUT(ut).Inverse() * inertial;

        /// <summary>Velocity of the co-rotating surface frame at a body-relative inertial position.</summary>
        public Vector3d FrameVelocityAt(Vector3d relPos) => Vector3d.Cross(AngularVelocity, relPos);

        // ------------------------------------------------------------------ surface

        /// <summary>Terrain height (above datum) under a body-relative inertial position at UT.</summary>
        public double TerrainHeightAt(Vector3d relPosInertial, double ut)
        {
            if (Terrain == null) return 0;
            Vector3d bf = InertialToBodyFixed(relPosInertial, ut).normalized;
            return Terrain.Height(bf);
        }

        /// <summary>Surface altitude (clamped to sea level where oceans exist).</summary>
        public double SurfaceHeightAt(Vector3d relPosInertial, double ut)
        {
            double h = TerrainHeightAt(relPosInertial, ut);
            if (HasOcean && h < 0) h = 0;
            return h;
        }

        /// <summary>The biome at a geographic latitude (north positive) and longitude (degrees).</summary>
        public Biome BiomeAt(double latDeg, double lonDeg) => Terrain?.BiomeAt(Geo.FromLatLon(latDeg, lonDeg));

        /// <summary>The biome under a body-relative inertial position at UT.</summary>
        public Biome BiomeAt(Vector3d relPosInertial, double ut) => Terrain?.BiomeAt(InertialToBodyFixed(relPosInertial, ut).normalized);

        public double GravityAtRadius(double r) => GM / (r * r);

        /// <summary>Circular orbital speed at a radius.</summary>
        public double CircularSpeed(double r) => Math.Sqrt(GM / r);

        public double EscapeSpeed(double r) => Math.Sqrt(2 * GM / r);

        public override string ToString() => Name;
    }
}
