using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TAP.Core;
using TAP.Simulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TAP.EditorTools
{
    /// <summary>Editor-only body authoring: transient drafts/history, schema controls and a live development scene.</summary>
    public sealed class CelestialBodyLabWindow : EditorWindow
    {
        private const string Folder = "Assets/TAP/Scripts/Editor/CelestialBodyLab/";
        [SerializeField] private string _sourcePath = "Assets/TAP/Resources/Data/system.json";
        private BodyLabDocument _document;
        private BodyLabPreview _preview;
        private readonly Dictionary<string, bool> _folds = new Dictionary<string, bool>();
        private ScrollView _fields;
        private Label _status, _facts, _point, _coverage;
        private Button _undo, _redo;
        private Image _image, _map, _atmosphereChart;
        private Texture2D _chart;
        private DoubleField _latitudeField, _longitudeField;
        private DoubleField _timeField;
        private IntegerField _seedField;
        private BodyLabOrbitView _orbitView;
        private CelestialSystem _orbitSystem;
        private Button _orbitPlay;
        private bool _orbitPlaying, _needsOrbit;
        private double _lastTickTime, _nextFactsTime;
        [SerializeField] private int _randomSeed = 1;
        [SerializeField] private string _randomParentId;
        [SerializeField] private BodyLabBiomeTheme _biomeTheme;
        [SerializeField] private bool _parentOrbitView;
        [SerializeField] private double _orbitTimeScale = 10000;
        [SerializeField] private double _latitude, _longitude, _patchWidth = 10000, _ut;
        [SerializeField] private float _lightAngle;
        [SerializeField] private bool _patch;
        private bool _needsPreview, _refine, _renderDirty;
        [SerializeField] private BodyLabPreview.ViewMode _mode;
        private double _due, _refineDue;
        private string _error, _notice;
        private Vector2 _lastPointer;
        public BodyLabDocument Document => _document;
        public BodyLabPreview Preview => _preview;
        // The menu opens one lab. A stable session key survives both domain reload and test-runner window recreation.
        private const string SessionKey = "TAP.BodyLab.Current";

        [MenuItem("TAP/Terrain/Celestial Body Lab")]
        public static void ShowLab()
        {
            var window = GetWindow<CelestialBodyLabWindow>("Celestial Body Lab");
            window.minSize = new Vector2(1000, 650);
            window.OpenDevelopmentScene();
        }

        private void OnEnable()
        {
            minSize = new Vector2(1000, 650);
            _lastTickTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            string stored = SessionState.GetString(SessionKey, "");
            if (_document == null && stored.Length > 0)
            {
                var state = JObject.Parse(stored);
                _sourcePath = (string)state["sourcePath"];
                SetDocument(BodyLabDocument.RestoreSession((JObject)state["document"], LoadPreset));
            }
            else if (_document == null && File.Exists(_sourcePath)) LoadBody(_sourcePath, null);
        }

        private void OnDisable()
        {
            if (_document != null)
                SessionState.SetString(SessionKey, new JObject { ["sourcePath"] = _sourcePath, ["document"] = _document.CaptureSession() }.ToString());
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            _preview?.Dispose(); _preview = null;
            _orbitPlaying = false;
            if (_chart != null) DestroyImmediate(_chart);
        }

        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) { _preview?.Dispose(); _preview = null; }
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (scene.path != BodyLabScene.Path) return;
            _preview?.Dispose(); _preview = null;
            _notice = "Development scene closed. Use Open Dev Scene to resume previews.";
            if (_image != null) _image.image = null;
            if (_map != null) _map.image = null;
            UpdateStatus();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "CelestialBodyLab.uxml");
            if (tree == null) { rootVisualElement.Add(new Label("Import the Celestial Body Lab UI assets, then reopen this window.")); return; }
            tree.CloneTree(rootVisualElement);
            rootVisualElement.style.flexGrow = 1;
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(Folder + "CelestialBodyLab.uss"));
            _status = rootVisualElement.Q<Label>("status");
            var split = new TwoPaneSplitView(0, 470, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            rootVisualElement.Q("content").Add(split);
            _fields = new ScrollView(); _fields.AddToClassList("body-lab-pane"); split.Add(_fields);
            var right = new ScrollView(); right.AddToClassList("body-lab-pane"); split.Add(right);
            BuildToolbar(); BuildPreviewPanel(right); RebuildFields(); UpdateStatus();
            rootVisualElement.RegisterCallback<KeyDownEvent>(e =>
            {
                if (!(e.ctrlKey || e.commandKey)) return;
                if (e.keyCode == KeyCode.Z) { History(e.shiftKey); e.StopImmediatePropagation(); }
                else if (e.keyCode == KeyCode.Y) { History(true); e.StopImmediatePropagation(); }
            }, TrickleDown.TrickleDown);
            var scene = SceneManager.GetSceneByPath(BodyLabScene.Path);
            if (scene.IsValid() && scene.isLoaded && _preview == null && !EditorApplication.isPlayingOrWillChangePlaymode) OpenDevelopmentScene();
        }

        private void BuildToolbar()
        {
            var toolbar = rootVisualElement.Q("toolbar"); toolbar.Clear();
            var source = new ObjectField("System JSON") { objectType = typeof(TextAsset), allowSceneObjects = false, value = AssetDatabase.LoadAssetAtPath<TextAsset>(_sourcePath) };
            source.style.width = 320; toolbar.Add(source);
            source.RegisterValueChangedCallback(e =>
            {
                string path = AssetDatabase.GetAssetPath(e.newValue);
                if (e.newValue == null || !CanLeaveDraft()) { source.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<TextAsset>(_sourcePath)); return; }
                Guard(() => LoadBody(path, null));
                BuildToolbar();
            });
            if (_document != null)
            {
                var choices = _document.BodyIds.ToList();
                if (!string.IsNullOrEmpty(_document.Id) && !choices.Contains(_document.Id)) choices.Add(_document.Id);
                string selected = _document.Id;
                if (!choices.Contains(selected)) selected = choices[0];
                var body = new PopupField<string>("Body", choices, selected); body.style.width = 240; toolbar.Add(body);
                body.RegisterValueChangedCallback(e =>
                {
                    if (!CanLeaveDraft()) { body.SetValueWithoutNotify(selected); return; }
                    Guard(() => LoadBody(_sourcePath, e.newValue)); BuildToolbar();
                });
            }
            toolbar.Add(new Button(() => Guard(() => { if (CanLeaveDraft()) { SetDocument(_document.NewBody(false)); RebuildFields(); BuildToolbar(); } })) { text = "New Body" });
            toolbar.Add(new Button(() => Guard(() => { SetDocument(_document.NewBody(true)); RebuildFields(); BuildToolbar(); })) { text = "Duplicate Draft" });
            toolbar.Add(new Button(() => Guard(() => { if (CanLeaveDraft()) LoadBody(_sourcePath, _document.IsNew ? null : _document.Id); BuildToolbar(); })) { text = "Reload" });
            var actions = rootVisualElement.Q("actions"); actions.Clear();
            _undo = new Button(() => History(false)) { text = "Undo", tooltip = "Ctrl+Z. History lives only in this editing session." }; actions.Add(_undo);
            _redo = new Button(() => History(true)) { text = "Redo", tooltip = "Ctrl+Y / Ctrl+Shift+Z" }; actions.Add(_redo);
            actions.Add(new Button(() => { _document?.Revert(); RebuildFields(); }) { text = "Revert Draft" });
            actions.Add(new Button(() => Guard(SaveToSystem)) { text = "Save to System" });
            actions.Add(new Button(() => Guard(ExportBody)) { text = "Export Body JSON" });
            actions.Add(new Button(() => Guard(ExportTerrain)) { text = "Export Terrain Preset" });
            actions.Add(new Button(() => Guard(() => { _document.Set("terrain", _document.EffectiveBody()["terrain"]); RebuildFields(); })) { text = "Make Terrain Inline", tooltip = "Copy resolved terrain into this body so inherited objects can be removed without editing a shared preset." });
            actions.Add(new Button(() => Guard(OpenDevelopmentScene)) { text = "Open Dev Scene" });
            BuildGenerationToolbar();
        }

        private void BuildGenerationToolbar()
        {
            var tools = rootVisualElement.Q("generation"); tools.Clear();
            _seedField = new IntegerField("Generation Seed") { name = "generationSeed", value = _randomSeed, tooltip = "Seed used by the next random body or biome edit. Successful random operations advance it. Set an earlier seed to reproduce the same settings." };
            _seedField.style.width = 220; tools.Add(_seedField);
            _seedField.RegisterValueChangedCallback(e => _randomSeed = e.newValue);
            tools.Add(new Button(() => { _randomSeed = Guid.NewGuid().GetHashCode(); _seedField.SetValueWithoutNotify(_randomSeed); }) { text = "Shuffle Seed", tooltip = "Choose a new generation seed without changing the draft." });
            var parents = new List<string> { "Automatic" }; parents.AddRange(_document?.MoonParentIds ?? Enumerable.Empty<string>());
            string choice = parents.Contains(_randomParentId ?? "") ? _randomParentId : "Automatic";
            _randomParentId = choice == "Automatic" ? null : choice;
            var parent = new PopupField<string>("Moon Parent", parents, choice) { name = "randomMoonParent", tooltip = "Parent for a new random moon. Automatic prefers the selected planet (or the selected moon's parent). Only saved planets can become parents." };
            parent.style.width = 250; tools.Add(parent);
            parent.RegisterValueChangedCallback(e => _randomParentId = e.newValue == "Automatic" ? null : e.newValue);
            tools.Add(new Button(() => Guard(() => GenerateRandomBody(false))) { text = "Random Planet", name = "randomPlanet", tooltip = "Create a new unsaved planet with radius, density-derived GM, orbit, rotation, terrain, biomes and atmosphere choices." });
            tools.Add(new Button(() => Guard(() => GenerateRandomBody(true))) { text = "Random Moon", name = "randomMoon", tooltip = "Create a new unsaved moon inside its parent's sphere of influence, with generated surface and physical parameters." });
            var theme = new EnumField("Biome Theme", _biomeTheme) { name = "biomeTheme", tooltip = "Starting palettes and region rules: temperate, arid, frozen, volcanic, cratered, or a randomized combination." };
            theme.style.width = 230; tools.Add(theme); theme.RegisterValueChangedCallback(e => _biomeTheme = (BodyLabBiomeTheme)e.newValue);
            tools.Add(new Button(() => Guard(() => ApplyBiomeTheme(_biomeTheme))) { text = "Apply Theme", name = "applyBiomeTheme", tooltip = "Replace biome and surface-colour rules in one undoable edit. Terrain heights and atmosphere are preserved." });
            tools.Add(new Button(() => Guard(() => ApplyBiomeTheme(BodyLabBiomeTheme.Random))) { text = "Random Biomes", name = "randomBiomes", tooltip = "Generate a biome palette and conditions for this body's existing terrain, including an unconditional fallback." });
            tools.Add(new Button(() => Guard(AddRandomBiome)) { text = "+ Random Biome", name = "addRandomBiome", tooltip = "Insert a new conditional region before existing biomes and add matching surface paint. Existing biomes and terrain remain available." });
        }

        public void GenerateRandomBody(bool moon)
        {
            if (_document == null || !CanLeaveDraft()) return;
            string preferred = _randomParentId ?? ((string)_document.Body["type"] == "moon" || _document.IsNew ? (string)_document.Body["parent"] : _document.Id);
            var generated = _document.NewRandomBody(moon, _randomSeed, preferred);
            SetDocument(generated); AdvanceSeed(); RebuildFields(); BuildToolbar();
            _notice = "Generated an unsaved " + (moon ? "moon" : "planet") + ". Save to System when ready.";
        }

        public void ApplyBiomeTheme(BodyLabBiomeTheme theme)
        {
            RequireSurfaceBody();
            _document.Commit(BodyLabRandomizer.ApplyBiomes(_document.Body, theme, _randomSeed, _document.EffectiveBody()));
            if (theme == BodyLabBiomeTheme.Random) AdvanceSeed();
            RebuildFields(); _notice = "Biome and colour rules updated as one Undo step.";
        }

        public void AddRandomBiome()
        {
            RequireSurfaceBody();
            _document.Commit(BodyLabRandomizer.AddBiome(_document.Body, _document.EffectiveBody(), _randomSeed));
            AdvanceSeed(); RebuildFields(); _notice = "Added a conditional biome and matching surface paint.";
        }

        private void RequireSurfaceBody()
        {
            if (_document == null || (string)_document.Body["type"] == "star") throw new FormatException("Choose a planet or moon before editing its biomes.");
        }
        private void AdvanceSeed() { _randomSeed = unchecked(_randomSeed + 1); _seedField?.SetValueWithoutNotify(_randomSeed); }

        private void BuildOrbitPanel(VisualElement parent)
        {
            var fold = new Foldout { text = "System Orbits", value = true, tooltip = "Preview the complete system with the current unsaved body parameters." }; parent.Add(fold);
            var controls = Row(fold);
            var focus = new Toggle("Parent View") { value = _parentOrbitView, tooltip = "Focus on the draft's parent and its children so moons remain visible. Disable to show every body in the system." }; controls.Add(focus);
            focus.RegisterValueChangedCallback(e => { _parentOrbitView = e.newValue; _orbitView.SetParentView(e.newValue); });
            _orbitPlay = new Button(() => { _orbitPlaying = !_orbitPlaying; _lastTickTime = EditorApplication.timeSinceStartup; _orbitPlay.text = _orbitPlaying ? "Pause" : "Play"; }) { text = "Play", name = "playOrbits", tooltip = "Animate all bodies using the game's orbital propagation and the shared Preview UT." }; controls.Add(_orbitPlay);
            controls.Add(new Button(() => _orbitView.Fit()) { text = "Fit", tooltip = "Frame the current system or parent view and reset its tilt/zoom." });
            var step = Row(fold);
            step.Add(new Button(() => StepOrbit(-0.25)) { text = "−¼ Orbit", tooltip = "Move backward by a quarter of the draft body's orbital period." });
            step.Add(new Button(() => StepOrbit(0.25)) { text = "+¼ Orbit", tooltip = "Move forward by a quarter of the draft body's orbital period." });
            step.Add(new Button(() => SetPreviewTime(_orbitSystem?.Def.startUT ?? 0)) { text = "Reset UT", tooltip = "Return to the system's start universal time." });
            var speed = new DoubleField("Time scale (s/s)") { value = _orbitTimeScale, name = "orbitTimeScale", tooltip = "Simulated seconds per real second during playback. Negative values play backward." }; fold.Add(speed);
            speed.RegisterValueChangedCallback(e => { _orbitTimeScale = double.IsNaN(e.newValue) || double.IsInfinity(e.newValue) ? 0 : MathD.Clamp(e.newValue, -1e12, 1e12); speed.SetValueWithoutNotify(_orbitTimeScale); });
            _orbitView = new BodyLabOrbitView(); fold.Add(_orbitView); _orbitView.SetParentView(_parentOrbitView); _orbitView.SetTime(_ut);
            var details = Text(fold); details.text = "Hover a body for its parent, period and apsides."; _orbitView.Hovered += text => details.text = text;
            fold.Add(new Label("Gold = draft · drag to tilt · scroll to zoom · distances to scale, markers enlarged."));
        }

        private void StepOrbit(double fraction)
        {
            var body = _orbitSystem?.Get(_document.Id);
            if (body?.Orbit != null) SetPreviewTime(_ut + body.Orbit.Period * fraction);
        }

        public void SetPreviewTime(double ut, bool diagnostics = true)
        {
            _ut = double.IsNaN(ut) || double.IsInfinity(ut) ? 0 : ut;
            _timeField?.SetValueWithoutNotify(_ut); _orbitView?.SetTime(_ut); _renderDirty = true;
            if (diagnostics) UpdateDiagnostics();
        }

        private void BuildPreviewPanel(VisualElement parent)
        {
            var row = Row(parent);
            var mode = new EnumField("View", _mode); row.Add(mode);
            mode.RegisterValueChangedCallback(e => { _mode = (BodyLabPreview.ViewMode)e.newValue; SetView(); });
            var patch = new Toggle("Surface Patch") { value = _patch }; row.Add(patch);
            patch.RegisterValueChangedCallback(e => { _patch = e.newValue; SetView(); _preview?.Frame(); });
            row.Add(new Button(() => { _preview?.Frame(); BodyLabScene.Frame(_patch); _renderDirty = true; }) { text = "Frame" });
            _image = new Image { scaleMode = ScaleMode.ScaleToFit, name = "previewImage" }; _image.AddToClassList("body-lab-preview"); parent.Add(_image);
            _image.RegisterCallback<GeometryChangedEvent>(_ => _renderDirty = true);
            _image.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                _lastPointer = e.localPosition; _image.CapturePointer(e.pointerId);
                if (e.clickCount == 2) _preview?.Frame(); e.StopPropagation();
            });
            _image.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_image.HasPointerCapture(e.pointerId)) return;
                Vector2 p = e.localPosition, delta = p - _lastPointer; _lastPointer = p;
                _preview?.Orbit(delta.x * 0.5f, delta.y * 0.5f); _renderDirty = true;
            });
            _image.RegisterCallback<PointerUpEvent>(e => { if (_image.HasPointerCapture(e.pointerId)) _image.ReleasePointer(e.pointerId); });
            _image.RegisterCallback<WheelEvent>(e => { _preview?.Zoom(e.delta.y); _renderDirty = true; e.StopPropagation(); });
            parent.Add(new Label("Drag to rotate · scroll to zoom · double-click to frame. Click the map to inspect a location."));
            _latitudeField = new DoubleField("Latitude (° N)") { value = _latitude, name = "previewLatitude", isDelayed = true }; parent.Add(_latitudeField);
            _longitudeField = new DoubleField("Longitude (° E)") { value = _longitude, name = "previewLongitude", isDelayed = true }; parent.Add(_longitudeField);
            _latitudeField.RegisterValueChangedCallback(e => SetCoordinates(MathD.Clamp(e.newValue, -90, 90), _longitude));
            _longitudeField.RegisterValueChangedCallback(e => SetCoordinates(_latitude, MathD.Clamp(e.newValue, -180, 180)));
            var width = new DoubleField("Patch width (m)") { value = _patchWidth, isDelayed = true }; parent.Add(width);
            width.RegisterValueChangedCallback(e => { _patchWidth = MathD.Clamp(e.newValue, 10, 1000000); width.SetValueWithoutNotify(_patchWidth); QueuePreview(); });
            _timeField = new DoubleField("Preview UT (s)") { value = _ut, name = "previewUT", tooltip = "Universal time in seconds. Updates body rotation and every body's position in the system orbit preview; it does not change saved epochs." }; parent.Add(_timeField);
            _timeField.RegisterValueChangedCallback(e => SetPreviewTime(e.newValue));
            var light = new Slider("Lighting angle (°)", -180, 180) { value = _lightAngle }; parent.Add(light);
            light.RegisterValueChangedCallback(e => { _lightAngle = e.newValue; _renderDirty = true; });
            _map = new Image { scaleMode = ScaleMode.StretchToFill, name = "mapImage" }; _map.AddToClassList("body-lab-map"); parent.Add(_map);
            _map.RegisterCallback<PointerDownEvent>(e =>
            {
                if (_preview?.Map == null) return;
                SetCoordinates(90 - 180 * e.localPosition.y / _map.contentRect.height, -180 + 360 * e.localPosition.x / _map.contentRect.width);
            });
            parent.Add(new Button(() => Guard(ExportMap)) { text = "Export Current Map PNG" });
            _point = Text(parent); _facts = Text(parent); _coverage = Text(parent);
            _atmosphereChart = new Image { scaleMode = ScaleMode.ScaleToFit }; _atmosphereChart.style.height = 150; parent.Add(_atmosphereChart);
            BuildOrbitPanel(parent);
            parent.Add(new HelpBox("Preview geometry and maps have finite resolution. Biome shares and height checks are sampled estimates. Material indices are stored for ART-07a.", HelpBoxMessageType.Info));
        }

        private static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement(); row.AddToClassList("body-lab-row"); parent.Add(row); return row;
        }
        private static Label Text(VisualElement parent) { var label = new Label(); label.AddToClassList("body-lab-readout"); parent.Add(label); return label; }

        public void LoadBody(string path, string id)
        {
            var system = JObject.Parse(File.ReadAllText(path));
            if (!(system["bodies"] is JArray bodies) || bodies.Count == 0) throw new FormatException("Select a system JSON containing a bodies list.");
            if (id == null || !bodies.Any(b => (string)b["id"] == id))
            {
                string homeId = (string)system["launchSite"]?["body"];
                id = bodies.Any(b => (string)b["id"] == homeId) ? homeId : (string)bodies[0]["id"];
            }
            var document = new BodyLabDocument(system.ToString(), id, LoadPreset);
            _sourcePath = path; SetDocument(document); RebuildFields();
        }

        private static string LoadPreset(string name)
        {
            string root = Path.GetFullPath("Assets/TAP/Resources/Data/Terrain") + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, name + ".json"));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new FormatException("Terrain presets must stay inside Resources/Data/Terrain.");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private void SetDocument(BodyLabDocument document)
        {
            if (_document != null) _document.Changed -= OnDraftChanged;
            _document = document; _document.Changed += OnDraftChanged;
            _folds.Clear(); _error = _notice = null; OnDraftChanged();
        }

        private void OnDraftChanged()
        {
            _needsOrbit = true;
            hasUnsavedChanges = _document?.Dirty == true;
            saveChangesMessage = "Save this body's draft to its system JSON before closing? Undo/Redo history is only in memory.";
            QueuePreview(); UpdateStatus();
        }

        private void History(bool redo)
        {
            if (redo) _document?.Redo(); else _document?.Undo();
            RebuildFields();
        }

        private void RebuildFields()
        {
            if (_fields == null || _document == null) return;
            var scroll = _fields.scrollOffset; _fields.Clear();
            JObject effective;
            try { effective = _document.EffectiveBody(); }
            catch (Exception e) { effective = _document.Body; _error = e.Message; }
            new BodyLabFields(_document, RebuildFields, _folds).Draw(_fields, effective);
            var raw = new Foldout { text = "Draft JSON (optional)", value = false };
            var text = new TextField { multiline = true, value = _document.Body.ToString(), isDelayed = true };
            raw.Add(text); raw.Add(new Button(() => Guard(() => { _document.Commit(JObject.Parse(text.value)); RebuildFields(); })) { text = "Apply Draft JSON" });
            _fields.Add(raw); _fields.schedule.Execute(() => _fields.scrollOffset = scroll);
            UpdateStatus();
        }

        public void OpenDevelopmentScene()
        {
            var scene = BodyLabScene.Open();
            _preview?.Dispose(); _preview = new BodyLabPreview(scene);
            QueuePreview(); BodyLabScene.Frame(_patch); _notice = "Development scene opened. Preview objects are temporary.";
            UpdateStatus();
        }

        public void SetCoordinates(double latitude, double longitude)
        {
            _latitude = MathD.Clamp(latitude, -90, 90); _longitude = MathD.Clamp(longitude, -180, 180);
            _latitudeField?.SetValueWithoutNotify(_latitude); _longitudeField?.SetValueWithoutNotify(_longitude);
            QueuePreview();
        }

        public void QueuePreview()
        {
            _preview?.Invalidate(); _needsPreview = true; _refine = false; _due = EditorApplication.timeSinceStartup + 0.25;
        }

        private void SetView()
        {
            _preview?.SetView(_patch, _mode); _renderDirty = true;
            if (_map != null) _map.image = _preview?.Map;
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup, elapsed = MathD.Clamp(now - _lastTickTime, 0, 0.2); _lastTickTime = now;
            if (_document == null || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                if (_orbitPlaying)
                {
                    SetPreviewTime(_ut + elapsed * _orbitTimeScale, false);
                    if (now > _nextFactsTime) { _nextFactsTime = now + 0.5; UpdateDiagnostics(); }
                }
                if (_preview == null)
                {
                    if (_needsOrbit && now >= _due) { UpdateOrbitSystem(_document.BuildSystem()); _needsOrbit = false; }
                    return;
                }
                if (_preview.Poll())
                {
                    _renderDirty = true; _refine = _preview.Current.Resolution < 56;
                    _refineDue = EditorApplication.timeSinceStartup + 0.6;
                    SetView(); UpdateDiagnostics(); SceneView.RepaintAll();
                }
                if (_preview.Error != null) _error = _preview.Error;
                if (!_preview.Busy && _needsPreview && EditorApplication.timeSinceStartup >= _due)
                {
                    _needsPreview = false;
                    var system = _document.BuildSystem(); UpdateOrbitSystem(system); _needsOrbit = false;
                    var body = system.Get(_document.Id);
                    _preview.Start(body, _latitude, _longitude, _patchWidth, false); _error = null;
                }
                else if (!_preview.Busy && _refine && !_needsPreview && EditorApplication.timeSinceStartup >= _refineDue)
                {
                    _refine = false;
                    _preview.Start(_preview.Current.Body, _latitude, _longitude, _patchWidth, true);
                }
                if (_renderDirty && _image != null && _image.contentRect.width > 0)
                {
                    _preview.Render((int)_image.contentRect.width, (int)_image.contentRect.height, _ut, _lightAngle);
                    _image.image = _preview.Image; _renderDirty = false;
                }
            }
            catch (Exception e) { _needsPreview = _refine = _needsOrbit = false; _error = e.Message; }
            UpdateStatus();
        }

        private void UpdateOrbitSystem(CelestialSystem system)
        {
            _orbitSystem = system; _orbitView?.SetSystem(system, _document.Id); _orbitView?.SetTime(_ut);
        }

        private void UpdateStatus()
        {
            _undo?.SetEnabled(_document?.CanUndo == true); _redo?.SetEnabled(_document?.CanRedo == true);
            if (_status == null) return;
            string state = _document == null ? "Choose a system JSON." : _document.Id + (_document.Dirty ? " · unsaved draft" : " · saved");
            string progress = _preview?.Busy == true ? " · generating preview…" : _needsPreview ? " · preview pending" : "";
            _status.text = state + progress + (_error != null ? "\n" + _error + " Last valid preview retained." : _notice != null ? "\n" + _notice : "");
            _status.style.color = _error != null ? new Color(1, 0.55f, 0.4f) : StyleKeyword.Null;
        }

        private void UpdateDiagnostics()
        {
            var r = _preview?.Current;
            if (r == null || _facts == null) return;
            var b = r.Body;
            var facts = new StringBuilder();
            facts.AppendLine($"{b.Name} · radius {b.Radius:N0} m · mass {b.Mass:G5} kg");
            facts.AppendLine($"Surface gravity {b.SurfaceGravity:F3} m/s² · escape speed {Math.Sqrt(2 * b.GM / b.Radius):F1} m/s");
            facts.AppendLine($"Rotation period {b.RotationPeriod:N1} s · solar day {b.SolarDay:N1} s");
            if (b.Orbit != null)
            {
                facts.AppendLine($"Orbital period {b.Orbit.Period:N1} s · SOI {b.SOIRadius:N0} m");
                var pos = b.GetPositionAtUT(_ut); facts.AppendLine($"At UT {_ut:N1}: position ({pos.x:G5}, {pos.y:G5}, {pos.z:G5}) m");
            }
            if (!b.IsStar)
            {
                var dir = Geo.FromLatLon(r.Latitude, r.Longitude);
                double height = b.Terrain.Height(dir);
                CubeSphere.FromSphere(dir, out int face, out double u, out double v);
                double step = 2.0 / b.Radius;
                Vector3d P(double uu, double vv) { var d = CubeSphere.ToSphere(face, uu, vv); return d * (b.Radius + b.Terrain.Height(d)); }
                var normal = Vector3d.Cross(P(u + step, v) - P(u - step, v), P(u, v + step) - P(u, v - step)).normalized;
                double slope = Math.Acos(MathD.Clamp(Math.Abs(Vector3d.Dot(normal, dir)), 0, 1)) * MathD.Rad2Deg;
                _point.text = $"{r.Latitude:F4}° N, {r.Longitude:F4}° E · height {height:F2} m · slope {slope:F2}° · biome {b.Terrain.BiomeAt(dir).Name}";
                facts.AppendLine($"Sampled heights {r.MinHeight:F1}…{r.MaxHeight:F1} m; declared {b.Terrain.MinHeight:F1}…{b.Terrain.MaxHeight:F1} m.");
                if (r.MinHeight < b.Terrain.MinHeight || r.MaxHeight > b.Terrain.MaxHeight) facts.AppendLine("Warning: sampled terrain exceeds its declared bounds.");
                var coverage = new StringBuilder("Approximate biome coverage (weighted by surface area):\n");
                for (int i = 0; i < r.Shares.Length; i++) coverage.AppendLine($"{b.Terrain.Biomes[i].Name}: {r.Shares[i]:F2}%" + (r.Shares[i] == 0 ? " · not found at this resolution" : ""));
                if (b.Terrain is LayeredTerrain t)
                    foreach (var area in t.FlatAreas) coverage.AppendLine($"Flat area {area.Id}: height {area.Height:F1} m, fitted tilt {area.SlopeDeg:F2}°");
                _coverage.text = coverage.ToString();
            }
            else { _point.text = "Star preview · surface terrain/biome views do not apply."; _coverage.text = $"Luminosity {b.Luminosity:G5} W · surface temperature {b.Def.surfaceTemperature:N0} K"; }
            if (b.HasAtmosphere) facts.AppendLine($"Sea-level density {b.Atmosphere.SeaLevelDensity:F4} kg/m³ · sound speed {b.Atmosphere.SpeedOfSound(0):F1} m/s");
            _facts.text = facts.ToString(); DrawAtmosphere(b);
        }

        private void DrawAtmosphere(CelestialBody body)
        {
            if (_chart != null) DestroyImmediate(_chart);
            _atmosphereChart.image = null;
            _atmosphereChart.style.display = body.HasAtmosphere ? DisplayStyle.Flex : DisplayStyle.None;
            if (!body.HasAtmosphere) return;
            var a = body.Atmosphere;
            var p = new TexturePainter(512, 150, new Color(0.04f, 0.05f, 0.07f));
            p.Text("PRESSURE / DENSITY / TEMPERATURE", 10, 130, 1, Color.white);
            p.Line(18, 18, 500, 18, 1, Color.gray); p.Line(18, 18, 18, 118, 1, Color.gray);
            double maxTemp = 1;
            for (int i = 0; i <= 100; i++) maxTemp = Math.Max(maxTemp, a.Temperature(a.Height * i / 100));
            Color[] colours = { new Color(0.3f, 0.65f, 1), new Color(0.4f, 1, 0.55f), new Color(1, 0.65f, 0.25f) };
            for (int curve = 0; curve < 3; curve++)
            {
                float last = 0;
                for (int i = 0; i <= 100; i++)
                {
                    double h = a.Height * i / 100;
                    double value = curve == 0 ? a.Pressure(h) / Math.Max(1, a.SeaLevelPressure) : curve == 1 ? a.Density(h) / Math.Max(1e-12, a.SeaLevelDensity) : a.Temperature(h) / maxTemp;
                    float y = 18 + 100 * (float)MathD.Clamp01(value);
                    if (i > 0) p.Line(18 + 4.82f * (i - 1), last, 18 + 4.82f * i, y, 1.5f, colours[curve]); last = y;
                }
            }
            p.Text("0 M", 18, 3, 1, Color.gray); p.Text($"{a.Height:F0} M", 410, 3, 1, Color.gray);
            _chart = p.ToTexture(false); _chart.hideFlags = HideFlags.HideAndDontSave; _atmosphereChart.image = _chart;
            _atmosphereChart.tooltip = "Altitude left to right. Blue: pressure / sea-level pressure. Green: density / sea-level density. Orange: temperature / maximum temperature.";
        }

        public void SaveToSystem()
        {
            string output = _document.SaveJson(File.ReadAllText(_sourcePath));
            File.WriteAllText(_sourcePath, output);
            _document.MarkSaved(output); AssetDatabase.ImportAsset(_sourcePath);
            _notice = "Saved " + _sourcePath + ". Shared terrain presets are unchanged."; _error = null; UpdateStatus(); BuildToolbar();
        }

        private void ExportBody()
        {
            _document.BuildSystem();
            string path = EditorUtility.SaveFilePanelInProject("Export body JSON", _document.Id, "json", "Body JSON can be added to a system's bodies list.", "Assets/TAP/Editor");
            if (path.Length == 0) return;
            File.WriteAllText(path, _document.Body.ToString() + "\n"); AssetDatabase.ImportAsset(path); _notice = "Exported " + path;
        }

        private void ExportTerrain()
        {
            _document.BuildSystem();
            if ((string)_document.Body["type"] == "star") throw new FormatException("A star has no terrain preset.");
            string path = EditorUtility.SaveFilePanelInProject("Export terrain preset", _document.Id + "_draft", "json", "Selecting an existing preset explicitly replaces that shared preset.", "Assets/TAP/Resources/Data/Terrain");
            if (path.Length == 0) return;
            File.WriteAllText(path, _document.EffectiveBody()["terrain"].ToString() + "\n"); AssetDatabase.ImportAsset(path);
            _notice = "Exported " + path; QueuePreview();
        }

        private void ExportMap()
        {
            if (_preview?.Map == null) throw new InvalidOperationException("Wait for a valid terrain preview first.");
            string path = EditorUtility.SaveFilePanel("Export current map", "Screenshots", _document.Id + "_" + _mode.ToString().ToLowerInvariant(), "png");
            if (path.Length > 0) File.WriteAllBytes(path, _preview.Map.EncodeToPNG());
        }

        private bool CanLeaveDraft()
        {
            if (_document?.Dirty != true) return true;
            int choice = EditorUtility.DisplayDialogComplex("Unsaved body draft", "Save this body to its system before switching?", "Save", "Cancel", "Discard");
            if (choice == 1) return false;
            if (choice == 0) { try { SaveToSystem(); } catch (Exception e) { _error = e.Message; UpdateStatus(); return false; } }
            return true;
        }

        public override void SaveChanges()
        {
            try { SaveToSystem(); base.SaveChanges(); }
            catch (Exception e) { _error = e.Message; UpdateStatus(); throw; }
        }

        public override void DiscardChanges()
        {
            if (_document != null) _document.Changed -= OnDraftChanged;
            _document = null;
            SessionState.EraseString(SessionKey);
            base.DiscardChanges();
        }

        private void Guard(Action action)
        {
            try { action(); _error = null; }
            catch (Exception e) { _error = e.Message; }
            UpdateStatus();
        }
    }
}
