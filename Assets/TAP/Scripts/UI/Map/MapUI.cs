using System;
using System.Collections.Generic;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>
    /// Map-view overlay: icons/labels for bodies, vessels, apsides, encounters, nodes and closest approach;
    /// click actions; clicking the orbit offers "add maneuver node" and "warp here"; nodes are dragged along the
    /// orbit by their marker, shaped with drag handles or the numeric editor, and deleted with the x button,
    /// a right-click on the node or the Delete key.
    /// </summary>
    public sealed class MapUI : MonoBehaviour
    {
        public FlightSceneController Scene;
        private MapView Map => Scene.Map;
        private FlightSim Sim => Scene.Sim;
        private Canvas _canvas;
        private RectTransform _root;
        private readonly List<MarkerWidget> _pool = new List<MarkerWidget>();
        private int _used;
        private GameObject _context;
        private RectTransform _contextButtons;
        private TextMeshProUGUI _contextTitle;
        private RectTransform _nodePanel;
        private TextMeshProUGUI _nodeText;
        private TMP_InputField _proField, _nrmField, _radField;
        private TextMeshProUGUI _stepText;
        private double _step = 10;
        private int _selectedNode = -1;
        private readonly Dictionary<string, Image> _handles = new Dictionary<string, Image>();
        private string _dragHandle;
        private Vector2 _dragStart;
        private Image _addPreview;
        private TextMeshProUGUI _orbitInfo;
        private RectTransform _orbitPanel;
        private TextMeshProUGUI _hint;
        private UIKit.ButtonRef _nodeDeleteBtn;
        private bool _draggingNode, _dragNodeMoved;
        private Vector2 _dragNodeStart;
        private int _rmbNode = -1;
        private Vector2 _rmbStart;
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

        private class MarkerWidget
        {
            public RectTransform Rt;
            public Image Icon;
            public TextMeshProUGUI Label;
            public TooltipTrigger Tip;
            public MapMarker Marker;
        }

        public static MapUI Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("MapUI", 20);
            var ui = canvas.gameObject.AddComponent<MapUI>();
            ui.Scene = scene;
            ui._canvas = canvas;
            ui._root = (RectTransform)canvas.transform;
            ui.Build();
            canvas.enabled = false;
            return ui;
        }

        private void Build()
        {
            _addPreview = UIKit.Image(_root, UIKit.Icon("plus"), new Color(0.3f, 0.6f, 1f, 0.9f), "AddPreview");
            _addPreview.rectTransform.sizeDelta = new Vector2(22, 22);
            _addPreview.rectTransform.anchorMin = _addPreview.rectTransform.anchorMax = Vector2.zero;
            _addPreview.enabled = false;

            // Context menu
            var cp = UIKit.Panel(_root, "Context");
            _context = cp.gameObject;
            cp.rectTransform.pivot = new Vector2(0, 1);
            cp.rectTransform.anchorMin = cp.rectTransform.anchorMax = Vector2.zero;
            cp.rectTransform.sizeDelta = new Vector2(220, 150);
            _contextTitle = UIKit.Label(cp.transform, "", 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(_contextTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(200, 20));
            _contextButtons = UIKit.Rect(cp.transform, "Buttons");
            UIKit.Place(_contextButtons, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -30), new Vector2(204, 110));
            UIKit.VLayout(_contextButtons.GetComponent<RectTransform>(), 4, 0);
            _context.SetActive(false);

            // Node gizmo handles
            foreach (var (key, icon, color) in new[]
                     {
                         ("pro", "nav_prograde", NavballWidget.Pro), ("retro", "nav_retrograde", NavballWidget.Pro),
                         ("nrm", "nav_normal", NavballWidget.Nrm), ("anrm", "nav_antinormal", NavballWidget.Nrm),
                         ("radout", "nav_radialout", NavballWidget.Rad), ("radin", "nav_radialin", NavballWidget.Rad),
                     })
            {
                var img = UIKit.Image(_root, UIKit.Icon(icon), color, "Handle " + key);
                img.rectTransform.sizeDelta = new Vector2(30, 30);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
                img.raycastTarget = true;
                string k = key;
                var trig = img.gameObject.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(e => { _dragHandle = k; _dragStart = ((PointerEventData)e).position; });
                trig.triggers.Add(down);
                UIKit.Tooltip(img.gameObject, () => $"Drag to add {HandleName(k)} delta-v (further = faster).");
                img.enabled = false;
                _handles[key] = img;
            }

            // Delete button next to the selected node
            _nodeDeleteBtn = UIKit.Button(_root, "×", DeleteSelected, 20);
            _nodeDeleteBtn.Rect.anchorMin = _nodeDeleteBtn.Rect.anchorMax = Vector2.zero;
            _nodeDeleteBtn.Rect.sizeDelta = new Vector2(26, 26);
            UIKit.Tooltip(_nodeDeleteBtn.Rect.gameObject, () => "Delete this maneuver node (or right-click the node, or press Delete)");
            _nodeDeleteBtn.Rect.gameObject.SetActive(false);

            // Node editor
            var np = UIKit.Panel(_root, "NodeEditor");
            _nodePanel = np.rectTransform;
            UIKit.Place(_nodePanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(420, 400));
            var title = UIKit.Label(np.transform, "MANEUVER NODE", 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(300, 20));
            float y = -36;
            _proField = AddComponentRow(np.transform, "Prograde", NavballWidget.Pro, y, v => EditNode(n => n.prograde = v), s => EditNode(n => n.prograde += s)); y -= 38;
            _nrmField = AddComponentRow(np.transform, "Normal", NavballWidget.Nrm, y, v => EditNode(n => n.normal = v), s => EditNode(n => n.normal += s)); y -= 38;
            _radField = AddComponentRow(np.transform, "Radial", NavballWidget.Rad, y, v => EditNode(n => n.radial = v), s => EditNode(n => n.radial += s)); y -= 40;
            // step selector
            var stepLbl = UIKit.Label(np.transform, "Step", 14, UIKit.TextDim);
            UIKit.Place(stepLbl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, y), new Vector2(60, 26));
            float sx = 70;
            foreach (double s in new[] { 0.1, 1.0, 10.0, 100.0 })
            {
                double sv = s;
                var b = UIKit.Button(np.transform, s < 1 ? "0.1" : s.ToString("0"), () => _step = sv, 14);
                UIKit.Place(b.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(sx, y), new Vector2(50, 26));
                sx += 54;
            }
            _stepText = UIKit.Label(np.transform, "", 14, UIKit.Accent);
            UIKit.Place(_stepText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(sx + 6, y), new Vector2(90, 26));
            y -= 34;
            var tl = UIKit.Label(np.transform, "Time", 14, UIKit.TextDim);
            UIKit.Place(tl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, y), new Vector2(60, 26));
            sx = 70;
            foreach (var (label, dt) in new[] { ("−orbit", -1.0), ("−10m", -600.0), ("−1m", -60.0), ("−10s", -10.0), ("+10s", 10.0), ("+1m", 60.0), ("+10m", 600.0), ("+orbit", 1.0) })
            {
                double d = dt;
                bool orbit = label.Contains("orbit");
                var b = UIKit.Button(np.transform, label, () => ShiftNodeTime(d, orbit), 12);
                UIKit.Place(b.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(sx, y), new Vector2(40, 26));
                sx += 43;
            }
            y -= 36;
            var del = UIKit.Button(np.transform, "Delete node", DeleteSelected, 14);
            UIKit.Place(del.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, y), new Vector2(120, 28));
            var warp = UIKit.Button(np.transform, "Warp to burn", WarpToNode, 14);
            UIKit.Place(warp.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(138, y), new Vector2(130, 28));
            var sas = UIKit.Button(np.transform, "SAS → node", () => { var v = Sim.ActiveVessel; if (v != null) FlightInput.SetSasMode(v, SasMode.Maneuver); }, 14);
            UIKit.Place(sas.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(274, y), new Vector2(130, 28));
            y -= 34;
            _nodeText = UIKit.Label(np.transform, "", 14, UIKit.TextColor);
            _nodeText.textWrappingMode = TextWrappingModes.Normal;
            _nodeText.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Place(_nodeText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, y), new Vector2(396, 110));
            _nodePanel.gameObject.SetActive(false);

            // Orbit info panel
            var op = UIKit.Panel(_root, "OrbitInfo");
            _orbitPanel = op.rectTransform;
            UIKit.Place(_orbitPanel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(360, 230));
            _orbitInfo = UIKit.Label(op.transform, "", 14, UIKit.TextColor);
            _orbitInfo.textWrappingMode = TextWrappingModes.Normal;
            _orbitInfo.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_orbitInfo.rectTransform, 12, 10, 8, 8);

            _hint = UIKit.Label(_root, "MAP  —  click your orbit: add node / warp here · drag a node along the orbit · right-click a node or Del: delete · click a body or vessel for actions · right-drag rotate · scroll zoom · Tab focus · M back",
                14, UIKit.TextDim, TextAlignmentOptions.Center);
            UIKit.Place(_hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1400, 20));
        }

        private static string HandleName(string k) => k switch
        {
            "pro" => "prograde", "retro" => "retrograde", "nrm" => "normal", "anrm" => "anti-normal", "radout" => "radial-out", _ => "radial-in",
        };

        private TMP_InputField AddComponentRow(Transform parent, string label, Color color, float y, Action<double> set, Action<double> add)
        {
            var l = UIKit.Label(parent, label, 15, color);
            UIKit.Place(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, y), new Vector2(90, 30));
            var minus = UIKit.Button(parent, "−", () => add(-_step), 18);
            UIKit.Place(minus.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(104, y), new Vector2(36, 30));
            var field = UIKit.Input(parent, "0.0", s => { if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)) set(d); }, 16);
            UIKit.Place((RectTransform)field.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(144, y), new Vector2(130, 30));
            var plus = UIKit.Button(parent, "+", () => add(_step), 18);
            UIKit.Place(plus.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(278, y), new Vector2(36, 30));
            var unit = UIKit.Label(parent, "m/s", 14, UIKit.TextDim);
            UIKit.Place(unit.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(320, y), new Vector2(60, 30));
            return field;
        }

        private ManeuverNodeRecord Selected
        {
            get
            {
                var v = Sim.ActiveVessel;
                if (v == null || _selectedNode < 0 || _selectedNode >= v.ManeuverNodes.Count) return null;
                return v.ManeuverNodes[_selectedNode];
            }
        }

        private void EditNode(Action<ManeuverNodeRecord> edit)
        {
            var n = Selected;
            if (n == null) return;
            edit(n);
            ManeuverPlanner.Changed(Sim.ActiveVessel);
            _selectedNode = Sim.ActiveVessel.ManeuverNodes.IndexOf(n);
            RefreshFields(n);
        }

        private void RefreshFields(ManeuverNodeRecord n)
        {
            if (!_proField.isFocused) _proField.text = n.prograde.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            if (!_nrmField.isFocused) _nrmField.text = n.normal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            if (!_radField.isFocused) _radField.text = n.radial.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void ShiftNodeTime(double dt, bool orbit)
        {
            var n = Selected;
            var v = Sim.ActiveVessel;
            if (n == null || v == null) return;
            double shift = dt;
            if (orbit)
            {
                var o = v.Orbit;
                if (o == null || !o.IsElliptic) return;
                shift = dt * o.Period;
            }
            n.ut = Math.Max(Sim.UT + 5, n.ut + shift);
            ManeuverPlanner.Changed(v);
            _selectedNode = v.ManeuverNodes.IndexOf(n);
        }

        private void DeleteSelected() => DeleteNode(_selectedNode);

        private void DeleteNode(int index)
        {
            var v = Sim.ActiveVessel;
            if (v == null || index < 0 || index >= v.ManeuverNodes.Count) return;
            ManeuverPlanner.RemoveNode(v, v.ManeuverNodes[index]);
            _selectedNode = v.ManeuverNodes.Count > 0 ? Math.Min(index, v.ManeuverNodes.Count - 1) : -1;
            _draggingNode = false;
            Scene.Notify("Maneuver node deleted", false);
        }

        private void WarpTo(double ut)
        {
            Sim.WarpTo(ut);
            if (!Sim.Warp.OnRails) Scene.Notify(Sim.Warp.LimitReason ?? "Time warp is not possible right now", true);
        }

        private void WarpToNode()
        {
            var v = Sim.ActiveVessel;
            if (v == null || v.ManeuverNodes.Count == 0) return;
            var info = ManeuverPlanner.Info(v, Sim.UT);
            Sim.WarpTo(info.TimeToBurnStart > 0 ? Sim.UT + info.TimeToBurnStart - 20 : Sim.UT);
        }

        // ------------------------------------------------------------------ per frame

        private void Update()
        {
            bool active = Map != null && Map.Active && !UiState.Paused;
            if (_canvas.enabled != active) _canvas.enabled = active;
            if (!active) { _context.SetActive(false); return; }
            float scale = _canvas.scaleFactor;
            var v = Sim.ActiveVessel;
            if (v != null && _selectedNode >= v.ManeuverNodes.Count) _selectedNode = v.ManeuverNodes.Count - 1;
            if (v != null && _selectedNode < 0 && v.ManeuverNodes.Count > 0) _selectedNode = 0;

            // Markers
            _used = 0;
            foreach (var m in Map.Markers) ShowMarker(m, scale);
            for (int i = _used; i < _pool.Count; i++) _pool[i].Rt.gameObject.SetActive(false);

            HandleMouse(scale);
            UpdateNodeGizmo(scale);
            UpdateNodePanel();
            UpdateOrbitPanel();
        }

        private void ShowMarker(MapMarker m, float scale)
        {
            if (!m.Visible) return;
            if (m.Type == MarkerType.ActiveVessel && Selected != null && false) return;
            if (_used >= _pool.Count) _pool.Add(CreateWidget());
            var w = _pool[_used++];
            w.Marker = m;
            w.Rt.gameObject.SetActive(true);
            w.Rt.anchoredPosition = (Vector2)m.Screen / scale;
            string icon; float size = 22; Color c = m.Color;
            switch (m.Type)
            {
                case MarkerType.Body: icon = "ring"; size = 30; c = new Color(1, 1, 1, 0.35f); break;
                case MarkerType.ActiveVessel: icon = "map_vessel"; c = UIKit.Good; size = 24; break;
                case MarkerType.Vessel: icon = "map_vessel"; c = new Color(0.8f, 0.85f, 0.9f); break;
                case MarkerType.Debris: icon = "map_debris"; c = new Color(0.6f, 0.6f, 0.6f); size = 14; break;
                case MarkerType.Eva: icon = "map_eva"; c = UIKit.Accent; break;
                case MarkerType.Flag: icon = "map_flag"; c = new Color(1f, 0.8f, 0.3f); break;
                case MarkerType.Apoapsis: icon = "map_ap"; size = 18; break;
                case MarkerType.Periapsis: icon = "map_pe"; size = 18; break;
                case MarkerType.Node: icon = "map_node"; size = 24; break;
                case MarkerType.Encounter: icon = "map_encounter"; break;
                case MarkerType.SoiExit: icon = "map_encounter"; break;
                case MarkerType.Impact: icon = "map_impact"; break;
                case MarkerType.AtmosphereEntry: icon = "dot"; size = 12; break;
                default: icon = "map_encounter"; size = 20; break;
            }
            w.Icon.sprite = UIKit.Icon(icon);
            w.Icon.color = c;
            w.Icon.rectTransform.sizeDelta = new Vector2(size, size);
            bool showLabel = m.Type == MarkerType.Body || m.Type == MarkerType.ActiveVessel || m.Type == MarkerType.Apoapsis || m.Type == MarkerType.Periapsis
                             || m.Type == MarkerType.Encounter || m.Type == MarkerType.Node || m.Type == MarkerType.Impact;
            w.Label.text = showLabel ? m.Label : "";
            w.Label.color = m.Type == MarkerType.Body ? UIKit.TextColor : c;
            string tip = m.Tooltip;
            w.Tip.Text = () => tip;
        }

        private MarkerWidget CreateWidget()
        {
            var rt = UIKit.Rect(_root, "Marker");
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(26, 26);
            rt.SetSiblingIndex(0);
            var icon = UIKit.Image(rt, null, Color.white, "Icon");
            icon.raycastTarget = true; // for the tooltip; markers never block map clicks or scrolling
            icon.gameObject.AddComponent<MapMarkerTag>();
            var label = UIKit.Label(rt, "", 13, UIKit.TextColor, TextAlignmentOptions.Left);
            label.rectTransform.anchoredPosition = new Vector2(66, 0);
            label.rectTransform.sizeDelta = new Vector2(110, 18);
            var tip = icon.gameObject.AddComponent<TooltipTrigger>();
            return new MarkerWidget { Rt = rt, Icon = icon, Label = label, Tip = tip };
        }

        private void HandleMouse(float scale)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            ClassifyPointer(mp, out bool overPanel, out bool overGizmo);
            UiState.PointerOverUi = overPanel && _dragHandle == null && !_draggingNode;
            bool blocked = overPanel || overGizmo;
            var v = Sim.ActiveVessel;

            // Dragging a node handle (delta-v) or the node itself along its orbit (time).
            if (_dragHandle != null)
            {
                if (!mouse.leftButton.isPressed) { _dragHandle = null; }
                else DragNode(mp);
            }
            if (_draggingNode)
            {
                _addPreview.enabled = false;
                if (!mouse.leftButton.isPressed) _draggingNode = false;
                else DragNodeAlongOrbit(mp);
                return;
            }

            // Hover preview on the trajectory.
            _addPreview.enabled = false;
            bool canPick = v != null && !v.IsEva && _dragHandle == null && !blocked;
            double hoverUT = double.NaN;
            if (canPick && NearMarker(mp, 14) == null && Map.PickTrajectory(mp, 10, out hoverUT))
            {
                _addPreview.enabled = true;
                _addPreview.rectTransform.anchoredPosition = mp / scale;
            }

            if (mouse.leftButton.wasPressedThisFrame && !blocked && _dragHandle == null)
            {
                var hit = NearMarker(mp, 14);
                if (hit != null)
                {
                    var m = hit.Marker;
                    if (m.Type == MarkerType.Node && m.Payload is int idx)
                    {
                        // Select the node; holding the button and moving drags it along the orbit.
                        _selectedNode = idx;
                        _context.SetActive(false);
                        _draggingNode = true;
                        _dragNodeMoved = false;
                        _dragNodeStart = mp;
                    }
                    else if (m.Payload != null) OpenContext(m, mp / scale);
                }
                else if (!double.IsNaN(hoverUT)) OpenTrajectoryContext(hoverUT, mp / scale);
                else _context.SetActive(false);
            }

            // Right-click (without dragging the camera) on a node deletes it.
            if (mouse.rightButton.wasPressedThisFrame)
            {
                _rmbNode = -1;
                var hit = blocked ? null : NearMarker(mp, 14);
                if (hit != null && hit.Marker.Type == MarkerType.Node && hit.Marker.Payload is int idx) { _rmbNode = idx; _rmbStart = mp; }
            }
            if (mouse.rightButton.wasReleasedThisFrame && _rmbNode >= 0)
            {
                if ((mp - _rmbStart).magnitude < 6f) DeleteNode(_rmbNode);
                _rmbNode = -1;
            }

            var kb = Keyboard.current;
            if (kb != null && !UiState.KeyboardCaptured && kb.deleteKey.wasPressedThisFrame && Selected != null) DeleteSelected();
        }

        /// <summary>
        /// Moves the selected node along the trajectory it sits on (the current orbit for the first node, the planned
        /// path between its neighbours for later ones), following the pointer.
        /// </summary>
        private void DragNodeAlongOrbit(Vector2 mp)
        {
            var v = Sim.ActiveVessel;
            var n = Selected;
            if (v == null || n == null) { _draggingNode = false; return; }
            if (!_dragNodeMoved)
            {
                if ((mp - _dragNodeStart).magnitude < 5f) return;
                _dragNodeMoved = true;
            }
            int i = v.ManeuverNodes.IndexOf(n);
            double minUT = Sim.UT + 5, maxUT = double.PositiveInfinity;
            if (i > 0) minUT = Math.Max(minUT, v.ManeuverNodes[i - 1].ut + 1);
            if (i >= 0 && i < v.ManeuverNodes.Count - 1) maxUT = v.ManeuverNodes[i + 1].ut - 1;
            var set = i > 0 ? MapView.TrajectorySet.Planned : MapView.TrajectorySet.Current;
            if (!Map.PickTrajectory(mp, 80, minUT, maxUT, set, out double ut)) return;
            if (Math.Abs(ut - n.ut) < 1e-3) return;
            n.ut = ut;
            ManeuverPlanner.Changed(v);
            _selectedNode = v.ManeuverNodes.IndexOf(n);
        }

        /// <summary>
        /// What the pointer is over: a panel (blocks map clicks and scrolling) or a node gizmo (handles and the delete
        /// button take their own clicks). Marker icons only carry tooltips and block nothing, so the map can be zoomed
        /// with the pointer on the vessel.
        /// </summary>
        private void ClassifyPointer(Vector2 mp, out bool overPanel, out bool overGizmo)
        {
            overPanel = overGizmo = false;
            var es = EventSystem.current;
            if (es == null) return;
            _uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = mp }, _uiHits);
            foreach (var h in _uiHits)
            {
                var go = h.gameObject;
                if (go == null || go.GetComponent<MapMarkerTag>() != null) continue;
                if (IsGizmo(go)) overGizmo = true;
                else overPanel = true;
            }
        }

        private bool IsGizmo(GameObject go)
        {
            foreach (var h in _handles.Values) if (h.gameObject == go) return true;
            return _nodeDeleteBtn != null && go.transform.IsChildOf(_nodeDeleteBtn.Rect);
        }

        private MarkerWidget NearMarker(Vector2 screen, float px)
        {
            MarkerWidget best = null;
            float bd = px;
            for (int i = 0; i < _used; i++)
            {
                var w = _pool[i];
                var t = w.Marker.Type;
                if (t != MarkerType.Body && t != MarkerType.Vessel && t != MarkerType.Debris && t != MarkerType.Eva && t != MarkerType.Flag && t != MarkerType.Node && t != MarkerType.ActiveVessel) continue;
                float d = Vector2.Distance(screen, w.Marker.Screen);
                if (t == MarkerType.Body) d -= 8;
                if (d < bd) { bd = d; best = w; }
            }
            return best;
        }

        private void OpenTrajectoryContext(double ut, Vector2 pos)
        {
            var v = Sim.ActiveVessel;
            if (v == null) return;
            var actions = new List<(string, Action)>
            {
                ("Add maneuver node", () =>
                {
                    var n = ManeuverPlanner.AddNode(v, ut);
                    _selectedNode = v.ManeuverNodes.IndexOf(n);
                    Scene.Notify("Maneuver node added — drag the handles, or drag the node along the orbit", false);
                }),
                ("Warp here", () => WarpTo(ut)),
            };
            ShowContext($"Orbit point · in {MathD.FormatDuration(ut - Sim.UT)}", actions, pos);
        }

        private void ShowContext(string title, List<(string, Action)> actions, Vector2 pos)
        {
            _context.SetActive(true);
            ((RectTransform)_context.transform).anchoredPosition = pos + new Vector2(16, -8);
            foreach (Transform c in _contextButtons) Destroy(c.gameObject);
            _contextTitle.text = title;
            foreach (var (label, act) in actions)
            {
                var a = act;
                var btn = UIKit.Button(_contextButtons, label, () => { a(); _context.SetActive(false); }, 14);
                UIKit.Size(btn.Button, 28);
            }
            ((RectTransform)_context.transform).sizeDelta = new Vector2(220, 40 + actions.Count * 32);
        }

        private void OpenContext(MapMarker m, Vector2 pos)
        {
            var v = Sim.ActiveVessel;
            var actions = new List<(string, Action)>();
            string title = "";
            if (m.Payload is CelestialBody b)
            {
                title = b.Name;
                actions.Add(("Focus camera", () => Map.FocusOn(b)));
                if (v != null && b != v.MainBody) actions.Add(("Set as target", () => { v.TargetId = "body:" + b.Id; Scene.Trajectory.ForceUpdate(); Scene.Notify($"Target: {b.Name}"); }));
            }
            else if (m.Payload is VesselHandle h)
            {
                title = h.Name;
                actions.Add(("Focus camera", () => Map.FocusOn(h)));
                if (v != null && h != Sim.ActiveHandle && h.Kind != VesselKind.Flag)
                    actions.Add(("Set as target", () => { v.TargetId = "vessel:" + h.Id; Scene.Trajectory.ForceUpdate(); Scene.Notify($"Target: {h.Name}"); }));
                if (h != Sim.ActiveHandle && h.Kind != VesselKind.Flag && h.Kind != VesselKind.Debris)
                    actions.Add(("Switch to vessel", () => { Scene.SetMap(false); Sim.SwitchTo(h); }));
                if (h.Kind == VesselKind.Debris && h != Sim.ActiveHandle)
                    actions.Add(("Switch to debris", () => { Scene.SetMap(false); Sim.SwitchTo(h); }));
            }
            if (v != null && !string.IsNullOrEmpty(v.TargetId)) actions.Add(("Clear target", () => { v.TargetId = null; if (v.Ctrl.SpeedMode == SpeedMode.Target) v.Ctrl.SpeedMode = SpeedMode.Orbit; }));
            ShowContext(title, actions, pos);
        }

        // ------------------------------------------------------------------ node gizmo

        private void UpdateNodeGizmo(float scale)
        {
            var n = Selected;
            foreach (var h in _handles.Values) h.enabled = false;
            bool show = n != null && Map.NodeScreenFrame(n, out _, out _, out _, out _);
            if (_nodeDeleteBtn.Rect.gameObject.activeSelf != show) _nodeDeleteBtn.Rect.gameObject.SetActive(show);
            if (!show || !Map.NodeScreenFrame(n, out var s, out var p, out var nr, out var rd)) return;
            Vector2 c = (Vector2)s / scale;
            _nodeDeleteBtn.Rect.anchoredPosition = c + new Vector2(28, 28);
            float len = 62;
            void Put(string k, Vector2 dir)
            {
                var img = _handles[k];
                if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
                img.enabled = true;
                img.rectTransform.anchoredPosition = c + dir.normalized * len;
            }
            Put("pro", p); Put("retro", -p);
            Put("nrm", nr); Put("anrm", -nr);
            Put("radout", rd); Put("radin", -rd);
        }

        private void DragNode(Vector2 mp)
        {
            var n = Selected;
            if (n == null || !Map.NodeScreenFrame(n, out var screen, out var pro, out var nrm, out var rad)) return;
            Vector2 dir = _dragHandle switch
            {
                "pro" => pro, "retro" => -pro, "nrm" => nrm, "anrm" => -nrm, "radout" => rad, _ => -rad,
            };
            if (dir.sqrMagnitude < 1e-4f) return;
            dir.Normalize();
            Vector2 handlePos = (Vector2)screen + dir * 62 * _canvas.scaleFactor;
            float pull = Vector2.Dot(mp - handlePos, dir); // pixels beyond the handle along its axis
            float rate = Mathf.Sign(pull) * Mathf.Pow(Mathf.Abs(pull) / 40f, 2f) * 20f; // m/s per second
            double d = rate * Time.unscaledDeltaTime;
            switch (_dragHandle)
            {
                case "pro": n.prograde += d; break;
                case "retro": n.prograde -= d; break;
                case "nrm": n.normal += d; break;
                case "anrm": n.normal -= d; break;
                case "radout": n.radial += d; break;
                default: n.radial -= d; break;
            }
            ManeuverPlanner.Changed(Sim.ActiveVessel);
            _selectedNode = Sim.ActiveVessel.ManeuverNodes.IndexOf(n);
        }

        private void UpdateNodePanel()
        {
            var n = Selected;
            var v = Sim.ActiveVessel;
            _nodePanel.gameObject.SetActive(n != null);
            if (n == null) return;
            RefreshFields(n);
            _stepText.text = $"±{_step:0.#} m/s";
            var info = ManeuverPlanner.Info(v, Sim.UT);
            var sb = new StringBuilder();
            double dv = ManeuverPlanner.Magnitude(n);
            v.GetPropulsion(out double thrust, out double isp);
            double bt = PatchedConics.BurnTime(dv, thrust, isp, v.TotalMass);
            sb.AppendLine($"Δv <b>{dv:N1} m/s</b>   burn {(double.IsNaN(bt) ? "N/A" : MathD.FormatDuration(bt))}   in {MathD.FormatDuration(n.ut - Sim.UT)}");
            var traj = Scene.Trajectory;
            if (traj.HasNodes && traj.Planned.Count > 0)
            {
                OrbitPatch after = null;
                foreach (var p in traj.Planned) if (p.StartUT >= n.ut - 1e-3) { after = p; break; }
                if (after != null)
                {
                    var o = after.Orbit;
                    string ap = o.IsElliptic ? MathD.FormatDistance(o.ApoapsisRadius - after.Body.Radius) : "escape";
                    sb.AppendLine($"Resulting orbit ({after.Body.Name}): Ap {ap}  Pe {MathD.FormatDistance(o.PeriapsisRadius - after.Body.Radius)}");
                }
                var enc = traj.FirstEncounter(true);
                if (enc != null)
                    sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>{enc.Body.Name} encounter: periapsis {MathD.FormatDistance(enc.Orbit.PeriapsisRadius - enc.Body.Radius)}, inclination {enc.Orbit.Inclination * MathD.Rad2Deg:0.0}°</color>");
                else sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>No encounter</color>");
            }
            if (v.ManeuverNodes.Count > 1) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}>Node {_selectedNode + 1} of {v.ManeuverNodes.Count} (click a node marker to select)</color>");
            _nodeText.text = sb.ToString();
        }

        private void UpdateOrbitPanel()
        {
            var h = Map.FocusVessel ?? Sim.ActiveHandle;
            var sb = new StringBuilder();
            if (Map.FocusBody != null)
            {
                var b = Map.FocusBody;
                sb.AppendLine($"<b>{b.Name}</b>");
                sb.AppendLine($"Radius {b.Radius / 1000:N0} km   Mass {b.Mass:0.000e+00} kg");
                sb.AppendLine($"Surface gravity {b.SurfaceGravity:0.00} m/s²   GM {b.GM:0.000e+00}");
                sb.AppendLine($"Rotation {MathD.FormatDuration(b.RotationPeriod)}{(b.Def.tidallyLocked ? " (tidally locked)" : "")}");
                if (b.Atmosphere != null) sb.AppendLine($"Atmosphere {b.Atmosphere.Height / 1000:0} km, {b.Atmosphere.SeaLevelPressure / 1000:0.0} kPa at sea level");
                if (b.Parent != null) sb.AppendLine($"Orbit {b.Orbit.SemiMajorAxis / 1000:N0} km, period {MathD.FormatDuration(b.Orbit.Period)}\nSphere of influence {b.SOIRadius / 1000:N0} km");
                sb.AppendLine($"<size=12><color=#{UIKit.Hex(UIKit.TextDim)}>{b.Def.description}</color></size>");
            }
            else if (h != null)
            {
                sb.AppendLine($"<b>{h.Name}</b>  <size=12>({h.Record.situation}, {h.Body.Name})</size>");
                var o = h.CurrentOrbit;
                if (o != null)
                {
                    double ut = Sim.UT;
                    sb.AppendLine($"Ap {(o.IsElliptic ? MathD.FormatDistance(o.ApoapsisRadius - h.Body.Radius) : "escape")}   in {(o.IsElliptic ? MathD.FormatDuration(o.TimeToApoapsis(ut)) : "—")}");
                    sb.AppendLine($"Pe {MathD.FormatDistance(o.PeriapsisRadius - h.Body.Radius)}   in {MathD.FormatDuration(o.TimeToPeriapsis(ut))}");
                    sb.AppendLine($"Inclination {o.Inclination * MathD.Rad2Deg:0.00}°   e {o.Eccentricity:0.0000}");
                    sb.AppendLine(o.IsElliptic ? $"Period {MathD.FormatDuration(o.Period)}   a {o.SemiMajorAxis / 1000:N1} km" : "Hyperbolic (escape)");
                    var traj = Scene.Trajectory;
                    if (h == Sim.ActiveHandle && traj.Current.Count > 0)
                    {
                        var p0 = traj.Current[0];
                        if (p0.EndType == PatchEnd.SoiEnter) sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>SOI change → {p0.NextBody.Name} in {MathD.FormatDuration(p0.EndUT - ut)}</color>");
                        if (p0.EndType == PatchEnd.SoiExit) sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>SOI change → {p0.NextBody.Name} in {MathD.FormatDuration(p0.EndUT - ut)}</color>");
                        if (p0.EndType == PatchEnd.Impact) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Bad)}>Impact in {MathD.FormatDuration(p0.EndUT - ut)}</color>");
                        var enc = traj.FirstEncounter(false);
                        if (enc != null) sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>{enc.Body.Name} periapsis {MathD.FormatDistance(enc.Orbit.PeriapsisRadius - enc.Body.Radius)}</color>");
                    }
                }
                else sb.AppendLine("Landed");
            }
            _orbitInfo.text = sb.ToString();
            _orbitPanel.sizeDelta = new Vector2(360, _orbitInfo.GetPreferredValues(_orbitInfo.text, 336, 0).y + 18);
        }
    }
}

namespace TAP.UI
{
    /// <summary>Marks map marker icons (tooltip targets that must not block map clicks or zooming).</summary>
    internal sealed class MapMarkerTag : MonoBehaviour { }
}
