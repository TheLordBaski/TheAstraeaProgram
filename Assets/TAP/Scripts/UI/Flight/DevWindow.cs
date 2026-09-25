using System;
using System.Collections.Generic;
using System.Globalization;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>
    /// Developer window (Alt+F12 or ` in flight, or the pause menu), like KSP's cheat menu: cheats, teleports,
    /// refills and camera settings. The cheats last until the game is closed. Drag the title to move the window.
    /// </summary>
    public sealed class DevWindow : MonoBehaviour
    {
        private const string PosKey = "TAP.DevWindowPos";
        private const float Width = 430f;

        public static DevWindow Instance { get; private set; }
        public FlightSceneController Scene;
        private FlightSim Sim => Scene.Sim;
        private Canvas _canvas;
        private GameObject _panel;
        private readonly List<(UIKit.ButtonRef btn, Func<bool> get)> _toggles = new List<(UIKit.ButtonRef, Func<bool>)>();
        private UIKit.ButtonRef _tellusBtn, _lumaBtn;
        private TMP_InputField _alt, _inc, _lat, _lon;
        private TextMeshProUGUI _zoomText, _info;
        private CelestialBody _body;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static DevWindow Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("DevWindow", 90);
            var w = canvas.gameObject.AddComponent<DevWindow>();
            w.Scene = scene;
            w._canvas = canvas;
            Instance = w;
            w.Build();
            w._panel.SetActive(false);
            return w;
        }

        private void Build()
        {
            var root = (RectTransform)_canvas.transform;
            var p = UIKit.Panel(root, "Developer", new Color(0.05f, 0.06f, 0.08f, 0.96f));
            _panel = p.gameObject;
            var window = p.rectTransform;
            // Top left, right of the mission guide and clear of the navball; the height follows the content.
            UIKit.Place(window, new Vector2(0, 1), new Vector2(0, 1), SavedPosition(), new Vector2(Width, 100));
            UIKit.VLayout(p, 5, 14);
            p.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = UIKit.Label(p.transform, "DEVELOPER TOOLS", 17, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(title, 24);
            title.raycastTarget = true;
            var drag = title.gameObject.AddComponent<DragWindow>();
            drag.Target = window;
            drag.Moved = SavePosition;
            var hint = UIKit.Label(p.transform, "Drag the title to move the window · Alt+F12 or ` closes it", 12, UIKit.TextDim);
            UIKit.Size(hint, 16);

            Section(p.transform, "Cheats (until the game is closed)");
            var r1 = Row(p.transform);
            Toggle(r1, "Infinite propellant", () => DevCheats.InfinitePropellant, on => DevCheats.InfinitePropellant = on,
                   "Engines, RCS and the EVA pack draw from their tanks without emptying them (empty tanks supply too).");
            Toggle(r1, "Infinite electricity", () => DevCheats.InfiniteElectricity, on => DevCheats.InfiniteElectricity = on,
                   "Electric charge is never used up.");
            var r2 = Row(p.transform);
            Toggle(r2, "No crash damage", () => DevCheats.NoCrashDamage, on => DevCheats.NoCrashDamage = on,
                   "Impacts, hard landings and splashdowns destroy nothing; landing legs don't break.");
            Toggle(r2, "Ignore heat", () => DevCheats.IgnoreHeat, on => DevCheats.IgnoreHeat = on,
                   "Parts heat up as usual but never burn up; parachutes don't burn.");
            var r3 = Row(p.transform);
            Toggle(r3, "Unbreakable joints", () => DevCheats.UnbreakableJoints, on => DevCheats.UnbreakableJoints = on,
                   "Joints never fail from structural loads; parachutes don't tear.");
            Flex(UIKit.Button(r3, "Refill all tanks", () => Report(DevTools.Refill(Sim)), 14).Button);

            Section(p.transform, "Teleport the active vessel");
            var bodies = Row(p.transform);
            _tellusBtn = UIKit.Button(bodies, "Tellus", () => SelectBody(Sim.System.Root), 14);
            Flex(_tellusBtn.Button);
            _lumaBtn = UIKit.Button(bodies, "Luma", () => SelectBody(Sim.System.Get("luma")), 14);
            Flex(_lumaBtn.Button);
            var orbitRow = Row(p.transform);
            _alt = Field(orbitRow, "Altitude (km)", "100");
            _inc = Field(orbitRow, "Inclination (°)", "0");
            Action(p.transform, "Set circular orbit", SetOrbit,
                   "Circular prograde orbit at that altitude, starting above the vessel's current longitude.");
            var landRow = Row(p.transform);
            _lat = Field(landRow, "Latitude (°)", "0");
            _lon = Field(landRow, "Longitude (°)", "0");
            Action(p.transform, "Land there (legs down, at rest)", PutDown,
                   "Stands the vessel upright on the ground at that latitude/longitude with its landing legs deployed.");

            Section(p.transform, "Camera");
            var zoom = Row(p.transform);
            var zl = UIKit.Label(zoom, "Mouse-wheel zoom speed", 14, UIKit.TextDim);
            UIKit.Size(zl, 28, 190);
            var minus = UIKit.Button(zoom, "−", () => ScrollZoom.Speed /= 1.25f, 16);
            UIKit.Size(minus.Button, 28, 40);
            _zoomText = UIKit.Label(zoom, "", 15, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Size(_zoomText, 28, 64);
            var plus = UIKit.Button(zoom, "+", () => ScrollZoom.Speed *= 1.25f, 16);
            UIKit.Size(plus.Button, 28, 40);

            _info = UIKit.Label(p.transform, "", 13, UIKit.TextDim);
            _info.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Size(_info, 36);
            Action(p.transform, "Close", Toggle, null);
        }

        private static void Section(Transform parent, string text)
        {
            var l = UIKit.Label(parent, text, 14, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(l, 22);
        }

        private static RectTransform Row(Transform parent)
        {
            var row = UIKit.Rect(parent, "Row");
            UIKit.Size(row, 28);
            UIKit.HLayout(row, 6, 0);
            return row;
        }

        private static void Flex(Component c) => UIKit.Size(c, 28).flexibleWidth = 1;

        private void Toggle(Transform row, string label, Func<bool> get, Action<bool> set, string tip)
        {
            var b = UIKit.Button(row, label, () => { set(!get()); Report($"{label}: {(get() ? "on" : "off")}"); }, 14);
            Flex(b.Button);
            UIKit.Tooltip(b.Button.gameObject, () => tip);
            _toggles.Add((b, get));
        }

        private static void Action(Transform parent, string label, Action act, string tip)
        {
            var b = UIKit.Button(parent, label, act, 14);
            UIKit.Size(b.Button, 30);
            if (tip != null) UIKit.Tooltip(b.Button.gameObject, () => tip);
        }

        private static TMP_InputField Field(Transform row, string label, string initial)
        {
            var l = UIKit.Label(row, label, 14, UIKit.TextDim);
            UIKit.Size(l, 28, 104);
            var f = UIKit.Input(row, initial, _ => { }, 15);
            UIKit.Size(f, 28, 88);
            return f;
        }

        private static Vector2 SavedPosition()
        {
            var def = new Vector2(330, -104);
            try
            {
                string s = PlayerPrefs.GetString(PosKey, "");
                var parts = s.Split(';');
                if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    return new Vector2(x, y);
            }
            catch (Exception) { }
            return def;
        }

        private static void SavePosition(Vector2 pos)
        {
            PlayerPrefs.SetString(PosKey, pos.x.ToString("0", CultureInfo.InvariantCulture) + ";" + pos.y.ToString("0", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        private void SelectBody(CelestialBody b)
        {
            if (b == null) return;
            _body = b;
            bool luma = b != Sim.System.Root;
            _alt.text = luma ? "30" : "100";
            if (luma) { _lat.text = "0"; _lon.text = "0"; }
            else
            {
                var site = Sim.System.Def.launchSite;
                _lat.text = (0.0 - site.latitude + 0.0).ToString("0.###", CultureInfo.InvariantCulture); // the site data counts latitude towards +Y (south)
                _lon.text = (site.longitude + 0.02).ToString("0.###", CultureInfo.InvariantCulture); // ~200 m east of the pad
            }
        }

        private static double Num(TMP_InputField f, double fallback) =>
            double.TryParse(f.text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : fallback;

        private void SetOrbit() => Report(DevTools.SetOrbit(Sim, _body ?? Sim.System.Root, Num(_alt, 100) * 1000, Num(_inc, 0)));

        private void PutDown() => Report(DevTools.PutDown(Sim, _body ?? Sim.System.Root, Num(_lat, 0), Num(_lon, 0)));

        private void Report(string msg)
        {
            Scene.Notify(msg, false);
            Sim.Log("[Dev] " + msg);
        }

        public void Toggle()
        {
            if (_panel == null) return;
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open && _body == null) SelectBody(Sim.ActiveVessel != null ? Sim.ActiveVessel.MainBody : Sim.System.Root);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && !UiState.KeyboardCaptured)
            {
                bool alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
                if ((alt && kb.f12Key.wasPressedThisFrame) || kb.backquoteKey.wasPressedThisFrame) Toggle();
            }
            if (!IsOpen) return;
            foreach (var (btn, get) in _toggles) if (btn.Active != get()) btn.SetActive(get());
            _tellusBtn.SetActive(_body == Sim.System.Root);
            _lumaBtn.SetActive(_body != null && _body != Sim.System.Root);
            _zoomText.text = $"{ScrollZoom.Speed:0.0#}×";
            var v = Sim.ActiveVessel;
            _info.text = v != null
                ? $"{v.VesselName}: {v.Situation} at {v.MainBody.Name}, altitude {v.Altitude / 1000:0.0} km. Teleports keep parts, resources and crew and clear manoeuvre nodes."
                : "No active vessel";
        }
    }
}
