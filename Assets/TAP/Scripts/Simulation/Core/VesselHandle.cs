using TAP.Core;
using TAP.Persistence;

namespace TAP.Simulation
{
    /// <summary>
    /// Every vessel in the universe (loaded or on rails). Unloaded vessels are propagated analytically:
    /// a Kepler orbit relative to their SOI body, or a fixed body-fixed position when landed.
    /// </summary>
    public sealed class VesselHandle
    {
        public VesselRecord Record;
        public Vessel Loaded;
        public CelestialBody Body;
        public Orbit Orbit;
        public bool Landed;
        public Vector3d LandedPosBF;       // CoM in body-fixed frame
        public QuaternionD LandedRotBF = QuaternionD.identity; // root rotation relative to body-fixed frame
        public double LastSoiCheckUT;

        public string Id => Record.id;
        public string Name => Record.name;
        public VesselKind Kind => Record.kind;
        public bool IsLoaded => Loaded != null;

        /// <summary>Position relative to <see cref="Body"/> (inertial) at UT.</summary>
        public Vector3d PositionRelBody(double ut)
        {
            if (Loaded != null && Loaded.Rb != null && !Loaded.Rb.isKinematic)
            {
                var sim = FlightSim.Instance;
                return sim.Frame.ToTrue(Loaded.Rb.worldCenterOfMass) + sim.Frame.BodyPosition(sim.Frame.Body, ut) - sim.Frame.BodyPosition(Body, ut);
            }
            if (Landed) return Body.BodyFixedToInertial(LandedPosBF, ut);
            if (Orbit != null) return Orbit.GetPositionAtUT(ut);
            return Vector3d.zero;
        }

        public Vector3d VelocityRelBody(double ut)
        {
            if (Loaded != null && Loaded.Rb != null && !Loaded.Rb.isKinematic)
                return Loaded.TrueVelocity + FlightSim.Instance.Frame.BodyVelocity(FlightSim.Instance.Frame.Body, ut) - FlightSim.Instance.Frame.BodyVelocity(Body, ut);
            if (Landed) return Body.FrameVelocityAt(Body.BodyFixedToInertial(LandedPosBF, ut));
            if (Orbit != null) return Orbit.GetVelocityAtUT(ut);
            return Vector3d.zero;
        }

        /// <summary>Position relative to the root body at UT.</summary>
        public Vector3d AbsolutePosition(double ut) => Body.GetPositionAtUT(ut) + PositionRelBody(ut);
        public Vector3d AbsoluteVelocity(double ut) => Body.GetVelocityAtUT(ut) + VelocityRelBody(ut);

        public Vector3d PositionRelTo(CelestialBody b, double ut) => AbsolutePosition(ut) - b.GetPositionAtUT(ut);
        public Vector3d VelocityRelTo(CelestialBody b, double ut) => AbsoluteVelocity(ut) - b.GetVelocityAtUT(ut);

        /// <summary>Current orbit relative to Body (null when landed).</summary>
        public Orbit CurrentOrbit
        {
            get
            {
                if (Loaded != null && !Landed) return Loaded.Orbit ?? Orbit;
                return Landed ? null : Orbit;
            }
        }
    }
}
