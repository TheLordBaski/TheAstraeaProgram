using System.Text;
using TAP.Game;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>Esc menu (pause, saves, reverts, leave) and the F1 control guide. Layout: Resources/UI/PauseMenu.uxml.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public FlightSceneController Scene;
        private VisualElement _root, _pausePanel, _helpPanel, _confirmPanel;
        private Label _confirmText;
        private System.Action _confirmAction;
        private Button _recoverBtn;
        private float _prevTimeScale = 1f;

        public static PauseMenu Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("PauseMenu", 100, "PauseMenu");
            var pm = doc.gameObject.AddComponent<PauseMenu>();
            pm.Scene = scene;
            pm._root = doc.rootVisualElement;
            Instance = pm;
            pm.Build();
            scene.Input.TogglePause = pm.TogglePause;
            scene.Input.ToggleHelp = pm.ToggleHelp;
            return pm;
        }

        private void Build()
        {
            _pausePanel = _root.Q("pause");
            _confirmPanel = _root.Q("confirm");
            _helpPanel = _root.Q("help");
            _confirmText = _root.Q<Label>("confirm-text");

            void On(string name, System.Action a) => _root.Q<Button>(name).clicked += a;
            On("resume", TogglePause);
            On("quicksave", () => { Scene.Quicksave(); TogglePause(); });
            On("quickload", () => { Unpause(); Scene.Quickload(); });
            On("revert-launch", () => Confirm("Revert to the moment of launch? Progress since launch is lost.", () => { Unpause(); Scene.RevertToLaunch(); }));
            On("revert-editor", () => Confirm("Discard this flight and return to the assembly building?", () => { Unpause(); Scene.RevertToEditor(); }));
            On("leave", () => { Unpause(); Scene.LeaveToHub(); });
            _recoverBtn = _root.Q<Button>("recover");
            _recoverBtn.clicked += () => { Unpause(); Scene.RecoverActive(); };
            On("controls-guide", ToggleHelp);
            On("settings", SettingsUI.Open);
            On("dev", () => { TogglePause(); DevWindow.Instance?.Toggle(); });
            On("main-menu", () => Confirm("Save and return to the main menu?", () => { Unpause(); Scene.LeaveToMenu(); }));
            On("quit", () => Confirm("Save and quit?", () => { Scene.CaptureSave(); GameSession.WritePersistent(); Application.Quit(); }));

            On("confirm-yes", () => { UIKit.Show(_confirmPanel, false); _confirmAction?.Invoke(); });
            On("confirm-no", () => UIKit.Show(_confirmPanel, false));

            BuildHelp(_root.Q("help-body"));
            On("help-close", ToggleHelp);

            UIKit.Show(_pausePanel, false);
            UIKit.Show(_confirmPanel, false);
            UIKit.Show(_helpPanel, false);
        }

        private void Confirm(string text, System.Action a)
        {
            _confirmText.text = text;
            _confirmAction = a;
            UIKit.Show(_confirmPanel, true);
        }

        /// <summary>The F1 control guide: flight keys in two columns, then EVA, map and tips.</summary>
        public static void BuildHelp(VisualElement into)
        {
            into.Add(UIKit.Label("CONTROL GUIDE", "help-title"));
            void Section(string title, (string keys, string action)[] rows, bool twoColumns)
            {
                into.Add(UIKit.Label(title, "help-section"));
                var grid = UIKit.Element("help-grid");
                foreach (var (k, a) in rows)
                {
                    var cell = UIKit.Element(twoColumns ? "help-cell help-cell--half" : "help-cell");
                    cell.Add(UIKit.Label(k, "help-key"));
                    cell.Add(UIKit.Label(a, "help-action"));
                    grid.Add(cell);
                }
                into.Add(grid);
            }
            Section("FLIGHT", Bindings.Flight, true);
            Section("EVA", Bindings.Eva, true);
            Section("MAP", Bindings.Map, true);
            into.Add(UIKit.Label("Tips: launch east (press W briefly at ~60 m/s, then SAS Prograde). Circularise at apoapsis with a maneuver node. " +
                                 "For Luma: target Luma in the map, add a node on your orbit and drag prograde until an encounter appears. " +
                                 "Point the heat shield retrograde for reentry and stage the parachute below ~18 km.", "help-tips text-dim wrap"));
        }

        public void TogglePause()
        {
            if (SettingsUI.IsOpen) { SettingsUI.Close(); return; }
            if (UIKit.Shown(_helpPanel)) { ToggleHelp(); if (!UIKit.Shown(_pausePanel)) return; }
            if (UIKit.Shown(_pausePanel)) Unpause();
            else
            {
                UIKit.Show(_pausePanel, true);
                UiState.Paused = true;
                Scene.Sim.Paused = true;
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0;
                _recoverBtn.SetEnabled(Scene.Sim.CanRecover(Scene.Sim.ActiveVessel));
            }
        }

        private void Unpause()
        {
            UIKit.Show(_pausePanel, false);
            UIKit.Show(_confirmPanel, false);
            UiState.Paused = false;
            Scene.Sim.Paused = false;
            Time.timeScale = _prevTimeScale <= 0 ? 1 : _prevTimeScale;
        }

        public void ToggleHelp()
        {
            bool open = !UIKit.Shown(_helpPanel);
            UIKit.Show(_helpPanel, open);
            UiState.HelpOpen = open;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UiState.Paused = false;
            Time.timeScale = 1;
        }
    }
}
