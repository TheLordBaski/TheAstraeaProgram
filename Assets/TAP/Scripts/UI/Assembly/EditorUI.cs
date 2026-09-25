using System;
using System.Collections.Generic;
using System.Linq;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>
    /// Assembly building interface: top bar (file, undo, symmetry, snap, launch), part palette with categories
    /// and search, engineer's report, staging stack with drag-and-drop, part menu, load/flights/help dialogs.
    /// </summary>
    public sealed partial class EditorUI : MonoBehaviour
    {
        private AssemblyScene _scene;
        private AssemblyEditor Ed => _scene.Editor;
        private Canvas _canvas;
        private RectTransform _root;

        // top bar
        private TMP_InputField _nameField;
        private UIKit.ButtonRef _undoBtn, _redoBtn, _symBtn, _snapBtn, _markerBtn, _envBtn, _flightsBtn, _launchBtn;
        // palette
        private string _category;
        private string _search = "";
        private RectTransform _paletteContent;
        private readonly Dictionary<string, UIKit.ButtonRef> _catButtons = new Dictionary<string, UIKit.ButtonRef>();
        private readonly List<(PartDefinition def, GameObject tile)> _tiles = new List<(PartDefinition, GameObject)>();
        // hint + toasts
        private TextMeshProUGUI _hint, _toast;
        private float _toastTime;
        private bool _pointerOverUi;

        public static EditorUI Create(AssemblyScene scene)
        {
            var canvas = UIKit.CreateCanvas("EditorUI", 10);
            var ui = canvas.gameObject.AddComponent<EditorUI>();
            ui._scene = scene;
            ui._canvas = canvas;
            ui._root = (RectTransform)canvas.transform;
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
                ui.Toast(GameSession.PendingMessage);
                GameSession.PendingMessage = null;
            }
            else if (scene.Editor.Design.parts.Count == 0)
                ui.Toast("Pick a command pod from the parts list, or press Load to start from a tested rocket");
            return ui;
        }

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
            var bar = UIKit.Panel(_root, "TopBar");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-16, 52);
            rt.anchoredPosition = new Vector2(0, -8);
            var h = UIKit.HLayout(bar, 6, 7);
            h.childAlignment = TextAnchor.MiddleLeft;

            var title = UIKit.Label(bar.transform, "ASSEMBLY", 16, UIKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UIKit.Size(title, 38, 100);
            _nameField = UIKit.Input(bar.transform, Ed.Design.name, s => Ed.Rename(s), 18);
            UIKit.Size(_nameField, 38, 240);
            UIKit.Tooltip(_nameField.gameObject, () => "Craft name (used as the file name when saving)");

            Btn(bar.transform, "New", 62, () => { Ed.NewCraft(); Toast("New craft (Ctrl+Z to undo)"); }, "Start an empty craft (undoable)");
            Btn(bar.transform, "Load", 62, OpenLoadDialog, "Load a starter rocket or a saved craft");
            Btn(bar.transform, "Save", 62, () => Ed.Save(), "Save the craft (Ctrl+S)");
            Spacer(bar.transform, 4);
            _undoBtn = Btn(bar.transform, "Undo", 62, Ed.Undo, "Undo (Ctrl+Z)");
            _redoBtn = Btn(bar.transform, "Redo", 62, Ed.Redo, "Redo (Ctrl+Y)");
            Spacer(bar.transform, 4);
            _symBtn = Btn(bar.transform, "Symmetry ×1", 118, () => Ed.CycleSymmetry(1),
                "Radial symmetry for surface-attached parts (X / Shift+X). Parts attached to a symmetric part are mirrored onto all its copies.");
            _snapBtn = Btn(bar.transform, "Angle snap", 104, Ed.ToggleAngleSnap, "Snap surface attachment to 15° steps and rotations to 90° (C)");
            _markerBtn = Btn(bar.transform, "CoM/CoT/CoP", 112, Ed.ToggleMarkers,
                "Show centre of mass (yellow), centre of thrust (purple) and centre of pressure (cyan) (V). Stable rockets keep the CoP below the CoM.");
            _envBtn = Btn(bar.transform, "Tellus sea level", 140, CycleEnvironment, "Environment used for the delta-v and TWR readouts");
            var flex = UIKit.Rect(bar.transform, "Flex");
            flex.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            _flightsBtn = Btn(bar.transform, "Flights", 90, OpenFlightsDialog, "Vessels already in flight: switch to one");
            Btn(bar.transform, "Help", 62, () => _help.SetActive(!_help.activeSelf), "Assembly controls (F1)");
            Btn(bar.transform, "Menu", 62, () => { Ed.StoreSessionCraft(); GameSession.WritePersistent(); GameSession.GoToMenu(); }, "Save the game and return to the main menu");
            _launchBtn = Btn(bar.transform, "LAUNCH", 120, Launch, "Roll the craft out to the launch pad");
            _launchBtn.Background.color = new Color(0.12f, 0.45f, 0.25f, 1f);
            _launchBtn.Text.fontStyle = FontStyles.Bold;
        }

        private UIKit.ButtonRef Btn(Transform parent, string label, float width, Action onClick, string tip)
        {
            var b = UIKit.Button(parent, label, onClick, 16);
            UIKit.Size(b.Button, 38, width);
            if (tip != null) UIKit.Tooltip(b.Button.gameObject, () => tip);
            return b;
        }

        private static void Spacer(Transform parent, float w)
        {
            var s = UIKit.Rect(parent, "Spacer");
            var le = s.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
        }

        private void CycleEnvironment()
        {
            var next = (EditorEnvironment)(((int)_scene.Environment + 1) % 3);
            _scene.SetEnvironment(next);
        }

        private static string EnvName(EditorEnvironment e) => e == EditorEnvironment.TellusSeaLevel ? "Tellus sea level" : e == EditorEnvironment.TellusVacuum ? "Tellus vacuum" : "Luma surface";

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
            var panel = UIKit.Panel(_root, "Palette");
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(8, 8);
            rt.offsetMax = new Vector2(8 + 348, -68);
            // Clicking the palette background while holding a part discards it (KSP-style "drop on the list").
            var drop = panel.gameObject.AddComponent<ClickHandler>();
            drop.OnClick = () => { if (Ed.IsHolding) Ed.DiscardHeld(); };

            var header = UIKit.Label(panel.transform, "PARTS", 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(header.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(200, 20));

            var grid = UIKit.Rect(panel.transform, "Categories");
            UIKit.Place(grid, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -32), new Vector2(332, 3 * 34 + 8));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(106, 32);
            gl.spacing = new Vector2(4, 4);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            foreach (var (id, label) in Categories)
            {
                string cid = id;
                var b = UIKit.Button(grid, label, () => SetCategory(cid), 15);
                _catButtons[id] = b;
            }

            var search = UIKit.Input(panel.transform, "", null, 16);
            UIKit.Place((RectTransform)search.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -148), new Vector2(332, 32));
            search.onValueChanged.AddListener(s => { _search = s ?? ""; FilterTiles(); });
            var ph = UIKit.Label(search.textViewport, "Search parts…", 15, UIKit.TextDim);
            UIKit.Stretch(ph.rectTransform);
            search.placeholder = ph;

            var scroll = UIKit.ScrollList(panel.transform, out var content, 4);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(8, 8);
            srt.offsetMax = new Vector2(-8, -188);
            // Replace the vertical list layout with a grid of tiles.
            DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
            var tg = content.gameObject.AddComponent<GridLayoutGroup>();
            tg.cellSize = new Vector2(106, 124);
            tg.spacing = new Vector2(4, 4);
            tg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tg.constraintCount = 3;
            tg.padding = new RectOffset(2, 2, 2, 2);
            _paletteContent = content;

            foreach (var def in Ed.Db.Parts)
            {
                if (def.category == "Hidden") continue;
                var d = def;
                var tile = UIKit.Button(content, "", () => { Ed.PickFromPalette(d); }, 12);
                var img = UIKit.Image(tile.Button.transform, PartThumbnails.Get(d), Color.white, "Thumb");
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(84, 84));
                var name = UIKit.Label(tile.Button.transform, d.title, 12, UIKit.TextColor, TextAlignmentOptions.Top);
                name.textWrappingMode = TextWrappingModes.Normal;
                name.overflowMode = TextOverflowModes.Ellipsis;
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(100, 32));
                UIKit.Tooltip(tile.Button.gameObject, () => PartInfoText.Describe(d, Ed.Db));
                _tiles.Add((d, tile.Button.gameObject));
            }
            SetCategory("Command");
        }

        private void SetCategory(string id)
        {
            _category = id;
            foreach (var kv in _catButtons) kv.Value.SetActive(kv.Key == id && string.IsNullOrEmpty(_search));
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
                tile.SetActive(show);
            }
            foreach (var kv in _catButtons) kv.Value.SetActive(kv.Key == _category && q.Length == 0);
        }

        // ------------------------------------------------------------------ bottom hint + toast

        private void BuildBottom()
        {
            var hintPanel = UIKit.Panel(_root, "Hint", new Color(0.03f, 0.04f, 0.06f, 0.75f));
            var rt = hintPanel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(-16, 8);
            rt.sizeDelta = new Vector2(1100, 34);
            hintPanel.raycastTarget = false;
            _hint = UIKit.Label(hintPanel.transform, "", 15, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Stretch(_hint.rectTransform, 10, 10, 2, 2);

            _toast = UIKit.Label(_root, "", 19, UIKit.TextColor, TextAlignmentOptions.Center);
            UIKit.Place(_toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-16, -80), new Vector2(1000, 60));
            _toast.textWrappingMode = TextWrappingModes.Normal;
            var outline = _toast.gameObject.AddComponent<Shadow>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public void Toast(string msg) => Toast(msg, 3.5f);

        public void Toast(string msg, float seconds)
        {
            _toast.text = msg;
            _toastTime = seconds;
            _toast.alpha = 1;
        }

        private const string IdleHint = "LMB part: pick up (the root takes the whole craft) · Alt+LMB: copy · RMB: part menu · Del: delete · RMB drag: orbit · Scroll: zoom · Shift+Scroll: up/down · F: frame";
        private const string HoldHint = "  ·  WASDQE rotate (Shift fine) · Space reset · X symmetry · C angle snap · Esc put back · Del delete";

        // ------------------------------------------------------------------ update

        private void Update()
        {
            var es = EventSystem.current;
            _pointerOverUi = es != null && es.IsPointerOverGameObject();
            if (_toastTime > 0)
            {
                _toastTime -= Time.unscaledDeltaTime;
                _toast.alpha = Mathf.Clamp01(_toastTime / 0.6f);
            }
            _hint.text = Ed.IsHolding ? (Ed.PlacementHint ?? "") + HoldHint : IdleHint;
            var kb = Keyboard.current;
            if (kb != null && !UiState.KeyboardCaptured)
            {
                if (kb.f1Key.wasPressedThisFrame) _help.SetActive(!_help.activeSelf);
                if (kb.escapeKey.wasPressedThisFrame && !Ed.IsHolding) CloseDialogs();
            }
            if (_partMenu.activeSelf) UpdatePartMenuPosition();
        }

        private void OnDesignChanged()
        {
            if (_nameField != null && !_nameField.isFocused) _nameField.SetTextWithoutNotify(Ed.Design.name);
            _undoBtn.SetInteractable(Ed.CanUndo);
            _redoBtn.SetInteractable(Ed.CanRedo);
            _symBtn.Text.text = $"Symmetry ×{Ed.Symmetry}";
            _symBtn.SetActive(Ed.Symmetry > 1);
            _snapBtn.SetActive(Ed.AngleSnap);
            _markerBtn.SetActive(Ed.ShowMarkers);
            int flights = 0;
            if (GameSession.Save != null)
                foreach (var v in GameSession.Save.vessels) if (v.kind == VesselKind.Ship || v.kind == VesselKind.EVA) flights++;
            _flightsBtn.Text.text = flights > 0 ? $"Flights ({flights})" : "Flights";
            _flightsBtn.SetInteractable(flights > 0);
        }

        /// <summary>Invisible click catcher (palette background).</summary>
        public sealed class ClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public Action OnClick;
            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left) OnClick?.Invoke();
            }
        }
    }
}
