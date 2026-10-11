using System.Text;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>Creates all flight-scene UI.</summary>
    public static class FlightUI
    {
        public static void Create(FlightSceneController scene)
        {
            BodyLabels.Create(scene);
            FlightHud.Create(scene);
            PauseMenu.Create(scene);
            PartActionMenu.Create(scene);
            MapUI.Create(scene);
            MissionGuide.Create(scene);
            DevWindow.Create(scene);
        }
    }

    /// <summary>Collapsible objective list for the complete lunar mission with contextual hints (tutorial).</summary>
    public sealed class MissionGuide : MonoBehaviour
    {
        public FlightSceneController Scene;
        private VisualElement _panel;
        /// <summary>Distance from the top of the screen to the guide's lower edge (0 while hidden), for panel layout.</summary>
        public static float BottomFromTop;
        private Label _text;
        private Button _toggle;
        private bool _expanded = true;
        private float _timer;

        public static MissionGuide Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("MissionGuide", 12, "MissionGuide");
            var root = doc.rootVisualElement;
            var g = doc.gameObject.AddComponent<MissionGuide>();
            g.Scene = scene;
            g._panel = root.Q("guide");
            g._text = root.Q<Label>("guide-text");
            var sys = TAP.Core.CelestialSystem.Default;
            var moon = sys.HomeMoon ?? sys.HomeBody;
            root.Q<Label>("guide-title").text = $"MISSION: TO {moon.Name.ToUpperInvariant()} AND BACK";
            g._toggle = root.Q<Button>("guide-toggle");
            g._toggle.clicked += () => { g._expanded = !g._expanded; g._timer = 0; };
            UIKit.Tooltip(g._toggle, () => "Collapse / expand the mission guide");
            scene.UiVisibilityChanged += visible => UIKit.Show(root, visible);
            return g;
        }

        private void Update()
        {
            // The guide's lower edge, measured every frame (the panel grows with its text).
            var r = _panel.layout;
            if (UIKit.Shown(_panel) && !float.IsNaN(r.height)) BottomFromTop = r.yMax;
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0) return;
            _timer = 0.5f;
            var tr = Scene.Tracker;
            if (tr == null) return;
            bool hide = UiState.MapActive;
            UIKit.Show(_panel, !hide);
            if (hide) { BottomFromTop = 0; return; }
            var sb = new StringBuilder();
            var cur = tr.Current;
            if (_expanded)
            {
                foreach (var m in MissionTracker.Lunar)
                {
                    bool done = tr.Has(m.Id);
                    if (done) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Good)}>✔ {m.Title}</color>");
                    else if (m == cur) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Accent)}>▶ <b>{m.Title}</b></color>");
                    else sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>○ {m.Title}</color>");
                }
                sb.AppendLine();
            }
            if (cur != null) sb.Append($"<size=13><color=#{UIKit.Hex(UIKit.Warn)}>Hint:</color> {cur.Hint}</size>");
            else sb.Append($"<color=#{UIKit.Hex(UIKit.Good)}><b>Mission complete — welcome home!</b></color>");
            _text.text = sb.ToString();
            _toggle.text = _expanded ? "–" : "+";
        }
    }
}
