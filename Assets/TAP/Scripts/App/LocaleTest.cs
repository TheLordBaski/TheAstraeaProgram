using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAP.App
{
    /// <summary>
    /// Autotest <c>locale</c> (FND-10): walks the menu, the space center, the assembly building, the flight display and the map with
    /// the audit on, and lists every text that was shown but is not in the English table (Strings/en.json), per screen. A text
    /// there never reached the translation, or has not been added to the table yet. With <c>-locale pseudo</c> the screens are in the
    /// pseudo-locale, which shows the same texts accented and longer (screenshots in the run's folder).
    /// </summary>
    public sealed class LocaleTest : MonoBehaviour
    {
        private const string SaveName = "AutoTest";
        private readonly StringBuilder _report = new StringBuilder();
        private readonly HashSet<string> _reported = new HashSet<string>();
        private int _total;
        private string _shots;

        private int _shot;

        /// <summary>Starts the audit in the language of <c>-locale</c> (default: the pseudo-locale).</summary>
        public static void Launch()
        {
            string language = PlayerPrefs.GetString("TAP.AutoTestLocale", "");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-locale") language = args[i + 1];
            if (string.IsNullOrEmpty(language)) language = Loc.Pseudo;
            var go = new GameObject("LocaleTest");
            DontDestroyOnLoad(go);
            var test = go.AddComponent<LocaleTest>();
            test.StartCoroutine(test.Run(language));
        }

        private IEnumerator Run(string language)
        {
            try { if (Directory.Exists(SaveStorage.SaveFolder(SaveName))) Directory.Delete(SaveStorage.SaveFolder(SaveName), true); } catch { }
            GameSettings.Current.gameplay.language = language;
            Loc.Audit = true;
            string reportPath = GameSession.AutoTestReportPath ?? Path.Combine(SaveStorage.Root, "autotest_locale.md");
            _shots = Path.Combine(Path.GetDirectoryName(reportPath), "locale_" + language);
            Directory.CreateDirectory(_shots);

            // ---- main menu
            yield return WaitScene(GameSession.MenuScene, () => UiDriver.Find("new") != null);
            yield return Visit("main menu");
            foreach (var (button, name) in new[] { ("new", "new game dialog"), ("load", "load dialog"), ("controls", "controls dialog") })
            {
                Press(button);
                yield return Visit(name);
                PressClose();
            }
            Press("settings");
            foreach (var tab in new[] { "Graphics", "Audio", "Gameplay", "Interface", "Controls" })
            {
                PressText(tab, "SettingsUI");
                yield return Visit("settings: " + tab);
            }
            PressClose();
            Press("report");
            yield return Visit("report a problem dialog");
            PressClose();
            Debug.LogException(new InvalidOperationException("Test exception for the audit"));
            yield return Visit("exception notice");
            PressText("Dismiss", "ProblemReport");
            Press("new");
            var field = UiDriver.Find<UnityEngine.UIElements.TextField>("new-name");
            if (field != null) field.value = SaveName;
            Press("new-start");

            // ---- space center
            yield return WaitScene(GameSession.HubScene, () => UiDriver.Find("bld-assembly") != null);
            yield return Visit("space center");
            Press("bld-tracking");
            yield return Visit("tracking station (empty)");
            PressClose();

            // ---- assembly building
            Press("bld-assembly");
            yield return WaitScene(GameSession.EditorScene, () => AssemblyScene.Instance != null && UiDriver.Find("launch") != null);
            yield return Visit("assembly building (empty)");
            foreach (var (name, label) in new[] { ("load", "load dialog"), ("flights", "flights dialog"), ("help", "help dialog") })
            {
                Press(name);
                yield return Visit("assembly: " + label);
                PressClose();
            }
            AssemblyScene.Instance.Editor.LoadDesign(FlightBootstrap.FindStarter("Luma Pathfinder"));
            yield return Visit("assembly building (a craft)");
            Press("env");
            yield return Visit("assembly: other environment");
            foreach (var category in new[] { "Command", "Tanks", "Engines", "Boosters", "Coupling", "Recovery", "Landing", "Utility", "Aero" })
            {
                PressText(category, "EditorUI");
                yield return Visit("assembly: parts, " + category);
            }
            Press("launch");

            // ---- flight
            yield return WaitScene(GameSession.FlightScene, () => FlightSceneController.Instance != null && FlightSceneController.Instance.Sim.ActiveVessel != null && TAP.UI.PauseMenu.Instance != null);
            yield return new WaitForSeconds(2f);
            yield return Visit("flight display (on the pad)");
            TAP.UI.PauseMenu.Instance.TogglePause();
            yield return Visit("pause menu");
            TAP.UI.PauseMenu.Instance.ToggleHelp();
            yield return Visit("control guide");
            TAP.UI.PauseMenu.Instance.ToggleHelp();
            TAP.UI.PauseMenu.Instance.TogglePause();
            FlightSceneController.Instance.ToggleMap();
            yield return new WaitForSeconds(1f);
            yield return Visit("map view");
            FlightSceneController.Instance.ToggleMap();

            Finish(reportPath);
        }

        // ------------------------------------------------------------------ steps

        private void Press(string name)
        {
            var b = UiDriver.Find<UnityEngine.UIElements.Button>(name) ?? UiDriver.FindByText(name);
            if (b == null) { _report.AppendLine($"(no button {name})"); return; }
            UiDriver.Press(b);
        }

        private void PressClose()
        {
            var b = UiDriver.FindByClass("dialog__close");
            if (b == null) { _report.AppendLine("(no dialog to close)"); return; }
            UiDriver.Press(b);
        }

        private void PressText(string text, string document)
        {
            var b = UiDriver.FindByText(text, document);
            if (b == null) { _report.AppendLine($"(no button {text} in {document})"); return; }
            UiDriver.Press(b);
        }

        /// <summary>Lets the screen settle, records what is new in the missing list, and takes a screenshot.</summary>
        private IEnumerator Visit(string screen)
        {
            for (int i = 0; i < 6; i++) yield return null;
            var fresh = Loc.Missing.Where(p => _reported.Add(p)).OrderBy(p => p, StringComparer.Ordinal).ToList();
            _total += fresh.Count;
            _report.AppendLine($"## {screen}: {fresh.Count} not in the table");
            foreach (var p in fresh) _report.AppendLine("    " + p.Replace("\n", "\\n"));
            string safe = new string(screen.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            try { ScreenCapture.CaptureScreenshot(Path.Combine(_shots, $"{_shot++:00}_{safe}.png")); } catch { }
        }

        private IEnumerator WaitScene(string scene, Func<bool> ready)
        {
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!(SceneManager.GetActiveScene().name == scene && ready()))
            {
                if (Time.realtimeSinceStartup > deadline) { _report.AppendLine($"!! never reached {scene}"); yield break; }
                yield return null;
            }
            for (int i = 0; i < 5; i++) yield return null;
        }

        private void Finish(string reportPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# The Astraea Program locale audit ({GameSettings.Current.gameplay.language})");
            sb.AppendLine($"Result: {(_total == 0 ? "PASSED" : "FAILED")}");
            sb.AppendLine($"Texts shown that are not in Strings/en.json: {_total} (of {Loc.Seen.Count} patterns shown)");
            sb.AppendLine($"The English table has {Loc.EnglishStrings.Count} texts.");
            sb.AppendLine();
            sb.Append(_report);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, sb.ToString());
                File.WriteAllLines(Path.ChangeExtension(reportPath, ".seen.txt"), Loc.Seen.OrderBy(p => p, StringComparer.Ordinal));
                Debug.Log("[AutoTest] report written to " + reportPath);
            }
            catch (Exception e) { Debug.LogError("report write failed: " + e.Message); }
            Debug.Log(sb.ToString());
            if (!Application.isEditor) Application.Quit(_total == 0 ? 0 : 1);
        }
    }
}
