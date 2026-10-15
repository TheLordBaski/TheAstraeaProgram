using System;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The resource framework (FLT-01, D-01): resources and propellant mixes as data, volume tanks, the launch supply.</summary>
    public class ResourceTests
    {
        private static PartDatabase Db => PartDatabase.Instance;

        private static string Text(string path) => Resources.Load<TextAsset>(path).text;

        private static JToken Part(JObject catalog, string id) => ((JArray)catalog["parts"]).First(p => (string)p["id"] == id);

        /// <summary>The shipped catalog plus what a mod would add: a new resource and mix, a hydrogen tank, nuclear and fusion engines.</summary>
        private static PartDatabase ModdedCatalog(out ContentReport report)
        {
            var cat = JObject.Parse(Text("Data/parts"));
            ((JArray)cat["resources"]).Add(JObject.Parse(@"{ ""id"": ""Helium3"", ""name"": ""Helium-3"", ""unit"": ""L"", ""density"": 0.15, ""flow"": ""Vessel"" }"));
            ((JArray)cat["mixes"]).Add(JObject.Parse(@"{ ""id"": ""fusion"", ""name"": ""Fusion fuel"",
                ""components"": [ { ""resource"": ""Helium3"", ""ratio"": 3 }, { ""resource"": ""Hydrogen"", ""ratio"": 1 } ] }"));
            var parts = (JArray)cat["parts"];
            JToken Clone(string from, string id)
            {
                var c = Part(cat, from).DeepClone();
                c["id"] = id;
                c["title"] = id;
                parts.Add(c);
                return c;
            }
            var hydroTank = Clone("tank_s1_short", "test_hydrogen_tank");
            hydroTank["resources"] = JArray.Parse(@"[ { ""id"": ""Hydrogen"", ""amount"": 500 } ]");
            var fusionTank = Clone("tank_s1_short", "test_fusion_tank");
            fusionTank["resources"] = JArray.Parse(@"[ { ""id"": ""fusion"", ""amount"": 500 } ]");
            Clone("eng_ember", "test_nuclear")["engine"]["propellant"] = "Hydrogen";
            Clone("eng_ember", "test_torch")["engine"]["propellant"] = "fusion";
            report = new ContentReport();
            var result = PartValidation.Check("modded.json", cat.ToString(), report);
            return PartDatabase.FromCheck(result, report);
        }

        private static DesignStats Fly(PartDatabase db, params string[] stack)
        {
            var d = new CraftDesign();
            var a = new CraftAssembler(d, db);
            int prev = a.AddRoot("probe_wren");
            foreach (string id in stack) prev = a.AttachStack(prev, "bottom", id, "top");
            a.AutoStage();
            return DesignAnalysis.Analyze(d, db, EditorEnvironment.HomeVacuum);
        }

        // ------------------------------------------------------------------ the shipped catalog

        [Test]
        public void ChemicalTank_HoldsFuelAndOxidiserByVolume_WithTheMassItAlwaysHad()
        {
            var tank = Db.Get("tank_s1_short");
            Assert.AreEqual(2, tank.resources.Count);
            Assert.AreEqual(500 * 14 / 33.0, tank.resources.Single(r => r.id == "Fuel").amount, 1e-9);
            Assert.AreEqual(500 * 19 / 33.0, tank.resources.Single(r => r.id == "Oxidiser").amount, 1e-9);
            // 500 units of the mix weigh 500 kg, as the 500 kg of the one LiquidFuel did.
            Assert.AreEqual(500, tank.ResourceMass(Db), 1e-9);
        }

        [Test]
        public void Propellant_AMixDrawsItsComponentsAtItsRatio_AndTheScarcestDecides()
        {
            var chemical = Db.GetPropellant("chemical");
            Assert.IsTrue(chemical.IsMix);
            Assert.AreEqual(1, chemical.Components.Sum(c => c.MassShare), 1e-12);
            // 14 L of Fuel at 0.81 kg and 19 L of Oxidiser at 1.14 kg: 11.34 kg to 21.66 kg.
            Assert.AreEqual(11.34 / 33, chemical.Components.Single(c => c.Resource.id == "Fuel").MassShare, 1e-12);
            // 14 L of Fuel could burn 33 kg of the mix, but 9.5 L of Oxidiser only 16.5 kg: the Oxidiser decides.
            double burnable = chemical.BurnableMass(id => id == "Fuel" ? 14 : 9.5);
            Assert.AreEqual(16.5, burnable, 1e-9);
            // A single resource is a propellant of one component: units times density.
            var xenon = Db.GetPropellant("Xenon");
            Assert.IsFalse(xenon.IsMix);
            Assert.AreEqual(10, xenon.BurnableMass(id => 100), 1e-9);
            Assert.IsNull(Db.GetPropellant("Unobtainium"));
        }

        [Test]
        public void StarterCraft_KeepTheirMassAndDeltaV_ThroughTheMix()
        {
            // Measured before the one LiquidFuel became a mix (2026-10-15), within the 0.5% FLT-01 allows.
            var expected = new (string name, double wet, double dvVac, double dvAsl)[]
            {
                ("Skylark Suborbital", 9205, 2447.27, 2039.39),
                ("Meridian Orbiter", 10135, 3901.18, 1899.52),
                ("Luma Pathfinder", 80015, 7643.13, 4460.34),
            };
            foreach (var c in StarterCraft.All(Db))
            {
                var want = expected.Single(e => e.name == c.name);
                var vac = DesignAnalysis.Analyze(c, Db, EditorEnvironment.HomeVacuum);
                var asl = DesignAnalysis.Analyze(c, Db, EditorEnvironment.HomeSeaLevel);
                Assert.AreEqual(want.wet, vac.WetMass, want.wet * 0.001, c.name + " mass");
                Assert.AreEqual(want.dvVac, vac.TotalDvVac, want.dvVac * 0.005, c.name + " vacuum Δv");
                Assert.AreEqual(want.dvAsl, asl.TotalDvAsl, want.dvAsl * 0.005, c.name + " sea-level Δv");
            }
        }

        // ------------------------------------------------------------------ a mod's resources and mixes

        [Test]
        public void ModContent_ANewResourceAndMixLoadAndFly_WithNoCode()
        {
            var db = ModdedCatalog(out var report);
            Assert.IsTrue(report.IsClean, report.ToString());

            var fusion = db.GetPropellant("fusion");
            Assert.IsTrue(fusion.IsMix);
            // The tank holds 3 parts Helium-3 to 1 part Hydrogen by volume.
            var tank = db.Get("test_fusion_tank");
            Assert.AreEqual(375, tank.resources.Single(r => r.id == "Helium3").amount, 1e-9);
            Assert.AreEqual(125, tank.resources.Single(r => r.id == "Hydrogen").amount, 1e-9);
            double fuelMass = 375 * 0.15 + 125 * 0.07;
            Assert.AreEqual(fuelMass, tank.ResourceMass(db), 1e-9);

            // The torch burns it all: the rocket equation with the whole mix.
            var s = Fly(db, "test_fusion_tank", "test_torch");
            double isp = db.Get("test_torch").engine.ispVac;
            double m0 = s.WetMass;
            Assert.AreEqual(isp * MathD.G0 * Math.Log(m0 / (m0 - fuelMass)), s.TotalDvVac, 1.0);
        }

        [Test]
        public void NuclearEngine_BurnsHydrogenOnly_AndAHydrogenTankHoldsFewerKilograms()
        {
            var db = ModdedCatalog(out _);
            // The same part, the same volume: hydrogen is light.
            double chemical = db.Get("tank_s1_short").ResourceMass(db), hydrogen = db.Get("test_hydrogen_tank").ResourceMass(db);
            Assert.AreEqual(500, chemical, 1e-9);
            Assert.AreEqual(35, hydrogen, 1e-9);

            // A chemical tank above a hydrogen tank: the nuclear engine uses the hydrogen and leaves the chemical mix alone.
            var s = Fly(db, "tank_s1_short", "test_hydrogen_tank", "test_nuclear");
            double isp = db.Get("test_nuclear").engine.ispVac;
            double m0 = s.WetMass;
            Assert.AreEqual(isp * MathD.G0 * Math.Log(m0 / (m0 - 35)), s.TotalDvVac, 1.0, "only the 35 kg of hydrogen burn");

            // With only the chemical tank it has nothing to burn.
            var dry = Fly(db, "tank_s1_short", "test_nuclear");
            Assert.AreEqual(0, dry.TotalDvVac, 1e-9);
            Assert.IsTrue(dry.Warnings.Exists(w => w.Text.Contains("no propellant")), "an engine with no propellant it can burn is reported");
        }

        [Test]
        public void Validation_ReportsABadMixAndAPartThatNamesAnUnknownOne()
        {
            var cat = JObject.Parse(Text("Data/parts"));
            ((JArray)cat["mixes"]).Add(JObject.Parse(@"{ ""id"": ""broken"", ""name"": ""Broken"", ""components"": [ { ""resource"": ""Unobtainium"", ""ratio"": 1 }, { ""resource"": ""Fuel"", ""ratio"": 0 } ] }"));
            ((JArray)cat["mixes"]).Add(JObject.Parse(@"{ ""id"": ""Fuel"", ""name"": ""Clashes"", ""components"": [ { ""resource"": ""Fuel"", ""ratio"": 1 } ] }"));
            Part(cat, "tank_s1_short")["resources"] = JArray.Parse(@"[ { ""id"": ""broken"", ""amount"": 5 } ]");
            var report = new ContentReport();
            var result = PartValidation.Check("bad.json", cat.ToString(), report);
            string all = report.ToString();
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "mix broken" && p.Field == "components[0].resource"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "mix broken" && p.Field == "components[1].ratio"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "mix Fuel" && p.Field == "id"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Entry == "part tank_s1_short" && p.Field == "resources[0].id"), all);
            Assert.IsNull(result.Catalog.mixes.FirstOrDefault(m => m.id == "broken"), "a mix with errors is left out");
            Assert.IsNotNull(result.Catalog.mixes.FirstOrDefault(m => m.id == "chemical"), "the good mix stays");
        }

        // ------------------------------------------------------------------ the launch site's supply

        [Test]
        public void Launch_FillsTheTanksFromTheSite_FullOrAtTheCraftsFill()
        {
            double Liquid(VesselRecord rec) => rec.parts.SelectMany(p => p.resources).Sum(r => Db.GetResource(r.id).flow == ResourceFlow.Stack ? r.amount * Db.GetResource(r.id).density : 0);
            double Solid(VesselRecord rec) => rec.parts.SelectMany(p => p.resources).Where(r => r.id == "SolidFuel").Sum(r => r.amount);

            double allLiquid = 0, allSolid = 0;
            foreach (var design in StarterCraft.All(Db))
            {
                var full = LaunchService.CreateLaunchRecord(design, Db, 0);
                double liquid = Liquid(full), solid = Solid(full);
                allLiquid += liquid; allSolid += solid;

                foreach (var node in design.parts)
                    (node.settings ??= new System.Collections.Generic.Dictionary<string, double>())[LaunchSupply.FillSetting] = 0.5;
                var half = LaunchService.CreateLaunchRecord(design, Db, 0);
                Assert.AreEqual(liquid * 0.5, Liquid(half), liquid * 1e-9 + 1e-9, design.name + ": a half-full launch carries half the propellant");
                Assert.AreEqual(solid, Solid(half), 1e-9, design.name + ": a booster's solid fuel can't be part-filled");
                Assert.AreEqual(0, LaunchSupply.Price(design, Db), "nothing has a price yet");
                // The tanks keep their capacity, so the readouts show them half full.
                foreach (var tank in half.parts.Where(p => Db.Get(p.partId).category == "Propellant").SelectMany(p => p.resources).Where(r => r.id == "Fuel"))
                    Assert.AreEqual(tank.max * 0.5, tank.amount, 1e-9);
            }
            Assert.Greater(allLiquid, 5000, "the tanks are full by default");
            Assert.Greater(allSolid, 1000, "the boosters are full too");
        }
    }
}
