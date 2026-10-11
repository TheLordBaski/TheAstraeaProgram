using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Map-view overlay: icons/labels for bodies, vessels, apsides, encounters, nodes and closest approach;
    /// click actions; clicking the orbit offers "add maneuver node" and "warp here"; nodes are dragged along the
    /// orbit by their marker, shaped with drag handles or the numeric editor, and deleted with the x button,
    /// a right-click on the node or the Delete key. Layout: Resources/UI/MapUI.uxml.
    /// </summary>
    public sealed class MapUI : MonoBehaviour
    {
        public FlightSceneController Scene;
        private MapView Map => Scene.Map;
        private FlightSim Sim => Scene.Sim;
        private VisualElement _root, _markerLayer, _handleLayer;
        private readonly List<MarkerWidget> _pool = new List<MarkerWidget>();
        private int _used;
        private VisualElement _context, _contextButtons;
        private Label _contextTitle;
        private VisualElement _nodePanel;
        private Label _nodeText;
        private TextField _proField, _nrmField, _radField;
        private Label _stepText;
        private double _step = 10;
        private int _selectedNode = -1;
        private readonly Dictionary<string, Image> _handles = new Dictionary<string, Image>();
        private string _dragHandle;
        private VisualElement _addPreview;
        private Label _orbitInfo;
        private Button _nodeDeleteBtn;
        private bool _draggingNode, _dragNodeMoved;
        private Vector2 _dragNodeStart;
        private int _rmbNode = -1;
        private Vector2 _rmbStart;
        private bool _wasActive = true;

        private sealed class MarkerWidget
        {
            public VisualElement Root;
            public Image Icon;
            public Label Label;
            public string Tip;
            public MapMarker Marker;
        }

        public static MapUI Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("MapUI", 20, "MapUI");
            var ui = doc.gameObject.AddComponent<MapUI>();
            ui.Scene = scene;
            ui._root = doc.rootVisualElement;
            ui.Build();
            UIKit.Show(ui._root, false);
            return ui;
        }

        private void Build()
        {
            _markerLayer = _root.Q("markers");
            _handleLayer = _root.Q("handles");
            _addPreview = _root.Q("add-preview");
            _addPreview.style.backgroundImage = new StyleBackground(UIKit.Icon("plus"));
            UIKit.Show(_addPreview, false);

            // Click menu
            _context = _root.Q("context");
            _contextTitle = _root.Q<Label>("context-title");
            _contextButtons = _root.Q("context-buttons");
            UIKit.Show(_context, false);

            // Node gizmo handles
            foreach (var (key, icon, color) in new[]
                     {
                         ("pro", "nav_prograde", NavballWidget.Pro), ("retro", "nav_retrograde", NavballWidget.Pro),
                         ("nrm", "nav_normal", NavballWidget.Nrm), ("anrm", "nav_antinormal", NavballWidget.Nrm),
                         ("radout", "nav_radialout", NavballWidget.Rad), ("radin", "nav_radialin", NavballWidget.Rad),
                     })
            {
                var img = UIKit.Image(UIKit.Icon(icon), color, "map-handle map-gizmo");
                img.pickingMode = PickingMode.Position;
                string k = key;
                img.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) _dragHandle = k; });
                UIKit.Tooltip(img, () => $"Drag to add {HandleName(k)} delta-v (further = faster).");
                UIKit.Show(img, false);
                _handleLayer.Add(img);
                _handles[key] = img;
            }

            // Delete button next to the selected node
            _nodeDeleteBtn = _root.Q<Button>("node-delete");
            _nodeDeleteBtn.clicked += DeleteSelected;
            UIKit.Tooltip(_nodeDeleteBtn, () => "Delete this maneuver node (or right-click the node, or press Delete)");
            UIKit.Show(_nodeDeleteBtn, false);

            // Node editor
            _nodePanel = _root.Q("node-panel");
            var rows = _root.Q("node-rows");
            _proField = AddComponentRow(rows, "Prograde", NavballWidget.Pro, v => EditNode(n => n.prograde = v), s => EditNode(n => n.prograde += s));
            _nrmField = AddComponentRow(rows, "Normal", NavballWidget.Nrm, v => EditNode(n => n.normal = v), s => EditNode(n => n.normal += s));
            _radField = AddComponentRow(rows, "Radial", NavballWidget.Rad, v => EditNode(n => n.radial = v), s => EditNode(n => n.radial += s));
            var steps = _root.Q("steps");
            foreach (double s in new[] { 0.1, 1.0, 10.0, 100.0 })
            {
                double sv = s;
                steps.Add(UIKit.Button(s < 1 ? "0.1" : s.ToString("0"), () => _step = sv, "node-stepbtn"));
            }
            _stepText = _root.Q<Label>("step-text");
            var times = _root.Q("time-steps");
            foreach (var (label, dt) in new[] { ("−orbit", -1.0), ("−10m", -600.0), ("−1m", -60.0), ("−10s", -10.0), ("+10s", 10.0), ("+1m", 60.0), ("+10m", 600.0), ("+orbit", 1.0) })
            {
                double d = dt;
                bool orbit = label.Contains("orbit");
                times.Add(UIKit.Button(label, () => ShiftNodeTime(d, orbit), "node-timebtn"));
            }
            _root.Q<Button>("node-del").clicked += DeleteSelected;
            _root.Q<Button>("node-warp").clicked += WarpToNode;
            _root.Q<Button>("node-sas").clicked += () => { var v = Sim.ActiveVessel; if (v != null) FlightInput.SetSasMode(v, SasMode.Maneuver); };
            _nodeText = _root.Q<Label>("node-text");
            UIKit.Show(_nodePanel, false);

            _orbitInfo = _root.Q<Label>("orbit-info");
        }

        private static string HandleName(string k) => k switch
        {
            "pro" => "prograde", "retro" => "retrograde", "nrm" => "normal", "anrm" => "anti-normal", "radout" => "radial-out", _ => "radial-in",
        };

        private TextField AddComponentRow(VisualElement parent, string label, Color color, Action<double> set, Action<double> add)
        {
            var row = UIKit.Element("node-row");
            var l = UIKit.Label(label, "node-component");
            l.style.color = color;
            row.Add(l);
            row.Add(UIKit.Button("−", () => add(-_step), "node-step"));
            var field = UIKit.Field("0.0", "node-field");
            field.isDelayed = true;
            field.RegisterValueChangedCallback(e =>
            {
                if (double.TryParse(e.newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) set(d);
            });
            row.Add(field);
            row.Add(UIKit.Button("+", () => add(_step), "node-step"));
            row.Add(UIKit.Label("m/s", "node-unit text-dim"));
            parent.Add(row);
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

        private static void SetField(TextField f, double v)
        {
            if (f.focusController != null && f.focusController.focusedElement == f) return;
            f.SetValueWithoutNotify(v.ToString("0.0", CultureInfo.InvariantCulture));
        }

        private void RefreshFields(ManeuverNodeRecord n)
        {
            SetField(_proField, n.prograde);
            SetField(_nrmField, n.normal);
            SetField(_radField, n.radial);
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
            if (active != _wasActive) { _wasActive = active; UIKit.Show(_root, active); }
            if (!active) { UIKit.Show(_context, false); return; }
            var v = Sim.ActiveVessel;
            if (v != null && _selectedNode >= v.ManeuverNodes.Count) _selectedNode = v.ManeuverNodes.Count - 1;
            if (v != null && _selectedNode < 0 && v.ManeuverNodes.Count > 0) _selectedNode = 0;

            // Markers
            _used = 0;
            foreach (var m in Map.Markers) ShowMarker(m);
            for (int i = _used; i < _pool.Count; i++) UIKit.Show(_pool[i].Root, false);

            HandleMouse();
            UpdateNodeGizmo();
            UpdateNodePanel();
            UpdateOrbitPanel();
        }

        private void ShowMarker(MapMarker m)
        {
            if (!m.Visible) return;
            if (_used >= _pool.Count) _pool.Add(CreateWidget());
            var w = _pool[_used++];
            w.Marker = m;
            UIKit.Show(w.Root, true);
            UIKit.Place(w.Root, UIKit.ScreenToPanel(m.Screen));
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
            w.Icon.tintColor = c;
            w.Icon.style.width = size;
            w.Icon.style.height = size;
            bool showLabel = m.Type == MarkerType.Body || m.Type == MarkerType.ActiveVessel || m.Type == MarkerType.Apoapsis || m.Type == MarkerType.Periapsis
                             || m.Type == MarkerType.Encounter || m.Type == MarkerType.Node || m.Type == MarkerType.Impact;
            w.Label.text = showLabel ? m.Label : "";
            w.Label.style.color = m.Type == MarkerType.Body ? UIKit.TextColor : c;
            w.Tip = m.Tooltip;
        }

        private MarkerWidget CreateWidget()
        {
            var w = new MarkerWidget { Root = UIKit.Element("map-marker") };
            w.Root.pickingMode = PickingMode.Ignore;
            // The icon takes the pointer for its tooltip only; markers never block map clicks or scrolling.
            w.Icon = UIKit.Image(null, Color.white, "map-marker__icon map-marker-hit");
            w.Icon.pickingMode = PickingMode.Position;
            w.Root.Add(w.Icon);
            w.Label = UIKit.Label("", "map-marker__label");
            w.Root.Add(w.Label);
            UIKit.Tooltip(w.Icon, () => w.Tip);
            _markerLayer.Add(w.Root);
            return w;
        }

        private void HandleMouse()
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
                UIKit.Show(_addPreview, false);
                if (!mouse.leftButton.isPressed) _draggingNode = false;
                else DragNodeAlongOrbit(mp);
                return;
            }

            // Hover preview on the trajectory.
            UIKit.Show(_addPreview, false);
            bool canPick = v != null && !v.IsEva && _dragHandle == null && !blocked;
            double hoverUT = double.NaN;
            if (canPick && NearMarker(mp, 14) == null && Map.PickTrajectory(mp, 10, out hoverUT))
            {
                UIKit.Show(_addPreview, true);
                UIKit.Place(_addPreview, UIKit.ScreenToPanel(mp));
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
                        UIKit.Show(_context, false);
                        _draggingNode = true;
                        _dragNodeMoved = false;
                        _dragNodeStart = mp;
                    }
                    else if (m.Payload != null) OpenContext(m, UIKit.ScreenToPanel(mp));
                }
                else if (!double.IsNaN(hoverUT)) OpenTrajectoryContext(hoverUT, UIKit.ScreenToPanel(mp));
                else UIKit.Show(_context, false);
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

            if (!UiState.KeyboardCaptured && Controls.Down("Map/DeleteNode") && Selected != null) DeleteSelected();
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
        /// with the pointer on the vessel. Panels of the other flight screens (staging, navball) count too.
        /// </summary>
        private static void ClassifyPointer(Vector2 mp, out bool overPanel, out bool overGizmo)
        {
            overPanel = overGizmo = false;
            var e = UIKit.Pick(mp);
            if (e == null || UIKit.Ancestor(e, "map-marker-hit") != null) return;
            if (UIKit.Ancestor(e, "map-gizmo") != null) overGizmo = true;
            else overPanel = true;
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
            UIKit.Show(_context, true);
            UIKit.Place(_context, pos + new Vector2(16, 8));
            _contextButtons.Clear();
            _contextTitle.text = title;
            foreach (var (label, act) in actions)
            {
                var a = act;
                _contextButtons.Add(UIKit.Button(label, () => { a(); UIKit.Show(_context, false); }, "context-btn"));
            }
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
                    actions.Add(("Set as target", () => { v.TargetId = "vessel:" + h.Id; Scene.Trajectory.ForceUpdate(); Scene.Notify($"Target: {Loc.Name(h.Name)}"); }));
                if (h != Sim.ActiveHandle && h.Kind != VesselKind.Flag && h.Kind != VesselKind.Debris)
                    actions.Add(("Switch to vessel", () => { Scene.SetMap(false); Sim.SwitchTo(h); }));
                if (h.Kind == VesselKind.Debris && h != Sim.ActiveHandle)
                    actions.Add(("Switch to debris", () => { Scene.SetMap(false); Sim.SwitchTo(h); }));
            }
            if (v != null && !string.IsNullOrEmpty(v.TargetId)) actions.Add(("Clear target", () => { v.TargetId = null; if (v.Ctrl.SpeedMode == SpeedMode.Target) v.Ctrl.SpeedMode = SpeedMode.Orbit; }));
            ShowContext(title, actions, pos);
        }

        // ------------------------------------------------------------------ node gizmo

        private void UpdateNodeGizmo()
        {
            var n = Selected;
            foreach (var h in _handles.Values) UIKit.Show(h, false);
            bool show = n != null && Map.NodeScreenFrame(n, out var s, out var p, out var nr, out var rd);
            UIKit.Show(_nodeDeleteBtn, show);
            if (!show) return;
            Map.NodeScreenFrame(n, out s, out p, out nr, out rd);
            Vector2 c = UIKit.ScreenToPanel(s);
            UIKit.Place(_nodeDeleteBtn, c + new Vector2(28, -28));
            const float len = 62;
            void Put(string k, Vector2 dir)
            {
                var img = _handles[k];
                if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
                UIKit.Show(img, true);
                // Screen directions have y up; panel coordinates have y down.
                Vector2 d = dir.normalized;
                UIKit.Place(img, c + new Vector2(d.x, -d.y) * len);
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
            // The handle sits 62 panel units from the node; in screen pixels that is scaled by the panel's scale.
            float pxPerUnit = Screen.height / Mathf.Max(1f, UIKit.PanelSize(_root).y);
            Vector2 handlePos = (Vector2)screen + dir * 62 * pxPerUnit;
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
            UIKit.Show(_nodePanel, n != null);
            if (n == null) return;
            RefreshFields(n);
            _stepText.text = $"±{_step:0.#} m/s";
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
            _nodeText.text = sb.ToString().TrimEnd('\n', '\r');
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
                sb.AppendLine($"Rotation {MathD.FormatDuration(b.RotationPeriod)}{(b.Def.tidallyLocked ? " (tidally locked)" : "")}" +
                              (b.IsStar ? "" : $", day {MathD.FormatDuration(b.SolarDay)}"));
                if (b.Atmosphere != null) sb.AppendLine($"Atmosphere {b.Atmosphere.Height / 1000:0} km, {b.Atmosphere.SeaLevelPressure / 1000:0.0} kPa at sea level");
                if (b.Parent != null) sb.AppendLine($"Orbit {b.Orbit.SemiMajorAxis / 1000:N0} km, period {MathD.FormatDuration(b.Orbit.Period)}\nSphere of influence {b.SOIRadius / 1000:N0} km");
                sb.Append($"<size=12><color=#{UIKit.Hex(UIKit.TextDim)}>{Loc.Data(b.Def.description)}</color></size>");
            }
            else if (h != null)
            {
                sb.AppendLine($"<b>{Loc.Name(h.Name)}</b>  <size=12>({h.Record.situation}, {h.Body.Name})</size>");
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
            _orbitInfo.text = sb.ToString().TrimEnd('\n', '\r');
        }
    }
}
