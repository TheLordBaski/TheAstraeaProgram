using System.Collections.Generic;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>Right-click a part to open its action window (actions from its modules + live status).</summary>
    public sealed class PartActionMenu : MonoBehaviour
    {
        public FlightSceneController Scene;
        private VisualElement _panel, _buttons, _resources, _stats;
        private Label _title, _info;
        private Part _part;
        private int _actionSig;
        // bar rows: one per resource, then skin, internal and joint load
        private readonly List<(VisualElement bar, Label val)> _resRows = new List<(VisualElement, Label)>(), _statRows = new List<(VisualElement, Label)>();
        private Label _mass;

        public static PartActionMenu Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("PartActions", 40, "PartActions");
            var root = doc.rootVisualElement;
            var m = doc.gameObject.AddComponent<PartActionMenu>();
            m.Scene = scene;
            m._panel = root.Q("part-window");
            m._title = root.Q<Label>("part-title");
            m._info = root.Q<Label>("part-info");
            m._buttons = root.Q("part-actions");
            m._resources = root.Q("part-resources");
            m._stats = root.Q("part-stats");
            foreach (var name in new[] { "SKIN", "INTERNAL", "JOINT LOAD" }) m._statRows.Add(Row(m._stats, name));
            var mass = UIKit.Element("fd-kv");
            mass.Add(UIKit.Label("MASS", "fd-kv__key"));
            m._mass = UIKit.Label("", "fd-kv__val no-loc");
            mass.Add(m._mass);
            m._stats.Add(mass);
            root.Q<Button>("part-close").clicked += m.Close;
            UIKit.Show(m._panel, false);
            return m;
        }

        public void Close()
        {
            _part = null;
            UIKit.Show(_panel, false);
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
            if (cam == null || UIKit.Pick(screen) != null || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
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
            UIKit.Show(_panel, true);
            UIKit.Place(_panel, UIKit.ScreenToPanel(screen) + new Vector2(18, 10));
            _actionSig = -1;
            _resources.Clear();
            _resRows.Clear();
            Refresh();
        }

        private static (VisualElement bar, Label val) Row(VisualElement parent, string name)
        {
            var row = UIKit.Element("part-res");
            row.Add(UIKit.Label(name, "part-res__name"));
            var bar = UIKit.Bar(false, "part-res__bar");
            row.Add(bar);
            var val = UIKit.Label("", "part-res__val no-loc");
            row.Add(val);
            parent.Add(row);
            return (bar, val);
        }

        private static void SetRow((VisualElement bar, Label val) row, double value, double max, string text, Color color)
        {
            UIKit.SetBar(row.bar, max > 0 ? (float)(value / max) : 0, color);
            row.val.text = text;
        }

        /// <summary>Green while there is plenty, amber when low; temperatures and loads the other way round.</summary>
        private static Color Level(double f, bool high) => (high ? f > 0.85 : f < 0.15) ? FlightHud.Red : (high ? f > 0.6 : f < 0.35) ? FlightHud.Amber : high ? FlightHud.Cyan : FlightHud.Green;

        private void Refresh()
        {
            var p = _part;
            _title.text = p.Def.title;
            var sb = new StringBuilder();
            var info = new List<string>();
            foreach (var m in p.Modules) m.CollectInfo(info);
            foreach (var s in info) sb.AppendLine(s);
            _info.text = sb.ToString().TrimEnd();
            _info.EnableInClassList("part-info--empty", info.Count == 0);

            if (_resRows.Count != p.Resources.Count)
            {
                _resources.Clear();
                _resRows.Clear();
                foreach (var r in p.Resources) _resRows.Add(Row(_resources, Loc.Data(r.Def.name).ToUpperInvariant()));
            }
            for (int i = 0; i < _resRows.Count; i++)
            {
                var r = p.Resources[i];
                SetRow(_resRows[i], r.Amount, r.Max, $"{r.Amount:N1} / {r.Max:N0} {r.Def.unit}", Level(r.Max > 0 ? r.Amount / r.Max : 0, false));
            }
            SetRow(_statRows[0], p.SkinTemp, p.Def.skinMaxTemp, $"{p.SkinTemp:N0} / {p.Def.skinMaxTemp:N0} K", Level(p.SkinTemp / p.Def.skinMaxTemp, true));
            SetRow(_statRows[1], p.InternalTemp, p.Def.maxTemp, $"{p.InternalTemp:N0} / {p.Def.maxTemp:N0} K", Level(p.InternalTemp / p.Def.maxTemp, true));
            SetRow(_statRows[2], p.SmoothedJointLoad, 1, $"{p.SmoothedJointLoad:P0}", Level(p.SmoothedJointLoad, true));
            _mass.text = $"{p.Mass:N0} kg";

            var actions = new List<PartAction>();
            if (p.Vessel == Scene.Sim.ActiveVessel || p.Vessel.IsEva == false)
                foreach (var m in p.Modules) m.CollectActions(actions);
            int sig = actions.Count;
            foreach (var a in actions) sig = sig * 31 + a.Label.GetHashCode();
            if (sig != _actionSig)
            {
                _actionSig = sig;
                _buttons.Clear();
                foreach (var a in actions)
                {
                    var act = a;
                    var b = UIKit.Button(act.Label, () => { act.Invoke?.Invoke(); _actionSig = -1; }, "part-action");
                    b.SetEnabled(act.Enabled && p.Vessel == Scene.Sim.ActiveVessel);
                    _buttons.Add(b);
                }
            }
        }
    }
}
