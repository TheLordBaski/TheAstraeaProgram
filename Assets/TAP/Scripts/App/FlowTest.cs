using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TAP.App
{
    /// <summary>
    /// Autotest <c>flow</c> (FND-07): walks the scene loop through the real screens, clicking the same buttons a player does:
    /// menu → hub → assembly → launch → recover → hub → assembly → launch → hub → tracking station → fly a vessel → hub.
    /// Started by the main menu; lives across the scene loads.
    /// </summary>
    public sealed class FlowTest : MonoBehaviour
    {
        private const string SaveName = "AutoTest";
        private readonly List<(string check, bool pass, string detail)> _checks = new List<(string, bool, string)>();
        private readonly List<string> _log = new List<string>();
        private bool _failed;
        private string _padVesselId;

        public static void Launch()
        {
            var go = new GameObject("FlowTest");
            DontDestroyOnLoad(go);
            go.AddComponent<FlowTest>();
        }

        private IEnumerator Start()
        {
            // A fresh save under the autotest name, so a run never touches the player's games.
            try { if (Directory.Exists(SaveStorage.SaveFolder(SaveName))) Directory.Delete(SaveStorage.SaveFolder(SaveName), true); } catch (Exception e) { Note("could not clear the old save: " + e.Message); }
            IEnumerator body = Flow();
            while (true)
            {
                try { if (!body.MoveNext()) break; }
                catch (Exception e) { Note("EXCEPTION: " + e); _failed = true; break; }
                if (_failed) break;
                yield return body.Current;
            }
            Finish();
        }

        private IEnumerator Flow()
        {
            yield return WaitScene(GameSession.MenuScene, () => FindUi("new") != null);
            var field = FindUi<TextField>("new-name");
            Click("new");
            if (field != null) field.value = SaveName;
            Click("new-start");

            yield return WaitScene(GameSession.HubScene, () => FindUi("bld-assembly") != null);
            Check("menu → hub", GameSession.Save != null && GameSession.Save.saveName == SaveName, $"save {GameSession.Save?.saveName}");

            // First rocket: launched, then recovered from the pause menu.
            Click("bld-assembly");
            yield return WaitScene(GameSession.EditorScene, () => AssemblyScene.Instance != null && FindUi("launch") != null);
            Check("hub → assembly building", true, "");
            AssemblyScene.Instance.Editor.LoadDesign(FlightBootstrap.FindStarter("Meridian Orbiter"));
            yield return null;
            Click("launch");
            yield return WaitScene(GameSession.FlightScene, FlightReady);
            Check("assembly → launch", FlightSceneController.Instance.Sim.ActiveVessel.Situation == Situation.Prelaunch, "rocket on the pad");
            yield return new WaitForSeconds(2f);
            TAP.UI.PauseMenu.Instance.TogglePause();
            yield return null;
            Click("recover");
            yield return WaitScene(GameSession.HubScene, () => FindUi("bld-assembly") != null);
            Check("recover → hub", GameSession.Save.vessels.Find(v => v.kind == VesselKind.Ship) == null, "the recovered rocket is gone from the save");
            var note = FindUi<Label>("hub-note");
            Check("hub says what happened", note != null && note.text.Contains("recovered"), note?.text);

            // Second rocket: left on the pad when leaving the flight.
            Click("bld-assembly");
            yield return WaitScene(GameSession.EditorScene, () => AssemblyScene.Instance != null && FindUi("launch") != null);
            AssemblyScene.Instance.Editor.LoadDesign(FlightBootstrap.FindStarter("Meridian Orbiter"));
            yield return null;
            Click("launch");
            yield return WaitScene(GameSession.FlightScene, FlightReady);
            yield return new WaitForSeconds(2f);
            TAP.UI.PauseMenu.Instance.TogglePause();
            yield return null;
            Click("leave");
            yield return WaitScene(GameSession.HubScene, () => FindUi("bld-tracking") != null);
            var onPad = GameSession.Save.vessels.Find(v => v.kind == VesselKind.Ship);
            Check("leave flight → hub keeps the vessel", onPad != null, onPad?.name ?? "no vessel in the save");
            if (onPad == null) yield break;
            _padVesselId = onPad.id;

            // Tracking station: fly the vessel.
            Click("bld-tracking");
            yield return null;
            Check("tracking station lists the vessel", FindUi("fly-" + _padVesselId) != null, onPad.name);
            Click("fly-" + _padVesselId);
            yield return WaitScene(GameSession.FlightScene, FlightReady);
            Check("tracking station → fly a vessel", FlightSceneController.Instance.Sim.ActiveVessel.Id == _padVesselId, FlightSceneController.Instance.Sim.ActiveVessel.VesselName);
            yield return new WaitForSeconds(1f);
            TAP.UI.PauseMenu.Instance.TogglePause();
            yield return null;
            Click("leave");
            yield return WaitScene(GameSession.HubScene, () => FindUi("bld-tracking") != null);
            Check("flight → hub", true, "back at the space center");

            // The pad flies the rocket that is waiting on it.
            Click("bld-pad");
            yield return WaitScene(GameSession.FlightScene, FlightReady);
            Check("launch pad → fly the rocket on it", FlightSceneController.Instance.Sim.ActiveVessel.Id == _padVesselId, "");
        }

        // ------------------------------------------------------------------ helpers

        private IEnumerator WaitScene(string scene, Func<bool> ready)
        {
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!(SceneManager.GetActiveScene().name == scene && ready()))
            {
                if (Time.realtimeSinceStartup > deadline) { Check("reached " + scene, false, $"still in {SceneManager.GetActiveScene().name} after 120 s"); yield break; }
                yield return null;
            }
            // The loading screen leaves two frames after the scene starts; wait it out so the next click sees the new screen.
            for (int i = 0; i < 5; i++) yield return null;
            Note("in " + scene);
        }

        private static bool FlightReady() =>
            FlightSceneController.Instance != null && FlightSceneController.Instance.Sim != null && FlightSceneController.Instance.Sim.ActiveVessel != null
            && TAP.UI.PauseMenu.Instance != null;

        private static VisualElement FindUi(string name) => FindUi<VisualElement>(name);

        private static T FindUi<T>(string name) where T : VisualElement
        {
            foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (doc.rootVisualElement == null) continue;
                var e = doc.rootVisualElement.Q<T>(name);
                if (e != null) return e;
            }
            return null;
        }

        /// <summary>Presses a button as a keyboard or controller would: the same handler a mouse click runs.</summary>
        private void Click(string name)
        {
            var b = FindUi<Button>(name);
            if (b == null) { Check("button " + name + " exists", false, "not on screen"); return; }
            Note("click " + name);
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
        }

        private void Check(string name, bool pass, string detail)
        {
            _checks.Add((name, pass, detail));
            Note($"CHECK {(pass ? "PASS" : "FAIL")}: {name} — {detail}");
            if (!pass) _failed = true;
        }

        private void Note(string msg)
        {
            string line = $"[AutoTest flow {Time.realtimeSinceStartup,7:F1}] {msg}";
            _log.Add(line);
            Debug.Log(line);
        }

        private void Finish()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# The Astraea Program automated mission report: flow");
            sb.AppendLine($"Result: {(_failed ? "FAILED" : "PASSED")}");
            sb.AppendLine($"Checks: {_checks.FindAll(c => c.pass).Count}/{_checks.Count} passed");
            sb.AppendLine();
            foreach (var c in _checks) sb.AppendLine($"- [{(c.pass ? "x" : " ")}] {c.check}: {c.detail}");
            sb.AppendLine();
            sb.AppendLine("## Log");
            foreach (var l in _log) sb.AppendLine("    " + l);
            string text = sb.ToString();
            Debug.Log(text);
            try
            {
                string path = GameSession.AutoTestReportPath;
                if (string.IsNullOrEmpty(path)) path = Path.Combine(SaveStorage.Root, "autotest_flow.md");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text);
                Debug.Log("[AutoTest] report written to " + path);
            }
            catch (Exception e) { Debug.LogError("report write failed: " + e.Message); }
            if (!Application.isEditor) Application.Quit(_failed ? 1 : 0);
        }
    }
}
