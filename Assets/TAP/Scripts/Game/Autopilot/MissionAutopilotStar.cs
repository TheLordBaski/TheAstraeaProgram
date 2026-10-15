using System;
using System.Collections;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Orbiting the star: low circular orbits flown in physics and on rails must follow Kepler exactly. Then its heat: a
    /// probe 300 Mm from the star burns up, one beyond the burn radius lives, and the photosphere kills whatever reaches it.
    /// </summary>
    public sealed partial class MissionAutopilot
    {
        private IEnumerator StarMission()
        {
            var star = Sim.System.Star;
            if (star == null) { Check("the system has a star", false, "no star body"); yield break; }
            var db = PartDatabase.Instance;
            ExpectLosses = true;
            CaptureMessages();
            DevCheats.IgnoreHeat = true; // the orbits are about Kepler, not survival: the heat comes after them
            // A low orbit 60 Mm up (a quarter of a revolution at 50x, the warp allowed there) and a high one 400 Mm up (a whole
            // revolution at 1,000x).
            foreach (var (altitude, revolutions) in new[] { (60e6, 0.25), (400e6, 1.0) })
            {
                SetPhase($"orbit {altitude / 1e6:F0} Mm above {star.Name}");
                double r = star.Radius + altitude;
                double vc = Math.Sqrt(star.GM / r);
                var planeUp = Vector3d.up;
                var p = new Vector3d(r, 0, 0);
                var v = Vector3d.Cross(planeUp, p).normalized * vc;
                var h = CreateOrbitalHandle(TestCraft.DockTarget(db), "Sunskimmer", p, v, Quaternion.LookRotation((Vector3)v.normalized, (Vector3)p.normalized), star);
                if (!Sim.SwitchTo(h)) { Check("probe in orbit of the star", false, "could not switch to the probe"); yield break; }
                yield return WaitSecondsAny(2);
                var probe = V;
                Check($"probe orbits {star.Name} at {altitude / 1e6:F0} Mm", probe != null && probe.MainBody == star && probe.Orbit != null && probe.Situation == Situation.Orbiting,
                    $"frame {Sim.Frame.Body.Name}, situation {probe?.Situation}, e {probe?.Orbit?.Eccentricity:E2}");
                if (probe == null) yield break;
                Orbit o = probe.Orbit;
                double period = o.Period;

                // Physics: 30 s of flight stays on the analytic orbit.
                double t0 = Sim.UT;
                yield return WaitSecondsAny(30);
                double drift = (probe.TruePosition - probe.Orbit.GetPositionAtUT(Sim.UT)).magnitude;
                Check("physics flight follows the orbit", drift < 5, $"{drift:F3} m off after {Sim.UT - t0:F0} s");

                // Rails: rails warp must be allowed here, and the revolutions keep the orbit.
                int max = Sim.MaxRailsIndex(out string reason);
                Check($"time warp allowed at {altitude / 1e6:F0} Mm", max >= (altitude < 100e6 ? 3 : 5), $"up to {TimeWarp.RailsRates[max]:F0}x" + (reason != null ? $" ({reason})" : ""));
                yield return WarpUntil(Sim.UT + revolutions * period, $"{revolutions:0.##} revolutions");
                probe = V;
                if (probe == null) yield break;
                double sma0 = o.SemiMajorAxis;
                double err = (probe.TruePosition - probe.Orbit.GetPositionAtUT(Sim.UT)).magnitude;
                Check($"{revolutions:0.##} revolutions on rails keep the orbit", probe.MainBody == star && Math.Abs(probe.Orbit.SemiMajorAxis - sma0) < 1 && err < 5,
                    $"a {probe.Orbit.SemiMajorAxis / 1e6:F3} Mm vs {sma0 / 1e6:F3} Mm, {err:F2} m off, {(Sim.UT - t0) / period:F2} periods");
                Check("still orbiting after the warp", probe.Situation == Situation.Orbiting, probe.Situation.ToString());
                Snap($"star_{altitude / 1e6:F0}Mm");
                // The map: the orbit line round the star, seen from far enough to take in the star.
                Scene.SetMap(true);
                Scene.Map.FocusOn(Sim.ActiveHandle);
                Scene.Map.ZoomTo((float)((star.Radius + altitude) * 3.2 / MapView.Scale));
                float until = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < until) yield return null;
                Snap($"star_map_{altitude / 1e6:F0}Mm");
                Check("the map shows the orbit round the star", Scene.Map.Active && Scene.Trajectory != null, $"map open, focus {Scene.Map.FocusVessel?.Name}, camera {Scene.Map.CamDistance:F0} units out, flight camera {(Scene.Camera.Cam.enabled ? "on" : "off")}");
                Scene.SetMap(false);
                yield return WaitSecondsAny(1);
            }

            // The photosphere: no surface, so a vessel inside it is gone, whatever it is made of.
            SetPhase("photosphere");
            var inside = CreateOrbitalHandle(TestCraft.DockTarget(db), "Sunfall", new Vector3d(star.Radius - 1e6, 0, 0), new Vector3d(0, 0, 1),
                Quaternion.identity, star);
            if (!Sim.SwitchTo(inside)) { Check("probe in the photosphere", false, "could not switch to the probe"); yield break; }
            var lost = V;
            int parts0 = lost.Parts.Count;
            yield return WaitSecondsAny(5);
            Check("a heat-proof vessel inside the photosphere is destroyed", lost == null || lost.Parts.Count < parts0, $"{(lost == null ? 0 : lost.Parts.Count)} of {parts0} parts left");

            // The heat: 300 Mm from the star's centre (50 Mm above the photosphere), in a circular orbit.
            DevCheats.IgnoreHeat = false;
            SetPhase("heat at 300 Mm");
            double[] distances = { 300e6, 0 };
            double burnRadius = 0;
            foreach (double d0 in distances)
            {
                double d = d0 > 0 ? d0 : burnRadius * 1.25; // second pass: just beyond where the weakest part overheats
                double vc = Math.Sqrt(star.GM / d);
                var hp = CreateOrbitalHandle(TestCraft.DockTarget(db), "Sunburner", new Vector3d(d, 0, 0), new Vector3d(0, 0, vc),
                    Quaternion.LookRotation(Vector3.forward, Vector3.right), star);
                if (!Sim.SwitchTo(hp)) { Check("probe in the heat", false, "could not switch to the probe"); yield break; }
                var hot = V;
                int n0 = hot.Parts.Count;
                burnRadius = Sim.StarBurnRadius(hp);
                int m0 = _messages.Count;
                yield return WaitSecondsAny(2);
                double expectedFlux = star.Luminosity / (4 * Math.PI * d * d);
                Check($"the sunlight {d / 1e6:F0} Mm from the star is L / 4πr²", hot != null && Math.Abs(hot.SolarFlux / expectedFlux - 1) < 0.01,
                    $"{hot?.SolarFlux / 1e3:F0} kW/m² vs {expectedFlux / 1e3:F0} kW/m²");
                if (d0 > 0)
                {
                    Check("the burn radius lies beyond 300 Mm and above the photosphere", burnRadius > d && burnRadius > star.Radius, $"{burnRadius / 1e6:F0} Mm");
                    double t0 = Sim.UT;
                    while (V == hot && hot != null && hot.Parts.Count == n0 && Sim.UT - t0 < 120) yield return null;
                    string msg = FindMessage(m0, "overheated");
                    Check("a probe 300 Mm from the star burns up", hot == null || hot.Parts.Count < n0, msg ?? $"still whole after {Sim.UT - t0:F0} s");
                    SetPhase("heat beyond the burn radius");
                }
                else
                {
                    yield return WaitSecondsAny(60);
                    double skin = 0;
                    if (hot != null) foreach (var p in hot.Parts) skin = Math.Max(skin, p.SkinTemp);
                    Check($"a probe {d / 1e6:F0} Mm from the star (1.25x the burn radius) survives a minute", hot != null && hot.Parts.Count == n0 && FindMessage(m0, "overheated") == null,
                        $"{(hot == null ? 0 : hot.Parts.Count)} of {n0} parts, hottest skin {skin:F0} K");
                }
            }
        }
    }
}
