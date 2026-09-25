using System;
using System.Collections.Generic;
using System.Globalization;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.UI
{
    /// <summary>
    /// Developer window (Alt+F12 or ` in flight, or the pause menu), like KSP's cheat menu: cheats, teleports,
    /// refills and camera settings. The cheats last until the game is closed.
    /// </summary>
    public sealed class DevWindow : MonoBehaviour
    {
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
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, 30), new Vector2(460, 640));
            UIKit.VLayout(p, 6, 14);

            var title = UIKit.Label(p.transform, "DEVELOPER TOOLS  <size=13><color=#8ea0b8>Alt+F12 or ` to close</color></size>", 17, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(title, 26);

            Section(p.transform, "Cheats (until the game is closed)");
            Toggle(p.transform, "Infinite propellant", () => DevCheats.InfinitePropellant, on => DevCheats.InfinitePropellant = on,
                   "Engines, RCS and the EVA pack draw from their tanks without emptying them (empty tanks supply too).");
            Toggle(p.transform, "Infinite electricity", () => DevCheats.InfiniteElectricity, on => DevCheats.InfiniteElectricity = on,
                   "Electric charge is never used up.");
            Toggle(p.transform, "No crash damage", () => DevCheats.NoCrashDamage, on => DevCheats.NoCrashDamage = on,
                   "Impacts, hard landings and splashdowns destroy nothing; landing legs don't break.");
            Toggle(p.transform, "Ignore heat", () => DevCheats.IgnoreHeat, on => DevCheats.IgnoreHeat = on,
                   "Parts heat up as usual but never burn up; parachutes don't burn.");
            Toggle(p.transform, "Unbreakable joints", () => DevCheats.UnbreakableJoints, on => DevCheats.UnbreakableJoints = on,
                   "Joints never fail from structural loads; parachutes don't tear.");
            Action("Refill all tanks", () => Report(DevTools.Refill(Sim)), p.transform);

            Section(p.transform, "Teleport the active vessel");
            var bodies = UIKit.Rect(p.transform, "Bodies");
            UIKit.Size(bodies, 30);
            UIKit.HLayout(bodies, 6, 0);
            var lbl = UIKit.Label(bodies, "Body", 14, UIKit.TextDim);
            UIKit.Size(lbl, 30, 90);
            _tellusBtn = UIKit.Button(bodies, "Tellus", () => SelectBody(Sim.System.Root), 14);
            UIKit.Size(_tellusBtn.Button, 30, 120);
            _lumaBtn = UIKit.Button(bodies, "Luma", () => SelectBody(Sim.System.Get("luma")), 14);
            UIKit.Size(_lumaBtn.Button, 30, 120);

            _alt = Field(p.transform, "Orbit altitude (km)", "100");
            _inc = Field(p.transform, "Inclination (°)", "0");
            Action("Set circular orbit", SetOrbit, p.transform);
            _lat = Field(p.transform, "Latitude (°)", "0");
            _lon = Field(p.transform, "Longitude (°)", "0");
            Action("Put down on the surface there", PutDown, p.transform);

            Section(p.transform, "Camera");
            var zoom = UIKit.Rect(p.transform, "Zoom");
            UIKit.Size(zoom, 30);
            UIKit.HLayout(zoom, 6, 0);
            var zl = UIKit.Label(zoom, "Zoom speed", 14, UIKit.TextDim);
            UIKit.Size(zl, 30, 150);
            var minus = UIKit.Button(zoom, "−", () => ScrollZoom.Speed /= 1.25f, 16);
            UIKit.Size(minus.Button, 30, 40);
            _zoomText = UIKit.Label(zoom, "", 15, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Size(_zoomText, 30, 70);
            var plus = UIKit.Button(zoom, "+", () => ScrollZoom.Speed *= 1.25f, 16);
            UIKit.Size(plus.Button, 30, 40);

            _info = UIKit.Label(p.transform, "", 13, UIKit.TextDim);
            _info.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Size(_info, 40);
            Action("Close", Toggle, p.transform);
        }

        private static void Section(Transform parent, string text)
        {
            var l = UIKit.Label(parent, text, 14, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(l, 24);
        }

        private void Toggle(Transform parent, string label, Func<bool> get, Action<bool> set, string tip)
        {
            UIKit.ButtonRef b = null;
            b = UIKit.Button(parent, label, () => { set(!get()); Report($"{label}: {(get() ? "on" : "off")}"); }, 14);
            UIKit.Size(b.Button, 28);
            UIKit.Tooltip(b.Button.gameObject, () => tip);
            _toggles.Add((b, get));
        }

        private void Action(string label, Action act, Transform parent)
        {
            var b = UIKit.Button(parent, label, act, 14);
            UIKit.Size(b.Button, 30);
        }

        private TMP_InputField Field(Transform parent, string label, string initial)
        {
            var row = UIKit.Rect(parent, label);
            UIKit.Size(row, 30);
            UIKit.HLayout(row, 6, 0);
            var l = UIKit.Label(row, label, 14, UIKit.TextDim);
            UIKit.Size(l, 30, 200);
            var f = UIKit.Input(row, initial, _ => { }, 15);
            UIKit.Size(f, 30, 120);
            return f;
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
                _lat.text = site.latitude.ToString("0.###", CultureInfo.InvariantCulture);
                _lon.text = (site.longitude + 0.02).ToString("0.###", CultureInfo.InvariantCulture); // just east of the pad
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
                ? $"{v.VesselName}: {v.Situation} at {v.MainBody.Name}, altitude {v.Altitude / 1000:0.0} km. Teleports keep parts, resources and crew; they clear manoeuvre nodes."
                : "No active vessel";
        }
    }
}
