using System;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Developer tools used by the developer window: teleports and refills for the active vessel.</summary>
    public static class DevTools
    {
        /// <summary>Puts the active vessel into a circular, prograde orbit around a body (inclination in degrees).</summary>
        public static string SetOrbit(FlightSim sim, CelestialBody body, double altitude, double inclinationDeg)
        {
            var h = sim.ActiveHandle;
            if (h == null) return "No active vessel";
            double minAlt = (body.Atmosphere != null ? body.Atmosphere.Height : Math.Max(0, body.Terrain != null ? body.Terrain.MaxHeight : 0)) + 1000;
            if (altitude < minAlt) return $"The orbit must be above {minAlt / 1000:F0} km at {body.Name}";
            if (body.Radius + altitude > body.SOIRadius * 0.9) return $"That is outside {body.Name}'s sphere of influence";
            // Start above the vessel's current position when it is at that body (projected onto the equator).
            Vector3d rel = h.AbsolutePosition(sim.UT) - body.GetPositionAtUT(sim.UT);
            Vector3d up = Vector3d.ProjectOnPlane(rel, Geo.NorthPole);
            up = up.sqrMagnitude > 1 ? up.normalized : Vector3d.right;
            double r = body.Radius + altitude;
            double inc = inclinationDeg * MathD.Deg2Rad;
            Vector3d dir = Geo.East(up) * Math.Cos(inc) + Geo.North(up) * Math.Sin(inc);
            Quaternion rot = Quaternion.LookRotation(-(Vector3)up, (Vector3)dir); // nose prograde, belly to the ground
            Teleport(sim, h, body, up * r, dir * Math.Sqrt(body.GM / r), rot, Situation.Orbiting);
            return $"{h.Name}: circular orbit of {body.Name}, {altitude / 1000:F0} km, inclination {inclinationDeg:0}°";
        }

        /// <summary>Stands the active vessel upright just above the ground at a latitude/longitude, at rest on the surface.</summary>
        public static string PutDown(FlightSim sim, CelestialBody body, double latDeg, double lonDeg)
        {
            var h = sim.ActiveHandle;
            if (h == null) return "No active vessel";
            float below = h.Loaded != null ? ExtentBelowCentreOfMass(h.Loaded) : 1f;
            Vector3d dirBF = TerrainGenerator.DirectionFromLatLon(latDeg, lonDeg);
            double ground = body.Terrain != null ? body.Terrain.Height(dirBF) : 0;
            if (body.HasOcean && ground < 0) ground = 0;
            Vector3d pos = body.BodyFixedToInertial(dirBF * (body.Radius + ground + below + 0.5), sim.UT);
            Vector3d up = pos.normalized;
            Quaternion rot = Quaternion.LookRotation((Vector3)Geo.North(up), (Vector3)up); // upright, right side east
            Teleport(sim, h, body, pos, body.FrameVelocityAt(pos), rot, Situation.Flying);
            return $"{h.Name}: put down on {body.Name} at {latDeg:0.##}°, {lonDeg:0.##}°";
        }

        /// <summary>Fills every tank of the active vessel.</summary>
        public static string Refill(FlightSim sim)
        {
            var v = sim.ActiveVessel;
            if (v == null) return "No active vessel";
            foreach (var p in v.Parts)
                foreach (var r in p.Resources) r.Amount = r.Max;
            return $"{v.VesselName}: all tanks full";
        }

        /// <summary>Replaces the vessel's state (it is rebuilt from its record, keeping parts, resources and crew).</summary>
        private static void Teleport(FlightSim sim, VesselHandle h, CelestialBody body, Vector3d pos, Vector3d vel, Quaternion rot, Situation situation)
        {
            sim.StopWarp();
            if (h.Loaded != null)
            {
                if (h.Loaded.PadHold) sim.ReleasePadHold(h.Loaded);
                sim.UnloadHandle(h);
            }
            var rec = h.Record;
            rec.bodyId = body.Id;
            rec.landed = false;
            rec.landedPos = null;
            rec.landedRot = null;
            rec.situation = situation;
            if (rec.launchUT < 0) rec.launchUT = sim.UT;
            rec.orbitPos = new[] { pos.x, pos.y, pos.z };
            rec.orbitVel = new[] { vel.x, vel.y, vel.z };
            rec.orbitEpoch = sim.UT;
            rec.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
            rec.angularVelocity = new[] { 0f, 0f, 0f };
            rec.maneuverNodes.Clear();
            sim.Handles.Remove(h);
            var nh = sim.CreateHandle(rec);
            sim.Handles.Add(nh);
            sim.SwitchTo(nh);
        }

        /// <summary>How far the vessel reaches below its centre of mass along its own long axis.</summary>
        private static float ExtentBelowCentreOfMass(Vessel v)
        {
            Vector3 com = v.WorldCoM;
            Vector3 down = v.transform.rotation * Vector3.down;
            float max = 0.5f;
            foreach (var r in v.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                Vector3 e = b.extents;
                float d = Vector3.Dot(b.center - com, down) + Mathf.Abs(down.x) * e.x + Mathf.Abs(down.y) * e.y + Mathf.Abs(down.z) * e.z;
                max = Mathf.Max(max, d);
            }
            return max;
        }
    }
}
