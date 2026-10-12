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

    /// <summary>Collapsible list of the objectives of the mission guide in use, with a hint for the next one (the tutorial).</summary>
    public sealed class MissionGuide : MonoBehaviour
    {
        public FlightSceneController Scene;
        private VisualElement _panel;
        /// <summary>Distance from the top of the screen to the guide's lower edge (0 while hidden), for panel layout.</summary>
        public static float BottomFromTop;
        private Label _text, _title;
        private Button _toggle, _next;
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
            g._title = root.Q<Label>("guide-title");
            g._next = root.Q<Button>("guide-set");
            g._next.clicked += () => { g.Scene.Tracker?.NextSet(); g._timer = 0; };
            UIKit.Tooltip(g._next, () => "Switch to the next mission guide");
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
            var set = tr.Set;
            if (set == null) { UIKit.Show(_panel, false); return; }
            _title.text = tr.Text(set.title).ToUpperInvariant();
            UIKit.Show(_next, ObjectiveSets.All.Count > 1);
            var cur = tr.Current;
            if (_expanded)
            {
                foreach (var m in set.objectives)
                {
                    string title = tr.Text(m.title);
                    bool done = tr.Has(m.id);
                    if (done) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Good)}>✔ {title}</color>");
                    else if (m == cur) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Accent)}>▶ <b>{title}</b></color>");
                    else sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>○ {title}</color>");
                }
                sb.AppendLine();
            }
            if (cur != null) sb.Append($"<size=13><color=#{UIKit.Hex(UIKit.Warn)}>Hint:</color> {tr.Text(cur.hint)}</size>");
            else sb.Append($"<color=#{UIKit.Hex(UIKit.Good)}><b>Mission complete — welcome home!</b></color>");
            _text.text = sb.ToString();
            _toggle.text = _expanded ? "–" : "+";
        }
    }
}
