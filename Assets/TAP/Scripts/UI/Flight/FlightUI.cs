using System.Text;
using TAP.Game;
using TMPro;
using UnityEngine;

namespace TAP.UI
{
    /// <summary>Creates all flight-scene UI.</summary>
    public static class FlightUI
    {
        public static void Create(FlightSceneController scene)
        {
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
        private RectTransform _panel;
        /// <summary>Distance from the top of the screen to the guide's lower edge (0 while hidden), for panel layout.</summary>
        public static float BottomFromTop;
        private TextMeshProUGUI _text;
        private UIKit.ButtonRef _toggle;
        private bool _expanded = true;
        private float _timer;

        public static MissionGuide Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("MissionGuide", 12);
            var g = canvas.gameObject.AddComponent<MissionGuide>();
            g.Scene = scene;
            var p = UIKit.Panel(canvas.transform, "Guide");
            g._panel = p.rectTransform;
            UIKit.Place(g._panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -270), new Vector2(330, 300));
            g._text = UIKit.Label(p.transform, "", 14, UIKit.TextColor);
            g._text.textWrappingMode = TextWrappingModes.Normal;
            g._text.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(g._text.rectTransform, 12, 12, 30, 8);
            var title = UIKit.Label(p.transform, "MISSION: TO LUMA AND BACK", 13, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(260, 18));
            g._toggle = UIKit.Button(p.transform, "–", () => g._expanded = !g._expanded, 16);
            UIKit.Place(g._toggle.Rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -4), new Vector2(26, 22));
            UIKit.Tooltip(g._toggle.Button.gameObject, () => "Collapse / expand the mission guide");
            scene.UiVisibilityChanged += visible => canvas.enabled = visible;
            return g;
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0) return;
            _timer = 0.5f;
            var tr = Scene.Tracker;
            if (tr == null) return;
            bool hide = UiState.MapActive;
            _panel.gameObject.SetActive(!hide);
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
            }
            if (cur != null) sb.AppendLine($"\n<size=13><color=#{UIKit.Hex(UIKit.Warn)}>Hint:</color> {cur.Hint}</size>");
            else sb.AppendLine($"\n<color=#{UIKit.Hex(UIKit.Good)}><b>Mission complete — welcome home!</b></color>");
            _text.text = sb.ToString();
            _toggle.Text.text = _expanded ? "–" : "+";
            float h = _text.GetPreferredValues(_text.text, 306, 0).y + 40;
            _panel.sizeDelta = new Vector2(330, h);
            BottomFromTop = 270 + h;
        }
    }
}
