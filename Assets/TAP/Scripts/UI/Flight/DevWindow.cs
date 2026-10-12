using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Developer window (Alt+F12 or ` in flight, or the pause menu), like KSP's cheat menu: cheats, teleports,
    /// refills and camera settings. The cheats last until the game is closed. Drag the title to move the window.
    /// Layout: Resources/UI/DevWindow.uxml.
    /// </summary>
    public sealed class DevWindow : MonoBehaviour
    {
        private const string PosKey = "TAP.DevWindowPos";

        public static DevWindow Instance { get; private set; }
        public FlightSceneController Scene;
        private FlightSim Sim => Scene.Sim;
        private VisualElement _panel;
        private readonly List<(Button btn, Func<bool> get)> _toggles = new List<(Button, Func<bool>)>();
        private readonly System.Collections.Generic.Dictionary<CelestialBody, Button> _bodyButtons = new System.Collections.Generic.Dictionary<CelestialBody, Button>();
        private TextField _alt, _inc, _lat, _lon;
        private Label _zoomText, _info;
        private CelestialBody _body;

        public bool IsOpen => _panel != null && UIKit.Shown(_panel);

        public static DevWindow Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("DevWindow", 90, "DevWindow");
            var w = doc.gameObject.AddComponent<DevWindow>();
            w.Scene = scene;
            Instance = w;
            w.Build(doc.rootVisualElement);
            UIKit.Show(w._panel, false);
            return w;
        }

        private void Build(VisualElement root)
        {
            _panel = root.Q("dev-window");
            // Top left, right of the mission guide and clear of the navball; the height follows the content.
            UIKit.Place(_panel, SavedPosition());
            var title = root.Q<Label>("dev-title");
            title.pickingMode = PickingMode.Position;
            title.AddManipulator(new WindowDrag(_panel) { Moved = SavePosition });

            Toggle(root, "cheat-prop", () => DevCheats.InfinitePropellant, on => DevCheats.InfinitePropellant = on,
                   "Engines, RCS and the EVA pack draw from their tanks without emptying them (empty tanks supply too).");
            Toggle(root, "cheat-ec", () => DevCheats.InfiniteElectricity, on => DevCheats.InfiniteElectricity = on,
                   "Electric charge is never used up.");
            Toggle(root, "cheat-crash", () => DevCheats.NoCrashDamage, on => DevCheats.NoCrashDamage = on,
                   "Impacts, hard landings and splashdowns destroy nothing; landing legs don't break.");
            Toggle(root, "cheat-heat", () => DevCheats.IgnoreHeat, on => DevCheats.IgnoreHeat = on,
                   "Parts heat up as usual but never burn up; parachutes don't burn.");
            Toggle(root, "cheat-joints", () => DevCheats.UnbreakableJoints, on => DevCheats.UnbreakableJoints = on,
                   "Joints never fail from structural loads; parachutes don't tear.");
            Action(root, "refill", () => Report(DevTools.Refill(Sim)), null);

            // Every body of the system with a surface is a place to go to: the home planet first.
            var bodies = root.Q("bodies");
            foreach (var body in Sim.System.Bodies.OrderBy(b => b == Sim.System.HomeBody ? 0 : 1))
            {
                if (body.IsStar) continue;
                var b = body;
                var button = UIKit.Button(b.Name, () => SelectBody(b), "dev-btn");
                bodies.Add(button);
                _bodyButtons[b] = button;
            }
            _alt = root.Q<TextField>("alt");
            _inc = root.Q<TextField>("inc");
            _lat = root.Q<TextField>("lat");
            _lon = root.Q<TextField>("lon");
            Action(root, "set-orbit", SetOrbit, "Circular prograde orbit at that altitude, starting above the vessel's current longitude.");
            Action(root, "put-down", PutDown, "Stands the vessel upright on the ground at that latitude/longitude with its landing legs deployed.");

            Toggle(root, "biomes", () => MapView.ShowBiomes, on => MapView.ShowBiomes = on,
                   "Paints the map view's planets with their biomes (open the map with M).");
            Action(root, "where", () => Report(DevTools.WhereAmI(Sim)), null);
            Action(root, "flatten", () => Report(DevTools.FlattenForBase(Sim)), null);
            Action(root, "unflatten", () => Report(DevTools.RemoveBaseFlats(Sim)), null);

            Action(root, "zoom-minus", () => ScrollZoom.Speed /= 1.25f, null);
            Action(root, "zoom-plus", () => ScrollZoom.Speed *= 1.25f, null);
            _zoomText = root.Q<Label>("zoom");
            _info = root.Q<Label>("info");
            Action(root, "throw", () => throw new InvalidOperationException("Test exception from the developer window"),
                   "Checks the notice and the report: the game logs the exception and offers to write a report.");
            Action(root, "close", Toggle, null);
        }

        private void Toggle(VisualElement root, string name, Func<bool> get, Action<bool> set, string tip)
        {
            var b = root.Q<Button>(name);
            string label = b.text;
            b.clicked += () => { set(!get()); Report($"{label}: {(get() ? "on" : "off")}"); };
            UIKit.Tooltip(b, () => tip);
            _toggles.Add((b, get));
        }

        private static void Action(VisualElement root, string name, Action act, string tip)
        {
            var b = root.Q<Button>(name);
            b.clicked += act;
            if (tip != null) UIKit.Tooltip(b, () => tip);
        }

        /// <summary>Top-left corner in panel units. Older versions stored the canvas offset (y negative).</summary>
        private static Vector2 SavedPosition()
        {
            var def = new Vector2(330, 104);
            try
            {
                string s = PlayerPrefs.GetString(PosKey, "");
                var parts = s.Split(';');
                if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    return new Vector2(x, Mathf.Abs(y));
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
            bool luma = b != Sim.System.HomeBody;
            _alt.value = luma ? "30" : "100";
            if (luma) { _lat.value = "0"; _lon.value = "0"; }
            else
            {
                var site = Sim.System.Def.launchSite;
                _lat.value = (site.latitude + 0.0).ToString("0.###", CultureInfo.InvariantCulture);
                _lon.value = (site.longitude + 0.02).ToString("0.###", CultureInfo.InvariantCulture); // ~200 m east of the pad
            }
        }

        private static double Num(TextField f, double fallback) =>
            double.TryParse(f.value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : fallback;

        private void SetOrbit() => Report(DevTools.SetOrbit(Sim, _body ?? Sim.System.HomeBody, Num(_alt, 100) * 1000, Num(_inc, 0)));

        private void PutDown() => Report(DevTools.PutDown(Sim, _body ?? Sim.System.HomeBody, Num(_lat, 0), Num(_lon, 0)));

        private void Report(string msg)
        {
            Scene.Notify(msg, false);
            Sim.Log("[Dev] " + msg);
        }

        public void Toggle()
        {
            if (_panel == null) return;
            bool open = !IsOpen;
            UIKit.Show(_panel, open);
            if (open && _body == null) SelectBody(Sim.ActiveVessel != null ? Sim.ActiveVessel.MainBody : Sim.System.HomeBody);
        }

        private void Update()
        {
            if (!UiState.KeyboardCaptured && Controls.Down("Dev/Window")) Toggle();
            if (!IsOpen) return;
            foreach (var (btn, get) in _toggles) UIKit.SetOn(btn, get());
            foreach (var kv in _bodyButtons) UIKit.SetOn(kv.Value, _body == kv.Key);
            _zoomText.text = $"{ScrollZoom.Speed:0.0#}×";
            var v = Sim.ActiveVessel;
            _info.text = v != null
                ? $"{v.VesselName}: {v.Situation} at {v.MainBody.Name}, altitude {v.Altitude / 1000:0.0} km. Teleports keep parts, resources and crew and clear manoeuvre nodes."
                : "No active vessel";
        }
    }
}
