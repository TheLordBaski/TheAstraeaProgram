using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>Main menu: continue / new sandbox / load game / controls / quit, over a slowly turning view of the home planet and its moon.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _loadDialog, _newDialog, _controls, _dataProblems;
        private ScrollView _loadList;
        private TextField _newName;
        /// <summary>Continue, New sandbox and Load game: off when the game data keeps a game from starting.</summary>
        private readonly List<Button> _gameButtons = new List<Button>();
        private Transform _planet, _moon;
        private string _latestSave;

        /// <param name="dataError">Why the game data keeps a game from starting (null when it doesn't).</param>
        public static MainMenuUI Create(string dataError = null)
        {
            var doc = UIKit.CreateDocument("MainMenu", 10, "MainMenu");
            var ui = doc.gameObject.AddComponent<MainMenuUI>();
            ui._root = doc.rootVisualElement;
            ui.BuildBackdrop();
            ui.Build();
            ui.BuildDataProblems(dataError);
            return ui;
        }

        // ------------------------------------------------------------------ 3D backdrop

        private void BuildBackdrop()
        {
            var camGo = new GameObject("MenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 5000f;
            camGo.tag = "MainCamera";
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            camGo.transform.position = new Vector3(0, 0, -30);
            camGo.transform.rotation = Quaternion.identity;
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null)
            {
                // Black space with stars, as the map view shows it (the material's own air is the launch site's daytime sky).
                var space = new Material(sky);
                space.SetFloat("_Atmosphere", 0);
                space.SetFloat("_StarBrightness", 0.9f);
                RenderSettings.skybox = space;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.035f, 0.05f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -0.25f, 0.55f));

            // The title picture is of the session system's home planet and its first moon.
            var sys = CelestialSystem.Default;
            _planet = Body(sys.HomeBody.Name, MapMaterial(sys.HomeBody), new Vector3(9f, -9f, 12f), 22f);
            _moon = Body(sys.HomeMoon?.Name ?? "Moon", MapMaterial(sys.HomeMoon), new Vector3(-14f, 7f, 40f), 5f);
            // Thin atmosphere rim
            var shellMat = Resources.Load<Material>("Materials/AtmosphereShell");
            if (shellMat != null)
            {
                var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(shell.GetComponent<Collider>());
                shell.transform.SetParent(_planet, false);
                shell.transform.localScale = Vector3.one * 1.03f;
                // The flight material is sized for the real planet; this one is 22 m across.
                var rim = new Material(shellMat);
                rim.SetVector("_PlanetCenter", _planet.position);
                rim.SetFloat("_PlanetRadius", 11f);
                rim.SetFloat("_AtmoRadius", 11f * 1.03f);
                rim.SetVector("_SunDir", -sun.transform.forward);
                shell.GetComponent<Renderer>().sharedMaterial = rim;
            }
        }

        private static string MapMaterial(CelestialBody b) => b == null ? null : "Materials/Map_" + b.Id.Substring(b.Id.LastIndexOf('/') + 1);

        private static Transform Body(string name, string mat, Vector3 pos, float diameter)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.name = name;
            s.transform.position = pos;
            s.transform.localScale = Vector3.one * diameter;
            var m = mat != null ? Resources.Load<Material>(mat) : null;
            if (m != null) s.GetComponent<Renderer>().sharedMaterial = m;
            // Unity's sphere UVs map longitude around Y; tilt a little for a nicer look.
            s.transform.rotation = Quaternion.Euler(12f, 0, 8f);
            return s.transform;
        }

        private void Update()
        {
            if (_planet != null) _planet.Rotate(Vector3.up, -1.2f * Time.deltaTime, Space.Self);
            if (_moon != null) _moon.Rotate(Vector3.up, -0.6f * Time.deltaTime, Space.Self);
            if (Controls.Down("Common/Cancel"))
            {
                SettingsUI.Close();
                ProblemReportUI.Close();
                UIKit.Show(_loadDialog, false);
                UIKit.Show(_newDialog, false);
                UIKit.Show(_controls, false);
                UIKit.Show(_dataProblems, false);
            }
        }

        // ------------------------------------------------------------------ menu

        private void Build()
        {
            _loadDialog = _root.Q("load-dialog");
            _newDialog = _root.Q("new-dialog");
            _controls = _root.Q("controls-dialog");
            _dataProblems = _root.Q("data-dialog");
            _loadList = _root.Q<ScrollView>("load-list");
            _newName = _root.Q<TextField>("new-name");
            // Every dialog's Close button hides its dialog.
            _root.Query<Button>(className: "dialog__close").ForEach(b =>
            {
                var dialog = UIKit.Ancestor(b, "dialog");
                b.clicked += () => UIKit.Show(dialog, false);
            });

            var cont = _root.Q<Button>("continue");
            _latestSave = FindLatestSave(out string savedAt);
            if (_latestSave != null)
            {
                cont.text = $"Continue  <size=15><color=#{UIKit.Hex(UIKit.TextDim)}>{Loc.Name(_latestSave)} · {Loc.Name(savedAt)}</color></size>";
                cont.clicked += () => Continue(_latestSave);
                _gameButtons.Add(cont);
            }
            else UIKit.Show(cont, false);
            var newBtn = _root.Q<Button>("new");
            newBtn.clicked += OpenNew;
            _gameButtons.Add(newBtn);
            var load = _root.Q<Button>("load");
            load.clicked += OpenLoad;
            _gameButtons.Add(load);
            _root.Q<Button>("controls").clicked += () => { _root.Q<ScrollView>("controls-list").Clear(); BuildControls(); UIKit.Show(_controls, true); };
            _root.Q<Button>("settings").clicked += SettingsUI.Open;
            _root.Q<Button>("report").clicked += () => ProblemReportUI.Open();
            _root.Q<Button>("quit").clicked += () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };

            _root.Q<Label>("subtitle").text = Objectives.Expand(CelestialSystem.Default, "Build rockets. Reach orbit. Walk on {moon}. Come home.");
            _root.Q<Label>("footer").text = SystemSummary() + "\n" +
                "Original work built with Unity " + Loc.Name(Application.unityVersion) + " (URP) · Version " + Loc.Name(Application.version);

            _root.Q<Button>("new-start").clicked += StartNew;
        }

        /// <summary>"A rocket-only spaceflight sandbox · Tellus (R 600 km, 1 g, 70 km atmosphere) and its airless moon Luma (R 220 km, 0.16 g, 13,000 km away)", from the data.</summary>
        private static string SystemSummary()
        {
            var sys = CelestialSystem.Default;
            var home = sys.HomeBody;
            string text = "A rocket-only spaceflight sandbox · " + $"{home.Name} (R {home.Radius / 1000:0} km, {home.SurfaceGravity / 9.80665:0.##} g" +
                          (home.Atmosphere != null ? $", {home.Atmosphere.Height / 1000:0} km atmosphere)" : ")");
            var moon = sys.HomeMoon;
            if (moon != null && moon.Orbit != null)
                text += $" and its {(moon.Atmosphere == null ? "airless " : "")}moon {moon.Name} (R {moon.Radius / 1000:0} km, {moon.SurfaceGravity / 9.80665:0.##} g, {moon.Orbit.SemiMajorAxis / 1000:N0} km away)";
            return text;
        }

        private static string FindLatestSave(out string savedAt)
        {
            savedAt = null;
            string best = null;
            DateTime bestTime = DateTime.MinValue;
            foreach (var name in SaveStorage.ListSaves())
            {
                if (name == "AutoTest") continue;
                try
                {
                    string path = SaveStorage.SlotPath(name, "persistent");
                    if (!System.IO.File.Exists(path)) continue;
                    var t = System.IO.File.GetLastWriteTime(path);
                    if (t > bestTime) { bestTime = t; best = name; }
                }
                catch { }
            }
            if (best != null) savedAt = bestTime.ToString("yyyy-MM-dd HH:mm");
            return best;
        }

        private void Continue(string saveName)
        {
            bool loaded;
            try { loaded = GameSession.LoadPersistent(saveName); }
            catch (SaveException e)
            {
                // Never start a new game under this name: that would overwrite the damaged file. The list says what is wrong.
                Debug.LogError(e.Message);
                OpenLoad();
                return;
            }
            if (!loaded) GameSession.NewGame(saveName);
            GameSession.GoToHub();
        }

        // ------------------------------------------------------------------ game data problems

        /// <summary>
        /// Problems found in the game data while loading it (FND-04): a notice under the menu opens the list. When they keep
        /// a game from starting, the game buttons are off and the list opens at once; it also opens for errors.
        /// </summary>
        private void BuildDataProblems(string dataError)
        {
            var report = ContentLog.Session;
            if (dataError == null && report.IsClean) return;
            string bad = UIKit.Hex(UIKit.Bad), warn = UIKit.Hex(UIKit.Warn), dim = UIKit.Hex(UIKit.TextDim);
            var sb = new System.Text.StringBuilder();
            if (dataError != null) sb.Append($"<color=#{bad}><b>A game can't start until these errors are fixed.</b></color>\n\n");
            else if (report.HasErrors) sb.Append("Parts and resources with errors are left out: craft that use them can't be loaded or flown until they are fixed.\n\n");
            if (dataError != null && !report.HasErrors) sb.Append($"<color=#{bad}>Error</color>  <noparse>{dataError}</noparse>\n\n");
            foreach (var p in report.Errors) sb.Append($"<color=#{bad}>Error</color>  <noparse>{p}</noparse>\n\n");
            foreach (var p in report.Warnings) sb.Append($"<color=#{warn}>Warning</color>  <noparse>{p}</noparse>\n\n");
            sb.Append($"<color=#{dim}>The files are in Assets/TAP/Resources. In the editor, TAP → Validate Content checks them all, and F8 reloads them while you play.</color>");
            _root.Q<Label>("data-text").text = sb.ToString();

            bool blocking = dataError != null || report.HasErrors;
            string label = dataError != null ? "The game data has errors: a game can't start" : "Game data: " + report.Counts;
            var notice = _root.Q<Button>("data-notice");
            notice.text = label + "  ·  details";
            notice.AddToClassList(blocking ? "blocking" : "warning");
            notice.clicked += () => UIKit.Show(_dataProblems, true);
            notice.RemoveFromClassList("hidden");
            if (dataError != null)
                foreach (var b in _gameButtons) b.SetEnabled(false);
            if (blocking) UIKit.Show(_dataProblems, true);
        }

        // ------------------------------------------------------------------ dialogs

        private void OpenNew()
        {
            string name = "Sandbox";
            int i = 2;
            while (SaveStorage.SlotExists(name, "persistent")) name = "Sandbox " + i++;
            _newName.value = name;
            UIKit.Show(_newDialog, true);
        }

        private void StartNew()
        {
            string name = SaveStorage.SanitizeFileName(string.IsNullOrWhiteSpace(_newName.value) ? "Sandbox" : _newName.value.Trim());
            if (SaveStorage.SlotExists(name, "persistent"))
            {
                // Never overwrite silently: pick a free name.
                int i = 2;
                while (SaveStorage.SlotExists(name + " " + i, "persistent")) i++;
                name = name + " " + i;
            }
            GameSession.NewGame(name);
            GameSession.GoToHub();
        }

        private void OpenLoad()
        {
            _loadList.Clear();
            int shown = 0;
            foreach (var name in SaveStorage.ListSaves())
            {
                bool hasPersistent = SaveStorage.SlotExists(name, "persistent");
                bool hasQuick = SaveStorage.SlotExists(name, "quicksave");
                if (!hasPersistent && !hasQuick) continue;
                shown++;
                string detail;
                try
                {
                    var s = SaveStorage.ReadSave(name, hasPersistent ? "persistent" : "quicksave", out string notice);
                    int ships = 0;
                    foreach (var v in s.vessels) if (v.kind == VesselKind.Ship) ships++;
                    var played = TimeSpan.FromSeconds(s.playTime);
                    detail = $"{Loc.Name(s.savedAt)} · {s.mode} · T+{Loc.Name(MathD.FormatUT(s.ut))} · {Loc.Plural(ships, "vessel", "vessels")} · {Loc.Plural(s.milestones.Count, "milestone", "milestones")} · {(int)played.TotalHours}:{played.Minutes:00} played";
                    if (notice != null) detail += " · restored from a backup";
                }
                catch (Exception e) { detail = "unreadable: " + e.Message; }
                var row = UIKit.Element("panel panel--light load-row");
                row.Add(UIKit.Label($"<b>{Loc.Name(name)}</b>\n<size=14><color=#{UIKit.Hex(UIKit.TextDim)}>{detail}</color></size>", "load-row__text"));
                string n = name;
                if (hasQuick)
                    row.Add(UIKit.Button("Quicksave", () =>
                    {
                        var s = SaveStorage.ReadSave(n, "quicksave");
                        if (s == null) return;
                        GameSession.EditorCraft = s.editorCraft;
                        GameSession.PendingMessage = "Quicksave loaded";
                        GameSession.LoadFlightState(s, FlightEntry.Quickload);
                    }, "load-row__btn"));
                if (hasPersistent) row.Add(UIKit.Button("Resume", () => Continue(n), "load-row__btn"));
                _loadList.Add(row);
            }
            if (shown == 0) _loadList.Add(UIKit.Label("No saved games yet.", "text-dim"));
            UIKit.Show(_loadDialog, true);
        }

        private void BuildControls()
        {
            var list = _root.Q<ScrollView>("controls-list");
            void Section(string title, string section)
            {
                list.Add(UIKit.Label(title, "controls-head"));
                foreach (var (k, a) in Controls.GuideRows(section))
                {
                    var row = UIKit.Element("controls-row");
                    row.Add(UIKit.Label(k, "controls-key"));
                    row.Add(UIKit.Label(a, "controls-action"));
                    list.Add(row);
                }
            }
            Section("FLIGHT", ControlTable.Flight);
            Section("EVA", ControlTable.Eva);
            Section("MAP VIEW", ControlTable.MapView);
            Section("ASSEMBLY BUILDING", ControlTable.Assembly);
            Section("DEVELOPER", ControlTable.Developer);
        }
    }
}
