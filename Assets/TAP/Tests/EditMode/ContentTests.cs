using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TAP.Tests
{
    /// <summary>Content validation and hot reload (FND-04): readable errors at load, broken entries left out, F8 in place.</summary>
    public class ContentTests
    {
        private static string Text(string path) => Resources.Load<TextAsset>(path).text;

        private static int LineOf(string text, string marker) => text.Substring(0, text.IndexOf(marker, StringComparison.Ordinal)).Split('\n').Length;

        /// <summary>The Hornet's engine without its vacuum Isp.</summary>
        private const string HornetEngine = "\"thrustVac\": 215000, \"ispVac\": 310, ";

        private static string Without(string text, string what, string with = "")
        {
            Assert.AreEqual(1, text.Split(new[] { what }, StringSplitOptions.None).Length - 1, "the edit applies once: " + what);
            return text.Replace(what, with);
        }

        [Test]
        public void AllContent_LoadsWithoutProblems()
        {
            var r = ContentCheck.All();
            Assert.IsTrue(r.Report.IsClean, r.Report.ToString());
            Assert.GreaterOrEqual(r.Systems, 2, r.Counts);
            Assert.GreaterOrEqual(r.Bodies, 7, r.Counts);
            Assert.GreaterOrEqual(r.Presets, 2, r.Counts);
            Assert.GreaterOrEqual(r.Parts, 30, r.Counts);
            Assert.GreaterOrEqual(r.Craft, 3, r.Counts);
        }

        [Test]
        public void BrokenEntry_GivesOneClearError_AndIsLeftOut()
        {
            string text = Text("Data/parts");
            string broken = Without(text, HornetEngine, "\"thrustVac\": 215000, ");
            var report = new ContentReport();
            var result = PartValidation.Check("Data/parts.json", broken, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            Assert.AreEqual(0, report.WarningCount, report.ToString());
            var e = report.Errors.Single();
            Assert.AreEqual("Data/parts.json", e.File);
            Assert.AreEqual(LineOf(text, HornetEngine), e.Line, "the line of the engine the field is missing from");
            Assert.AreEqual("part eng_hornet", e.Entry);
            Assert.AreEqual("engine.ispVac", e.Field);
            Assert.AreEqual("a number above 0 (seconds)", e.Expected);
            Assert.AreEqual("nothing", e.Found);
            Assert.AreEqual($"Data/parts.json line {e.Line}: part eng_hornet, engine.ispVac: expected a number above 0 (seconds), found nothing.", e.ToString());

            // The part is left out and the others load; asking for it says why.
            int all = PartValidation.Check("Data/parts.json", text, new ContentReport()).Catalog.parts.Count;
            Assert.IsFalse(result.Catalog.parts.Exists(p => p.id == "eng_hornet"));
            Assert.AreEqual(all - 1, result.Catalog.parts.Count);
            var db = PartDatabase.FromCheck(result, report);
            Assert.IsNull(db.Get("eng_hornet"));
            Assert.IsTrue(db.IsBroken("eng_hornet"));
            StringAssert.Contains("Hornet", db.WhyMissing("eng_hornet"));
            StringAssert.Contains("engine.ispVac: expected a number above 0 (seconds), found nothing", db.WhyMissing("eng_hornet"));
            StringAssert.Contains("there is no part", db.WhyMissing("eng_nonesuch"));

            // A craft and a saved vessel that use it say so instead of failing in flight.
            var meridian = SaveStorage.FromJson<CraftDesign>(Text("Craft/Meridian_Orbiter"));
            var craftReport = new ContentReport();
            CraftValidation.Check("Craft/Meridian_Orbiter.json", Text("Craft/Meridian_Orbiter"), db, craftReport);
            Assert.AreEqual(1, craftReport.ErrorCount, craftReport.ToString());
            StringAssert.Contains("engine.ispVac", craftReport.Errors.Single().Hint);
            var vessel = new VesselRecord { name = meridian.name };
            foreach (var p in meridian.parts) vessel.parts.Add(new PartRecord { partId = p.partId });
            StringAssert.Contains("engine.ispVac", Vessel.CantBuild(vessel, db));
            Assert.IsNull(Vessel.CantBuild(vessel, PartDatabase.FromCheck(PartValidation.Check("Data/parts.json", text, new ContentReport()), new ContentReport())));
        }

        [Test]
        public void WrongKinds_AndUnknownFields_AreReported()
        {
            const string json = @"{
  ""resources"": [ { ""id"": ""LiquidFuel"", ""name"": ""Liquid fuel"", ""density"": 5, ""flow"": ""Stak"" } ],
  ""categories"": [ ""Engines"" ],
  ""parts"": [
    { ""id"": ""eng_test"", ""title"": ""Test Engine"", ""category"": ""Engines"", ""dryMass"": ""heavy"",
      ""engine"": { ""thrustvac"": 1000, ""ispVac"": 300, ""ispAsl"": 250, ""trust"": 5, ""thrustDir"": [0, 1] } }
  ]
}";
            var report = new ContentReport();
            var result = PartValidation.Check("test.json", json, report);
            string all = report.ToString();
            var dryMass = report.Errors.Single(p => p.Field == "dryMass");
            Assert.AreEqual("a number", dryMass.Expected);
            Assert.AreEqual("\"heavy\"", dryMass.Found);
            Assert.AreEqual(5, dryMass.Line);
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "resource LiquidFuel" && p.Field == "flow" && p.Expected == "one of Part, Stack, Vessel"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Field == "engine.thrustDir" && p.Expected == "a list of 3 numbers, not all 0"), all);
            var unknown = report.Warnings.Single(p => p.Field == "engine.trust");
            StringAssert.Contains("did you mean thrustVac?", unknown.Hint);
            var capitals = report.Warnings.Single(p => p.Field == "engine.thrustvac");
            Assert.AreEqual("\"thrustVac\"", capitals.Expected);
            // The miswritten capitals still load, so thrust is there; the part is left out for its errors.
            Assert.IsFalse(report.Errors.Any(p => p.Field == "engine.thrustVac"), all);
            Assert.IsEmpty(result.Catalog.parts);
            Assert.IsEmpty(result.Catalog.resources, "a resource with an error is left out too");
        }

        [Test]
        public void SyntaxError_SaysWhere()
        {
            var report = new ContentReport();
            Assert.IsNull(PartValidation.Check("Data/parts.json", "{\n  \"parts\": [\n    { \"id\": \"a\" \"title\": \"b\" }\n  ]\n}", report));
            var e = report.Errors.Single();
            Assert.AreEqual(3, e.Line);
            Assert.AreEqual("valid JSON", e.Expected);
            StringAssert.StartsWith("a syntax error", e.Found);

            report = new ContentReport();
            PartValidation.Check("Data/parts.json", "{ \"parts\": [], \"parts\": [] }", report);
            Assert.AreEqual("each field once", report.Errors.Single().Expected);
        }

        [Test]
        public void SystemErrors_PointAtTheFileAndLineThatHoldTheValue()
        {
            string system = Text("Data/system");
            string luma = Text("Data/Terrain/luma");
            // Luma's GM in system.json, and a crater size in the luma preset.
            int gmAt = system.IndexOf("\"gm\"", system.IndexOf("\"id\": \"luma\"", StringComparison.Ordinal), StringComparison.Ordinal);
            string gmText = system.Substring(gmAt, system.IndexOf('\n', gmAt) - gmAt).TrimEnd('\r', ',');
            string brokenSystem = system.Substring(0, gmAt) + "\"gm\": -5" + system.Substring(gmAt + gmText.Length);
            int cellAt = luma.IndexOf("\"cell\":", StringComparison.Ordinal);
            string cellText = luma.Substring(cellAt, luma.IndexOfAny(new[] { ',', '}' }, cellAt) - cellAt);
            string brokenLuma = luma.Substring(0, cellAt) + "\"cell\": 0" + luma.Substring(cellAt + cellText.Length);

            var report = new ContentReport();
            SystemValidation.Check("Data/system.json", brokenSystem, name => name == "luma" ? brokenLuma : CelestialSystem.PresetLoader(name), report);
            var gm = report.Errors.Single(p => p.Field == "gm");
            Assert.AreEqual("body luma", gm.Entry);
            Assert.AreEqual("Data/system.json", gm.File);
            Assert.AreEqual(LineOf(system, gmText), gm.Line);
            Assert.AreEqual("-5", gm.Found);
            var cell = report.Errors.Single(p => p.Field != null && p.Field.EndsWith(".cell"));
            Assert.AreEqual("Data/Terrain/luma.json", cell.File, "a value from the preset points at the preset");
            Assert.AreEqual(LineOf(luma, cellText), cell.Line);
            Assert.AreEqual("body luma", cell.Entry);

            // The game's loader refuses it with every error listed, rather than failing later, and logs them once.
            ContentLog.Clear();
            LogAssert.Expect(LogType.Error, new Regex(@"^Content errors \(2\)\. The system can't be loaded until they are fixed\.\n  Data/system\.json line"));
            var ex = Assert.Throws<ContentException>(() => CelestialSystem.Load("Data/system.json", brokenSystem, "home",
                name => name == "luma" ? brokenLuma : CelestialSystem.PresetLoader(name)));
            StringAssert.Contains("gm: expected a number above 0 (the standard gravitational parameter GM, m³/s²), found -5", ex.Message);
            Assert.AreEqual(2, ex.Report.ErrorCount, ex.Report.ToString());
        }

        [Test]
        public void SystemGraph_NeedsOneRootParentsAndOrbits()
        {
            var system = JObject.Parse(Text("Data/system"));
            var bodies = (JArray)system["bodies"];
            var luma = bodies.Children<JObject>().First(b => (string)b["id"] == "luma");
            var tellus = bodies.Children<JObject>().First(b => (string)b["id"] == "tellus");
            luma.Remove("orbit");
            tellus["parent"] = "luma";
            system["launchSite"]["body"] = "astraea";
            var report = new ContentReport();
            SystemValidation.CheckGraph(system, report, "Data/system.json");
            string all = report.ToString();
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "body luma" && p.Field == "orbit"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Field == "parent" && p.Found.StartsWith("a loop")), all);
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "launch site" && p.Field == "body" && p.Found.Contains("a star")), all);
        }

        [Test]
        public void Galaxy_NeedsFilesAndAHome()
        {
            var report = new ContentReport();
            Galaxy.Load("Data/galaxy.json", "{ \"home\": \"hom\", \"systems\": [ { \"id\": \"home\", \"name\": \"A\", \"file\": \"Data/sytem\" } ] }",
                report, f => f == "Data/system");
            Assert.IsTrue(report.Errors.Any(p => p.Field == "home"), report.ToString());
            Assert.IsTrue(report.Errors.Any(p => p.Field == "systems[0].file" && p.Hint == "there is no such file"), report.ToString());
        }

        [Test]
        public void HotReload_ChangesPartsInPlace()
        {
            string text = Text("Data/parts");
            var db = PartDatabase.Load("Data/parts.json", text);
            var hornet = db.Get("eng_hornet");
            var engine = hornet.engine;

            var r = db.Reload("Data/parts.json", Without(text, "\"thrustVac\": 215000,", "\"thrustVac\": 250000,"));
            Assert.IsTrue(r.Problems.IsClean, r.Problems.ToString());
            Assert.AreSame(hornet, db.Get("eng_hornet"), "the same definition, so parts already built see it");
            Assert.AreSame(engine, hornet.engine, "and the same engine, which an engine module holds");
            Assert.AreEqual(250000, engine.thrustVac);
            CollectionAssert.AreEqual(new[] { "Hornet LV-215 Engine: engine.thrustVac 215000 → 250000" }, r.Changes);

            // A new version with an error keeps the previous values.
            r = db.Reload("Data/parts.json", Without(text, HornetEngine, "\"thrustVac\": 300000, "));
            Assert.AreEqual(1, r.Problems.ErrorCount);
            Assert.AreEqual(250000, engine.thrustVac);
            StringAssert.Contains("keeps its previous values", r.Kept.Single());
        }

        [Test]
        public void HotReload_ChangesABodysAtmosphereAtOnce()
        {
            var sys = CelestialSystem.Load("Data/system.json", Text("Data/system"));
            var tellus = sys.Get("tellus");
            var def = tellus.Def;
            double p0 = tellus.Atmosphere.Pressure(0);
            var next = JObject.FromObject(def).ToObject<BodyDefinition>();
            next.atmosphere.seaLevelPressure *= 2;
            next.displayName = "Tellus Prime";
            tellus.ApplyLive(next);
            Assert.AreSame(def, tellus.Def);
            Assert.AreEqual(2 * p0, tellus.Atmosphere.Pressure(0), 1e-6);
            Assert.AreEqual("Tellus Prime", tellus.Name);
        }
    }
}
