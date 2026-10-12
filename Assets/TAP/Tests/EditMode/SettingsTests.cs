using System.IO;
using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace TAP.Tests
{
    /// <summary>FND-08: the settings file, its defaults, safe mode and the move out of PlayerPrefs. No test applies settings to the window or the pipeline.</summary>
    public class SettingsTests
    {
        private string _root;
        private static readonly string[] NoArgs = new string[0];
        private float? _zoom;
        private int? _autosave, _quality, _clouds, _scatter;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "tap_settings_" + System.Guid.NewGuid().ToString("N"));
            SaveStorage.RootOverride = _root;
            GameSettings.Forget();
            // The old keys belong to the developer's editor: put them back after a test.
            _zoom = PlayerPrefs.HasKey("TAP.ZoomSpeed") ? PlayerPrefs.GetFloat("TAP.ZoomSpeed") : (float?)null;
            _autosave = Int("TAP.AutosaveMinutes"); _quality = Int("TAP.PlanetQuality"); _clouds = Int("TAP.PlanetClouds"); _scatter = Int("TAP.PlanetScatter");
            ClearOldKeys();
        }

        [TearDown]
        public void TearDown()
        {
            SaveStorage.RootOverride = null;
            GameSettings.Forget();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            ClearOldKeys();
            if (_zoom.HasValue) PlayerPrefs.SetFloat("TAP.ZoomSpeed", _zoom.Value);
            Restore("TAP.AutosaveMinutes", _autosave); Restore("TAP.PlanetQuality", _quality); Restore("TAP.PlanetClouds", _clouds); Restore("TAP.PlanetScatter", _scatter);
            PlayerPrefs.Save();
        }

        private static int? Int(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
        private static void Restore(string key, int? v) { if (v.HasValue) PlayerPrefs.SetInt(key, v.Value); }

        private static void ClearOldKeys()
        {
            foreach (var k in new[] { "TAP.ZoomSpeed", "TAP.AutosaveMinutes", "TAP.PlanetQuality", "TAP.PlanetClouds", "TAP.PlanetScatter" }) PlayerPrefs.DeleteKey(k);
        }

        [Test]
        public void NoFile_GivesTheDefaults()
        {
            GameSettings.Load(NoArgs);
            var s = GameSettings.Current;
            Assert.AreEqual(GraphicsPreset.High, s.graphics.preset);
            Assert.AreEqual(WindowMode.Windowed, s.graphics.windowMode);
            Assert.AreEqual(1f, s.audio.master);
            Assert.AreEqual(5, s.gameplay.autosaveMinutes);
            Assert.IsFalse(File.Exists(GameSettings.FilePath), "defaults need no file");
        }

        [Test]
        public void EverySetting_SurvivesARestart_AndDeletingTheFileRestoresTheDefaults()
        {
            GameSettings.Load(NoArgs);
            var s = GameSettings.Current;
            s.graphics.width = 1600; s.graphics.height = 900; s.graphics.windowMode = WindowMode.Borderless; s.graphics.vsync = false; s.graphics.frameCap = 120;
            s.graphics.ApplyPreset(GraphicsPreset.Ultra); s.graphics.shadows = ShadowSetting.Medium; s.graphics.renderScale = 0.8f; s.graphics.MatchPreset();
            s.audio.master = 0.4f; s.audio.music = 0.1f; s.audio.effects = 0.2f; s.audio.interfaceSounds = 0.3f; s.audio.ambience = 0.5f;
            s.gameplay.autosaveMinutes = 15; s.gameplay.tooltips = false; s.gameplay.zoomSpeed = 2.5f; s.gameplay.units = UnitSystem.Imperial;
            s.ui.uiScale = 1.25f; s.ui.hudScale = 0.85f;
            GameSettings.Save();
            string saved = File.ReadAllText(GameSettings.FilePath);

            GameSettings.Forget();
            GameSettings.Load(NoArgs);
            Assert.AreEqual(saved, SaveStorage.ToJson(GameSettings.Current), "a restart reads back exactly what was saved");
            var r = GameSettings.Current;
            Assert.AreEqual(GraphicsPreset.Custom, r.graphics.preset);
            Assert.AreEqual(0.8f, r.graphics.renderScale);
            Assert.AreEqual(2.5f, r.gameplay.zoomSpeed);
            Assert.AreEqual(0.85f, r.ui.hudScale);

            File.Delete(GameSettings.FilePath);
            GameSettings.Forget();
            GameSettings.Load(NoArgs);
            Assert.AreEqual(SaveStorage.ToJson(new GameSettings()), SaveStorage.ToJson(GameSettings.Current), "no file, all defaults");
        }

        [Test]
        public void SafeMode_Is1280x720WindowedLow_AndNeverWritesTheFile()
        {
            GameSettings.Load(NoArgs);
            GameSettings.Current.graphics.width = 2560; GameSettings.Current.graphics.height = 1440; GameSettings.Current.graphics.ApplyPreset(GraphicsPreset.Ultra);
            GameSettings.Save();
            string before = File.ReadAllText(GameSettings.FilePath);

            GameSettings.Forget();
            GameSettings.Load(new[] { "TAP.exe", "-safe-mode" });
            var g = GameSettings.Current.graphics;
            Assert.AreEqual((1280, 720), (g.width, g.height));
            Assert.AreEqual(WindowMode.Windowed, g.windowMode);
            Assert.AreEqual(GraphicsPreset.Low, g.preset);
            Assert.AreEqual(ShadowSetting.Low, g.shadows);
            GameSettings.Current.audio.master = 0.3f;
            GameSettings.Save();
            Assert.AreEqual(before, File.ReadAllText(GameSettings.FilePath), "safe mode leaves the player's file alone");
        }

        [Test]
        public void ScreenSizeFromTheCommandLine_LastsForTheSessionOnly()
        {
            GameSettings.Load(NoArgs);
            GameSettings.Current.graphics.width = 2560; GameSettings.Current.graphics.height = 1440;
            GameSettings.Save();

            GameSettings.Forget();
            GameSettings.Load(new[] { "TAP.exe", "-screen-width", "1024", "-screen-height", "768" });
            Assert.AreEqual((1024, 768), (GameSettings.Current.graphics.width, GameSettings.Current.graphics.height));
            GameSettings.Current.audio.master = 0.6f; // another change saves the file, but not the override
            GameSettings.Save();

            GameSettings.Forget();
            GameSettings.Load(NoArgs);
            Assert.AreEqual((2560, 1440), (GameSettings.Current.graphics.width, GameSettings.Current.graphics.height));
            Assert.AreEqual(0.6f, GameSettings.Current.audio.master);
        }

        [Test]
        public void DamagedFile_GivesTheDefaults_AndIsKept()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(GameSettings.FilePath, "{ \"graphics\": { \"windowMode\": \"Sideways\" ");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("damaged"));
            GameSettings.Load(NoArgs);
            Assert.AreEqual(SaveStorage.ToJson(new GameSettings()), SaveStorage.ToJson(GameSettings.Current));
            Assert.IsTrue(File.Exists(GameSettings.FilePath + ".damaged"));
        }

        [Test]
        public void ValuesOutOfRange_AreBroughtBack()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(GameSettings.FilePath, "{ \"graphics\": { \"renderScale\": 9, \"width\": 3 }, \"audio\": { \"master\": 7 }, \"gameplay\": { \"autosaveMinutes\": -4 } }");
            GameSettings.Load(NoArgs);
            Assert.AreEqual(1.5f, GameSettings.Current.graphics.renderScale);
            Assert.AreEqual(GameSettings.MinWidth, GameSettings.Current.graphics.width);
            Assert.AreEqual(1f, GameSettings.Current.audio.master);
            Assert.AreEqual(0, GameSettings.Current.gameplay.autosaveMinutes);
        }

        [Test]
        public void TheOldPlayerPrefs_MigrateOnceAndAreRemoved()
        {
            PlayerPrefs.SetFloat("TAP.ZoomSpeed", 2.5f);
            PlayerPrefs.SetInt("TAP.AutosaveMinutes", 10);
            PlayerPrefs.SetInt("TAP.PlanetQuality", 1);
            PlayerPrefs.SetInt("TAP.PlanetScatter", 0);
            GameSettings.Load(NoArgs);
            Assert.AreEqual(2.5f, GameSettings.Current.gameplay.zoomSpeed);
            Assert.AreEqual(10, GameSettings.Current.gameplay.autosaveMinutes);
            Assert.AreEqual(PlanetQuality.Medium, GameSettings.Current.graphics.planetQuality);
            Assert.AreEqual(0, GameSettings.Current.graphics.scatterDensity);
            Assert.IsTrue(File.Exists(GameSettings.FilePath));
            Assert.IsFalse(PlayerPrefs.HasKey("TAP.ZoomSpeed"), "the registry no longer holds the setting");
        }

        [Test]
        public void APreset_MatchesItsOwnSettings_AndAnyChangeMakesItCustom()
        {
            var g = new GraphicsOptions();
            foreach (var p in new[] { GraphicsPreset.Low, GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra })
            {
                g.ApplyPreset(p);
                g.MatchPreset();
                Assert.AreEqual(p, g.preset);
            }
            g.renderScale = 0.9f;
            g.MatchPreset();
            Assert.AreEqual(GraphicsPreset.Custom, g.preset);
        }

        [Test]
        public void AHudPanel_HugsTheEdgeItWasDroppedNear_AndKeepsItOnAWiderScreen()
        {
            var size = new Vector2(400, 200);
            var hd = new Vector2(1920, 1080);
            // dropped 20 px from the right and bottom edges: it stays 20 px from them on a 21:9 screen
            var s = HudPanelState.At(new Vector2(1500, 860), size, hd, 1.2f);
            Assert.AreEqual((2, 2), (s.ax, s.ay));
            Assert.AreEqual(new Vector2(20, 20), new Vector2(s.x, s.y));
            Assert.AreEqual(1.2f, s.scale);
            Assert.AreEqual(new Vector2(2520 - 420, 860), s.TopLeft(new Vector2(2520, 1080), size));
            // the middle third keeps its distance from the centre line
            var c = HudPanelState.At(new Vector2(780, 16), size, hd, 1f);
            Assert.AreEqual((1, 0), (c.ax, c.ay));
            Assert.AreEqual(new Vector2(1080, 16), c.TopLeft(new Vector2(2520, 1080), size));
            // never off the screen, however far it was stored
            var far = new HudPanelState { ax = 2, ay = 2, x = -500, y = 5000 };
            Assert.AreEqual(new Vector2(1520, 0), far.TopLeft(hd, size));
        }

        [Test]
        public void HudLayouts_SurviveTheFile_AndBadValuesAreRepaired()
        {
            GameSettings.Load(NoArgs);
            GameSettings.Current.ui.hud["staging"] = new HudPanelState { ax = 0, ay = 2, x = 16, y = 16, scale = 9f, hidden = true };
            GameSettings.Save();
            GameSettings.Forget();
            GameSettings.Load(NoArgs);
            var s = GameSettings.Current.ui.hud["staging"];
            Assert.IsTrue(s.hidden);
            Assert.AreEqual(2f, s.scale, "the scale is clamped");
            Assert.AreEqual((0, 2), (s.ax, s.ay));
        }
    }
}
