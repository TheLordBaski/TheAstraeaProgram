using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Developer window (Alt+F12 or ` in flight, or the pause menu), like KSP's cheat menu grown into a console: a page per
    /// subject (tabs down the left), live readouts, and a search that filters every page at once. Drag the title to move
    /// it and the corner grip to resize it; position, size and page are remembered. The cheats last until the game is
    /// closed. Layout: Resources/UI/DevWindow.uxml; the pages are built here.
    /// To add a tool, add a line to a Build… method below: <see cref="Toggle"/> (on/off, lit while on), <see cref="Act"/>
    /// (a key), <see cref="Field"/> (a number to type) or <see cref="Watch"/> (a live value); <see cref="Section"/> starts
    /// a group and <see cref="Page"/> a tab. Search and refresh pick them up without more wiring.
    /// </summary>
    public sealed class DevWindow : MonoBehaviour
    {
        private const string PosKey = "TAP.DevWindowPos", SizeKey = "TAP.DevWindowSize", TabKey = "TAP.DevWindowTab";

        public static DevWindow Instance { get; private set; }
        public FlightSceneController Scene;
        private FlightSim Sim => Scene.Sim;
        private Vessel V => Sim.ActiveVessel;

        private sealed class PageView
        {
            public string Title;
            public Button Tab;
            public VisualElement Root;
        }

        private VisualElement _panel, _pageHost, _tabHost, _grid;
        private ScrollView _pagesView, _tabsView;
        private Label _status, _cheats;
        private TextField _search;
        private readonly List<PageView> _pages = new List<PageView>();
        private PageView _page, _building;
        private readonly List<(Button btn, Func<bool> get)> _toggles = new List<(Button, Func<bool>)>();
        private readonly List<(PageView page, Label val, Func<string> get)> _watches = new List<(PageView, Label, Func<string>)>();
        private readonly Dictionary<CelestialBody, Button> _bodyButtons = new Dictionary<CelestialBody, Button>();
        private TextField _alt, _inc, _lat, _lon;
        private CelestialBody _body;
        private float _refresh, _fps;

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
            UIKit.Place(_panel, SavedVector(PosKey, new Vector2(330, 104)));
            var size = SavedVector(SizeKey, Vector2.zero);
            if (size.x > 0) { _panel.style.width = size.x; _panel.style.height = size.y; }
            var title = root.Q<Label>("dev-title");
            title.pickingMode = PickingMode.Position;
            title.AddManipulator(new WindowDrag(_panel) { Moved = p => SaveVector(PosKey, p) });
            root.Q("dev-grip").AddManipulator(new WindowResize(_panel) { Min = new Vector2(520, 340), Resized = s => SaveVector(SizeKey, s) });
            root.Q<Button>("dev-close").clicked += Toggle;
            UIKit.Tooltip(root.Q<Button>("dev-close"), () => Controls.Expand("Close  [{Dev/Window}]"));
            _cheats = root.Q<Label>("dev-cheats");
            UIKit.Tooltip(_cheats, () => "A cheat is on (Cheats page). They last until the game is closed.");
            _cheats.pickingMode = PickingMode.Position;
            _status = root.Q<Label>("dev-status");
            _search = root.Q<TextField>("dev-search");
            _search.textEdition.placeholder = "Search tools…";
            _search.RegisterValueChangedCallback(_ => Filter());
            _tabsView = root.Q<ScrollView>("dev-tabs");
            _tabHost = _tabsView.contentContainer;
            _pagesView = root.Q<ScrollView>("dev-pages");
            _pageHost = _pagesView.contentContainer;

            BuildCheats();
            BuildTeleport();
            BuildVessel();
            BuildSimulation();
            BuildTerrain();
            BuildCamera();
            BuildDebug();

            string saved = PlayerPrefs.GetString(TabKey, "");
            ShowPage(_pages.Find(p => p.Title == saved) ?? _pages[0]);
        }

        // ------------------------------------------------------------------ pages

        private void BuildCheats()
        {
            Page("CHEATS");
            Section("Cheats", "Until the game is closed.");
            Toggle("Infinite propellant", () => DevCheats.InfinitePropellant, on => DevCheats.InfinitePropellant = on,
                   "Engines, RCS and the EVA pack draw from their tanks without emptying them (empty tanks supply too).");
            Toggle("Infinite electricity", () => DevCheats.InfiniteElectricity, on => DevCheats.InfiniteElectricity = on,
                   "Electric charge is never used up.");
            Toggle("No crash damage", () => DevCheats.NoCrashDamage, on => DevCheats.NoCrashDamage = on,
                   "Impacts, hard landings and splashdowns destroy nothing; landing legs don't break.");
            Toggle("Ignore heat", () => DevCheats.IgnoreHeat, on => DevCheats.IgnoreHeat = on,
                   "Parts heat up as usual but never burn up; parachutes don't burn.");
            Toggle("Unbreakable joints", () => DevCheats.UnbreakableJoints, on => DevCheats.UnbreakableJoints = on,
                   "Joints never fail from structural loads; parachutes don't tear.");
            Section("Resources");
            Act("Refill all tanks", () => Report(DevTools.Refill(Sim)), "Fills every tank and battery of the active vessel.");
        }

        private void BuildTeleport()
        {
            Page("TELEPORT");
            Section("Where", "Every body of the system: the star and the gas giants have no ground, only orbits.");
            _grid.AddToClassList("dev-grid--tight");
            foreach (var body in Sim.System.Bodies.OrderBy(b => b == Sim.System.HomeBody ? 0 : 1))
            {
                var b = body;
                _bodyButtons[b] = Act(b.Name, () => SelectBody(b), null, "dev-key--small");
            }
            Section("Orbit", "A circular prograde orbit, starting above the vessel's current longitude.");
            _alt = Field("Altitude (km)", "100");
            _inc = Field("Inclination (°)", "0");
            Act("Set circular orbit", SetOrbit, "Circular prograde orbit at that altitude, starting above the vessel's current longitude.");
            Section("Surface");
            _lat = Field("Latitude (°)", "0");
            _lon = Field("Longitude (°)", "0");
            Act("Land there (legs down, at rest)", PutDown, "Stands the vessel upright on the ground at that latitude/longitude with its landing legs deployed.");
            Section("Now", "Teleports keep parts, resources and crew and clear manoeuvre nodes.");
            Watch("VESSEL", () => V == null ? "—" : $"{V.VesselName} · {V.Situation} · {V.MainBody.Name}", true);
        }

        private void BuildVessel()
        {
            Page("VESSEL");
            Section("Flight");
            Watch("ALTITUDE", () => V == null ? "—" : MathD.FormatDistance(V.Altitude, 2));
            Watch("ABOVE TERRAIN", () => V == null ? "—" : MathD.FormatDistance(V.AltitudeAGL, 2));
            Watch("SURFACE SPEED", () => V == null ? "—" : $"{V.SurfaceSpeed:N1} m/s");
            Watch("ORBITAL SPEED", () => V == null ? "—" : $"{V.OrbitalSpeed:N1} m/s");
            Watch("VERTICAL SPEED", () => V == null ? "—" : $"{V.VerticalSpeed:N1} m/s");
            Watch("G-FORCE", () => V == null ? "—" : $"{V.GForce:0.00} g");
            Section("Orbit");
            Watch("APOAPSIS", () => V?.Orbit == null ? "—" : V.Orbit.IsElliptic ? MathD.FormatDistance(V.Orbit.ApoapsisRadius - V.MainBody.Radius, 2) : "escape");
            Watch("PERIAPSIS", () => V?.Orbit == null ? "—" : MathD.FormatDistance(V.Orbit.PeriapsisRadius - V.MainBody.Radius, 2));
            Watch("INCLINATION", () => V?.Orbit == null ? "—" : $"{V.Orbit.Inclination * MathD.Rad2Deg:0.000}°");
            Watch("ECCENTRICITY", () => V?.Orbit == null ? "—" : $"{V.Orbit.Eccentricity:0.00000}");
            Watch("PERIOD", () => V?.Orbit == null || !V.Orbit.IsElliptic ? "—" : MathD.FormatDuration(V.Orbit.Period));
            Section("Air");
            Watch("MACH", () => V == null ? "—" : $"{V.Mach:0.00}");
            Watch("DYNAMIC PRESSURE", () => V == null ? "—" : $"{V.DynamicPressure / 1000:0.00} kPa");
            Watch("STATIC PRESSURE", () => V == null ? "—" : $"{V.StaticPressure / 1000:0.000} kPa");
            Watch("AIR TEMPERATURE", () => V == null ? "—" : $"{V.AirTemperature:0} K");
            Section("Craft");
            Watch("MASS", () => V == null ? "—" : $"{V.TotalMass / 1000:0.000} t");
            Watch("PARTS", () => V == null ? "—" : $"{V.Parts.Count}");
            Watch("STAGE", () => V == null ? "—" : $"{V.CurrentStage}");
            Watch("THROTTLE", () => V == null ? "—" : $"{V.Ctrl.Throttle * 100:0}%");
            Watch("SAS", () => V == null ? "—" : V.Ctrl.Sas ? V.Ctrl.SasMode.ToString() : "off");
            Watch("RCS", () => V == null ? "—" : V.Ctrl.Rcs ? "on" : "off");
            Watch("MAX JOINT LOAD", () => V == null || V.Parts.Count == 0 ? "—" : $"{V.Parts.Max(x => x.SmoothedJointLoad):P0}");
            Watch("HOTTEST PART", () =>
            {
                if (V == null || V.Parts.Count == 0) return "—";
                var p = V.Parts.OrderByDescending(x => x.SkinTemp / x.Def.skinMaxTemp).First();
                return $"{p.Def.title}: skin {p.SkinTemp:N0} K, {p.SkinTemp / p.Def.skinMaxTemp:P0} of its limit";
            }, true);
        }

        private void BuildSimulation()
        {
            Page("SIMULATION");
            Section("Time");
            Watch("UNIVERSAL TIME", () => $"{Sim.UT:N1} s");
            Watch("DATE", () => MathD.FormatUT(Sim.UT));
            Watch("WARP", () => Sim.Warp.OnRails ? $"rails {TimeWarp.RailsRates[Sim.Warp.RailsIndex]:N0}×" : $"physics {TimeWarp.PhysicsRates[Sim.Warp.PhysicsIndex]:0}×");
            Watch("PAUSED", () => Sim.Paused ? "yes" : "no");
            Watch("TIME SCALE", () => $"{Time.timeScale:0.##}");
            Watch("PHYSICS STEP", () => $"{Time.fixedDeltaTime * 1000:0.0} ms");
            Section("Performance");
            Watch("FRAME RATE", () => $"{_fps:0} fps");
            Watch("FRAME TIME", () => _fps > 0 ? $"{1000 / _fps:0.0} ms" : "—");
            Watch("MANAGED MEMORY", () => $"{GC.GetTotalMemory(false) / 1048576.0:N0} MB");
            Section("Vessels");
            Watch("LOADED", () => $"{Sim.LoadedVessels.Count}");
            Watch("IN THE UNIVERSE", () => $"{Sim.Handles.Count}");
        }

        private void BuildTerrain()
        {
            Page("TERRAIN");
            Section("Maps");
            Toggle("Biome map", () => MapView.ShowBiomes, on => MapView.ShowBiomes = on,
                   "Paints the map view's planets with their biomes (open the map with M).");
            Act("Where am I?", () => Report(DevTools.WhereAmI(Sim)), "Body, latitude, longitude, biome and height of the ground under the vessel.");
            Section("Bases");
            Act("Flatten ground for a base", () => Report(DevTools.FlattenForBase(Sim)), "Flattens a pad 150 m east of the vessel.");
            Act("Remove it", () => Report(DevTools.RemoveBaseFlats(Sim)), "Puts the land back as it was.");
        }

        private void BuildCamera()
        {
            Page("CAMERA");
            Section("Zoom");
            Watch("WHEEL ZOOM SPEED", () => $"{ScrollZoom.Speed:0.0#}×");
            Act("Slower", () => ScrollZoom.Speed /= 1.25f);
            Act("Faster", () => ScrollZoom.Speed *= 1.25f);
        }

        private void BuildDebug()
        {
            Page("DEBUG");
            Section("Problems");
            Act("Throw a test exception", () => throw new InvalidOperationException("Test exception from the developer window"),
                "Checks the notice and the report: the game logs the exception and offers to write a report.");
        }

        // ------------------------------------------------------------------ builders

        private void Page(string title)
        {
            var p = new PageView { Title = title, Root = UIKit.Element("dev-page") };
            p.Root.Add(UIKit.Label(title, "fd-head-title dev-page-title"));
            p.Tab = UIKit.Button(title, () => ShowPage(p), "fd-key dev-tab");
            UIKit.AddBar(p.Tab);
            _tabHost.Add(p.Tab);
            _pageHost.Add(p.Root);
            _pages.Add(p);
            _building = p;
        }

        private void Section(string title, string note = null)
        {
            var s = UIKit.Element("dev-sec");
            s.userData = Searchable(title, note);
            s.Add(UIKit.Label(title.ToUpperInvariant(), "fd-lbl dev-sec__title"));
            if (note != null) s.Add(UIKit.Label(note, "dev-sec__note wrap"));
            _grid = UIKit.Element("dev-grid");
            s.Add(_grid);
            _building.Root.Add(s);
        }

        private Button Toggle(string label, Func<bool> get, Action<bool> set, string tip)
        {
            var b = Act(label, () => { set(!get()); Report($"{label}: {(get() ? "on" : "off")}"); }, tip, "fd-key");
            UIKit.AddBar(b);
            _toggles.Add((b, get));
            return b;
        }

        private Button Act(string label, Action act, string tip = null, string classes = null)
        {
            var b = UIKit.Button(label, act, "dev-key dev-item " + classes);
            b.userData = Searchable(label, tip);
            if (tip != null) UIKit.Tooltip(b, () => tip);
            _grid.Add(b);
            return b;
        }

        private TextField Field(string label, string value)
        {
            var row = UIKit.Element("dev-field-row dev-item");
            row.userData = Searchable(label, null);
            row.Add(UIKit.Label(label, "dev-field-row__label"));
            var f = UIKit.Field(value, "dev-field");
            row.Add(f);
            _grid.Add(row);
            return f;
        }

        /// <summary>A live value; <paramref name="wide"/> gives a long one (a name) the whole row.</summary>
        private void Watch(string label, Func<string> value, bool wide = false)
        {
            var row = UIKit.Element("fd-kv dev-watch dev-item" + (wide ? " dev-watch--wide" : ""));
            row.userData = Searchable(label, null);
            row.Add(UIKit.Label(label, "fd-kv__key"));
            var val = UIKit.Label("", "fd-kv__val no-loc");
            row.Add(val);
            _grid.Add(row);
            _watches.Add((_building, val, value));
        }

        private static string Searchable(string label, string tip) => (label + " " + tip).ToLowerInvariant();

        // ------------------------------------------------------------------ paging and search

        private void ShowPage(PageView p)
        {
            _page = p;
            PlayerPrefs.SetString(TabKey, p.Title);
            if (_search.value.Length > 0) _search.value = ""; // Filter() shows the page
            else Filter();
            _pagesView.scrollOffset = Vector2.zero;
        }

        /// <summary>
        /// No query: the chosen page. A query: every page, each item shown if its label or tooltip has it, or every item of a
        /// section or page whose title has it.
        /// </summary>
        private void Filter()
        {
            string q = _search.value.Trim().ToLowerInvariant();
            bool searching = q.Length > 0;
            _pagesView.EnableInClassList("dev-pages--searching", searching);
            _tabsView.EnableInClassList("dev-tabs--searching", searching);
            foreach (var p in _pages)
            {
                UIKit.SetOn(p.Tab, !searching && p == _page);
                int pageHits = 0;
                bool pageHit = !searching || p.Title.ToLowerInvariant().Contains(q);
                foreach (var sec in p.Root.Children())
                {
                    if (!sec.ClassListContains("dev-sec")) continue;
                    int hits = 0;
                    bool secHit = pageHit || ((string)sec.userData).Contains(q);
                    sec.Query(className: "dev-item").ForEach(item =>
                    {
                        bool hit = secHit || ((string)item.userData).Contains(q);
                        UIKit.Show(item, hit);
                        if (hit) hits++;
                    });
                    UIKit.Show(sec, hits > 0);
                    pageHits += hits;
                }
                UIKit.Show(p.Root, searching ? pageHits > 0 : p == _page);
            }
            Refresh();
        }

        // ------------------------------------------------------------------ teleport

        private void SelectBody(CelestialBody b)
        {
            if (b == null) return;
            _body = b;
            bool luma = b != Sim.System.HomeBody;
            _alt.value = DefaultAltitudeKm(b).ToString(CultureInfo.InvariantCulture);
            if (luma) { _lat.value = "0"; _lon.value = "0"; }
            else
            {
                var site = Sim.System.Def.launchSite;
                _lat.value = (site.latitude + 0.0).ToString("0.###", CultureInfo.InvariantCulture);
                _lon.value = (site.longitude + 0.02).ToString("0.###", CultureInfo.InvariantCulture); // ~200 m east of the pad
            }
        }

        /// <summary>A sensible first orbit for a body (km): well above its air, or 60,000 km over a star.</summary>
        private static double DefaultAltitudeKm(CelestialBody b)
        {
            if (b.IsStar) return 60000;
            if (b.Atmosphere != null) return Math.Ceiling((b.Atmosphere.Height + 20000) / 1000);
            return 30;
        }

        private static double Num(TextField f, double fallback) =>
            double.TryParse(f.value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : fallback;

        private void SetOrbit() => Report(DevTools.SetOrbit(Sim, _body ?? Sim.System.HomeBody, Num(_alt, 100) * 1000, Num(_inc, 0)));

        private void PutDown() => Report(DevTools.PutDown(Sim, _body ?? Sim.System.HomeBody, Num(_lat, 0), Num(_lon, 0)));

        // ------------------------------------------------------------------ window

        private void Report(string msg)
        {
            _status.text = msg;
            Scene.Notify(msg, false);
            Sim.Log("[Dev] " + msg);
        }

        /// <summary>A saved "x;y" in panel units. Older versions stored the canvas offset (y negative).</summary>
        private static Vector2 SavedVector(string key, Vector2 def)
        {
            var parts = PlayerPrefs.GetString(key, "").Split(';');
            if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                return new Vector2(x, Mathf.Abs(y));
            return def;
        }

        private static void SaveVector(string key, Vector2 v)
        {
            PlayerPrefs.SetString(key, v.x.ToString("0", CultureInfo.InvariantCulture) + ";" + v.y.ToString("0", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        public void Toggle()
        {
            if (_panel == null) return;
            bool open = !IsOpen;
            UIKit.Show(_panel, open);
            if (open && _body == null) SelectBody(V != null ? V.MainBody : Sim.System.HomeBody);
            if (open) Refresh();
        }

        private void Refresh()
        {
            foreach (var (btn, get) in _toggles) UIKit.SetOn(btn, get());
            foreach (var kv in _bodyButtons) UIKit.SetOn(kv.Value, _body == kv.Key);
            UIKit.Show(_cheats, DevCheats.Any);
            bool searching = _search.value.Length > 0;
            foreach (var (page, val, get) in _watches)
            {
                if (!searching && page != _page) continue;
                try { val.text = get(); }
                catch (Exception) { val.text = "—"; } // a readout must never break the window
            }
        }

        private void Update()
        {
            if (!UiState.KeyboardCaptured && Controls.Down("Dev/Window")) Toggle();
            float dt = Time.unscaledDeltaTime;
            if (dt > 0) _fps = _fps <= 0 ? 1 / dt : Mathf.Lerp(_fps, 1 / dt, 0.05f);
            if (!IsOpen) return;
            _refresh -= dt;
            if (_refresh > 0) return;
            _refresh = 0.2f;
            Refresh();
        }
    }
}
