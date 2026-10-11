using System;
using System.Collections.Generic;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Assembly building interface: top bar (file, undo, symmetry, snap, launch), part palette with categories
    /// and search, engineer's report, staging stack with drag-and-drop, part menu, load/flights/help dialogs.
    /// Layout: Resources/UI/EditorUI.uxml + .uss.
    /// </summary>
    public sealed partial class EditorUI : MonoBehaviour
    {
        private AssemblyScene _scene;
        private AssemblyEditor Ed => _scene.Editor;
        private VisualElement _root;

        // top bar
        private TextField _nameField;
        private Button _undoBtn, _redoBtn, _symBtn, _snapBtn, _markerBtn, _envBtn, _flightsBtn, _launchBtn;
        // palette
        private string _category;
        private string _search = "";
        private readonly Dictionary<string, Button> _catButtons = new Dictionary<string, Button>();
        private readonly List<(PartDefinition def, VisualElement tile)> _tiles = new List<(PartDefinition, VisualElement)>();
        // hint + toasts
        private Label _hint, _toast;
        private float _toastTime;
        private bool _pointerOverUi;

        public static EditorUI Create(AssemblyScene scene)
        {
            var doc = UIKit.CreateDocument("EditorUI", 10, "EditorUI");
            var ui = doc.gameObject.AddComponent<EditorUI>();
            ui._scene = scene;
            ui._root = doc.rootVisualElement;
            ui.Build();
            scene.Editor.PointerOverUi = () => ui._pointerOverUi;
            scene.CameraRig.PointerOverUi = () => ui._pointerOverUi;
            scene.Editor.Message += ui.Toast;
            scene.Editor.DesignChanged += ui.OnDesignChanged;
            scene.Editor.PartContextRequested += ui.OpenPartMenu;
            scene.StatsUpdated += ui.OnStatsUpdated;
            ui.OnDesignChanged();
            if (!string.IsNullOrEmpty(GameSession.PendingMessage))
            {
                // What happened on the way here (a recovery, a rocket that couldn't be loaded): long enough to read.
                ui.Toast(GameSession.PendingMessage, 8f);
                GameSession.PendingMessage = null;
            }
            else if (scene.Editor.Design.parts.Count == 0)
                ui.Toast("Pick a command pod from the parts list, or press Load to start from a tested rocket");
            return ui;
        }

        private T Q<T>(string name) where T : VisualElement => _root.Q<T>(name);

        private void Build()
        {
            BuildTopBar();
            BuildPalette();
            BuildRightPanel();
            BuildBottom();
            BuildDialogs();
        }

        // ------------------------------------------------------------------ top bar

        private void BuildTopBar()
        {
            _nameField = Q<TextField>("craft-name");
            _nameField.value = Ed.Design.name;
            _nameField.isDelayed = true;
            _nameField.RegisterValueChangedCallback(e => Ed.Rename(e.newValue));
            UIKit.Tooltip(_nameField, () => "Craft name (used as the file name when saving)");

            Btn("new", () => { Ed.NewCraft(); Toast("New craft (Ctrl+Z to undo)"); }, "Start an empty craft (undoable)");
            Btn("load", OpenLoadDialog, "Load a starter rocket or a saved craft");
            Btn("save", () => Ed.Save(), "Save the craft (Ctrl+S)");
            _undoBtn = Btn("undo", Ed.Undo, "Undo (Ctrl+Z)");
            _redoBtn = Btn("redo", Ed.Redo, "Redo (Ctrl+Y)");
            _symBtn = Btn("symmetry", () => Ed.CycleSymmetry(1),
                "Radial symmetry for surface-attached parts (X / Shift+X). Parts attached to a symmetric part are mirrored onto all its copies.");
            _snapBtn = Btn("snap", Ed.ToggleAngleSnap, "Snap surface attachment to 15° steps and rotations to 90° (C)");
            _markerBtn = Btn("markers", Ed.ToggleMarkers,
                "Show centre of mass (yellow), centre of thrust (purple) and centre of pressure (cyan) (V). Stable rockets keep the CoP below the CoM.");
            _envBtn = Btn("env", CycleEnvironment, "Environment used for the delta-v and TWR readouts");
            _envBtn.text = EnvName(EditorEnvironment.HomeSeaLevel);
            _flightsBtn = Btn("flights", OpenFlightsDialog, "Vessels already in flight: switch to one");
            Btn("help", ToggleHelp, "Assembly controls");
            Btn("hub", () => { Ed.StoreSessionCraft(); GameSession.GoToHub(); }, "Save the game and return to the space center");
            Btn("menu", () => { Ed.StoreSessionCraft(); GameSession.WritePersistent(); GameSession.GoToMenu(); }, "Save the game and return to the main menu");
            _launchBtn = Btn("launch", Launch, "Roll the craft out to the launch pad");
        }

        private Button Btn(string name, Action onClick, string tip)
        {
            var b = Q<Button>(name);
            b.clicked += onClick;
            if (tip != null) UIKit.Tooltip(b, () => tip);
            return b;
        }

        private void CycleEnvironment()
        {
            var next = (EditorEnvironment)(((int)_scene.Environment + 1) % 3);
            _scene.SetEnvironment(next);
        }

        private static string EnvName(EditorEnvironment e) => DesignAnalysis.EnvironmentName(e);

        private void Launch()
        {
            string err = _scene.TryLaunch();
            if (err != null) Toast("<color=#" + UIKit.Hex(UIKit.Bad) + ">Can't launch:</color> " + err, 5f);
        }

        // ------------------------------------------------------------------ palette

        private static readonly (string id, string label)[] Categories =
        {
            ("Command", "Command"), ("Propellant", "Tanks"), ("Engines", "Engines"), ("Boosters", "Boosters"),
            ("Coupling", "Coupling"), ("Recovery", "Recovery"), ("Ground", "Landing"), ("Utility", "Utility"), ("Aero", "Aero"),
        };

        private void BuildPalette()
        {
            var panel = Q<VisualElement>("palette");
            // Clicking the palette background while holding a part discards it (KSP-style "drop on the list").
            panel.RegisterCallback<ClickEvent>(e =>
            {
                if (e.button == 0 && Ed.IsHolding && UIKit.Ancestor(e.target as VisualElement, "btn") == null
                    && UIKit.Ancestor(e.target as VisualElement, "field") == null) Ed.DiscardHeld();
            });

            var grid = Q<VisualElement>("categories");
            foreach (var (id, label) in Categories)
            {
                string cid = id;
                var b = UIKit.Button(label, () => SetCategory(cid), "category");
                grid.Add(b);
                _catButtons[id] = b;
            }

            var search = Q<TextField>("search");
            search.textEdition.placeholder = "Search parts…";
            search.RegisterValueChangedCallback(e => { _search = e.newValue ?? ""; FilterTiles(); });

            var content = Q<ScrollView>("tiles");
            foreach (var def in Ed.Db.Parts)
            {
                if (def.category == "Hidden" || !ModeRules.Current.PartAvailable(def.id)) continue;
                var d = def;
                var tile = UIKit.Button("", () => Ed.PickFromPalette(d), "tile");
                tile.Add(UIKit.Image(PartThumbnails.Get(d), Color.white, "tile__thumb"));
                tile.Add(UIKit.Label(d.title, "tile__name"));
                UIKit.Tooltip(tile, () => PartInfoText.Describe(d, Ed.Db));
                content.Add(tile);
                _tiles.Add((d, tile));
            }
            SetCategory("Command");
        }

        private void SetCategory(string id)
        {
            _category = id;
            FilterTiles();
        }

        private void FilterTiles()
        {
            string q = _search.Trim().ToLowerInvariant();
            foreach (var (def, tile) in _tiles)
            {
                bool show = q.Length > 0
                    ? def.title.ToLowerInvariant().Contains(q) || (def.description ?? "").ToLowerInvariant().Contains(q) || def.category.ToLowerInvariant().Contains(q)
                    : def.category == _category;
                UIKit.Show(tile, show);
            }
            foreach (var kv in _catButtons) UIKit.SetOn(kv.Value, kv.Key == _category && q.Length == 0);
        }

        // ------------------------------------------------------------------ bottom hint + toast

        private void BuildBottom()
        {
            _hint = Q<Label>("hint");
            _toast = Q<Label>("toast");
        }

        public void Toast(string msg) => Toast(msg, 3.5f);

        public void Toast(string msg, float seconds)
        {
            _toast.text = msg;
            _toastTime = seconds;
            _toast.style.opacity = 1;
        }

        private const string IdleHint = "LMB part: pick up (the root moves the whole craft) · Alt+LMB: copy · RMB: part menu · Del: delete · RMB drag: orbit · Scroll: zoom · Shift+Scroll: up/down · F: frame";
        private const string HoldHint = "  ·  WASDQE rotate (Shift fine) · Space reset · X symmetry · C angle snap · Esc put back · Del delete";

        // ------------------------------------------------------------------ update

        private void Update()
        {
            _pointerOverUi = UIKit.PointerOverUi();
            if (_toastTime > 0)
            {
                // At most a tenth of a second a frame: the first frame after loading the scene lasts seconds, and a message
                // shown on arrival would be gone before it was drawn.
                _toastTime -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                _toast.style.opacity = Mathf.Clamp01(_toastTime / 0.6f);
            }
            _hint.text = Ed.IsHolding ? (Ed.PlacementHint ?? "") + HoldHint : IdleHint;
            if (!UiState.KeyboardCaptured)
            {
                if (Controls.Down("Common/Help")) ToggleHelp();
                if (Controls.Down("Common/Cancel") && !Ed.IsHolding) CloseDialogs();
            }
            if (UIKit.Shown(_partMenu)) UpdatePartMenuPosition();
        }

        private void OnDesignChanged()
        {
            if (_nameField != null && (_nameField.focusController == null || _nameField.focusController.focusedElement != _nameField))
                _nameField.SetValueWithoutNotify(Ed.Design.name);
            _undoBtn.SetEnabled(Ed.CanUndo);
            _redoBtn.SetEnabled(Ed.CanRedo);
            _symBtn.text = $"Symmetry ×{Ed.Symmetry}";
            UIKit.SetOn(_symBtn, Ed.Symmetry > 1);
            UIKit.SetOn(_snapBtn, Ed.AngleSnap);
            UIKit.SetOn(_markerBtn, Ed.ShowMarkers);
            int flights = 0;
            if (GameSession.Save != null)
                foreach (var v in GameSession.Save.vessels) if (v.kind == VesselKind.Ship || v.kind == VesselKind.EVA) flights++;
            _flightsBtn.text = flights > 0 ? $"Flights ({flights})" : "Flights";
            _flightsBtn.SetEnabled(flights > 0);
        }
    }
}
