using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TAP.Core;
using TAP.Persistence;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The galaxy of star systems (FND-15) and the star at the root of the home system (FND-01).</summary>
    public class SystemTests
    {
        [Test]
        public void Galaxy_ListsTheHomeAndDebugSystems()
        {
            var g = Galaxy.LoadFromResources();
            Assert.AreEqual("home", g.HomeSystemId);
            Assert.NotNull(g.Find("home"));
            Assert.IsTrue(g.Find("debug").debug, "the debug system is never offered to players");
        }

        [Test]
        public void HomeSystem_HasTheStarAtItsRoot()
        {
            var sys = CelestialSystem.LoadFromResources();
            Assert.AreEqual("home", sys.Id);
            Assert.NotNull(sys.Star);
            Assert.IsTrue(sys.Root.IsStar);
            Assert.IsNull(sys.Star.Terrain, "stars have no surface");
            var tellus = sys.Get("tellus");
            Assert.AreSame(tellus, sys.HomeBody);
            Assert.AreSame(sys.Star, tellus.Parent);
            Assert.AreSame(tellus, sys.Get("home/tellus"), "qualified ids name bodies across systems");
            Assert.IsNull(sys.Get("debug/tellus"));
            Assert.AreEqual(439.87, tellus.Orbit.Period / tellus.SolarDay, 0.01, "a Tellus year in Tellus days");
            Assert.AreEqual(85831e3, tellus.SOIRadius, 1e5, "Tellus's sphere of influence");
            Assert.Less(sys.Get("luma").Orbit.SemiMajorAxis, tellus.SOIRadius * 0.2);
        }

        [Test]
        public void Sunlight_ComesFromTheStar()
        {
            var sys = CelestialSystem.LoadFromResources();
            var tellus = sys.Get("tellus");
            double ut = sys.Def.startUT;
            // At the start the sun shines from where the vertical slice's fixed sun did (projected onto the orbit plane).
            Vector3d start = sys.SunDirectionFrom(tellus, Vector3d.zero, ut);
            Vector3d expected = new Vector3d(0.574, 0, -0.819).normalized;
            Assert.Less((start - expected).magnitude, 1e-3);
            // A quarter of a year later it comes from a direction 90° away.
            Vector3d later = sys.SunDirectionFrom(tellus, Vector3d.zero, ut + tellus.Orbit.Period / 4);
            Assert.AreEqual(0, Vector3d.Dot(start, later), 1e-3);
            // About 1,360 W/m² at Tellus, four times that at half the distance.
            double flux = sys.SolarFlux(tellus.GetPositionAtUT(ut));
            Assert.AreEqual(1360, flux, 5);
            Assert.AreEqual(4 * flux, sys.SolarFlux(tellus.GetPositionAtUT(ut) * 0.5), 1);
        }

        [Test]
        public void SolarOrbit_ReturnsAfterOnePeriod()
        {
            var star = CelestialSystem.LoadFromResources().Star;
            var r0 = new Vector3d(1.7e10, 0, 3e9);
            var v0 = new Vector3d(-2e3, 150, -8.1e3);
            var o = new Orbit(r0, v0, 1000, star.GM);
            Assert.Less(o.Eccentricity, 1);
            Assert.Less((o.GetPositionAtUT(1000 + o.Period) - r0).magnitude, 1.0);
            Assert.Less((o.GetVelocityAtUT(1000 + o.Period) - v0).magnitude, 1e-4);
        }

        [Test]
        public void LeavingTellus_IsContinuousIntoTheStarFrame()
        {
            var sys = CelestialSystem.LoadFromResources();
            var tellus = sys.Get("tellus");
            double r = tellus.Radius + 100e3;
            // 3.6 km/s beyond circular speed: a hyperbolic escape.
            var o = new Orbit(new Vector3d(r, 0, 0), new Vector3d(0, 0, -(Math.Sqrt(tellus.GM / r) + 3600)), 0, tellus.GM);
            var patches = PatchedConics.Predict(o, tellus, 0, new System.Collections.Generic.List<NodeSpec>(), 3);
            Assert.GreaterOrEqual(patches.Count, 2);
            Assert.AreEqual(PatchEnd.SoiExit, patches[0].EndType);
            Assert.AreSame(sys.Star, patches[1].Body);
            double t = patches[0].EndUT;
            Vector3d before = patches[0].Orbit.GetPositionAtUT(t) + tellus.GetPositionAtUT(t);
            Vector3d after = patches[1].Orbit.GetPositionAtUT(t) + sys.Star.GetPositionAtUT(t);
            Vector3d vBefore = patches[0].Orbit.GetVelocityAtUT(t) + tellus.GetVelocityAtUT(t);
            Vector3d vAfter = patches[1].Orbit.GetVelocityAtUT(t) + sys.Star.GetVelocityAtUT(t);
            Assert.Less((before - after).magnitude, 0.1, "position jump at the SOI exit");
            Assert.Less((vBefore - vAfter).magnitude, 1e-4, "velocity jump at the SOI exit");
        }

        [Test]
        public void DebugSystem_NamesNoHomeBodies()
        {
            var sys = Galaxy.LoadFromResources().LoadSystem("debug");
            Assert.AreEqual("debug", sys.Id);
            Assert.NotNull(sys.Star);
            Assert.AreEqual("testworld", sys.HomeBody.Id);
            Assert.IsNull(sys.Get("tellus"));
            Assert.IsNull(sys.Get("luma"));
            Assert.AreSame(sys.HomeBody, sys.Get("debug/testworld"));
        }

        [Test]
        public void SliceSaves_LoadIntoTheHomeSystem()
        {
            // Saves from before star systems carry no system id: their vessels belong to the home system.
            var save = SaveStorage.FromJson<GameSave>("{\"version\":1,\"ut\":100,\"vessels\":[{\"name\":\"Old\",\"bodyId\":\"luma\"}]}");
            var v = save.vessels[0];
            Assert.AreEqual("home", v.systemId);
            var sys = CelestialSystem.LoadFromResources();
            Assert.AreSame(sys.Get("luma"), sys.Get(v.systemId + "/" + v.bodyId));
            // And the id survives a round trip.
            v.systemId = "debug";
            Assert.AreEqual("debug", SaveStorage.FromJson<GameSave>(SaveStorage.ToJson(save)).vessels[0].systemId);
        }

        [Test]
        public void GameplayCode_NeverTreatsTheRootAsThePlanet()
        {
            // The hierarchy root is the star; gameplay asks for HomeBody or Star. Only the system classes use Root.
            var offenders = FindInScripts(new Regex(@"\b(System|sys)\.Root\b"), "Core/Bodies/");
            Assert.IsEmpty(offenders, "use CelestialSystem.HomeBody or Star: " + string.Join(", ", offenders));
        }

        [Test]
        public void GameplayCode_LooksUpNoBodyById()
        {
            // Bodies come from the system (HomeBody, HomeMoon, Star, the target...): a literal id only works in the home
            // system. Core keeps the terrain generator ids ("tellus", "luma") and data defaults.
            var offenders = FindInScripts(new Regex("\"(tellus|luma|astraea)\""), "Core/");
            Assert.IsEmpty(offenders, "use CelestialSystem.HomeBody, HomeMoon or Star: " + string.Join(", ", offenders));
        }

        /// <summary>Lines of the game's scripts matching a pattern, as "path:line", skipping one folder.</summary>
        private static System.Collections.Generic.List<string> FindInScripts(Regex pattern, string skipPrefix)
        {
            string scripts = Path.Combine(Application.dataPath, "TAP", "Scripts");
            var found = new System.Collections.Generic.List<string>();
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(scripts.Length + 1).Replace('\\', '/');
                if (rel.StartsWith(skipPrefix)) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (pattern.IsMatch(lines[i])) found.Add($"{rel}:{i + 1}");
            }
            return found;
        }
    }
}
