using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TAP.Core;
using TAP.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace TAP.Tests
{
    /// <summary>Authoring contracts: session history, inheritance, safe saves and the actual terrain preview.</summary>
    public class BodyLabTests
    {
        private const string Home = "Assets/TAP/Resources/Data/system.json";
        private static string Preset(string name) => File.ReadAllText("Assets/TAP/Resources/Data/Terrain/" + name + ".json");
        private static BodyLabDocument Doc(string id = "tellus") => new BodyLabDocument(File.ReadAllText(Home), id, Preset);

        [Test]
        public void UndoRedo_IsInMemoryAndANewEditClearsRedo()
        {
            var d = Doc(); double radius = (double)d.Body["radius"];
            d.Set("radius", new JValue(radius + 100)); d.Set("gm", new JValue(4e12));
            d.Undo(); Assert.AreEqual(radius + 100, (double)d.Body["radius"]);
            d.Undo(); Assert.AreEqual(radius, (double)d.Body["radius"]); Assert.IsFalse(d.Dirty);
            d.Redo(); Assert.AreEqual(radius + 100, (double)d.Body["radius"]);
            d.Set("description", new JValue("New description")); Assert.IsFalse(d.CanRedo);
            var saved = JObject.Parse(d.SaveJson(File.ReadAllText(Home)));
            Assert.AreEqual("New description", (string)saved["bodies"][1]["description"]);
            Assert.IsFalse(saved.Descendants().OfType<JProperty>().Any(x => new[] { "undo", "redo", "history" }.Contains(x.Name)));
        }

        [Test]
        public void AContinuousGestureIsOneUndoStep()
        {
            var d = Doc(); double radius = (double)d.Body["radius"];
            d.Set("radius", new JValue(radius + 1), true); d.Set("radius", new JValue(radius + 2), true);
            d.EndGesture(); d.Set("radius", new JValue(radius + 3), true);
            d.Undo(); Assert.AreEqual(radius + 2, (double)d.Body["radius"]);
            d.Undo(); Assert.AreEqual(radius, (double)d.Body["radius"]);
        }

        [Test]
        public void SessionMemoryRestoresANewBodyAndItsUndoRedoWithoutChangingExports()
        {
            var d = Doc().NewBody(false); d.Set("id", new JValue("draftMoon")); d.Set("radius", new JValue(120000.0)); d.Undo();
            var restored = BodyLabDocument.RestoreSession(d.CaptureSession(), Preset);
            Assert.IsTrue(restored.IsNew); Assert.IsTrue(restored.Dirty); Assert.IsTrue(restored.CanUndo); Assert.IsTrue(restored.CanRedo);
            Assert.IsTrue(JToken.DeepEquals(d.Body, restored.Body));
            restored.Redo(); Assert.AreEqual(120000, (double)restored.Body["radius"]);
            Assert.IsNull(restored.Body["undo"]); Assert.IsNull(restored.Body["redo"]);
        }

        [Test]
        public void AnInheritedListCopiesAllEntriesWithoutChangingThePreset()
        {
            var d = Doc(); var original = d.EffectiveBody(); string preset = Preset("tellus");
            var layers = (JArray)original["terrain"]["layers"];
            d.Set("terrain.layers[0].amplitude", new JValue(1234.0));
            Assert.AreEqual("tellus", (string)d.Body["terrain"]["preset"]);
            Assert.AreEqual(layers.Count, ((JArray)d.Body["terrain"]["layers"]).Count);
            Assert.AreEqual(1234, (double)d.EffectiveBody().SelectToken("terrain.layers[0].amplitude"));
            Assert.AreEqual(preset, Preset("tellus"));
            d.Undo(); Assert.IsNull(d.Body.SelectToken("terrain.layers"));
        }

        [Test]
        public void InheritedObjectsUseSparseOverridesAndCanRevertToThePreset()
        {
            var d = Doc("luma"); d.Set("terrain.shading.amount", new JValue(0.4));
            Assert.AreEqual(1, ((JObject)d.Body.SelectToken("terrain.shading")).Properties().Count());
            Assert.IsNotNull(d.EffectiveBody().SelectToken("terrain.shading.slopeDarkening"));
            d.Remove("terrain.shading.amount");
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse(Preset("luma"))["shading"]["amount"], d.EffectiveBody().SelectToken("terrain.shading.amount")));
        }

        [Test]
        public void ASingletonMaskCanBeEditedAsAListAndUndoRestoresItsJsonShape()
        {
            var d = Doc("luma"); var body = d.EffectiveBody();
            body["terrain"] = new JObject
            {
                ["layers"] = new JArray(new JObject { ["constant"] = 0, ["mask"] = new JObject { ["of"] = "latitude", ["atLeast"] = 0 } }),
                ["biomes"] = new JArray(new JObject { ["id"] = "all", ["when"] = new JObject { ["of"] = "height", ["atLeast"] = 0 } })
            };
            d.Commit(body); d.Set("terrain.layers[0].mask[0].atLeast", new JValue(20.0));
            Assert.IsInstanceOf<JArray>(d.Body.SelectToken("terrain.layers[0].mask"));
            Assert.AreEqual(20, (double)d.EffectiveBody().SelectToken("terrain.layers[0].mask[0].atLeast"));
            d.Undo(); Assert.IsInstanceOf<JObject>(d.Body.SelectToken("terrain.layers[0].mask"));
        }

        [Test]
        public void SavePreservesOtherBodiesUnknownPropertiesAndConcurrentUnrelatedEdits()
        {
            var latest = JObject.Parse(File.ReadAllText(Home)); var moon = latest["bodies"][2].DeepClone();
            latest["futureSystemSetting"] = "keep"; latest["bodies"][2]["futureBodySetting"] = 17;
            var d = Doc(); d.Set("terrain.seed", new JValue(345));
            var output = JObject.Parse(d.SaveJson(latest.ToString()));
            Assert.AreEqual("keep", (string)output["futureSystemSetting"]);
            Assert.AreEqual(17, (int)output["bodies"][2]["futureBodySetting"]);
            latest["bodies"][2] = moon;
            Assert.IsTrue(JToken.DeepEquals(latest["bodies"][2], JObject.Parse(File.ReadAllText(Home))["bodies"][2]));
            Assert.AreEqual("tellus", (string)output["bodies"][1]["terrain"]["preset"]);
        }

        [Test]
        public void SaveRefusesToOverwriteExternalChangesToThisBody()
        {
            var latest = JObject.Parse(File.ReadAllText(Home)); latest["bodies"][1]["radius"] = 700000;
            var d = Doc(); d.Set("description", new JValue("Edited"));
            StringAssert.Contains("changed on disk", Assert.Throws<FormatException>(() => d.SaveJson(latest.ToString())).Message);
        }

        [Test]
        public void RenameUpdatesParentsAndLaunchSiteAndStillSupportsUndoAfterSaving()
        {
            var d = Doc(); d.Set("id", new JValue("newTellus"));
            string saved = d.SaveJson(File.ReadAllText(Home)); var json = JObject.Parse(saved);
            Assert.AreEqual("newTellus", (string)json["bodies"][2]["parent"]);
            Assert.AreEqual("newTellus", (string)json["launchSite"]["body"]);
            d.MarkSaved(saved); Assert.IsFalse(d.Dirty); d.Undo(); Assert.IsTrue(d.Dirty);
            var restored = JObject.Parse(d.SaveJson(saved)); Assert.AreEqual("tellus", (string)restored["launchSite"]["body"]);
        }

        [Test]
        public void NewBodyCanRoundTripThroughTheExistingLoader()
        {
            var d = Doc().NewBody(false); var original = d.Body;
            d.Set("radius", new JValue(120000.0)); d.Revert(); Assert.IsTrue(JToken.DeepEquals(original, d.Body));
            var sys = d.BuildSystem(); Assert.IsNotNull(sys.Get(d.Id).Terrain);
            string saved = d.SaveJson(File.ReadAllText(Home));
            var loaded = CelestialSystem.FromJson(saved, "lab", Preset);
            foreach (var dir in new[] { Vector3d.up, Vector3d.right, Geo.FromLatLon(30, -40) })
                Assert.AreEqual(sys.Get(d.Id).Terrain.Height(dir), loaded.Get(d.Id).Terrain.Height(dir));
            d.MarkSaved(saved); Assert.IsFalse(d.Dirty);
        }

        [TestCase("home", "tellus")]
        [TestCase("home", "luma")]
        [TestCase("debug", "testrock")]
        public void PreviewUsesGameHeightsBiomesAndAreaWeightedCoverage(string system, string id)
        {
            string path = system == "home" ? Home : "Assets/TAP/Resources/Data/system_debug.json";
            var d = new BodyLabDocument(File.ReadAllText(path), id, Preset);
            var b = d.BuildSystem().Get(id);
            var r = BodyLabPreview.Generate(b, 12, 34, 5000, 6, 32, 1, CancellationToken.None);
            Assert.AreEqual(6, r.Globe.Length); Assert.AreEqual(32 * 16, r.SurfaceMap.Length);
            Assert.AreEqual(100, r.Shares.Sum(), 1e-8);
            var chunk = r.Globe[0]; var dir = (chunk.CenterBF + (Vector3d)chunk.Positions[0]).normalized;
            var biome = b.Terrain.BiomeAt(dir).Color;
            Assert.AreEqual(biome.r, r.GlobeBiomes[0][0].r); Assert.AreEqual(biome.g, r.GlobeBiomes[0][0].g);
            Assert.IsTrue(r.MinHeight >= b.Terrain.MinHeight && r.MaxHeight <= b.Terrain.MaxHeight);
        }

        [Test]
        public void ValidationRejectsCyclesDuplicateIdsAndInvalidCurves()
        {
            var d = Doc(); d.Set("parent", new JValue("luma")); Assert.Throws<FormatException>(() => d.BuildSystem());
            d = Doc(); d.Set("id", new JValue("luma")); Assert.Throws<FormatException>(() => d.BuildSystem());
            d = Doc(); d.Set("atmosphere.temperatureCurve", new JArray(new JArray(0, 280), new JArray(0, 250)));
            StringAssert.Contains("increase", Assert.Throws<FormatException>(() => d.BuildSystem()).Message);
        }

        [Test]
        public void InvalidPresetNamesRemainEditable()
        {
            var d = Doc(); d.Set("terrain.preset", new JValue("missing"));
            Assert.Throws<FileNotFoundException>(() => d.BuildSystem());
            d.Set("terrain.preset", new JValue("tellus")); Assert.IsNotNull(d.BuildSystem().Get("tellus"));
        }

        [Test]
        public void FormIncludesBodyAndEveryTerrainSectionAndChangingAControlChangesTheDraft()
        {
            var d = Doc(); var root = new VisualElement();
            var host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.Show(); host.rootVisualElement.Add(root);
                new BodyLabFields(d, () => { }, new System.Collections.Generic.Dictionary<string, bool>()).Draw(root, d.EffectiveBody());
                Assert.IsNotNull(root.Q<DoubleField>("radius"));
                root.Q<DoubleField>("radius").value = 610000;
                Assert.AreEqual(610000, (double)d.Body["radius"]);
                Assert.IsNotNull(root.Q<IntegerField>("terrain.seed"));
                Assert.IsNotNull(root.Q<DoubleField>("orbit.semiMajorAxis"));
                Assert.IsNotNull(root.Q<DoubleField>("atmosphere.scaleHeight"));
                Assert.IsNotNull(root.Q<DoubleField>("luminosity"));
            }
            finally { host.Close(); }
        }

        [Test]
        public void CancelledPreviewDoesNotFinishProducingAMap()
        {
            var cancel = new CancellationTokenSource(); cancel.Cancel();
            Assert.Throws<OperationCanceledException>(() => BodyLabPreview.Generate(Doc().BuildSystem().Get("tellus"), 0, 0, 1000, 6, 32, 1, cancel.Token));
            cancel.Dispose();
        }

        [UnityTest]
        public IEnumerator AnObsoleteWorkerCannotReplaceThePreviewAndTemporaryObjectsAreReleased()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var preview = new BodyLabPreview(scene);
            try
            {
                preview.Start(Doc().BuildSystem().Get("tellus"), 0, 0, 1000, false);
                preview.Invalidate();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (preview.Busy && DateTime.UtcNow < deadline) { preview.Poll(); yield return null; }
                Assert.IsFalse(preview.Busy); Assert.IsNull(preview.Current);
                preview.Start(Doc("luma").BuildSystem().Get("luma"), 30, 40, 2000, false);
                deadline = DateTime.UtcNow.AddSeconds(10);
                while (preview.Busy && DateTime.UtcNow < deadline) { preview.Poll(); yield return null; }
                Assert.AreEqual("luma", preview.Current?.Body.Id);
                Assert.IsNull(preview.Error);
                preview.Dispose();
                Assert.AreEqual(0, scene.GetRootGameObjects().Length);
            }
            finally { preview.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
