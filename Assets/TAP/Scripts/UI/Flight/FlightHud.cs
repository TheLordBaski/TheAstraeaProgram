using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Flight HUD in the Flight Deck style: bezelled units with night glass and lit hardware keys. The clock and time warp, the
    /// menu keys, a warnings memo, the staging stack (or the EVA panel), the primary flight display (navball, speed, altitude,
    /// throttle, g, SAS) and the orbit / maneuver / target / resources / data display. <see cref="HudLayout"/> lets the player
    /// move, scale and hide every unit. Layout lives in Resources/UI/FlightHud.uxml + .uss; this class fills it in.
    /// </summary>
    public sealed partial class FlightHud : MonoBehaviour
    {
        // Flight Deck colours (match FlightHud.uss)
        internal static readonly Color White = new Color32(238, 243, 245, 255), Dim = new Color32(127, 140, 149, 255);
        internal static readonly Color Cyan = new Color32(94, 230, 242, 255), Green = new Color32(72, 226, 124, 255);
        internal static readonly Color Magenta = new Color32(255, 90, 217, 255), Amber = new Color32(255, 181, 71, 255);
        internal static readonly Color Red = new Color32(255, 74, 61, 255);

        public FlightSceneController Scene;
        public FlightSim Sim => Scene.Sim;
        private VisualElement _root;
        private HudLayout _layout;

        // clock
        private Label _met, _ut, _warpRate, _warpLimit;
        private readonly List<Button> _warpCells = new List<Button>();
        private Button _physBtn;
        // primary flight display
        private NavballWidget _navball;
        private Label _speedMode, _speed, _hspeed, _altMode, _altitude, _vspeed, _throttleText, _gText;
        private VisualElement _throttle, _gmeter;
        private Button _sasBtn, _rcsBtn, _legsBtn;
        private bool _altTerrain;
        private readonly Dictionary<SasMode, Button> _sasModes = new Dictionary<SasMode, Button>();
        // orbit / maneuver display
        private static string _page = "orb"; // kept across flights in a session
        private readonly Dictionary<string, (VisualElement page, Button key)> _pages = new Dictionary<string, (VisualElement, Button)>();
        private OrbitPlot _plot;
        private Label _orbTitle, _orbState, _orbNote, _mnvState, _mnvTimeLabel, _mnvTime, _mnvDv, _mnvEmpty, _tgtTitle, _tgtEmpty, _resCrew, _dataTitle, _dataState;
        private VisualElement _mnvBody, _mnvProgress, _tgtRows, _resRows;
        private readonly Dictionary<string, Label> _rows = new Dictionary<string, Label>();
        private readonly Dictionary<string, VisualElement> _rowEls = new Dictionary<string, VisualElement>();
        // other units
        private VisualElement _stagePanel, _evaPanel, _burnPanel, _memo, _messages, _layoutMenu, _layoutRows, _clockPanel, _mfdPanel;
        private ScrollView _stageList;
        private Label _stageTotal, _evaInfo, _burnInfo, _bigNotice;
        private readonly List<StageRow> _stageRows = new List<StageRow>();
        private string _stageSignature, _memoSignature;
        private readonly List<(Label text, float until)> _messageItems = new List<(Label, float)>();
        private float _bigNoticeUntil;
        private float _slowTimer;

        public static FlightHud Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("FlightHUD", 10, "FlightHud");
            var hud = doc.gameObject.AddComponent<FlightHud>();
            hud.Scene = scene;
            hud._root = doc.rootVisualElement;
            hud.Build();
            scene.Notification += hud.OnMessage;
            scene.UiVisibilityChanged += visible => UIKit.Show(hud._root, visible);
            return hud;
        }

        private void OnDestroy()
        {
            _layout?.Dispose();
            if (Scene != null) Scene.Notification -= OnMessage;
        }

        private T Q<T>(string name) where T : VisualElement => _root.Q<T>(name);
        private VisualElement Q(string name) => _root.Q(name);

        private void Build()
        {
            // bezel screws, and the light bar on every key
            _root.Query(className: "fd-unit").ForEach(AddScrews);
            _root.Query<Button>(className: "fd-key").ForEach(AddBar);
            BuildClock();
            BuildKeys();
            BuildPfd();
            BuildMfd();
            BuildPanels();
            BuildLayout();
        }

        internal static void AddScrews(VisualElement unit)
        {
            foreach (var corner in new[] { "tl", "tr", "bl", "br" })
            {
                var s = UIKit.Element("fd-screw fd-screw--" + corner);
                s.pickingMode = PickingMode.Ignore;
                unit.Insert(0, s);
            }
        }

        internal static void AddBar(Button key)
        {
            var bar = UIKit.Element("fd-key__bar");
            bar.pickingMode = PickingMode.Ignore;
            key.Insert(0, bar);
        }

        /// <summary>A hardware key made in code: a light bar and an icon or a label.</summary>
        private static Button Key(string text, Action onClick, string classes, string icon = null, Color? tint = null)
        {
            var b = UIKit.Button(text, onClick, "fd-key " + classes);
            AddBar(b);
            if (icon != null) b.Add(UIKit.Image(UIKit.Icon(icon), tint ?? White, "fd-key__icon"));
            return b;
        }

        private static Button WithIcon(Button b, string icon, Color tint)
        {
            b.Add(UIKit.Image(UIKit.Icon(icon), tint, "fd-key__icon"));
            return b;
        }

        // ------------------------------------------------------------------ clock and keys

        private void BuildClock()
        {
            _clockPanel = Q("clock");
            _met = Q<Label>("met");
            _ut = Q<Label>("ut");
            _warpRate = Q<Label>("warp-rate");
            _warpLimit = Q<Label>("warp-limit");
            var slower = Q<Button>("warp-slower");
            slower.clicked += () => { if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex - 1); else Sim.SetRailsWarp(Sim.Warp.RailsIndex - 1); };
            UIKit.Tooltip(slower, () => Controls.Expand("Slower time warp  [{Flight/WarpSlower}]"));
            var stop = Q<Button>("warp-stop");
            stop.clicked += () => Sim.StopWarp();
            UIKit.Tooltip(stop, () => Controls.Expand("Stop time warp  [{Flight/WarpStop}]"));
            var faster = Q<Button>("warp-faster");
            faster.clicked += () => Sim.SetRailsWarp(Sim.Warp.RailsIndex + 1);
            UIKit.Tooltip(faster, () => Controls.Expand("Faster time warp  [{Flight/WarpFaster}]\nHigh rates are limited by altitude, atmosphere and thrust."));
            _physBtn = Q<Button>("warp-phys");
            _physBtn.clicked += () => Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex + 1);
            UIKit.Tooltip(_physBtn, () => Controls.Expand("Physics warp, up to 4x (simulates normally)  [{Flight/Modifier} + {Flight/WarpFaster}]"));
            var cells = Q("warp-cells");
            for (int i = 0; i < TimeWarp.RailsRates.Length; i++)
            {
                int index = i;
                double rate = TimeWarp.RailsRates[i];
                var b = UIKit.Button(rate >= 1000 ? $"{rate / 1000:0}K" : $"{rate:0}", () => Sim.SetRailsWarp(index), "warp-cell no-loc");
                UIKit.Tooltip(b, () => $"Time warp {rate:N0}x");
                cells.Add(b);
                _warpCells.Add(b);
            }
        }

        private void BuildKeys()
        {
            UIKit.Tooltip(WithIcon(Q<Button>("map-btn"), "key_map", White), () => Controls.Expand("Map  [{Flight/ToggleMap}]"));
            Q<Button>("map-btn").clicked += () => Scene.ToggleMap();
            UIKit.Tooltip(WithIcon(Q<Button>("help-btn"), "key_help", White), () => Controls.Expand("Control guide  [{Common/Help}]"));
            Q<Button>("help-btn").clicked += () => PauseMenu.Instance?.ToggleHelp();
            UIKit.Tooltip(WithIcon(Q<Button>("menu-btn"), "key_menu", White), () => Controls.Expand("Pause menu  [{Common/Cancel}]"));
            Q<Button>("menu-btn").clicked += () => PauseMenu.Instance?.TogglePause();
            var layoutBtn = WithIcon(Q<Button>("layout-btn"), "key_layout", White);
            UIKit.Tooltip(layoutBtn, () => "HUD layout: show, hide or reset the panels");
            layoutBtn.clicked += () => ShowLayoutMenu(!UIKit.Shown(_layoutMenu));
        }

        // ------------------------------------------------------------------ primary flight display

        private void BuildPfd()
        {
            _navball = NavballWidget.Create(gameObject, Q("navball"), Sim, 172);
            var speedBox = Q<Button>("speed-box");
            speedBox.clicked += CycleSpeedMode;
            UIKit.Tooltip(speedBox, () => "Speed. Click: surface / orbit / target.\nThe reference also sets the prograde markers and SAS modes.");
            _speedMode = Q<Label>("speed-mode");
            _speed = Q<Label>("speed");
            _hspeed = Q<Label>("hspeed");
            var altBox = Q<Button>("alt-box");
            altBox.clicked += () => _altTerrain = !_altTerrain;
            UIKit.Tooltip(altBox, () => "Altitude. Click: above sea level / above the terrain.");
            _altMode = Q<Label>("alt-mode");
            _altitude = Q<Label>("altitude");
            _vspeed = Q<Label>("vspeed");

            _throttle = Q("throttle");
            _throttleText = Q<Label>("throttle-text");
            UIKit.Tooltip(_throttle, () => Controls.Expand("Throttle  [{Flight/ThrottleUp}] / [{Flight/ThrottleDown}], full [{Flight/ThrottleFull}], cut [{Flight/ThrottleCut}]"));
            _gmeter = Q("gmeter");
            _gText = Q<Label>("g-text");
            UIKit.Tooltip(_gmeter, () => "Proper acceleration (g-force), 0-10 g");

            _sasBtn = WithIcon(Q<Button>("sas-btn"), "key_sas", White);
            _sasBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) { v.Ctrl.Sas = !v.Ctrl.Sas; v.Attitude.ResetHold(); } };
            UIKit.Tooltip(_sasBtn, () => Controls.Expand("Stability assist  [{Flight/ToggleSas}]\nHolds attitude or the selected direction with reaction wheels, engine gimbal and RCS."));
            _rcsBtn = WithIcon(Q<Button>("rcs-btn"), "key_rcs", White);
            _rcsBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.Rcs = !v.Ctrl.Rcs; };
            UIKit.Tooltip(_rcsBtn, () => Controls.Expand("Reaction control  [{Flight/ToggleRcs}]\nUses monopropellant for rotation and translation."));
            _legsBtn = WithIcon(Q<Button>("legs-btn"), "key_legs", White);
            _legsBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.LegsDeployed = !v.Ctrl.LegsDeployed; };
            UIKit.Tooltip(_legsBtn, () => Controls.Expand("Landing legs  [{Flight/ToggleLegs}]"));

            // SAS modes as icon keys down both sides of the glass, each in its marker's colour
            var modes = new (SasMode mode, string icon, string control, Color color, bool left)[]
            {
                (SasMode.StabilityAssist, "sas_hold", "SasHold", White, true),
                (SasMode.Prograde, "nav_prograde", "SasPrograde", NavballWidget.Pro, true),
                (SasMode.Retrograde, "nav_retrograde", "SasRetrograde", NavballWidget.Pro, true),
                (SasMode.Normal, "nav_normal", "SasNormal", NavballWidget.Nrm, true),
                (SasMode.AntiNormal, "nav_antinormal", "SasAntiNormal", NavballWidget.Nrm, true),
                (SasMode.RadialOut, "nav_radialout", "SasRadialOut", NavballWidget.Rad, false),
                (SasMode.RadialIn, "nav_radialin", "SasRadialIn", NavballWidget.Rad, false),
                (SasMode.Target, "nav_target", "SasTarget", NavballWidget.Tgt, false),
                (SasMode.AntiTarget, "nav_antitarget", "SasAntiTarget", NavballWidget.Tgt, false),
                (SasMode.Maneuver, "nav_maneuver", "SasManeuver", NavballWidget.Man, false),
            };
            var left = Q("sas-left");
            var right = Q("sas-right");
            foreach (var md in modes)
            {
                var mode = md.mode;
                var b = Key("", () => { var v = Sim.ActiveVessel; if (v != null) FlightInput.SetSasMode(v, mode); }, "sas-key", md.icon, md.color);
                string control = md.control;
                UIKit.Tooltip(b, () => Controls.Expand(ControlTable.Find("Flight/" + control)?.Label + "  [{Flight/" + control + "}]"));
                (md.left ? left : right).Add(b);
                _sasModes[mode] = b;
            }
        }

        private void CycleSpeedMode()
        {
            var v = Sim.ActiveVessel;
            if (v == null) return;
            v.Ctrl.SpeedMode = v.Ctrl.SpeedMode == SpeedMode.Surface ? SpeedMode.Orbit : v.Ctrl.SpeedMode == SpeedMode.Orbit
                ? (string.IsNullOrEmpty(v.TargetId) ? SpeedMode.Surface : SpeedMode.Target) : SpeedMode.Surface;
        }

        // ------------------------------------------------------------------ orbit / maneuver display

        private void BuildMfd()
        {
            _mfdPanel = Q("mfd");
            var tips = new Dictionary<string, string>
            {
                ["orb"] = "Orbit", ["mnv"] = "Maneuver node", ["tgt"] = "Target", ["res"] = "Resources", ["data"] = "Flight data",
            };
            foreach (var kv in tips)
            {
                string id = kv.Key, tip = kv.Value;
                var key = Q<Button>("page-" + id + "-key");
                key.clicked += () => ShowPage(id);
                UIKit.Tooltip(key, () => tip);
                _pages[id] = (Q("page-" + id), key);
            }
            _plot = new OrbitPlot();
            _plot.StretchToParentSize();
            Q("orbit-plot").Add(_plot);
            _orbTitle = Q<Label>("orb-title");
            _orbState = Q<Label>("orb-state");
            _orbNote = Q<Label>("orb-note");
            Rows(Q("orb-rows"), "orb", ("ap", "AP"), ("tap", "T-AP"), ("pe", "PE"), ("tpe", "T-PE"), ("inc", "INC"), ("ecc", "ECC"), ("per", "PERIOD"));
            _mnvState = Q<Label>("mnv-state");
            _mnvTimeLabel = Q<Label>("mnv-time-label");
            _mnvTime = Q<Label>("mnv-time");
            _mnvDv = Q<Label>("mnv-dv");
            _mnvEmpty = Q<Label>("mnv-empty");
            _mnvBody = Q("mnv-body");
            _mnvProgress = Q("mnv-progress");
            Rows(Q("mnv-rows"), "mnv", ("burn", "BURN TIME"), ("node", "NODE IN"), ("align", "ALIGN"), ("total", "TOTAL ΔV"));
            _tgtTitle = Q<Label>("tgt-title");
            _tgtEmpty = Q<Label>("tgt-empty");
            _tgtRows = Q("tgt-rows");
            Rows(_tgtRows, "tgt", ("dist", "DISTANCE"), ("rel", "REL. SPEED"), ("ca", "CLOSEST"), ("cat", "CLOSEST IN"), ("carel", "REL. SPEED THERE"), ("enc", "ENCOUNTER PE"));
            _resCrew = Q<Label>("res-crew");
            _resRows = Q("res-rows");
            _dataTitle = Q<Label>("data-title");
            _dataState = Q<Label>("data-state");
            Rows(Q("data-rows"), "data", ("surf", "SURF"), ("orb", "ORB"), ("mach", "MACH"), ("q", "Q"), ("p", "PRESSURE"), ("temp", "AIR TEMP"),
                ("mass", "MASS"), ("twr", "TWR"), ("dvs", "ΔV STAGE"), ("dvt", "ΔV TOTAL"), ("parts", "PARTS"), ("stage", "STAGE"));
            ShowPage(_page);
        }

        private void Rows(VisualElement parent, string prefix, params (string id, string label)[] rows)
        {
            foreach (var (id, label) in rows)
            {
                var row = UIKit.Element("mfd-row");
                row.pickingMode = PickingMode.Ignore;
                row.Add(UIKit.Label(label, "mfd-row__key"));
                var val = UIKit.Label("", "mfd-row__val no-loc");
                row.Add(val);
                parent.Add(row);
                _rows[prefix + "." + id] = val;
                _rowEls[prefix + "." + id] = row;
            }
        }

        private void ShowPage(string id)
        {
            _page = id;
            foreach (var kv in _pages)
            {
                UIKit.Show(kv.Value.page, kv.Key == id);
                UIKit.SetOn(kv.Value.key, kv.Key == id);
            }
            _slowTimer = 0;
        }

        // ------------------------------------------------------------------ other units and the layout

        private void BuildPanels()
        {
            _stagePanel = Q("staging");
            _stageList = Q<ScrollView>("stage-list");
            _stageTotal = Q<Label>("stage-total");
            UIKit.Tooltip(_stageTotal, () => "Δv left in all stages, at the current altitude");
            _evaPanel = Q("eva");
            _evaInfo = Q<Label>("eva-text");
            _burnPanel = Q("burn");
            _burnInfo = Q<Label>("burn-text");
            _memo = Q("memo");
            _messages = Q("messages");
            _bigNotice = Q<Label>("big-notice");
            UIKit.Show(_bigNotice, false);
        }

        private void BuildLayout()
        {
            _layout = new HudLayout();
            HudPanelState At(int ax, int ay, float x, float y) => new HudPanelState { ax = ax, ay = ay, x = x, y = y };
            _layout.Add("clock", "Clock and time warp", _clockPanel, At(0, 0, 16, 16));
            _layout.Add("keys", "Menu keys", Q("keys"), At(2, 0, 16, 16), canHide: false);
            _layout.Add("memo", "Warnings", _memo, At(1, 0, 0, 16));
            _layout.Add("staging", "Staging", _stagePanel, At(0, 2, 16, 16));
            _layout.Add("eva", "EVA", _evaPanel, At(0, 2, 16, 16));
            _layout.Add("pfd", "Navball", Q("pfd"), At(1, 2, 0, 10));
            _layout.Add("burn", "Burn", _burnPanel, At(1, 2, 0, 286));
            _layout.Add("mfd", "Orbit and maneuver", _mfdPanel, At(2, 2, 16, 16));
            _layout.Want("eva", false);
            _layout.Want("burn", false);
            _layout.Want("memo", false);

            _layoutMenu = Q("layout-menu");
            _layoutRows = Q("layout-rows");
            Q<Button>("layout-reset").clicked += () => _layout.Reset();
            Q<Button>("layout-close").clicked += () => ShowLayoutMenu(false);
            _layout.Changed += () => { if (UIKit.Shown(_layoutMenu)) FillLayoutMenu(); };
        }

        private void ShowLayoutMenu(bool on)
        {
            UIKit.Show(_layoutMenu, on);
            UIKit.SetOn(Q<Button>("layout-btn"), on);
            if (!on) return;
            FillLayoutMenu();
            // under the menu keys, kept on the screen
            var keys = Q("keys").worldBound;
            var screen = UIKit.PanelSize(_root);
            // held by the right edge, so it stays put when the window changes size
            float w = 262, right = Mathf.Clamp(screen.x - keys.xMax, 8, screen.x - w - 8), y = keys.yMax + 6;
            if (y > screen.y - 300) y = Mathf.Max(8, keys.yMin - 330);
            _layoutMenu.style.left = StyleKeyword.Auto;
            _layoutMenu.style.right = right;
            _layoutMenu.style.top = y;
        }

        private void FillLayoutMenu()
        {
            _layoutRows.Clear();
            foreach (var (id, title, hidden) in _layout.Panels())
            {
                string panel = id;
                bool shown = !hidden;
                var row = Key(title, () => _layout.SetHidden(panel, shown), "layout-row");
                UIKit.SetOn(row, shown);
                UIKit.Tooltip(row, () => shown ? "Shown. Click to hide." : "Hidden. Click to show.");
                _layoutRows.Add(row);
            }
        }

        public void OnMessage(string msg, bool important)
        {
            if (_messages == null) return;
            var t = UIKit.Label(msg, important ? "message message--important" : "message");
            _messages.Add(t);
            _messageItems.Add((t, Time.unscaledTime + (important ? 7f : 4.5f)));
            while (_messageItems.Count > 6)
            {
                _messageItems[0].text.RemoveFromHierarchy();
                _messageItems.RemoveAt(0);
            }
            if (important && (msg.StartsWith("Liftoff") || msg.StartsWith("Docked") || msg.StartsWith("Flag planted")))
            {
                _bigNotice.text = msg;
                UIKit.Show(_bigNotice, true);
                _bigNoticeUntil = Time.unscaledTime + 3;
            }
        }
    }
}
