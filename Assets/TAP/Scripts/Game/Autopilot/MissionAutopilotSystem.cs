using System;
using System.Collections;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Missions across the star system (FND-01): leaving the home planet's sphere of influence and coming back.</summary>
    public sealed partial class MissionAutopilot
    {
        /// <summary>
        /// A probe leaves the home planet on a hyperbola whose solar orbit has the planet's own period (1:1 resonance),
        /// so it drifts out, circles the star for a year at maximum warp and meets the planet again. The departure is
        /// searched with the patched-conic predictor until it shows the return encounter; the flight then checks every
        /// transition against the prediction: rails and sphere-of-influence changes in both directions over the
        /// longest distances in the home system.
        /// </summary>
        private IEnumerator EscapeMission()
        {
            SetPhase("escape planning");
            var sys = Sim.System;
            var home = sys.HomeBody;
            var star = sys.Star;
            if (star == null || home.Parent != star)
            {
                Check("home planet orbits a star", false, star == null ? "no star in this system" : $"{home.Name} orbits {home.Parent?.Name}");
                yield break;
            }
            double ut0 = Sim.UT + 60;
            if (!FindResonantDeparture(home, star, ut0, out Vector3d pos, out Vector3d vel, out List<OrbitPatch> plan, out string how))
            {
                Check("return trajectory found by patched-conic search", false, how);
                yield break;
            }
            var exitPatch = plan[0];
            var solarPatch = plan[1];
            double tExit = exitPatch.EndUT, tReturn = solarPatch.EndUT;
            Check("return trajectory found by patched-conic search", true,
                $"{how}; leaves {home.Name}'s sphere after {(tExit - ut0) / 3600:F1} h, back after {(tReturn - ut0) / home.RotationPeriod:F1} {home.Name} days, periapsis {(plan[2].Orbit.PeriapsisRadius - home.Radius) / 1000:F0} km");

            // The probe starts at the planned periapsis (no engine needed: this checks the simulation, not a burn).
            var o0 = new Orbit(pos, vel, ut0, home.GM);
            o0.GetStateAtUT(Sim.UT, out Vector3d p, out Vector3d v);
            var h = CreateOrbitalHandle(TestCraft.DockTarget(PartDatabase.Instance), "Wanderer", p, v,
                Quaternion.LookRotation((Vector3)v.normalized, (Vector3)p.normalized));
            if (!Sim.SwitchTo(h)) { Check("probe on its departure hyperbola", false, "could not switch to the probe"); yield break; }
            yield return WaitSecondsAny(1);
            Check("probe on its departure hyperbola", V != null && V.MainBody == home && V.Orbit != null && V.Orbit.Eccentricity > 1,
                $"e {V?.Orbit?.Eccentricity:F3}, {V?.Orbit?.PeriapsisRadius / 1000 - home.Radius / 1000:F0} km periapsis");

            SetPhase("leaving the home planet");
            yield return WarpUntil(tExit + 3600, $"leaving {home.Name}'s sphere of influence");
            bool inStarFrame = Sim.Frame.Body == star && V != null && V.MainBody == star;
            Vector3d actualExit = V != null ? V.TruePosition : Vector3d.zero; // relative to the star
            Vector3d plannedExit = solarPatch.Orbit.GetPositionAtUT(Sim.UT);
            double exitError = (actualExit - plannedExit).magnitude;
            Check($"left {home.Name}'s sphere of influence into {star.Name}'s", inStarFrame && exitError < 1000,
                $"frame {Sim.Frame.Body.Name}, {(actualExit - (home.GetPositionAtUT(Sim.UT) - star.GetPositionAtUT(Sim.UT))).magnitude / 1000:F0} km from {home.Name}, " +
                $"{exitError:F1} m from the prediction");
            if (!inStarFrame) yield break;
            var solar = V.Orbit;
            double periodRatio = solar.Period / home.Orbit.Period;
            Check($"solar orbit resonant with {home.Name}", Math.Abs(periodRatio - 1) < 0.002,
                $"period {solar.Period / home.RotationPeriod:F2} vs {home.Orbit.Period / home.RotationPeriod:F2} {home.Name} days, e {solar.Eccentricity:F4}");

            SetPhase("a year around the star");
            double tCoast = Sim.UT;
            yield return WarpUntil(tReturn + 3600, $"back to {home.Name}");
            bool back = V != null && Sim.Frame.Body == home && V.MainBody == home;
            double years = (Sim.UT - tCoast) / home.Orbit.Period;
            Check($"back in {home.Name}'s sphere of influence after a year", back && years > 0.9,
                $"frame {Sim.Frame.Body.Name} after {years:F2} years at warp");
            if (!back) yield break;
            var arrival = V.Orbit;
            double pePlanned = plan[2].Orbit.PeriapsisRadius, peActual = arrival.PeriapsisRadius;
            Check("arrival matches the prediction", Math.Abs(peActual - pePlanned) < 0.01 * home.SOIRadius,
                $"periapsis {(peActual - home.Radius) / 1000:F0} km vs {(pePlanned - home.Radius) / 1000:F0} km planned");
        }

        /// <summary>
        /// Searches a departure from a low circular orbit whose patched-conic prediction leaves the planet's sphere of
        /// influence and meets the planet again after one solar orbit. The hyperbolic excess velocity points mostly away
        /// from the star; its along-track part is scanned until the solar period matches the planet's.
        /// </summary>
        private static bool FindResonantDeparture(CelestialBody home, CelestialBody star, double ut, out Vector3d pos, out Vector3d vel,
                                                  out List<OrbitPatch> plan, out string how)
        {
            pos = vel = Vector3d.zero;
            plan = null;
            how = "no departure found";
            Vector3d rHome = home.GetPositionAtUT(ut) - star.GetPositionAtUT(ut);
            Vector3d vHome = home.GetVelocityAtUT(ut) - star.GetVelocityAtUT(ut);
            Vector3d outward = rHome.normalized, along = vHome.normalized;
            Vector3d n = Vector3d.Cross(rHome, vHome).normalized;
            double r0 = home.Radius + 120000;
            double best = double.MaxValue;
            foreach (double vR in new[] { 350.0, 500.0, 700.0 })
            {
                for (double vT = -300; vT <= 300; vT += 1.0)
                {
                    Vector3d vinf = outward * vR + along * vT;
                    double vi = vinf.magnitude;
                    Vector3d d = vinf / vi;
                    double e = 1 + r0 * vi * vi / home.GM;
                    double thetaInf = Math.Acos(-1 / e);
                    // Periapsis direction: the asymptote rotated back by the asymptote angle, in the prograde sense.
                    Vector3d pDir = d * Math.Cos(thetaInf) - Vector3d.Cross(n, d) * Math.Sin(thetaInf);
                    double vp = Math.Sqrt(vi * vi + 2 * home.GM / r0);
                    Vector3d p0 = pDir * r0, v0 = Vector3d.Cross(n, pDir) * vp;
                    var patches = PatchedConics.Predict(new Orbit(p0, v0, ut, home.GM), home, ut, new List<NodeSpec>(), 3);
                    if (patches.Count < 3 || patches[0].EndType != PatchEnd.SoiExit || patches[1].EndType != PatchEnd.SoiEnter
                        || patches[1].NextBody != home) continue;
                    double pe = patches[2].Orbit.PeriapsisRadius;
                    double score = Math.Abs(pe - home.SOIRadius * 0.3); // a comfortable pass well inside the sphere
                    if (score < best)
                    {
                        best = score;
                        pos = p0;
                        vel = v0;
                        plan = patches;
                        how = $"excess speed {vi:F0} m/s ({vR:F0} outward, {vT:+0;-0} along track)";
                    }
                }
                if (plan != null) return true;
            }
            return false;
        }
    }
}
