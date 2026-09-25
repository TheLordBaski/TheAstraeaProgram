using System.Collections.Generic;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>Right-click a part to open its action window (actions from its modules + live status).</summary>
    public sealed class PartActionMenu : MonoBehaviour
    {
        public FlightSceneController Scene;
        private Canvas _canvas;
        private RectTransform _panel;
        private TextMeshProUGUI _title, _info;
        private RectTransform _buttons;
        private Part _part;
        private int _actionSig;

        public static PartActionMenu Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("PartActions", 40);
            var m = canvas.gameObject.AddComponent<PartActionMenu>();
            m.Scene = scene;
            m._canvas = canvas;
            var p = UIKit.Panel(canvas.transform, "PartWindow");
            m._panel = p.rectTransform;
            m._panel.pivot = new Vector2(0, 1);
            m._panel.anchorMin = m._panel.anchorMax = Vector2.zero;
            m._panel.sizeDelta = new Vector2(300, 200);
            m._title = UIKit.Label(p.transform, "", 16, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(m._title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(250, 22));
            var close = UIKit.Button(p.transform, "×", () => m.Close(), 18);
            UIKit.Place(close.Rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(26, 24));
            m._info = UIKit.Label(p.transform, "", 13, UIKit.TextColor);
            m._info.textWrappingMode = TextWrappingModes.Normal;
            m._info.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Place(m._info.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -32), new Vector2(280, 100));
            m._buttons = UIKit.Rect(p.transform, "Buttons");
            var vl = UIKit.VLayout(m._buttons.GetComponent<RectTransform>(), 4, 0);
            m._panel.gameObject.SetActive(false);
            return m;
        }

        public void Close()
        {
            _part = null;
            _panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasReleasedThisFrame && !UiState.MapActive && !UiState.Paused)
            {
                // Distinguish a click from a camera drag
                if (_dragDistance < 6f) TryOpen(mouse.position.ReadValue());
                _dragDistance = 0;
            }
            if (mouse != null && mouse.rightButton.isPressed) _dragDistance += mouse.delta.ReadValue().magnitude;
            if (_part == null) return;
            if (_part.Destroyed || _part.Vessel == null) { Close(); return; }
            Refresh();
        }

        private float _dragDistance;

        private void TryOpen(Vector2 screen)
        {
            var cam = Scene.Camera.Cam;
            if (cam == null || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            var ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f, (1 << Layers.Parts) | (1 << Layers.EVA), QueryTriggerInteraction.Ignore))
            {
                var p = hit.collider.GetComponentInParent<Part>();
                if (p != null)
                {
                    Open(p, screen);
                    return;
                }
            }
            Close();
        }

        public void Open(Part p, Vector2 screen)
        {
            _part = p;
            _panel.gameObject.SetActive(true);
            float scale = _canvas.scaleFactor;
            Vector2 pos = screen / scale + new Vector2(18, -10);
            _panel.anchoredPosition = pos;
            _actionSig = -1;
            Refresh();
        }

        private void Refresh()
        {
            var p = _part;
            _title.text = p.Def.title;
            var sb = new StringBuilder();
            var info = new List<string>();
            foreach (var m in p.Modules) m.CollectInfo(info);
            foreach (var s in info) sb.AppendLine(s);
            foreach (var r in p.Resources) sb.AppendLine($"{r.Def.name}: {r.Amount:N1}/{r.Max:N0} {r.Def.unit}");
            sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>Skin {p.SkinTemp:N0}/{p.Def.skinMaxTemp:N0} K   Internal {p.InternalTemp:N0}/{p.Def.maxTemp:N0} K</color>");
            sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>Mass {p.Mass:N0} kg   Joint load {p.SmoothedJointLoad:P0}</color>");
            _info.text = sb.ToString();
            float infoH = _info.GetPreferredValues(_info.text, 280, 0).y;
            _info.rectTransform.sizeDelta = new Vector2(280, infoH);

            var actions = new List<PartAction>();
            if (p.Vessel == Scene.Sim.ActiveVessel || p.Vessel.IsEva == false)
                foreach (var m in p.Modules) m.CollectActions(actions);
            int sig = actions.Count;
            foreach (var a in actions) sig = sig * 31 + a.Label.GetHashCode();
            if (sig != _actionSig)
            {
                _actionSig = sig;
                foreach (Transform c in _buttons) Destroy(c.gameObject);
                foreach (var a in actions)
                {
                    var act = a;
                    var b = UIKit.Button(_buttons, act.Label, () => { act.Invoke?.Invoke(); _actionSig = -1; }, 14);
                    UIKit.Size(b.Button, 28);
                    b.SetInteractable(act.Enabled && p.Vessel == Scene.Sim.ActiveVessel);
                }
            }
            float btnH = actions.Count * 32;
            UIKit.Place(_buttons, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -36 - infoH), new Vector2(280, btnH));
            _panel.sizeDelta = new Vector2(300, 46 + infoH + btnH);
        }
    }
}
