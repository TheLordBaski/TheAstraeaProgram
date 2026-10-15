using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// A tour of the system for the eyes: a screenshot of every body from a few distances and angles (the flight camera
    /// flies free, the game stands still), so a new world can be looked at without flying there. Editor only; the
    /// pictures go next to the mission report (<c>autotest_shots/tour_*.png</c>). PlayerPrefs <c>TAP.TourBodies</c>
    /// ("magnus,anulus") limits it to some bodies.
    /// </summary>
    public sealed partial class MissionAutopilot
    {
        private readonly struct Shot
        {
            public readonly string Name;
            public readonly double Radii;      // camera distance from the body's centre, in body radii
            public readonly double PhaseDeg;   // angle from the star's direction round the spin axis: 0 full, 180 new
            public readonly double ElevationDeg; // above the equatorial plane
            public Shot(string name, double radii, double phase, double elevation) { Name = name; Radii = radii; PhaseDeg = phase; ElevationDeg = elevation; }
        }

        private IEnumerator TourMission()
        {
            ExpectLosses = true; // each picture takes a probe that is gone afterwards: no active vessel between two of them
            var db = PartDatabase.Instance;
            var only = new HashSet<string>(PlayerPrefs.GetString("TAP.TourBodies", "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            string dir = Path.Combine(SaveStorage.Root, "autotest_shots");
            Directory.CreateDirectory(dir);
            if (!Scene.UiHidden) Scene.Input.ToggleUi?.Invoke();
            Scene.Camera.enabled = false;
            foreach (var b in Sim.System.Bodies)
            {
                if (only.Count > 0 && !only.Contains(b.Id)) continue;
                var shots = new List<Shot>();
                if (b.IsStar) shots.Add(new Shot("far", 6, 0, 10));
                else
                {
                    double near = b.Radius < 100e3 ? 4 : 3;
                    shots.Add(new Shot("near", near, 50, 12));
                    shots.Add(new Shot("far", 14, 35, 0));
                    if (b.Atmosphere != null) shots.Add(new Shot("limb", 1.5, 95, 4));
                    if (b.Rings.Count > 0)
                    {
                        shots.Add(new Shot("rings_high", 3.8, 55, 28));
                        shots.Add(new Shot("rings_edge", 3.2, 75, 3));
                        shots.Add(new Shot("rings_dark", 3.4, 150, 14));
                    }
                }
                foreach (var shot in shots) yield return TourShot(db, b, shot, dir);
                if (Failed) yield break;
            }
            // The map, from the whole system down to a giant's moons.
            if (only.Count == 0 || only.Contains("map"))
            {
                var star = Sim.System.Star;
                Sim.Paused = true;
                Time.timeScale = 0f;
                if (star != null)
                {
                    yield return TourMap("system", star, 9e11, dir);
                    yield return TourMap("inner", star, 4e10, dir);
                }
                foreach (var g in Sim.System.Bodies.FindAll(x => x.IsGasGiant))
                {
                    double reach = 0;
                    foreach (var m in g.Children) reach = Math.Max(reach, m.Orbit.ApoapsisRadius);
                    yield return TourMap("map_" + g.Id, g, Math.Max(reach * 2.6, g.Radius * 8), dir);
                }
                Scene.SetMap(false);
            }
            Check("every body photographed", true, $"{Directory.GetFiles(dir, "tour_*.png").Length} pictures in {dir}");
            Scene.Camera.enabled = true;
            Sim.Paused = false;
            Time.timeScale = 1f;
        }

        private IEnumerator TourShot(PartDatabase db, CelestialBody b, Shot shot, string dir)
        {
            Sim.Paused = false;
            Time.timeScale = 1f;
            double ut = Sim.UT;
            // From the star's side, turned round the spin axis by the phase and lifted above the equator by the elevation.
            Vector3d sun = Sim.System.SunDirectionFrom(b, Vector3d.zero, ut);
            Vector3d flat = new Vector3d(sun.x, 0, sun.z).normalized;
            double phase = shot.PhaseDeg * MathD.Deg2Rad, elev = shot.ElevationDeg * MathD.Deg2Rad;
            Vector3d turned = flat * Math.Cos(phase) + Vector3d.Cross(Vector3d.up, flat) * Math.Sin(phase);
            Vector3d toCam = (turned * Math.Cos(elev) + Vector3d.up * Math.Sin(elev)).normalized;
            double dist = b.IsStar ? b.Radius * shot.Radii : Math.Min(b.Radius * shot.Radii, b.SOIRadius * 0.8);

            // A probe parked where the camera will be: it keeps the frame, the floating origin and the loading of the
            // bodies around the camera, not at the body's centre.
            Vector3d p = toCam * dist;
            Vector3d v = Vector3d.Cross(Vector3d.up, p).normalized * Math.Sqrt(b.GM / dist);
            var h = CreateOrbitalHandle(TestCraft.DockTarget(db), "Camera probe", p, v, Quaternion.identity, b);
            if (!Sim.SwitchTo(h)) { Check($"camera at {b.Name}", false, "could not switch to the probe"); yield break; }
            yield return WaitSecondsAny(0.5);
            if (V != null) foreach (var r in V.GetComponentsInChildren<Renderer>()) r.enabled = false;
            Sim.Paused = true;
            Time.timeScale = 0f;

            float until = Time.realtimeSinceStartup + (b.IsGasGiant || b.Radius > 400e3 ? 5f : 3.5f);
            while (Time.realtimeSinceStartup < until)
            {
                PlaceTourCamera(b, toCam, dist);
                yield return null;
            }
            PlaceTourCamera(b, toCam, dist);
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(dir, $"tour_{b.Id}_{shot.Name}.png");
            ScreenCapture.CaptureScreenshot(path, 2);
            Note($"tour: {Path.GetFileName(path)}");
            for (int i = 0; i < 4; i++) yield return null;
            // The probe's job is done: another one comes for the next picture.
            if (V != null) Sim.DestroyHandle(V.Handle, "tour");
        }

        private IEnumerator TourMap(string name, CelestialBody focus, double metres, string dir)
        {
            Scene.SetMap(true);
            Scene.Map.FocusOn(focus);
            Scene.Map.ZoomTo((float)(metres / MapView.Scale));
            float until = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(dir, $"tour_{name}.png");
            ScreenCapture.CaptureScreenshot(path, 2);
            Note($"tour: {Path.GetFileName(path)}");
            for (int i = 0; i < 4; i++) yield return null;
        }

        /// <summary>Puts the free camera at <paramref name="dist"/> from the body along <paramref name="toCam"/>, looking at its centre.</summary>
        private void PlaceTourCamera(CelestialBody b, Vector3d toCam, double dist)
        {
            var frame = Sim.Frame;
            Vector3d centre = frame.BodyPosition(b, Sim.UT);
            Vector3d camTrue = centre + toCam * dist;
            var t = Scene.Camera.Cam.transform;
            t.position = frame.ToUnity(camTrue);
            Vector3 look = (Vector3)(centre - camTrue).normalized;
            t.rotation = Quaternion.LookRotation(look, Mathf.Abs(look.y) > 0.98f ? Vector3.forward : Vector3.up);
        }
    }
}
