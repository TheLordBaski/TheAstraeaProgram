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
    /// Flight HUD: altimeter, clock + warp, navball cluster (speed modes, throttle, SAS/RCS/modes, g-meter),
    /// staging stack, resources, flight data, burn/target info, warnings, EVA panel and messages.
    /// Layout lives in Resources/UI/FlightHud.uxml + .uss; this class fills it in.
    /// </summary>
    public sealed partial class FlightHud : MonoBehaviour
    {
        public FlightSceneController Scene;
        public FlightSim Sim => Scene.Sim;
        private VisualElement _root;

        // Top
        private Label _altitude, _altMode, _vspeed, _situation, _clock, _warpText, _warpLimit;
        private bool _altTerrain;
        // Navball cluster
        private NavballWidget _navball;
        private Label _speed;
        private Button _speedMode;
        private VisualElement _throttle, _gmeter;
        private Label _throttleText, _gText;
        private Button _sasBtn, _rcsBtn, _legsBtn;
        private readonly Dictionary<SasMode, Button> _sasModes = new Dictionary<SasMode, Button>();
        // Panels
        private Label _data, _resources, _burnInfo, _targetInfo, _warnings, _evaInfo;
        private VisualElement _burnPanel, _targetPanel, _warnPanel, _evaPanel, _stagePanel;
        private ScrollView _stageList;
        private readonly List<StageRow> _stageRows = new List<StageRow>();
        private string _stageSignature;
        private VisualElement _messages;
        private readonly List<(Label text, float until)> _messageItems = new List<(Label, float)>();
        private Label _bigNotice;
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
            if (Scene != null) Scene.Notification -= OnMessage;
        }

        private T Q<T>(string name) where T : VisualElement => _root.Q<T>(name);

        private void Build()
        {
            BuildTop();
            BuildNavballCluster();
            BuildPanels();
        }

        // ------------------------------------------------------------------ layout

        private void BuildTop()
        {
            _altitude = Q<Label>("altitude");
            _altMode = Q<Label>("alt-mode");
            _vspeed = Q<Label>("vspeed");
            _situation = Q<Label>("situation");
            var modeBtn = Q<Button>("alt-mode-btn");
            modeBtn.clicked += () => _altTerrain = !_altTerrain;
            UIKit.Tooltip(modeBtn, () => "Click to toggle altitude above sea level / above terrain");

            _clock = Q<Label>("clock-text");
            _warpText = Q<Label>("warp-text");
            _warpLimit = Q<Label>("warp-limit");
            var slower = Q<Button>("warp-slower");
            slower.clicked += () => { if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex - 1); else Sim.SetRailsWarp(Sim.Warp.RailsIndex - 1); };
            UIKit.Tooltip(slower, () => "Slower time warp  [,]");
            var stop = Q<Button>("warp-stop");
            stop.clicked += () => Sim.StopWarp();
            UIKit.Tooltip(stop, () => "Stop time warp  [/]");
            var faster = Q<Button>("warp-faster");
            faster.clicked += () => Sim.SetRailsWarp(Sim.Warp.RailsIndex + 1);
            UIKit.Tooltip(faster, () => "Faster time warp  [.]\nHigh rates are limited by altitude, atmosphere and thrust.\nAlt + . for physics warp (2-4x).");
            var phys = Q<Button>("warp-phys");
            phys.clicked += () => Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex + 1);
            UIKit.Tooltip(phys, () => "Physics warp (up to 4x, simulates normally)  [Alt + .]");

            _bigNotice = Q<Label>("big-notice");
            Q<Button>("map-btn").clicked += () => Scene.ToggleMap();
            Q<Button>("help-btn").clicked += () => PauseMenu.Instance?.ToggleHelp();
            Q<Button>("menu-btn").clicked += () => PauseMenu.Instance?.TogglePause();
        }

        private void BuildNavballCluster()
        {
            _navball = NavballWidget.Create(gameObject, Q<VisualElement>("navball"), Sim, 236);

            _speedMode = Q<Button>("speed-mode");
            _speedMode.clicked += CycleSpeedMode;
            UIKit.Tooltip(_speedMode, () => "Speed reference: Surface / Orbit / Target.\nDetermines the prograde/retrograde markers and SAS modes.");
            _speed = Q<Label>("speed");

            _throttle = Q<VisualElement>("throttle");
            _throttleText = Q<Label>("throttle-text");
            UIKit.Tooltip(_throttle, () => "Throttle: Shift/Ctrl, Z = full, X = cut");
            _gmeter = Q<VisualElement>("gmeter");
            _gText = Q<Label>("g-text");
            UIKit.Tooltip(_gmeter, () => "Proper acceleration (g-force), 0-10 g");

            _sasBtn = Q<Button>("sas-btn");
            _sasBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) { v.Ctrl.Sas = !v.Ctrl.Sas; v.Attitude.ResetHold(); } };
            UIKit.Tooltip(_sasBtn, () => "Stability assist (T). Holds attitude or the selected direction using reaction wheels, engine gimbal and RCS.");
            _rcsBtn = Q<Button>("rcs-btn");
            _rcsBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.Rcs = !v.Ctrl.Rcs; };
            UIKit.Tooltip(_rcsBtn, () => "Reaction control (R). Uses monopropellant for rotation and H/N/I/K/J/L translation.");
            _legsBtn = Q<Button>("legs-btn");
            _legsBtn.clicked += () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.LegsDeployed = !v.Ctrl.LegsDeployed; };
            UIKit.Tooltip(_legsBtn, () => "Landing legs (G)");

            // SAS modes (right, two columns): positions match the old canvas (centre x from the cluster's middle).
            var modes = new (SasMode mode, string icon, string tip, Color color)[]
            {
                (SasMode.StabilityAssist, null, "Hold current attitude  [1]", UIKit.TextColor),
                (SasMode.Maneuver, "nav_maneuver", "Point at maneuver burn  [0]", NavballWidget.Man),
                (SasMode.Prograde, "nav_prograde", "Prograde  [2]", NavballWidget.Pro),
                (SasMode.Retrograde, "nav_retrograde", "Retrograde  [3]", NavballWidget.Pro),
                (SasMode.Normal, "nav_normal", "Orbit normal  [4]", NavballWidget.Nrm),
                (SasMode.AntiNormal, "nav_antinormal", "Anti-normal  [5]", NavballWidget.Nrm),
                (SasMode.RadialOut, "nav_radialout", "Radial out  [6]", NavballWidget.Rad),
                (SasMode.RadialIn, "nav_radialin", "Radial in  [7]", NavballWidget.Rad),
                (SasMode.Target, "nav_target", "Towards target  [8]", NavballWidget.Tgt),
                (SasMode.AntiTarget, "nav_antitarget", "Away from target  [9]", NavballWidget.Tgt),
            };
            var holder = Q<VisualElement>("sas-modes");
            for (int i = 0; i < modes.Length; i++)
            {
                var md = modes[i];
                var mode = md.mode;
                var b = UIKit.Button(md.icon == null ? "HOLD" : "", () => { var v = Sim.ActiveVessel; if (v != null) FlightInput.SetSasMode(v, mode); }, "sas-mode");
                if (md.icon != null) b.Add(UIKit.Image(UIKit.Icon(md.icon), md.color, "btn__icon"));
                int col = i % 2, row = i / 2;
                b.style.left = 310 + 172 + col * 40 - 18;
                b.style.bottom = 250 - row * 38;
                string tip = md.tip;
                UIKit.Tooltip(b, () => "SAS: " + tip);
                holder.Add(b);
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

        private void BuildPanels()
        {
            _stagePanel = Q<VisualElement>("staging");
            _stageList = Q<ScrollView>("stage-list");
            _resources = Q<Label>("resources-text");
            _data = Q<Label>("data-text");
            _burnPanel = Q<VisualElement>("burn");
            _burnInfo = Q<Label>("burn-text");
            _targetPanel = Q<VisualElement>("target");
            _targetInfo = Q<Label>("target-text");
            _warnPanel = Q<VisualElement>("warnings");
            _warnings = Q<Label>("warnings-text");
            _evaPanel = Q<VisualElement>("eva");
            _evaInfo = Q<Label>("eva-text");
            _messages = Q<VisualElement>("messages");
            UIKit.Show(_burnPanel, false);
            UIKit.Show(_targetPanel, false);
            UIKit.Show(_warnPanel, false);
            UIKit.Show(_evaPanel, false);
        }

        public void OnMessage(string msg, bool important)
        {
            if (_messages == null) return;
            var t = UIKit.Label(msg, important ? "message message--important" : "message");
            _messages.Add(t);
            _messageItems.Add((t, Time.unscaledTime + (important ? 7f : 4.5f)));
            while (_messageItems.Count > 7)
            {
                _messageItems[0].text.RemoveFromHierarchy();
                _messageItems.RemoveAt(0);
            }
            if (important && (msg.StartsWith("Liftoff") || msg.StartsWith("Docked") || msg.StartsWith("Flag planted")))
            {
                _bigNotice.text = msg;
                _bigNoticeUntil = Time.unscaledTime + 3;
            }
        }
    }
}
