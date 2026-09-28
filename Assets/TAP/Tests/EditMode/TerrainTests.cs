using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TAP.Core;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>Terrain and biomes as data (FND-02): the layered generator, biomes, flat areas and determinism.</summary>
    public class TerrainTests
    {
        private static IEnumerable<(string key, LayeredTerrain terrain)> AllTerrains()
        {
            foreach (var (file, id) in new[] { ("Data/system", "home"), ("Data/system_debug", "debug") })
            {
                var sys = CelestialSystem.LoadFromResources(file, id);
                foreach (var b in sys.Bodies)
                    if (b.Terrain is LayeredTerrain t) yield return ($"{id}/{b.Id}", t);
            }
        }

        private static Vector3d RandomDir(System.Random r)
        {
            while (true)
            {
                var v = new Vector3d(r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1);
                double m = v.sqrMagnitude;
                if (m > 1e-6 && m <= 1) return v / Math.Sqrt(m);
            }
        }

        private static LayeredTerrain Build(string json, double radius = 100000, bool ocean = false) =>
            new LayeredTerrain(Newtonsoft.Json.JsonConvert.DeserializeObject<TerrainDefinition>(json), radius, ocean, "test");

        [Test]
        public void Terrain_IsTheSameEveryTime()
        {
            foreach (var (key, a) in AllTerrains())
            {
                // A second terrain from the same definition, sampled in another order.
                var b = new LayeredTerrain(a.Def, a.Radius, a.HasOcean, a.BodyId);
                var dirs = new List<Vector3d>();
                var r = new System.Random(7);
                for (int i = 0; i < 400; i++) dirs.Add(RandomDir(r));
                var fa = new double[a.FieldCount];
                var fb = new double[b.FieldCount];
                for (int i = 0; i < dirs.Count; i++)
                {
                    var d = dirs[i];
                    var e = dirs[dirs.Count - 1 - i];
                    double ha = a.Sample(d, fa, 0, true);
                    b.Sample(e, fb, 0, true);
                    double hb = b.Sample(d, fb, 0, true);
                    Assert.AreEqual(BitConverter.DoubleToInt64Bits(ha), BitConverter.DoubleToInt64Bits(hb), $"{key}: height differs at {d}");
                    Assert.AreEqual(a.Colorize(d, ha, fa, 0, 0.9), b.Colorize(d, hb, fb, 0, 0.9), $"{key}: colour differs");
                    Assert.AreEqual(a.BiomeAt(d).Id, b.BiomeAt(d).Id, $"{key}: biome differs");
                }
            }
        }

        /// <summary>
        /// Heights and biomes match the recorded fingerprint bit for bit. It was recorded on Windows; on Linux this proves
        /// the platforms agree. After a deliberate change to terrain data, record it again (TAP → Terrain → Record Fingerprint).
        /// </summary>
        [Test]
        public void Terrain_MatchesTheRecordedFingerprint()
        {
            string path = Path.Combine(Application.dataPath, "TAP/Tests/EditMode/TerrainFingerprint.txt");
            Assert.IsTrue(File.Exists(path), "no fingerprint recorded");
            var terrains = new Dictionary<string, LayeredTerrain>();
            foreach (var (key, t) in AllTerrains()) terrains[key] = t;
            var res = TerrainFingerprint.Verify(File.ReadAllText(path), k => terrains.TryGetValue(k, out var t) ? t : null);
            Assert.IsEmpty(res.Changed, "terrain data changed since the fingerprint was recorded: record it again (TAP → Terrain → Record Fingerprint)");
            Assert.IsEmpty(res.Missing, "bodies in the fingerprint are gone");
            Assert.Greater(res.Checked, 1000);
            Assert.AreEqual(0, res.Mismatches, string.Join("\n", res.Details));
        }

        [Test]
        public void DetMath_AgreesWithSystemMath()
        {
            var r = new System.Random(3);
            for (int i = 0; i < 20000; i++)
            {
                double x = (r.NextDouble() - 0.5) * 40;
                Assert.AreEqual(Math.Exp(x), DetMath.Exp(x), Math.Exp(x) * 1e-15);
                double p = Math.Exp((r.NextDouble() - 0.5) * 60);
                Assert.AreEqual(Math.Log(p), DetMath.Log(p), 1e-15 * Math.Max(1, Math.Abs(Math.Log(p))));
                double a = (r.NextDouble() - 0.5) * 20;
                Assert.AreEqual(Math.Sin(a), DetMath.Sin(a), 1e-15);
                Assert.AreEqual(Math.Cos(a), DetMath.Cos(a), 1e-15);
                double s = r.NextDouble() * 2 - 1;
                Assert.AreEqual(Math.Asin(s), DetMath.Asin(s), 1e-15);
                Assert.AreEqual(Math.Acos(s), DetMath.Acos(s), 1e-15);
                double yy = r.NextDouble() - 0.5, xx = r.NextDouble() - 0.5;
                Assert.AreEqual(Math.Atan2(yy, xx), DetMath.Atan2(yy, xx), 1e-15);
                double q = r.NextDouble();
                Assert.AreEqual(Math.Pow(q, 1.6), DetMath.Pow(q, 1.6), 1e-14);
            }
        }

        [Test]
        public void SeededRandom_IsSystemRandom()
        {
            for (int seed = -20; seed < 400; seed += 3)
            {
                var a = new System.Random(seed);
                var b = new SeededRandom(seed);
                for (int i = 1; i < 300; i++) Assert.AreEqual(a.Next(i), b.Next(i), $"seed {seed}");
            }
        }

        [Test]
        public void Biomes_CoverTellusAndLuma()
        {
            var sys = CelestialSystem.LoadFromResources();
            foreach (var (id, min) in new[] { ("tellus", 8), ("luma", 6) })
            {
                var t = sys.Get(id).Terrain;
                var seen = new HashSet<string>();
                var r = new System.Random(11);
                for (int i = 0; i < 3000; i++) seen.Add(t.BiomeAt(RandomDir(r)).Id);
                Assert.GreaterOrEqual(seen.Count, min, $"{id} biomes found: {string.Join(", ", seen)}");
                Assert.GreaterOrEqual(t.Biomes.Count, min);
            }
            var tellus = sys.Get("tellus");
            var site = sys.Def.launchSite;
            Assert.AreEqual("launchComplex", tellus.BiomeAt(site.latitude, site.longitude).Id, "the pad stands in the launch complex's biome");
            Assert.AreEqual("northPole", sys.Get("luma").BiomeAt(89, 0).Id);
            Assert.AreEqual("southPole", sys.Get("luma").BiomeAt(-89, 0).Id);
        }

        [Test]
        public void BiomeRules_TheFirstMatchWins()
        {
            // Latitude bands where the later rules overlap the earlier ones.
            var t = Build(@"{ ""biomes"": [
                { ""id"": ""cap"", ""when"": { ""of"": ""latitude"", ""atLeast"": 60 } },
                { ""id"": ""north"", ""when"": { ""of"": ""latitude"", ""atLeast"": 0 } },
                { ""id"": ""band"", ""when"": [ { ""of"": ""latitude"", ""atLeast"": -30 }, { ""of"": ""longitude"", ""range"": [0, 90] } ] },
                { ""id"": ""rest"" } ] }");
            Assert.AreEqual("cap", t.BiomeAt(Geo.FromLatLon(70, 10)).Id, "the first rule wins over the second");
            Assert.AreEqual("north", t.BiomeAt(Geo.FromLatLon(10, 10)).Id);
            Assert.AreEqual("band", t.BiomeAt(Geo.FromLatLon(-10, 45)).Id, "all conditions of a rule must hold");
            Assert.AreEqual("rest", t.BiomeAt(Geo.FromLatLon(-10, 120)).Id);
            Assert.AreEqual("rest", t.BiomeAt(Geo.FromLatLon(-60, 45)).Id);
        }

        [Test]
        public void Heights_StayWithinTheirBounds()
        {
            foreach (var (key, t) in AllTerrains())
            {
                var r = new System.Random(5);
                double lo = double.MaxValue, hi = double.MinValue;
                for (int i = 0; i < 1500; i++)
                {
                    double h = t.Height(RandomDir(r));
                    lo = Math.Min(lo, h);
                    hi = Math.Max(hi, h);
                }
                Assert.GreaterOrEqual(lo, t.MinHeight, $"{key} goes below its minHeight");
                Assert.LessOrEqual(hi, t.MaxHeight, $"{key} goes above its maxHeight");
            }
        }

        [Test]
        public void NewBody_NeedsOnlyJson()
        {
            // Testrock lives in the debug system's JSON alone and uses every kind of layer.
            var sys = CelestialSystem.LoadFromResources("Data/system_debug", "debug");
            var rock = sys.Get("testrock");
            Assert.NotNull(rock);
            var t = (LayeredTerrain)rock.Terrain;
            foreach (var kind in new[] { "craters", "rift", "canyons", "terraces", "dunes" })
                Assert.IsTrue(t.Def.layers.Exists(l => kind switch
                {
                    "craters" => l.craters != null, "rift" => l.rift != null, "canyons" => l.canyons != null,
                    "terraces" => l.terraces != null, _ => l.dunes != null,
                }), $"testrock has no {kind}");
            var seen = new HashSet<string>();
            var r = new System.Random(2);
            for (int i = 0; i < 2000; i++) seen.Add(t.BiomeAt(RandomDir(r)).Id);
            Assert.GreaterOrEqual(seen.Count, 5, string.Join(", ", seen));
            // Its fitted flat area is a plane through the ground there.
            var pad = t.FlatAreas[0];
            Assert.AreEqual("testPad", pad.Id);
            Assert.AreEqual(pad.PlaneHeight(pad.Center, t.Radius), t.Height(pad.Center), 1e-6);
        }

        [Test]
        public void Ellipsoid_ShapesAnElongatedBody()
        {
            // 30 × 20 × 15 km, drawn on a 20 km sphere (Granum, SYS-14).
            var t = Build(@"{ ""minHeight"": -6000, ""maxHeight"": 11000, ""layers"": [ { ""ellipsoid"": [30000, 20000, 15000] } ] }", 20000);
            Assert.AreEqual(10000, t.Height(Vector3d.right), 1e-6);
            Assert.AreEqual(0, t.Height(Vector3d.up), 1e-6);
            Assert.AreEqual(-5000, t.Height(Vector3d.forward), 1e-6);
            var d = new Vector3d(1, 1, 1).normalized;
            double r = 20000 + t.Height(d);
            Assert.AreEqual(1, Math.Pow(d.x * r / 30000, 2) + Math.Pow(d.y * r / 20000, 2) + Math.Pow(d.z * r / 15000, 2), 1e-9, "the surface lies on the ellipsoid");
        }

        [Test]
        public void Presets_AreOverriddenByTheBody()
        {
            string system = @"{ ""bodies"": [
                { ""id"": ""a"", ""radius"": 1000, ""gm"": 1, ""terrain"": { ""preset"": ""p"", ""seed"": 9, ""maxHeight"": 123 } } ] }";
            string preset = @"{ ""seed"": 1, ""minHeight"": -50, ""maxHeight"": 50, ""layers"": [ { ""constant"": 20 } ] }";
            var sys = CelestialSystem.FromJson(system, "t", name => name == "p" ? preset : null);
            var t = (LayeredTerrain)sys.Get("a").Terrain;
            Assert.AreEqual(9, t.Def.seed, "the body's seed replaces the preset's");
            Assert.AreEqual(123, t.MaxHeight);
            Assert.AreEqual(-50, t.MinHeight, "entries the body leaves out come from the preset");
            Assert.AreEqual(20, t.Height(Vector3d.up), 1e-12);
            var ex = Assert.Throws<FormatException>(() => CelestialSystem.FromJson(system.Replace("\"p\"", "\"missing\""), "t", name => null));
            StringAssert.Contains("missing", ex.Message);
        }

        [Test]
        public void BadData_GivesAClearError()
        {
            var ex = Assert.Throws<FormatException>(() => Build(@"{ ""layers"": [ { ""from"": ""nowhere"" } ] }"));
            StringAssert.Contains("nowhere", ex.Message);
            ex = Assert.Throws<FormatException>(() => Build(@"{ ""layers"": [ { ""noise"": { ""type"": ""wobbly"" } } ] }"));
            StringAssert.Contains("wobbly", ex.Message);
            ex = Assert.Throws<FormatException>(() => Build(@"{ ""colors"": [ { ""color"": ""green"" } ] }"));
            StringAssert.Contains("green", ex.Message);
            ex = Assert.Throws<FormatException>(() => Build(@"{ ""layers"": [ { ""constant"": 1, ""mask"": { ""of"": ""slope"", ""below"": 10 } } ] }"));
            StringAssert.Contains("slope", ex.Message);
        }

        [Test]
        public void FlatArea_LevelsTheGroundAndBlendsBack()
        {
            var t = (LayeredTerrain)CelestialSystem.LoadFromResources().Get("luma").Terrain;
            t = new LayeredTerrain(t.Def, t.Radius, t.HasOcean, "luma");
            var center = Geo.FromLatLon(12.3, 45.6);
            var probes = new List<(Vector3d dir, double dist, double before)>();
            var up = center;
            var r = new System.Random(4);
            for (int i = 0; i < 300; i++)
            {
                double dist = r.NextDouble() * 900, ang = r.NextDouble() * Math.PI * 2;
                var d = (up + (Geo.East(up) * Math.Cos(ang) + Geo.North(up) * Math.Sin(ang)) * (dist / t.Radius)).normalized;
                probes.Add((d, DetMath.Angle(d, up) * t.Radius, t.Height(d)));
            }
            int edits = 0;
            t.Edited += (c, angle) => edits++;
            var area = t.Flatten("base1", center, 200, 300, level: true);
            Assert.AreEqual(1, edits, "adding a flat area tells the renderer");
            Assert.AreEqual(0, area.SlopeDeg, 1e-6, "a level area has no tilt");
            foreach (var (d, dist, before) in probes)
            {
                double h = t.Height(d);
                if (dist < 199) Assert.AreEqual(area.PlaneHeight(d, t.Radius), h, 1e-6, "inside the radius the ground is the plane");
                if (dist > 501) Assert.AreEqual(before, h, "beyond radius + blend the land is untouched");
            }
            Assert.IsTrue(t.RemoveFlatArea("base1"));
            Assert.AreEqual(2, edits);
            foreach (var (d, _, before) in probes) Assert.AreEqual(before, t.Height(d), "removing it restores the land exactly");
        }

        [Test]
        public void FlatArea_TiltsWithTheLand()
        {
            // Ground rising 10 m per 100 m northwards: the height is the latitude (degrees) × 1,745 m on a 1,000 km body.
            const string slope = @"{ ""fields"": [ { ""id"": ""lat"", ""term"": { ""of"": ""latitude"" } } ],
                ""layers"": [ { ""from"": ""lat"", ""amplitude"": AMP } ] }";
            var t = Build(slope.Replace("AMP", "1745.329252"), 1000000);
            var center = Geo.FromLatLon(0, 30);
            var tilted = t.Flatten("slope", center, 150, 100);
            Assert.AreEqual(Math.Atan(0.1) * MathD.Rad2Deg, tilted.SlopeDeg, 0.01, "the plane follows the land");
            var north = (center + Geo.North(center) * (100 / t.Radius)).normalized;
            Assert.AreEqual(tilted.Height + 10, tilted.PlaneHeight(north, t.Radius), 0.05, "and rises the same way");
            Assert.AreEqual(t.Height(north), tilted.PlaneHeight(north, t.Radius), 0.05, "so the ground inside hardly moves");

            // On 45° ground the plane stops at the steepest tilt allowed.
            var steep = Build(slope.Replace("AMP", "17453.29252"), 1000000);
            Assert.AreEqual(20, steep.Flatten("steep", center, 150, 100, maxSlope: 20).SlopeDeg, 0.01);

            var level = t.Flatten("level", Geo.FromLatLon(0, -30), 150, 100, level: true);
            Assert.AreEqual(0, level.SlopeDeg, 1e-9);
            Assert.AreEqual(t.Height(Geo.FromLatLon(0, -30)), level.Height, 0.01, "a level plane sits at the mean height of the ground");

            // Saved and loaded again, the area is the same plane.
            var def = tilted.ToDef();
            t.RemoveFlatArea("slope");
            var again = t.AddFlatArea(def);
            Assert.AreEqual(tilted.Height, again.Height, 1e-9);
            Assert.AreEqual(0, Vector3d.AngleRad(tilted.Normal, again.Normal), 1e-12);
        }

        [Test]
        public void WaitingRocket_MovesWithThePad()
        {
            // A rocket rolled out to the old pad at 0° E before FND-02: root 5 m above the deck, its centre of mass 0.3 m
            // off the axis, turned 10° about the vertical.
            var sys = CelestialSystem.LoadFromResources();
            var site = sys.Def.launchSite;
            var body = sys.Get(site.body);
            double deck = body.Radius + site.padAltitude + site.padDeckHeight;
            var oldUp = Geo.FromLatLon(0, 0);
            var oldRot = TAP.Game.LaunchService.PadRotationAt(oldUp) * QuaternionD.AngleAxisRad(10 * MathD.Deg2Rad, Vector3d.up);
            var com = new Vector3d(0.3, 2, 0);
            var root = oldUp * (deck + 5);
            var p = root + oldRot * com;
            var rec = new TAP.Persistence.VesselRecord
            {
                bodyId = body.Id, systemId = sys.Id, landed = true, launchUT = -1, situation = TAP.Persistence.Situation.Prelaunch,
                landedPos = new[] { p.x, p.y, p.z }, landedRot = new[] { (float)oldRot.x, (float)oldRot.y, (float)oldRot.z, (float)oldRot.w },
                comOffset = new[] { 0.3f, 2f, 0f },
            };
            Assert.AreEqual(1, TAP.Game.LaunchService.MoveWaitingRocketsToPad(new List<TAP.Persistence.VesselRecord> { rec }, sys));
            var np = new Vector3d(rec.landedPos[0], rec.landedPos[1], rec.landedPos[2]);
            var nr = new QuaternionD(rec.landedRot[0], rec.landedRot[1], rec.landedRot[2], rec.landedRot[3]);
            var newRoot = np - nr * com;
            Assert.AreEqual(0, Vector3d.AngleRad(newRoot, site.UpBF) * body.Radius, 0.01, "the root stands above the new pad");
            Assert.AreEqual(deck + 5, newRoot.magnitude, 0.01, "at the same height");
            var before = TAP.Game.LaunchService.PadRotationAt(oldUp).Inverse() * oldRot;
            var after = TAP.Game.LaunchService.PadRotationAt(site.UpBF).Inverse() * nr;
            Assert.AreEqual(1, Math.Abs(before.x * after.x + before.y * after.y + before.z * after.z + before.w * after.w), 1e-6, "turned the same way against the pad");
            Assert.AreEqual(0, TAP.Game.LaunchService.MoveWaitingRocketsToPad(new List<TAP.Persistence.VesselRecord> { rec }, sys), "already on the pad");
            // The frame in doubles is Unity's LookRotation(north, up), only more precise.
            var q = Quaternion.LookRotation((Vector3)Geo.North(site.UpBF), (Vector3)site.UpBF);
            var f = TAP.Game.LaunchService.PadRotationAt(site.UpBF);
            Assert.AreEqual(1, Math.Abs(q.x * f.x + q.y * f.y + q.z * f.z + q.w * f.w), 1e-6);
        }

        [Test]
        public void LaunchSite_StandsOnItsFlatArea()
        {
            var sys = CelestialSystem.LoadFromResources();
            var site = sys.Def.launchSite;
            var tellus = (LayeredTerrain)sys.Get(site.body).Terrain;
            var area = tellus.FlatAreas[0];
            Assert.AreEqual(site.flatArea, area.Id);
            Assert.AreEqual(area.Height, site.padAltitude);
            Assert.AreEqual(0, Vector3d.AngleRad(site.UpBF, area.Center), 1e-12);
            Assert.AreEqual(site.padAltitude, tellus.Height(site.UpBF), 1e-6);
            Assert.AreEqual(0, site.latitude, 1e-9, "on the equator, for equatorial orbits");
        }
    }
}
