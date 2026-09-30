using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TAP.Core;
using TAP.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace TAP.Tests
{
    public class BodyLabExpansionTests
    {
        private const string Home = "Assets/TAP/Resources/Data/system.json";
        private static string Preset(string name) => File.ReadAllText("Assets/TAP/Resources/Data/Terrain/" + name + ".json");
        private static BodyLabDocument Doc(string id = "tellus") => new BodyLabDocument(File.ReadAllText(Home), id, Preset);

        [TestCase(false, 0)] [TestCase(false, 1)] [TestCase(false, 42)] [TestCase(false, -7)] [TestCase(false, int.MinValue)]
        [TestCase(true, 0)] [TestCase(true, 1)] [TestCase(true, 42)] [TestCase(true, -7)] [TestCase(true, int.MaxValue)]
        public void GeneratedBodiesAreRepeatableAndRoundTripThroughTheGame(bool moon, int seed)
        {
            var d = Doc();
            var a = d.NewRandomBody(moon, seed, d.Id); var b = d.NewRandomBody(moon, seed, d.Id);
            Assert.IsTrue(JToken.DeepEquals(a.Body, b.Body));
            Assert.AreEqual(moon ? "moon" : "planet", (string)a.Body["type"]);
            var sys = a.BuildSystem(); var body = sys.Get(a.Id);
            Assert.Greater(body.Mass, 0); Assert.Greater(body.SurfaceGravity, 0);
            Assert.Greater(body.Orbit.PeriapsisRadius, body.Parent.Radius + body.Radius);
            Assert.Greater(body.SOIRadius, body.Radius + (body.Atmosphere?.Height ?? 0) + body.Terrain.MaxHeight);
            if (moon) Assert.Less(body.Orbit.ApoapsisRadius, body.Parent.SOIRadius * 0.36);
            Assert.Greater(body.Terrain.Biomes.Count, 3);
            Assert.IsNull(a.Body["terrain"]["biomes"].Last["when"]);
            var loaded = CelestialSystem.FromJson(a.SaveJson(File.ReadAllText(Home)), "lab", Preset).Get(a.Id);
            Assert.AreEqual(body.Terrain.Height(Vector3d.up), loaded.Terrain.Height(Vector3d.up));
            Assert.IsTrue(a.IsNew); Assert.IsTrue(a.Dirty);
            a.Set("radius", new JValue(body.Radius * 0.9)); a.Revert(); Assert.IsTrue(JToken.DeepEquals(a.Body, b.Body));
        }

        [Test]
        public void SavingSinglePrecisionColorsDoesNotCauseAnExternalChangeConflict()
        {
            var d = Doc(); d.Set("mapColor", new JArray(0.23f, 0.47f, 0.63f));
            string saved = d.SaveJson(File.ReadAllText(Home)); d.MarkSaved(saved);
            d.Set("description", new JValue("Updated after colour save"));
            Assert.DoesNotThrow(() => d.SaveJson(saved));
        }

        [Test]
        public void ManySeedsProduceVariedAtmospheresSizesAndBoundedTerrain()
        {
            var d = Doc(); int atmospheric = 0, airless = 0;
            double minRadius = double.PositiveInfinity, maxRadius = 0;
            for (int seed = 0; seed < 24; seed++)
            {
                var body = d.NewRandomBody(seed % 2 == 0, seed, d.Id).BuildSystem().Bodies.Last();
                if (body.HasAtmosphere) atmospheric++; else airless++;
                minRadius = Math.Min(minRadius, body.Radius); maxRadius = Math.Max(maxRadius, body.Radius);
                for (int lat = -75; lat <= 75; lat += 30)
                    for (int lon = -180; lon < 180; lon += 40)
                    {
                        var dir = Geo.FromLatLon(lat, lon); double height = body.Terrain.Height(dir);
                        Assert.IsFalse(double.IsNaN(height) || double.IsInfinity(height), "seed " + seed);
                        Assert.That(height, Is.InRange(body.Terrain.MinHeight, body.Terrain.MaxHeight), "seed " + seed);
                        Assert.IsNotNull(body.Terrain.BiomeAt(dir));
                    }
            }
            Assert.Greater(atmospheric, 0); Assert.Greater(airless, 0); Assert.Greater(maxRadius, minRadius * 2);
        }

        [TestCase(BodyLabBiomeTheme.Temperate)] [TestCase(BodyLabBiomeTheme.Arid)] [TestCase(BodyLabBiomeTheme.Frozen)]
        [TestCase(BodyLabBiomeTheme.Volcanic)] [TestCase(BodyLabBiomeTheme.Cratered)] [TestCase(BodyLabBiomeTheme.Random)]
        public void BiomeThemesPreserveGeometryAndPresetAndAreOneUndoStep(BodyLabBiomeTheme theme)
        {
            var d = Doc(); var original = d.Body; var heights = d.BuildSystem().Get(d.Id).Terrain;
            d.Commit(BodyLabRandomizer.ApplyBiomes(d.Body, theme, 12, d.EffectiveBody()));
            Assert.AreEqual("tellus", (string)d.Body["terrain"]["preset"]);
            var body = d.BuildSystem().Get(d.Id);
            foreach (var dir in new[] { Vector3d.up, Vector3d.right, Geo.FromLatLon(30, 42) }) Assert.AreEqual(heights.Height(dir), body.Terrain.Height(dir));
            Assert.AreEqual(body.Terrain.Biomes.Count, ((JArray)d.Body["terrain"]["colors"]).Count);
            d.Undo(); Assert.IsTrue(JToken.DeepEquals(original, d.Body)); Assert.IsFalse(d.CanUndo);
            d.Redo(); Assert.IsTrue(d.Dirty);
        }

        [Test]
        public void AnAddedBiomeRetainsPreviousRulesAndFallbackAndGetsAUniqueId()
        {
            var d = Doc(); var original = d.Body; var effective = d.EffectiveBody();
            d.Commit(BodyLabRandomizer.AddBiome(d.Body, effective, 1));
            d.Commit(BodyLabRandomizer.AddBiome(d.Body, d.EffectiveBody(), 2));
            var biomes = (JArray)d.Body["terrain"]["biomes"];
            Assert.AreEqual(((JArray)effective["terrain"]["biomes"]).Count + 2, biomes.Count);
            Assert.AreEqual(biomes.Count, biomes.Select(b => (string)b["id"]).Distinct().Count());
            Assert.IsTrue(JToken.DeepEquals(effective["terrain"]["biomes"].Last, biomes.Last));
            Assert.IsTrue(JToken.DeepEquals(effective["terrain"]["colors"][0], d.Body["terrain"]["colors"][0]));
            Assert.IsNotNull(d.BuildSystem().Get(d.Id).Terrain);
            d.Undo(); d.Undo(); Assert.IsTrue(JToken.DeepEquals(original, d.Body));
        }

        [Test]
        public void EverySchemaPropertyHasContextSpecificHoverHelpAndUnits()
        {
            Type[] types = { typeof(BodyDefinition), typeof(OrbitDefinition), typeof(AtmosphereDefinition), typeof(TerrainDefinition), typeof(NoiseDef),
                typeof(TerrainFieldDef), typeof(WeightDef), typeof(SpotDef), typeof(TerrainLayerDef), typeof(TermDef), typeof(CraterSizeDef), typeof(CraterFieldDef),
                typeof(RiftDef), typeof(CanyonDef), typeof(TerraceDef), typeof(DuneDef), typeof(FlatAreaDef), typeof(PlaceDef), typeof(ShoreDef), typeof(ColorRuleDef), typeof(ShadingDef), typeof(BiomeDef) };
            foreach (var type in types)
                foreach (var field in type.GetFields()) Assert.IsNotNull(BodyLabHelp.Describe(type, field.Name), type.Name + "." + field.Name);
            StringAssert.Contains("(m)", BodyLabHelp.Label(typeof(BodyDefinition), "radius"));
            StringAssert.DoesNotContain("(m)", BodyLabHelp.Label(typeof(CraterFieldDef), "radius"));
            StringAssert.DoesNotContain("(m)", BodyLabHelp.Label(typeof(CanyonDef), "width"));
            var d = Doc().NewRandomBody(false, 1); var root = new VisualElement();
            new BodyLabFields(d, () => { }, new System.Collections.Generic.Dictionary<string, bool>()).Draw(root, d.EffectiveBody());
            StringAssert.Contains("mass", root.Q<DoubleField>("gm").tooltip);
            StringAssert.Contains("ellipse", root.Q<DoubleField>("orbit.semiMajorAxis").tooltip);
            StringAssert.Contains("radian", root.Q<DoubleField>("terrain.fields[0].noise.frequency").tooltip);
        }

        [Test]
        public void OrbitPreviewUsesActualSystemPropagationAndParentRelativeMoonPositions()
        {
            var d = Doc("luma"); d.Set("orbit.inclinationDeg", new JValue(32)); d.Set("orbit.lanDeg", new JValue(67));
            var sys = d.BuildSystem(); var moon = sys.Get(d.Id); double ut = moon.Orbit.Period * 0.37;
            var all = BodyLabOrbitFrame.Sample(sys, moon.Id, ut, false);
            Assert.AreEqual(sys.Bodies.Count, all.Entries.Length);
            Assert.AreEqual(moon.GetPositionAtUT(ut), all.Entries.First(e => e.Body == moon).Position);
            var parent = BodyLabOrbitFrame.Sample(sys, moon.Id, ut, true);
            Assert.AreSame(moon.Parent, parent.Focus);
            var m = parent.Entries.First(e => e.Body == moon);
            Assert.Less((m.Position - moon.Orbit.GetPositionAtUT(ut)).magnitude, 1e-5);
            Assert.IsTrue(m.Path.Any(p => Math.Abs(p.y) > 1000));
            Assert.AreEqual(m.Path[0], m.Path.Last());
            var later = BodyLabOrbitFrame.Sample(sys, moon.Id, ut + moon.Orbit.Period * 0.25, true).Entries.First(e => e.Body == moon);
            Assert.Greater((later.Position - m.Position).magnitude, moon.Radius);
        }

        [UnityTest]
        public IEnumerator OrbitVisualDrawsInAnAttachedEditorPanel()
        {
            var host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.Show(); host.position = new Rect(200, 200, 600, 400);
                var view = new BodyLabOrbitView(); view.style.width = 580; view.style.height = 340; host.rootVisualElement.Add(view);
                var d = Doc(); view.SetSystem(d.BuildSystem(), d.Id); view.SetTime(12345);
                yield return null; yield return null;
                Assert.Greater(view.contentRect.width, 0); Assert.AreEqual(3, view.Frame.Entries.Length);
                view.SetParentView(true); yield return null;
                Assert.IsNotNull(view.Frame.Focus);
            }
            finally { host.Close(); }
        }
    }
}
