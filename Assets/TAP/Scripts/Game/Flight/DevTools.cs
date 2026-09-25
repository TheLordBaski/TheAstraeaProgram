using System;
using System.Collections.Generic;
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
            Vector3d pos = up * r, vel = dir * Math.Sqrt(body.GM / r);
            Quaternion rot = Quaternion.LookRotation(-(Vector3)up, (Vector3)dir); // nose prograde, belly to the ground
            Teleport(sim, h, body, rec =>
            {
                rec.situation = Situation.Orbiting;
                rec.orbitPos = new[] { pos.x, pos.y, pos.z };
                rec.orbitVel = new[] { vel.x, vel.y, vel.z };
                rec.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
            });
            return $"{h.Name}: circular orbit of {body.Name}, {altitude / 1000:F0} km, inclination {inclinationDeg:0}°";
        }

        /// <summary>
        /// Lands the active vessel near a geographic latitude/longitude (north and east positive): on the nearest ground
        /// within 400 m level enough for it to stand (a tall rocket topples on a slope), square to the ground, landing
        /// legs deployed, its base 0.2 m above the highest ground under it and at rest with the surface. It loads like a
        /// landed vessel from a save.
        /// </summary>
        public static string PutDown(FlightSim sim, CelestialBody body, double latDeg, double lonDeg)
        {
            var h = sim.ActiveHandle;
            if (h == null) return "No active vessel";
            if (latDeg < -90 || latDeg > 90) return "The latitude must be between -90° and 90°";
            float below = 1f, radius = 1f;
            if (h.Loaded != null)
            {
                DeployLegs(h.Loaded);
                Footprint(h.Loaded, out below, out radius);
            }
            Vector3d dirBF = Geo.FromLatLon(latDeg, lonDeg);
            Vector3d posBF, upBF;
            string where = "";
            bool sea = body.HasOcean && body.Terrain != null && body.Terrain.Height(dirBF) < 0;
            if (sea || body.Terrain == null)
            {
                upBF = dirBF;
                posBF = dirBF * (body.Radius + below + 0.2);
                if (sea) where = "on the sea";
            }
            else
            {
                // Stands if the slope stays well inside the tipping angle (base radius over centre-of-mass height): a tall
                // rocket on its engine bell needs almost flat ground, a lander on its legs takes up to 5°.
                double maxSlope = MathD.Clamp(0.25 * Math.Atan2(radius, below) * MathD.Rad2Deg, 0.75, 5.0);
                var g = FindLevelSpot(body, dirBF, radius, maxSlope, out double moved);
                upBF = g.Normal;
                posBF = g.Dir * (body.Radius + g.Height) + upBF * (g.Clearance + 0.2 + below);
                dirBF = g.Dir;
                Geo.ToLatLon(g.Dir, out latDeg, out lonDeg);
                where = (moved > 0 ? $"moved {moved:F0} m to level ground, " : "") + $"slope {g.SlopeDeg:F1}°";
            }
            // Square to the ground with the belly (+Z) to the north, as on the pad (north is the same in body-fixed axes).
            Vector3d north = Vector3d.ProjectOnPlane(Geo.North(dirBF), upBF).normalized;
            Quaternion rotBF = Quaternion.LookRotation((Vector3)north, (Vector3)upBF);
            Teleport(sim, h, body, rec =>
            {
                rec.situation = sea ? Situation.Splashed : Situation.Landed;
                rec.landed = true;
                rec.landedPos = new[] { posBF.x, posBF.y, posBF.z };
                rec.landedRot = new[] { rotBF.x, rotBF.y, rotBF.z, rotBF.w };
                rec.rotation = new[] { rotBF.x, rotBF.y, rotBF.z, rotBF.w };
            });
            return $"{h.Name}: landed on {body.Name} at {latDeg:0.####}°, {lonDeg:0.####}°" + (where.Length > 0 ? $" ({where})" : "");
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

        /// <summary>
        /// Replaces the vessel's state: it is unloaded (kept even inside an atmosphere), <paramref name="place"/> writes the
        /// new position into its record, and it is rebuilt from the record, keeping parts, resources and crew.
        /// </summary>
        private static void Teleport(FlightSim sim, VesselHandle h, CelestialBody body, Action<VesselRecord> place)
        {
            sim.StopWarp();
            if (h.Loaded != null)
            {
                if (h.Loaded.PadHold) sim.ReleasePadHold(h.Loaded);
                sim.UnloadHandle(h, mayBeLost: false);
            }
            var rec = h.Record;
            rec.bodyId = body.Id;
            if (rec.launchUT < 0) rec.launchUT = sim.UT;
            rec.landed = false;
            rec.landedPos = null;
            rec.landedRot = null;
            rec.orbitPos = null;
            rec.orbitVel = null;
            rec.orbitEpoch = sim.UT;
            rec.angularVelocity = new[] { 0f, 0f, 0f };
            rec.maneuverNodes.Clear();
            place(rec);
            sim.Handles.Remove(h);
            var nh = sim.CreateHandle(rec);
            sim.Handles.Add(nh);
            sim.SwitchTo(nh);
        }

        /// <summary>Extends the landing legs at once (they are saved deployed with the vessel).</summary>
        private static void DeployLegs(Vessel v)
        {
            bool any = false;
            foreach (var p in v.Parts)
                foreach (var m in p.Modules)
                    if (m is LandingLegModule leg && !leg.Broken)
                    {
                        leg.SetDeployed(true);
                        leg.DeployProgress = 1f;
                        any = true;
                    }
            if (any && v.Ctrl != null) v.Ctrl.LegsDeployed = true;
        }

        /// <summary>
        /// How far the vessel reaches below its centre of mass along its root's up axis, and the radius of the base it
        /// stands on: the circle inside the box of every solid collider reaching within 0.3 m of the bottom (an engine
        /// bell's rim, not its box corners) and the deployed landing-leg feet, in root space. Independent of how the
        /// vessel is oriented now.
        /// </summary>
        private static void Footprint(Vessel v, out float below, out float radius)
        {
            Transform root = v.transform;
            Vector3 com = v.LocalCenterOfMass;
            var boxes = new List<(Vector3 min, Vector3 max)>();
            foreach (var c in v.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger || !LocalBounds(c, out Bounds b)) continue;
                Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 q = root.InverseTransformPoint(c.transform.TransformPoint(corner));
                    mn = Vector3.Min(mn, q);
                    mx = Vector3.Max(mx, q);
                }
                boxes.Add((mn, mx));
            }
            var feet = new List<Vector3>();
            foreach (var p in v.Parts)
                foreach (var m in p.Modules)
                    if (m is LandingLegModule leg && !leg.Broken)
                        feet.Add(root.InverseTransformPoint(leg.DeployedFootBottom));
            float lowest = com.y - 0.3f;
            foreach (var (mn, _) in boxes) lowest = Mathf.Min(lowest, mn.y);
            foreach (var f in feet) lowest = Mathf.Min(lowest, f.y);
            below = com.y - lowest;
            radius = 0.3f;
            foreach (var (mn, mx) in boxes)
            {
                if (mn.y > lowest + 0.3f) continue;
                Vector2 centre = new Vector2((mn.x + mx.x) * 0.5f - com.x, (mn.z + mx.z) * 0.5f - com.z);
                float inner = Mathf.Min(mx.x - mn.x, mx.z - mn.z) * 0.5f;
                radius = Mathf.Max(radius, centre.magnitude + inner);
            }
            foreach (var f in feet)
                if (f.y < lowest + 0.3f) radius = Mathf.Max(radius, new Vector2(f.x - com.x, f.z - com.z).magnitude);
        }

        /// <summary>The ground under a circular footprint, as a best-fit plane.</summary>
        private struct Ground
        {
            public Vector3d Dir;       // body-fixed direction of the footprint centre
            public Vector3d Normal;    // body-fixed normal of the plane
            public double Height;      // plane height above the radius at the centre (m)
            public double SlopeDeg;
            public double Clearance;   // highest ground above the plane (m)
            public double Bumps;       // ground spread the plane doesn't explain (m)
        }

        /// <summary>
        /// The nearest spot within 400 m where the ground under a footprint of <paramref name="radius"/> is level enough
        /// (slope under <paramref name="maxSlopeDeg"/>, bumps under a fifth of the radius), or the most level spot found.
        /// </summary>
        private static Ground FindLevelSpot(CelestialBody body, Vector3d dir, double radius, double maxSlopeDeg, out double moved)
        {
            Vector3d north = Geo.North(dir), east = Geo.East(dir);
            double maxBumps = Math.Max(0.1, Math.Min(0.35, radius * 0.2));
            var best = default(Ground);
            double bestScore = double.MaxValue;
            moved = 0;
            foreach (double dist in new double[] { 0, 12, 25, 45, 70, 100, 140, 190, 250, 320, 400 })
            {
                int n = dist == 0 ? 1 : Math.Max(8, (int)(2 * Math.PI * dist / 25));
                for (int i = 0; i < n; i++)
                {
                    double a = 2 * Math.PI * i / n;
                    Vector3d c = (dir + (east * Math.Cos(a) + north * Math.Sin(a)) * (dist / body.Radius)).normalized;
                    var g = Survey(body, c, radius);
                    if (g.SlopeDeg < maxSlopeDeg && g.Bumps < maxBumps) { moved = dist; return g; }
                    double score = g.SlopeDeg / maxSlopeDeg + g.Bumps / maxBumps;
                    if (score < bestScore) { bestScore = score; best = g; moved = dist; }
                }
            }
            return best;
        }

        /// <summary>Fits a plane to the ground under a circular footprint (centre, and rings at the radius and half of it).</summary>
        private static Ground Survey(CelestialBody body, Vector3d c, double radius)
        {
            var terrain = body.Terrain;
            Vector3d north = Geo.North(c), east = Geo.East(c);
            double r = Math.Max(radius, 1.0);
            const int n = 12;
            var xs = new double[2 * n + 1];
            var ys = new double[2 * n + 1];
            var hs = new double[2 * n + 1];
            hs[0] = terrain.Height(c);
            for (int ring = 0; ring < 2; ring++)
            {
                double rr = ring == 0 ? r : r * 0.5;
                for (int i = 0; i < n; i++)
                {
                    double a = 2 * Math.PI * (i + 0.5 * ring) / n;
                    int k = 1 + ring * n + i;
                    xs[k] = Math.Cos(a) * rr;
                    ys[k] = Math.Sin(a) * rr;
                    hs[k] = terrain.Height((c + (east * xs[k] + north * ys[k]) / body.Radius).normalized);
                }
            }
            // Least squares on symmetric rings: the mean is the centre height, the gradient comes from sum(h p) / sum(p^2).
            double mean = 0, sx = 0, sy = 0, sxx = 0;
            for (int k = 0; k < hs.Length; k++) { mean += hs[k]; sx += hs[k] * xs[k]; sy += hs[k] * ys[k]; sxx += xs[k] * xs[k]; }
            mean /= hs.Length;
            double gx = sx / sxx, gy = sy / sxx; // sum(x^2) == sum(y^2) on symmetric rings
            double above = double.MinValue, belowPlane = double.MaxValue;
            for (int k = 0; k < hs.Length; k++)
            {
                double d = hs[k] - (mean + gx * xs[k] + gy * ys[k]);
                above = Math.Max(above, d);
                belowPlane = Math.Min(belowPlane, d);
            }
            return new Ground
            {
                Dir = c,
                Normal = (c - east * gx - north * gy).normalized,
                Height = mean,
                SlopeDeg = Math.Atan(Math.Sqrt(gx * gx + gy * gy)) * MathD.Rad2Deg,
                Clearance = Math.Max(0, above),
                Bumps = above - belowPlane,
            };
        }

        /// <summary>A collider's box in its own transform's space.</summary>
        private static bool LocalBounds(Collider c, out Bounds b)
        {
            switch (c)
            {
                case BoxCollider box:
                    b = new Bounds(box.center, box.size);
                    return true;
                case SphereCollider s:
                    b = new Bounds(s.center, Vector3.one * (2 * s.radius));
                    return true;
                case CapsuleCollider cap:
                    Vector3 size = Vector3.one * (2 * cap.radius);
                    size[cap.direction] = Mathf.Max(cap.height, 2 * cap.radius);
                    b = new Bounds(cap.center, size);
                    return true;
                case MeshCollider mc when mc.sharedMesh != null:
                    b = mc.sharedMesh.bounds;
                    return true;
            }
            b = default;
            return false;
        }
    }
}
