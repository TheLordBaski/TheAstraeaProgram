using System.Text;
using TAP.Game;
using TAP.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>Esc menu (pause, saves, reverts, leave) and the F1 control guide.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public FlightSceneController Scene;
        private Canvas _canvas;
        private GameObject _pausePanel, _helpPanel, _confirmPanel;
        private TextMeshProUGUI _confirmText;
        private System.Action _confirmAction;
        private UIKit.ButtonRef _recoverBtn;
        private float _prevTimeScale = 1f;

        public static PauseMenu Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("PauseMenu", 100);
            var pm = canvas.gameObject.AddComponent<PauseMenu>();
            pm.Scene = scene;
            pm._canvas = canvas;
            Instance = pm;
            pm.Build();
            scene.Input.TogglePause = pm.TogglePause;
            scene.Input.ToggleHelp = pm.ToggleHelp;
            return pm;
        }

        private void Build()
        {
            var root = (RectTransform)_canvas.transform;
            // Pause panel
            var dim = UIKit.Rect(root, "Dim");
            UIKit.Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.5f);
            _pausePanel = dim.gameObject;
            var p = UIKit.Panel(dim, "Pause");
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 666));
            UIKit.VLayout(p, 8, 20);
            var title = UIKit.Label(p.transform, "PAUSED", 30, UIKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UIKit.Size(title, 44);
            AddButton(p.transform, "Resume  [Esc]", TogglePause);
            AddButton(p.transform, "Quicksave  [F5]", () => { Scene.Quicksave(); TogglePause(); });
            AddButton(p.transform, "Quickload  [hold F9]", () => { Unpause(); Scene.Quickload(); });
            AddButton(p.transform, "Restart flight (revert to launch)", () => Confirm("Revert to the moment of launch? Progress since launch is lost.", () => { Unpause(); Scene.RevertToLaunch(); }));
            AddButton(p.transform, "Revert to assembly building", () => Confirm("Discard this flight and return to the assembly building?", () => { Unpause(); Scene.RevertToEditor(); }));
            AddButton(p.transform, "Leave flight (keep vessels in play)", () => { Unpause(); Scene.LeaveToEditor(); });
            _recoverBtn = AddButton(p.transform, "Recover vessel", () => { Unpause(); Scene.RecoverActive(); });
            AddButton(p.transform, "Control guide  [F1]", () => { ToggleHelp(); });
            AddButton(p.transform, "Developer tools  [Alt+F12]", () => { TogglePause(); DevWindow.Instance?.Toggle(); });
            AddButton(p.transform, "Main menu", () => Confirm("Save and return to the main menu?", () => { Unpause(); Scene.LeaveToMenu(); }));
            AddButton(p.transform, "Quit to desktop", () => Confirm("Save and quit?", () => { Scene.CaptureSave(); GameSession.WritePersistent(); Application.Quit(); }));
            _pausePanel.SetActive(false);

            // Confirm dialog
            var cd = UIKit.Rect(root, "ConfirmDim");
            UIKit.Stretch(cd);
            cd.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            _confirmPanel = cd.gameObject;
            var cp = UIKit.Panel(cd, "Confirm");
            UIKit.Place(cp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 200));
            _confirmText = UIKit.Label(cp.transform, "", 19, UIKit.TextColor, TextAlignmentOptions.Center);
            _confirmText.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Place(_confirmText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(480, 100));
            var yes = UIKit.Button(cp.transform, "Yes", () => { _confirmPanel.SetActive(false); _confirmAction?.Invoke(); }, 18);
            UIKit.Place(yes.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 20), new Vector2(150, 40));
            var no = UIKit.Button(cp.transform, "Cancel", () => _confirmPanel.SetActive(false), 18);
            UIKit.Place(no.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(90, 20), new Vector2(150, 40));
            _confirmPanel.SetActive(false);

            // Help
            var hp = UIKit.Panel(root, "Help", new Color(0.04f, 0.05f, 0.07f, 0.95f));
            UIKit.Place(hp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 760));
            _helpPanel = hp.gameObject;
            var ht = UIKit.Label(hp.transform, BuildHelp(), 16, UIKit.TextColor);
            ht.textWrappingMode = TextWrappingModes.Normal;
            ht.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(ht.rectTransform, 26, 26, 20, 50);
            var close = UIKit.Button(hp.transform, "Close  [F1]", ToggleHelp, 17);
            UIKit.Place(close.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(160, 34));
            _helpPanel.SetActive(false);
        }

        private UIKit.ButtonRef AddButton(Transform parent, string label, System.Action a)
        {
            var b = UIKit.Button(parent, label, a, 18);
            UIKit.Size(b.Button, 42);
            return b;
        }

        private void Confirm(string text, System.Action a)
        {
            _confirmText.text = text;
            _confirmAction = a;
            _confirmPanel.SetActive(true);
        }

        public static string BuildHelp()
        {
            var sb = new StringBuilder();
            string acc = UIKit.Hex(UIKit.Accent), dim = UIKit.Hex(UIKit.TextDim);
            sb.AppendLine($"<size=26><b><color=#{acc}>CONTROL GUIDE</color></b></size>");
            sb.AppendLine();
            sb.Append("<b>FLIGHT</b>\n");
            int i = 0;
            foreach (var (k, a) in Bindings.Flight)
            {
                sb.Append($"<color=#{acc}>{k,-18}</color> {a}");
                sb.Append(i % 2 == 0 ? "<pos=50%>" : "\n");
                i++;
            }
            sb.AppendLine("\n");
            sb.Append("<b>EVA</b>\n");
            foreach (var (k, a) in Bindings.Eva) sb.AppendLine($"<color=#{acc}>{k}</color>  {a}");
            sb.AppendLine();
            sb.Append("<b>MAP</b>\n");
            foreach (var (k, a) in Bindings.Map) sb.AppendLine($"<color=#{acc}>{k}</color>  {a}");
            sb.AppendLine();
            sb.AppendLine($"<color=#{dim}>Tips: launch east (press W briefly at ~60 m/s, then SAS Prograde). Circularise at apoapsis with a maneuver node. " +
                          "For Luma: target Luma in the map, add a node on your orbit and drag prograde until an encounter appears. " +
                          "Point the heat shield retrograde for reentry and stage the parachute below ~18 km.</color>");
            return sb.ToString();
        }

        public void TogglePause()
        {
            if (_helpPanel.activeSelf) { _helpPanel.SetActive(false); if (!_pausePanel.activeSelf) return; }
            if (_pausePanel.activeSelf) Unpause();
            else
            {
                _pausePanel.SetActive(true);
                UiState.Paused = true;
                Scene.Sim.Paused = true;
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0;
                _recoverBtn.SetInteractable(Scene.Sim.CanRecover(Scene.Sim.ActiveVessel));
            }
        }

        private void Unpause()
        {
            _pausePanel.SetActive(false);
            _confirmPanel.SetActive(false);
            UiState.Paused = false;
            Scene.Sim.Paused = false;
            Time.timeScale = _prevTimeScale <= 0 ? 1 : _prevTimeScale;
        }

        public void ToggleHelp()
        {
            _helpPanel.SetActive(!_helpPanel.activeSelf);
            UiState.HelpOpen = _helpPanel.activeSelf;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UiState.Paused = false;
            Time.timeScale = 1;
        }
    }
}
