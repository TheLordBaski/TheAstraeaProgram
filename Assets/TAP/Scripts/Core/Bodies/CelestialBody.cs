using System;
using System.Collections.Generic;

namespace TAP.Core
{
    /// <summary>
    /// Runtime celestial body. Positions returned by <see cref="GetPositionAtUT"/> are relative to the
    /// root body (the home planet) in the shared inertial frame. Body rotation is about +Y.
    /// </summary>
    public sealed class CelestialBody
    {
        public readonly BodyDefinition Def;
        public readonly string Id;
        public readonly string Name;
        public readonly double Radius;
        public readonly double GM;
        public CelestialBody Parent { get; private set; }
        public readonly List<CelestialBody> Children = new List<CelestialBody>();
        public Orbit Orbit { get; private set; }
        public double SOIRadius { get; private set; } = double.PositiveInfinity;
        public double RotationPeriod { get; private set; }
        public readonly double InitialRotation; // rad
        public readonly Atmosphere Atmosphere;
        public TerrainGenerator Terrain { get; private set; }
        public readonly bool HasOcean;
        public readonly double[] WarpAltitudeLimits;

        public bool HasAtmosphere => Atmosphere != null;
        public double Mass => GM / MathD.G;
        public double SurfaceGravity => GM / (Radius * Radius);
        public double AngularSpeed => MathD.TwoPi / RotationPeriod;
        public Vector3d AngularVelocity => Vector3d.up * AngularSpeed;

        public CelestialBody(BodyDefinition def)
        {
            Def = def;
            Id = def.id;
            Name = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
            Radius = def.radius;
            GM = def.gm;
            RotationPeriod = def.rotationPeriod > 0 ? def.rotationPeriod : 86400;
            InitialRotation = def.initialRotationDeg * MathD.Deg2Rad;
            if (def.atmosphere != null && def.atmosphere.height > 0) Atmosphere = new Atmosphere(def.atmosphere);
            HasOcean = def.hasOcean;
            WarpAltitudeLimits = def.warpAltitudeLimits;
        }

        internal void Link(CelestialBody parent, LaunchSiteDefinition site)
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
            Terrain = TerrainGenerator.Create(Def, site);
        }

        // ------------------------------------------------------------------ position

        /// <summary>Position relative to the root body (inertial).</summary>
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

        public Vector3d SurfacePositionBodyFixed(double latDeg, double lonDeg, double altitude)
        {
            Vector3d dir = TerrainGenerator.DirectionFromLatLon(latDeg, lonDeg);
            return dir * (Radius + altitude);
        }

        public double GravityAtRadius(double r) => GM / (r * r);

        /// <summary>Circular orbital speed at a radius.</summary>
        public double CircularSpeed(double r) => Math.Sqrt(GM / r);

        public double EscapeSpeed(double r) => Math.Sqrt(2 * GM / r);

        public override string ToString() => Name;
    }
}
