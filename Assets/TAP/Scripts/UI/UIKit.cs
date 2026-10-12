using System;
using System.Collections.Generic;
using UnityEngine;
using TAP.Game;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// UI Toolkit kit for the game screens. Every screen is a UIDocument on one shared panel (1920×1080 reference,
    /// scaled like the old canvases), styled by Resources/UI/Theme.uss. The colours below match the theme tokens and
    /// are used where code writes rich text.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.055f, 0.07f, 0.095f, 0.88f);
        public static readonly Color PanelLight = new Color(0.11f, 0.14f, 0.19f, 0.92f);
        public static readonly Color Accent = new Color(0.32f, 0.78f, 1f, 1f);
        public static readonly Color AccentDim = new Color(0.2f, 0.45f, 0.62f, 1f);
        public static readonly Color TextColor = new Color(0.9f, 0.93f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.58f, 0.65f, 0.74f, 1f);
        public static readonly Color Good = new Color(0.4f, 0.9f, 0.5f, 1f);
        public static readonly Color Warn = new Color(1f, 0.75f, 0.25f, 1f);
        public static readonly Color Bad = new Color(1f, 0.35f, 0.3f, 1f);

        /// <summary>Every screen made with <see cref="CreateDocument"/>; the localizer walks their text.</summary>
        public static readonly List<UIDocument> Documents = new List<UIDocument>();

        private static PanelSettings _panelSettings;
        private static IPanel _panel;
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        /// <summary>The panel all game screens share (null until the first document exists).</summary>
        public static IPanel Panel => _panel;

        public static PanelSettings PanelSettings
        {
            get
            {
                if (_panelSettings == null) _panelSettings = CreatePanelSettings();
                return _panelSettings;
            }
        }

        private static PanelSettings CreatePanelSettings()
        {
            // A copy of an editor-made asset: it carries the ICU data text shaping needs in a player (a runtime instance has none).
            var source = Resources.Load<PanelSettings>("UI/GamePanel");
            if (source == null) Debug.LogError("UI panel settings Resources/UI/GamePanel.asset are missing");
            var ps = source != null ? UnityEngine.Object.Instantiate(source) : ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "TAP Game Panel";
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/GameTheme");
            if (ps.themeStyleSheet == null) Debug.LogError("UI theme Resources/UI/GameTheme.tss is missing");
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ApplyScale(ps);
            GameSettings.Changed += () => ApplyScale(ps);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.clearColor = false;
            ps.textSettings = CreateTextSettings();
            return ps;
        }

        /// <summary>The interface scale setting: a larger scale shrinks the reference screen the layouts are drawn on.</summary>
        private static void ApplyScale(PanelSettings ps)
        {
            if (ps == null) return;
            float s = GameSettings.Current.ui.uiScale;
            ps.referenceResolution = new Vector2Int(Mathf.RoundToInt(1920 / s), Mathf.RoundToInt(1080 / s));
        }

        /// <summary>
        /// Liberation Sans lacks symbols such as ✔ ▶ ⚠ ≈ █. A dynamic font asset made from an installed system font
        /// (Segoe UI Symbol on Windows, DejaVu Sans on Linux) is the panel's fallback, so they render.
        /// </summary>
        private static PanelTextSettings CreateTextSettings()
        {
            var ts = ScriptableObject.CreateInstance<PanelTextSettings>();
            ts.name = "TAP Text Settings";
            var fallbacks = new List<FontAsset>();
            foreach (var family in new[] { "Segoe UI Symbol", "DejaVu Sans", "Noto Sans Symbols2", "Segoe UI", "Arial Unicode MS" })
            {
                try
                {
                    var fa = FontAsset.CreateFontAsset(family, "Regular", 90);
                    if (fa == null) continue;
                    fa.name = "TAP Symbols (" + family + ")";
                    fa.hideFlags = HideFlags.DontSave;
                    fallbacks.Add(fa);
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Symbol font {family} unavailable: {e.Message}");
                }
            }
            ts.fallbackFontAssets = fallbacks;
            return ts;
        }

        /// <summary>
        /// Creates a screen: a UIDocument on the shared panel with the UXML from Resources/UI/&lt;uxml&gt;.
        /// Its root lets the pointer through to the world; only panels and controls take clicks.
        /// </summary>
        public static UIDocument CreateDocument(string name, int sortingOrder, string uxml = null, string uss = null)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings;
            doc.sortingOrder = sortingOrder;
            if (uxml != null)
            {
                var tree = Resources.Load<VisualTreeAsset>("UI/" + uxml);
                if (tree == null) Debug.LogError($"UI layout Resources/UI/{uxml}.uxml is missing");
                doc.visualTreeAsset = tree;
            }
            go.SetActive(true);
            Documents.Add(doc);
            var root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.AddToClassList("screen");
            if (uss != null)
            {
                var sheet = Resources.Load<StyleSheet>("UI/" + uss);
                if (sheet == null) Debug.LogError($"UI style Resources/UI/{uss}.uss is missing");
                else root.styleSheets.Add(sheet);
            }
            Prepare(root);
            if (_panel == null || _panel != root.panel) _panel = root.panel;
            return doc;
        }

        /// <summary>
        /// Readies a subtree: our buttons drop Unity's default button style and keyboard focus (Space and Enter must
        /// never press a button: Space stages the rocket), text fields capture the keyboard while focused, and scroll
        /// lists stop the camera from zooming while the pointer is over them. Safe to call again on new content.
        /// </summary>
        public static void Prepare(VisualElement root)
        {
            root.Query<Button>().ForEach(b =>
            {
                b.focusable = false;
                if (b.ClassListContains("btn")) b.RemoveFromClassList(UnityEngine.UIElements.Button.ussClassName);
            });
            root.Query<TextField>().ForEach(CaptureKeyboard);
            root.Query<ScrollView>().ForEach(sv =>
            {
                // Lists scroll vertically only; long rows clip instead of scrolling sideways.
                sv.mode = ScrollViewMode.Vertical;
                sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                BlockCameraScroll(sv);
            });
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------------ elements

        public static Label Label(string text, string classes = null)
        {
            var l = new Label(text) { enableRichText = true, pickingMode = PickingMode.Ignore };
            AddClasses(l, classes);
            return l;
        }

        public static Button Button(string text, Action onClick, string classes = null)
        {
            var b = new Button(onClick) { text = text, focusable = false };
            b.RemoveFromClassList(UnityEngine.UIElements.Button.ussClassName);
            b.AddToClassList("btn");
            AddClasses(b, classes);
            return b;
        }

        public static VisualElement Element(string classes = null, string name = null)
        {
            var e = new VisualElement();
            if (name != null) e.name = name;
            AddClasses(e, classes);
            return e;
        }

        public static Image Image(Sprite sprite, Color? tint = null, string classes = null)
        {
            var img = new Image { sprite = sprite, tintColor = tint ?? Color.white, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            AddClasses(img, classes);
            return img;
        }

        /// <summary>A track with a fill; set it with <see cref="SetBar"/>.</summary>
        public static VisualElement Bar(bool vertical = false, string classes = null)
        {
            var bar = Element("bar" + (vertical ? " bar--vertical" : ""));
            bar.Add(Element("bar__fill"));
            AddClasses(bar, classes);
            return bar;
        }

        public static void SetBar(VisualElement bar, float fraction, Color color)
        {
            var fill = bar.ElementAt(0);
            float pct = Mathf.Clamp01(fraction) * 100f;
            if (bar.ClassListContains("bar--vertical")) fill.style.height = Length.Percent(pct);
            else fill.style.width = Length.Percent(pct);
            fill.style.backgroundColor = color;
        }

        public static TextField Field(string value, string classes = null)
        {
            var f = new TextField { value = value ?? "" };
            f.AddToClassList("field");
            AddClasses(f, classes);
            CaptureKeyboard(f);
            return f;
        }

        public static void AddClasses(VisualElement e, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var c in classes.Split(' ')) if (c.Length > 0) e.AddToClassList(c);
        }

        /// <summary>The nearest ancestor (or the element itself) carrying a USS class.</summary>
        public static VisualElement Ancestor(VisualElement e, string className)
        {
            for (var p = e; p != null; p = p.parent) if (p.ClassListContains(className)) return p;
            return null;
        }

        public static void Show(VisualElement e, bool on)
        {
            // Always assign: an element hidden by a USS class has no inline display yet, and reads as Flex.
            e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool Shown(VisualElement e) =>
            e.style.display.keyword == StyleKeyword.Undefined ? e.resolvedStyle.display != DisplayStyle.None : e.style.display.value != DisplayStyle.None;

        public static void SetOn(VisualElement e, bool on) => e.EnableInClassList("on", on);

        /// <summary>Places an absolutely positioned element at panel coordinates (its own pivot via USS translate).</summary>
        public static void Place(VisualElement e, Vector2 panelPos)
        {
            e.style.left = panelPos.x;
            e.style.top = panelPos.y;
        }

        public static Sprite Icon(string name)
        {
            if (Icons.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Textures/Icons/" + name);
            Icons[name] = s;
            return s;
        }

        // ------------------------------------------------------------------ input

        public static Vector2 MouseScreen => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        /// <summary>Screen pixels (origin bottom left, as the Input System and cameras use) to panel coordinates.</summary>
        public static Vector2 ScreenToPanel(Vector2 screen) =>
            _panel == null ? new Vector2(screen.x, Screen.height - screen.y) : RuntimePanelUtils.ScreenToPanel(_panel, new Vector2(screen.x, Screen.height - screen.y));

        /// <summary>The element under a screen point that takes the pointer, or null over the world.</summary>
        public static VisualElement Pick(Vector2 screen) => _panel?.Pick(ScreenToPanel(screen));

        public static bool PointerOverUi() => Pick(MouseScreen) != null;

        /// <summary>The panel's size in panel units (the 1920×1080 reference stretched to the screen's aspect).</summary>
        public static Vector2 PanelSize(VisualElement anyElement)
        {
            var r = anyElement.panel?.visualTree?.layout ?? new Rect(0, 0, 1920, 1080);
            return r.size;
        }

        /// <summary>While a text field has focus the game ignores the keyboard (flight and editor keys).</summary>
        public static void CaptureKeyboard(TextField f)
        {
            if (f.ClassListContains("kb-capture")) return;
            f.AddToClassList("kb-capture");
            f.RegisterCallback<FocusInEvent>(_ => TAP.Game.UiState.KeyboardCaptured = true);
            f.RegisterCallback<FocusOutEvent>(_ => TAP.Game.UiState.KeyboardCaptured = false);
            f.RegisterCallback<DetachFromPanelEvent>(_ => { if (f.focusController?.focusedElement == f) TAP.Game.UiState.KeyboardCaptured = false; });
            f.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) f.Blur();
            }, TrickleDown.TrickleDown);
        }

        /// <summary>The camera does not zoom while the pointer is over a scroll list.</summary>
        public static void BlockCameraScroll(VisualElement e)
        {
            if (e.ClassListContains("scroll-blocks-camera")) return;
            e.AddToClassList("scroll-blocks-camera");
            e.RegisterCallback<PointerEnterEvent>(_ => TAP.Game.FlightCamera.UiBlocksScroll = true);
            e.RegisterCallback<PointerLeaveEvent>(_ => TAP.Game.FlightCamera.UiBlocksScroll = false);
            e.RegisterCallback<DetachFromPanelEvent>(_ => TAP.Game.FlightCamera.UiBlocksScroll = false);
        }

        public static void Tooltip(VisualElement e, Func<string> text) => TooltipView.Register(e, text);

        // ------------------------------------------------------------------ text

        public static string Fmt(double v, string unit, int decimals = 0)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "--";
            return v.ToString("N" + decimals) + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>Single floating tooltip that follows the pointer, on its own document above every screen.</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        private static TooltipView _instance;
        private VisualElement _box;
        private Label _text;
        private VisualElement _source;
        private Func<string> _func;
        private float _shownAt;

        private static TooltipView Instance
        {
            get
            {
                if (_instance == null)
                {
                    var doc = UIKit.CreateDocument("Tooltip", 500);
                    _instance = doc.gameObject.AddComponent<TooltipView>();
                    _instance._box = UIKit.Element("tooltip panel panel--deep");
                    _instance._box.pickingMode = PickingMode.Ignore;
                    _instance._text = UIKit.Label("", "wrap");
                    _instance._box.Add(_instance._text);
                    UIKit.Show(_instance._box, false);
                    doc.rootVisualElement.Add(_instance._box);
                }
                return _instance;
            }
        }

        public static void Register(VisualElement e, Func<string> text)
        {
            e.RegisterCallback<PointerEnterEvent>(_ => { var t = Instance; t._source = e; t._func = text; t._shownAt = Time.unscaledTime; });
            e.RegisterCallback<PointerLeaveEvent>(_ => { if (_instance != null && _instance._source == e) _instance._source = null; });
            e.RegisterCallback<DetachFromPanelEvent>(_ => { if (_instance != null && _instance._source == e) _instance._source = null; });
        }

        private void Update()
        {
            Vector2 mouse = UIKit.ScreenToPanel(UIKit.MouseScreen);
            // The source must still be on screen under the pointer (hidden or moved elements end their tooltip).
            bool show = GameSettings.Current.gameplay.tooltips && _source != null && _source.panel != null && _func != null && _source.worldBound.Contains(mouse)
                        && Time.unscaledTime - _shownAt > 0.35f;
            string s = show ? _func() : null;
            if (string.IsNullOrEmpty(s)) { UIKit.Show(_box, false); return; }
            if (_text.text != s) _text.text = s;
            UIKit.Show(_box, true);
            Vector2 size = new Vector2(_box.resolvedStyle.width, _box.resolvedStyle.height);
            Vector2 screen = UIKit.PanelSize(_box);
            Vector2 pos = mouse + new Vector2(16, 16);
            if (pos.x + size.x > screen.x - 4) pos.x = screen.x - size.x - 4;
            if (pos.y + size.y > screen.y - 4) pos.y = mouse.y - size.y - 8;
            UIKit.Place(_box, pos);
        }
    }

    /// <summary>
    /// Drag handle for a window (put it on the title): dragging moves the window, keeping its title reachable;
    /// <see cref="Moved"/> reports the new top-left corner when the drag ends.
    /// </summary>
    public sealed class WindowDrag : PointerManipulator
    {
        private readonly VisualElement _window;
        public Action<Vector2> Moved;
        private bool _dragging;
        private Vector2 _start, _origin;

        public WindowDrag(VisualElement window) { _window = window; activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse }); }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove);
            target.UnregisterCallback<PointerUpEvent>(OnUp);
        }

        private void OnDown(PointerDownEvent e)
        {
            if (!CanStartManipulation(e)) return;
            _dragging = true;
            _start = e.position;
            _origin = new Vector2(_window.resolvedStyle.left, _window.resolvedStyle.top);
            target.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnMove(PointerMoveEvent e)
        {
            if (!_dragging || !target.HasPointerCapture(e.pointerId)) return;
            Vector2 p = _origin + ((Vector2)e.position - _start);
            Vector2 screen = UIKit.PanelSize(_window);
            float w = _window.resolvedStyle.width;
            // Keep 80 px of the window inside the screen horizontally and its title on screen.
            p.x = Mathf.Clamp(p.x, 80 - w, screen.x - 80);
            p.y = Mathf.Clamp(p.y, 0, screen.y - 80);
            UIKit.Place(_window, p);
        }

        private void OnUp(PointerUpEvent e)
        {
            if (!_dragging || !CanStopManipulation(e)) return;
            _dragging = false;
            target.ReleasePointer(e.pointerId);
            Moved?.Invoke(new Vector2(_window.resolvedStyle.left, _window.resolvedStyle.top));
            e.StopPropagation();
        }
    }
}
