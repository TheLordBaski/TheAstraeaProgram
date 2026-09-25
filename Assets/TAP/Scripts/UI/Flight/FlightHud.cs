using System;
using System.Collections.Generic;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>
    /// Flight HUD: altimeter, clock + warp, navball cluster (speed modes, throttle, SAS/RCS/modes, g-meter),
    /// staging stack, resources, flight data, burn/target info, warnings, EVA panel and messages.
    /// </summary>
    public sealed partial class FlightHud : MonoBehaviour
    {
        public FlightSceneController Scene;
        public FlightSim Sim => Scene.Sim;
        public Canvas Canvas;
        private RectTransform _root;

        // Top
        private TextMeshProUGUI _altitude, _altMode, _vspeed, _situation, _clock, _warpText, _warpLimit;
        private bool _altTerrain;
        // Navball cluster
        private NavballWidget _navball;
        private TextMeshProUGUI _speed;
        private UIKit.ButtonRef _speedMode;
        private UIKit.BarRef _throttle, _gmeter;
        private TextMeshProUGUI _throttleText, _gText;
        private UIKit.ButtonRef _sasBtn, _rcsBtn, _legsBtn;
        private readonly Dictionary<SasMode, UIKit.ButtonRef> _sasModes = new Dictionary<SasMode, UIKit.ButtonRef>();
        // Panels
        private TextMeshProUGUI _data, _resources, _burnInfo, _targetInfo, _warnings, _evaInfo;
        private RectTransform _burnPanel, _targetPanel, _warnPanel, _evaPanel, _dataPanel, _resPanel, _stagePanel;
        private RectTransform _stageContent;
        private readonly List<GameObject> _stageRows = new List<GameObject>();
        private string _stageSignature;
        private RectTransform _messages;
        private readonly List<(TextMeshProUGUI text, float until)> _messageItems = new List<(TextMeshProUGUI, float)>();
        private TextMeshProUGUI _bigNotice;
        private float _bigNoticeUntil;
        private float _slowTimer;

        public static FlightHud Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("FlightHUD", 10);
            var hud = canvas.gameObject.AddComponent<FlightHud>();
            hud.Scene = scene;
            hud.Canvas = canvas;
            hud.Build();
            scene.Notification += hud.OnMessage;
            scene.UiVisibilityChanged += visible => canvas.enabled = visible;
            TooltipView.Ensure();
            return hud;
        }

        private void OnDestroy()
        {
            if (Scene != null) Scene.Notification -= OnMessage;
        }

        private void Build()
        {
            _root = (RectTransform)Canvas.transform;
            BuildTop();
            BuildNavballCluster();
            BuildStaging();
            BuildResources();
            BuildDataPanel();
            BuildInfoPanels();
            BuildMessages();
            BuildTopButtons();
        }

        // ------------------------------------------------------------------ layout

        private void BuildTop()
        {
            var alt = UIKit.Panel(_root, "Altimeter");
            UIKit.Place(alt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(360, 92));
            _altitude = UIKit.Label(alt.transform, "0 m", 34, UIKit.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
            UIKit.Place(_altitude.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(340, 40));
            _altMode = UIKit.Label(alt.transform, "ALT (SEA)", 13, UIKit.TextDim, TextAlignmentOptions.Left);
            UIKit.Place(_altMode.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -6), new Vector2(120, 18));
            var modeBtn = UIKit.Button(alt.transform, "", () => _altTerrain = !_altTerrain);
            modeBtn.Background.color = new Color(0, 0, 0, 0.01f);
            UIKit.Stretch(modeBtn.Rect);
            UIKit.Tooltip(modeBtn.Button.gameObject, () => "Click to toggle altitude above sea level / above terrain");
            _vspeed = UIKit.Label(alt.transform, "", 16, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Place(_vspeed.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(340, 20));
            _situation = UIKit.Label(alt.transform, "", 13, UIKit.TextDim, TextAlignmentOptions.Center);
            UIKit.Place(_situation.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 7), new Vector2(340, 18));

            var clock = UIKit.Panel(_root, "Clock");
            UIKit.Place(clock.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -8), new Vector2(330, 92));
            _clock = UIKit.Label(clock.transform, "", 16, UIKit.TextColor);
            UIKit.Place(_clock.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(310, 40));
            _clock.textWrappingMode = TextWrappingModes.Normal;
            var row = UIKit.Rect(clock.transform, "WarpRow");
            UIKit.Place(row, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 8), new Vector2(310, 30));
            UIKit.HLayout(row.GetComponent<RectTransform>(), 4, 0);
            var slower = UIKit.Button(row, "◀◀", () => { if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex - 1); else Sim.SetRailsWarp(Sim.Warp.RailsIndex - 1); }, 15);
            UIKit.Size(slower.Button, 30, 46);
            UIKit.Tooltip(slower.Button.gameObject, () => "Slower time warp  [,]");
            var stop = UIKit.Button(row, "■", () => Sim.StopWarp(), 15);
            UIKit.Size(stop.Button, 30, 40);
            UIKit.Tooltip(stop.Button.gameObject, () => "Stop time warp  [/]");
            var faster = UIKit.Button(row, "▶▶", () => Sim.SetRailsWarp(Sim.Warp.RailsIndex + 1), 15);
            UIKit.Size(faster.Button, 30, 46);
            UIKit.Tooltip(faster.Button.gameObject, () => "Faster time warp  [.]\nHigh rates are limited by altitude, atmosphere and thrust.\nAlt + . for physics warp (2-4x).");
            var phys = UIKit.Button(row, "P×", () => Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex + 1), 15);
            UIKit.Size(phys.Button, 30, 40);
            UIKit.Tooltip(phys.Button.gameObject, () => "Physics warp (up to 4x, simulates normally)  [Alt + .]");
            _warpText = UIKit.Label(row, "1x", 18, UIKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UIKit.Size(_warpText, 30, 90);
            _warpLimit = UIKit.Label(clock.transform, "", 12, UIKit.Warn);
            UIKit.Place(_warpLimit.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, -2), new Vector2(320, 16));

            _bigNotice = UIKit.Label(_root, "", 30, UIKit.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
            UIKit.Place(_bigNotice.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 50));
        }

        private void BuildTopButtons()
        {
            var bar = UIKit.Rect(_root, "TopButtons");
            UIKit.Place(bar, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(300, 38));
            UIKit.HLayout(bar.GetComponent<RectTransform>(), 6, 0).childAlignment = TextAnchor.MiddleRight;
            var map = UIKit.Button(bar, "Map [M]", () => Scene.ToggleMap(), 15);
            UIKit.Size(map.Button, 36, 90);
            var help = UIKit.Button(bar, "Help [F1]", () => PauseMenu.Instance?.ToggleHelp(), 15);
            UIKit.Size(help.Button, 36, 96);
            var pause = UIKit.Button(bar, "Menu [Esc]", () => PauseMenu.Instance?.TogglePause(), 15);
            UIKit.Size(pause.Button, 36, 104);
        }

        private void BuildNavballCluster()
        {
            var cluster = UIKit.Rect(_root, "NavballCluster");
            UIKit.Place(cluster, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(620, 330));
            var bg = UIKit.Panel(cluster, "Back");
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(560, 300));
            _navball = NavballWidget.Create(cluster, Sim, 236);
            _navball.Root.anchorMin = _navball.Root.anchorMax = new Vector2(0.5f, 0);
            _navball.Root.anchoredPosition = new Vector2(0, 142);

            // Speed display
            var sp = UIKit.Panel(cluster, "Speed", UIKit.PanelLight);
            UIKit.Place(sp.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 292), new Vector2(250, 36));
            _speedMode = UIKit.Button(sp.transform, "SURF", CycleSpeedMode, 13);
            UIKit.Place(_speedMode.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(64, 30));
            UIKit.Tooltip(_speedMode.Button.gameObject, () => "Speed reference: Surface / Orbit / Target.\nDetermines the prograde/retrograde markers and SAS modes.");
            _speed = UIKit.Label(sp.transform, "0.0 m/s", 21, UIKit.Good, TextAlignmentOptions.Right, FontStyles.Bold);
            UIKit.Place(_speed.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(170, 30));

            // Throttle gauge (left)
            _throttle = UIKit.Bar(cluster, true);
            UIKit.Place(_throttle.Back.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-158, 150), new Vector2(22, 200));
            _throttleText = UIKit.Label(cluster, "0%", 14, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Place(_throttleText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-158, 36), new Vector2(60, 20));
            var tl = UIKit.Label(cluster, "THR", 12, UIKit.TextDim, TextAlignmentOptions.Center);
            UIKit.Place(tl.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-158, 264), new Vector2(60, 16));
            UIKit.Tooltip(_throttle.Back.gameObject, () => "Throttle: Shift/Ctrl, Z = full, X = cut");
            // G meter
            _gmeter = UIKit.Bar(cluster, true);
            UIKit.Place(_gmeter.Back.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-190, 150), new Vector2(14, 200));
            _gText = UIKit.Label(cluster, "0.0g", 13, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Place(_gText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-196, 36), new Vector2(50, 20));
            UIKit.Tooltip(_gmeter.Back.gameObject, () => "Proper acceleration (g-force), 0-10 g");

            // SAS / RCS toggles (left bottom)
            _sasBtn = UIKit.Button(cluster, "SAS", () => { var v = Sim.ActiveVessel; if (v != null) { v.Ctrl.Sas = !v.Ctrl.Sas; v.Attitude.ResetHold(); } }, 15);
            UIKit.Place(_sasBtn.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-240, 250), new Vector2(62, 30));
            UIKit.Tooltip(_sasBtn.Button.gameObject, () => "Stability assist (T). Holds attitude or the selected direction using reaction wheels, engine gimbal and RCS.");
            _rcsBtn = UIKit.Button(cluster, "RCS", () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.Rcs = !v.Ctrl.Rcs; }, 15);
            UIKit.Place(_rcsBtn.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-240, 214), new Vector2(62, 30));
            UIKit.Tooltip(_rcsBtn.Button.gameObject, () => "Reaction control (R). Uses monopropellant for rotation and H/N/I/K/J/L translation.");
            _legsBtn = UIKit.Button(cluster, "LEGS", () => { var v = Sim.ActiveVessel; if (v != null) v.Ctrl.LegsDeployed = !v.Ctrl.LegsDeployed; }, 15);
            UIKit.Place(_legsBtn.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-240, 178), new Vector2(62, 30));
            UIKit.Tooltip(_legsBtn.Button.gameObject, () => "Landing legs (G)");

            // SAS modes (right, two columns)
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
            for (int i = 0; i < modes.Length; i++)
            {
                var md = modes[i];
                var mode = md.mode;
                var b = UIKit.Button(cluster, md.icon == null ? "HOLD" : "", () => { var v = Sim.ActiveVessel; if (v != null) FlightInput.SetSasMode(v, mode); }, 12,
                    md.icon != null ? UIKit.Icon(md.icon) : null);
                if (b.Icon != null) b.Icon.color = md.color;
                int col = i % 2, row = i / 2;
                UIKit.Place(b.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(172 + col * 40, 250 - row * 38), new Vector2(36, 34));
                string tip = md.tip;
                UIKit.Tooltip(b.Button.gameObject, () => "SAS: " + tip);
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

        private void BuildStaging()
        {
            var p = UIKit.Panel(_root, "Staging");
            _stagePanel = p.rectTransform;
            UIKit.Place(_stagePanel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(250, 520));
            var title = UIKit.Label(p.transform, "STAGES  [Space]", 14, UIKit.TextDim);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(230, 18));
            var scroll = UIKit.ScrollList(p.transform, out _stageContent, 4);
            UIKit.Stretch((RectTransform)scroll.transform, 6, 6, 28, 6);
        }

        private void BuildResources()
        {
            var p = UIKit.Panel(_root, "Resources");
            _resPanel = p.rectTransform;
            UIKit.Place(_resPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -52), new Vector2(300, 190));
            _resources = UIKit.Label(p.transform, "", 15, UIKit.TextColor);
            _resources.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Stretch(_resources.rectTransform, 12, 10, 8, 8);
            _resources.alignment = TextAlignmentOptions.TopLeft;
        }

        private void BuildDataPanel()
        {
            var p = UIKit.Panel(_root, "FlightData");
            _dataPanel = p.rectTransform;
            UIKit.Place(_dataPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -250), new Vector2(300, 380));
            _data = UIKit.Label(p.transform, "", 14, UIKit.TextColor);
            _data.textWrappingMode = TextWrappingModes.Normal;
            _data.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_data.rectTransform, 12, 10, 8, 8);
        }

        private void BuildInfoPanels()
        {
            var bp = UIKit.Panel(_root, "BurnInfo");
            _burnPanel = bp.rectTransform;
            UIKit.Place(_burnPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 342), new Vector2(360, 84));
            _burnInfo = UIKit.Label(bp.transform, "", 16, UIKit.TextColor, TextAlignmentOptions.Center);
            _burnInfo.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Stretch(_burnInfo.rectTransform, 8, 8, 6, 6);

            var tp = UIKit.Panel(_root, "TargetInfo");
            _targetPanel = tp.rectTransform;
            UIKit.Place(_targetPanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -108), new Vector2(330, 150));
            _targetInfo = UIKit.Label(tp.transform, "", 14, UIKit.TextColor);
            _targetInfo.textWrappingMode = TextWrappingModes.Normal;
            _targetInfo.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_targetInfo.rectTransform, 10, 10, 8, 8);

            var wp = UIKit.Panel(_root, "Warnings", new Color(0.25f, 0.05f, 0.03f, 0.85f));
            _warnPanel = wp.rectTransform;
            UIKit.Place(_warnPanel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(520, 90));
            _warnings = UIKit.Label(wp.transform, "", 15, UIKit.Warn);
            _warnings.textWrappingMode = TextWrappingModes.Normal;
            _warnings.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_warnings.rectTransform, 10, 10, 6, 6);

            var ep = UIKit.Panel(_root, "EVA");
            _evaPanel = ep.rectTransform;
            // Bottom left, where the staging stack sits in flight (hidden on EVA): clear of the navball.
            UIKit.Place(_evaPanel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(400, 150));
            _evaInfo = UIKit.Label(ep.transform, "", 16, UIKit.TextColor);
            _evaInfo.textWrappingMode = TextWrappingModes.Normal;
            _evaInfo.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_evaInfo.rectTransform, 12, 12, 8, 8);
        }

        private void BuildMessages()
        {
            _messages = UIKit.Rect(_root, "Messages");
            UIKit.Place(_messages, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(760, 200));
            var v = UIKit.VLayout(_messages.GetComponent<RectTransform>(), 2, 0);
            v.childAlignment = TextAnchor.UpperCenter;
        }

        public void OnMessage(string msg, bool important)
        {
            if (_messages == null) return;
            var t = UIKit.Label(_messages, msg, important ? 18 : 15, important ? UIKit.Warn : UIKit.TextColor, TextAlignmentOptions.Center);
            t.fontStyle = important ? FontStyles.Bold : FontStyles.Normal;
            UIKit.Size(t, important ? 24 : 20);
            _messageItems.Add((t, Time.unscaledTime + (important ? 7f : 4.5f)));
            while (_messageItems.Count > 7)
            {
                Destroy(_messageItems[0].text.gameObject);
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
