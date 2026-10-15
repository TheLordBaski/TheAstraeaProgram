using System;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The part catalogue (PRT-01): every part of the plan in data, its stats within its role's template, hidden until its release.</summary>
    public class PartCatalogueTests
    {
        private static PartDatabase Db => PartDatabase.Instance;

        private static string Text(string path) => Resources.Load<TextAsset>(path).text;

        [TearDown]
        public void Reset() => PartDatabase.ShowUnreleased = false;

        [Test]
        public void Catalogue_HasEveryPartOfThePlan_ByRelease()
        {
            // The plan's counts (05-parts.md): 36 in the slice, then +40, +63, +9, +20 = 168. The rocketeer and the flag are not parts you build.
            var built = Db.AllParts.Where(p => p.category != "Hidden").ToList();
            Assert.AreEqual(36, built.Count(p => p.milestone == "M0"));
            Assert.AreEqual(40, built.Count(p => p.milestone == "EA1"));
            Assert.AreEqual(63, built.Count(p => p.milestone == "EA2"));
            Assert.AreEqual(9, built.Count(p => p.milestone == "EA3"));
            Assert.AreEqual(20, built.Count(p => p.milestone == "EA4"));
            Assert.AreEqual(168, built.Count);
            // Every id of the plan's table is there: the ones named in the catalogue document.
            foreach (string id in new[] { "pod_mayfly1", "eng_wasp", "srb_spit", "tank_s3_long", "coupler_quad_s3", "solar_fold", "antenna_highgain", "sci_gravimeter",
                                          "wheel_l", "drill_l", "isru_s", "grabber", "fuel_line", "shield_inflatable", "hydro_s2", "xenon_radial", "lab_quill" })
                Assert.IsNotNull(Db.GetAny(id), id);
        }

        [Test]
        public void EveryPart_MeetsItsRoleTemplate()
        {
            var problems = PartTemplates.Instance.Check(Db);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void TheCheck_FlagsAnEngineTooWeakForItsRole()
        {
            // A lifter with a tenth of the Hornet's thrust is no lifter (TWR below its role's band).
            var cat = JObject.Parse(Text("Data/parts"));
            var hornet = ((JArray)cat["parts"]).First(p => (string)p["id"] == "eng_hornet");
            hornet["engine"]["thrustVac"] = 21500;
            var db = PartDatabase.FromCheck(PartValidation.Check("weak.json", cat.ToString(), new ContentReport()), new ContentReport());
            var problems = PartTemplates.Instance.Check(db);
            Assert.IsTrue(problems.Any(p => p.StartsWith("eng_hornet: twrAsl")), string.Join("\n", problems));
            Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
        }

        [Test]
        public void TheCheck_FlagsAWrongSizeAnUnknownRoleAndAForeignManufacturer()
        {
            var cat = JObject.Parse(Text("Data/parts"));
            var parts = (JArray)cat["parts"];
            var tank = parts.First(p => (string)p["id"] == "tank_s1_short");
            tank["diameter"] = 2.0;
            tank["manufacturer"] = "Acme";
            var dock = parts.First(p => (string)p["id"] == "dock_s1");
            dock["role"] = "structure.teleporter";
            var db = PartDatabase.FromCheck(PartValidation.Check("wrong.json", cat.ToString(), new ContentReport()), new ContentReport());
            string all = string.Join("\n", PartTemplates.Instance.Check(db));
            StringAssert.Contains("tank_s1_short: diameter 2 m is not S1's", all);
            StringAssert.Contains("tank_s1_short: manufacturer \"Acme\"", all);
            StringAssert.Contains("dock_s1: role \"structure.teleporter\" has no template", all);
        }

        [Test]
        public void TheConventions_NameEveryManufacturerAndTheirPersonality()
        {
            var t = PartTemplates.Instance;
            Assert.GreaterOrEqual(t.manufacturers.Count, 6);
            foreach (var m in t.manufacturers)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(m.personality), m.id + " needs a personality");
                Assert.IsTrue(Db.AllParts.Any(p => p.manufacturer == m.id), m.id + " makes no part");
            }
            CollectionAssert.AreEqual(new[] { "M0", "EA1", "EA2", "EA3", "EA4" }, t.milestones);
            // Size classes: 0.625, 1.25, 2.5 and 3.75 m, with attach-node sizes 0 to 3.
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, new[] { "S0", "S1", "S2", "S3" }.Select(s => t.Size(s).nodeSize).ToArray());
            Assert.AreEqual(3.75f, t.Size("S3").diameter);
        }

        // ------------------------------------------------------------------ hidden until the release

        [Test]
        public void PartsOfALaterRelease_AreHidden_UntilTheirMilestone()
        {
            Assert.AreEqual("M0", PartTemplates.Instance.release, "only the slice is out until PRT-12a releases EA1");
            Assert.IsNull(Db.Get("eng_wasp"), "an EA1 part can't be built yet");
            Assert.IsNotNull(Db.GetAny("eng_wasp"));
            StringAssert.Contains("arrives with EA1", Db.WhyMissing("eng_wasp"));
            Assert.IsFalse(Db.Parts.Any(p => p.milestone != "M0"), "the part list shows the released parts only");
            Assert.AreEqual(38, Db.Parts.Count, "the 36 parts of the slice, the rocketeer and the flag");
            Assert.IsNotNull(Db.Get("eng_hornet"));

            PartDatabase.ShowUnreleased = true;
            Assert.IsNotNull(Db.Get("eng_wasp"), "tools and tests can see everything");
            Assert.AreEqual(170, Db.Parts.Count);
        }

        [Test]
        public void RaisingTheRelease_ShowsTheNextMilestone()
        {
            var t = PartTemplates.Load(Text(PartTemplates.ResourcePath).Replace("\"release\": \"M0\"", "\"release\": \"EA2\""));
            Assert.IsTrue(t.Released("M0"));
            Assert.IsTrue(t.Released("EA1"));
            Assert.IsTrue(t.Released("EA2"));
            Assert.IsFalse(t.Released("EA3"));
            Assert.IsFalse(t.Released("EA4"));
        }

        [Test]
        public void ACraftThatNamesAnUnreleasedPart_CantBuildIt()
        {
            var d = new TAP.Persistence.CraftDesign();
            var a = new TAP.Construction.CraftAssembler(d, Db);
            int root = a.AddRoot("probe_wren");
            var e = Assert.Throws<ArgumentException>(() => a.AttachStack(root, "bottom", "tank_s0_long", "top"));
            StringAssert.Contains("arrives with EA1", e.Message);
        }
    }
}
