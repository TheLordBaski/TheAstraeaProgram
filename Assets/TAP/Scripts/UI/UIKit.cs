using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>Code-built uGUI toolkit with a consistent dark "mission control" style.</summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.055f, 0.07f, 0.095f, 0.88f);
        public static readonly Color PanelLight = new Color(0.11f, 0.14f, 0.19f, 0.92f);
        public static readonly Color Border = new Color(0.35f, 0.55f, 0.75f, 0.35f);
        public static readonly Color Accent = new Color(0.32f, 0.78f, 1f, 1f);
        public static readonly Color AccentDim = new Color(0.2f, 0.45f, 0.62f, 1f);
        public static readonly Color TextColor = new Color(0.9f, 0.93f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.58f, 0.65f, 0.74f, 1f);
        public static readonly Color Good = new Color(0.4f, 0.9f, 0.5f, 1f);
        public static readonly Color Warn = new Color(1f, 0.75f, 0.25f, 1f);
        public static readonly Color Bad = new Color(1f, 0.35f, 0.3f, 1f);
        public static readonly Color ButtonColor = new Color(0.14f, 0.19f, 0.26f, 0.95f);
        public static readonly Color ButtonHover = new Color(0.2f, 0.3f, 0.42f, 1f);
        public static readonly Color ButtonActive = new Color(0.16f, 0.45f, 0.62f, 1f);

        private static TMP_FontAsset _font;
        private static Sprite _panel, _outline, _circle;
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (_font == null) _font = TMP_Settings.defaultFontAsset;
                    AddSymbolFallback(_font);
                }
                return _font;
            }
        }

        private static bool _symbolFallbackTried;
        private const string RuntimeSymbolFontName = "TAP Symbols (runtime)";

        /// <summary>
        /// The bundled SDF font lacks symbols such as ✔ ▶ ⚠ ≈. A dynamic font asset built from an installed
        /// system font (Segoe UI Symbol on Windows) is added as a runtime fallback so they render.
        /// </summary>
        private static void AddSymbolFallback(TMP_FontAsset font)
        {
            if (_symbolFallbackTried || font == null || !Application.isPlaying) return;
            _symbolFallbackTried = true;
            try
            {
                // In the editor the font asset outlives play sessions: drop fallbacks created by earlier sessions
                // (their runtime materials are gone even if the asset object survives).
                font.fallbackFontAssetTable?.RemoveAll(f => f == null || f.material == null || f.name == RuntimeSymbolFontName);
                foreach (var family in new[] { "Segoe UI Symbol", "Segoe UI", "Arial Unicode MS", "DejaVu Sans" })
                {
                    var fa = TMP_FontAsset.CreateFontAsset(family, "Regular");
                    if (fa == null) continue;
                    fa.name = RuntimeSymbolFontName;
                    fa.hideFlags = HideFlags.DontSave;
                    if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    font.fallbackFontAssetTable.Add(fa);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Symbol font fallback unavailable: " + e.Message);
            }
        }

        public static Sprite PanelSprite => _panel != null ? _panel : (_panel = Resources.Load<Sprite>("Textures/UI/panel"));
        public static Sprite OutlineSprite => _outline != null ? _outline : (_outline = Resources.Load<Sprite>("Textures/UI/panel_outline"));
        public static Sprite CircleSprite => _circle != null ? _circle : (_circle = Resources.Load<Sprite>("Textures/UI/circle"));

        public static Sprite Icon(string name)
        {
            if (Icons.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Textures/Icons/" + name);
            Icons[name] = s;
            return s;
        }

        // ------------------------------------------------------------------ roots

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------------ layout primitives

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect at a normalized anchor point with a pivot, offset and size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color? color = null, bool outline = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PanelSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = color ?? PanelColor;
            img.raycastTarget = true;
            if (outline && OutlineSprite != null)
            {
                var o = Rect(rt, "Outline");
                Stretch(o);
                o.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // decoration, not a row of a layout group
                var oi = o.gameObject.AddComponent<Image>();
                oi.sprite = OutlineSprite;
                oi.type = UnityEngine.UI.Image.Type.Sliced;
                oi.color = Border;
                oi.raycastTarget = false;
            }
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size = 18, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect(parent, "Label");
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? TextColor;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            return t;
        }

        public static Image Image(Transform parent, Sprite sprite, Color? color = null, string name = "Image")
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        public sealed class ButtonRef
        {
            public Button Button;
            public Image Background;
            public TextMeshProUGUI Text;
            public Image Icon;
            public RectTransform Rect => (RectTransform)Button.transform;
            public bool Active;
            public void SetActive(bool on)
            {
                Active = on;
                Background.color = on ? ButtonActive : ButtonColor;
                var cb = Button.colors;
                cb.normalColor = Color.white;
                Button.colors = cb;
            }
            public void SetInteractable(bool on) { Button.interactable = on; if (Text != null) Text.color = on ? TextColor : TextDim; }
        }

        public static ButtonRef Button(Transform parent, string label, Action onClick, float fontSize = 17, Sprite icon = null)
        {
            var rt = Rect(parent, "Button " + label);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = PanelSprite;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = ButtonColor;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.4f, 1f);
            colors.pressedColor = new Color(0.8f, 0.9f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.colorMultiplier = 1.4f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var r = new ButtonRef { Button = btn, Background = bg };
            if (icon != null)
            {
                r.Icon = Image(rt, icon, TextColor, "Icon");
                Stretch(r.Icon.rectTransform, 4, 4, 4, 4);
            }
            if (!string.IsNullOrEmpty(label))
            {
                r.Text = Label(rt, label, fontSize, TextColor, TextAlignmentOptions.Center);
                Stretch(r.Text.rectTransform, 4, 4, 2, 2);
            }
            // Keep keyboard focus out of buttons so Space/Enter never re-trigger them.
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            return r;
        }

        public sealed class BarRef
        {
            public Image Back, Fill;
            public TextMeshProUGUI Text;
            public void Set(float fraction, Color color, string text = null)
            {
                Fill.fillAmount = Mathf.Clamp01(fraction);
                Fill.color = color;
                if (Text != null && text != null) Text.text = text;
            }
        }

        public static BarRef Bar(Transform parent, bool vertical = false, bool withText = false, float textSize = 13)
        {
            var rt = Rect(parent, "Bar");
            var back = rt.gameObject.AddComponent<Image>();
            back.sprite = PanelSprite;
            back.type = UnityEngine.UI.Image.Type.Sliced;
            back.color = new Color(0, 0, 0, 0.55f);
            back.raycastTarget = false;
            var f = Rect(rt, "Fill");
            Stretch(f, 2, 2, 2, 2);
            var fill = f.gameObject.AddComponent<Image>();
            fill.sprite = Texture2DSprite.White;
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = vertical ? UnityEngine.UI.Image.FillMethod.Vertical : UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;
            var r = new BarRef { Back = back, Fill = fill };
            if (withText)
            {
                r.Text = Label(rt, "", textSize, TextColor, TextAlignmentOptions.Center);
                Stretch(r.Text.rectTransform, 2, 2, 0, 0);
            }
            return r;
        }

        public static TMP_InputField Input(Transform parent, string initial, Action<string> onEnd, float size = 17)
        {
            var rt = Rect(parent, "Input");
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = PanelSprite;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);
            var area = Rect(rt, "Text Area");
            Stretch(area, 8, 8, 2, 2);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Label(area, "", size, TextColor, TextAlignmentOptions.Left);
            Stretch(text.rectTransform);
            var field = rt.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.fontAsset = Font;
            field.pointSize = size;
            field.text = initial;
            field.onSelect.AddListener(_ => TAP.Game.UiState.KeyboardCaptured = true);
            field.onDeselect.AddListener(_ => TAP.Game.UiState.KeyboardCaptured = false);
            field.onEndEdit.AddListener(s => { TAP.Game.UiState.KeyboardCaptured = false; onEnd?.Invoke(s); });
            return field;
        }

        public static ScrollRect ScrollList(Transform parent, out RectTransform content, float spacing = 4)
        {
            var rt = Rect(parent, "Scroll");
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28;
            var vp = Rect(rt, "Viewport");
            Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            var vpImg = vp.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);
            content = Rect(vp, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 0);
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing;
            vl.childControlHeight = true;
            vl.childControlWidth = true;
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth = true;
            vl.padding = new RectOffset(2, 2, 2, 2);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp;
            sr.content = content;
            rt.gameObject.AddComponent<ScrollBlocker>();
            return sr;
        }

        public static LayoutElement Size(Component c, float height, float width = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }

        public static VerticalLayoutGroup VLayout(Component c, float spacing = 4, int pad = 8)
        {
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(Component c, float spacing = 4, int pad = 4)
        {
            var h = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;
            return h;
        }

        public static void Tooltip(GameObject go, Func<string> text)
        {
            var t = go.GetComponent<TooltipTrigger>();
            if (t == null) t = go.AddComponent<TooltipTrigger>();
            t.Text = text;
        }

        public static string Fmt(double v, string unit, int decimals = 0)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "--";
            return v.ToString("N" + decimals) + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>Lazily created 4x4 white sprite (for fills).</summary>
    public static class Texture2DSprite
    {
        private static Sprite _white;
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var t = new Texture2D(4, 4);
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    t.SetPixels(px);
                    t.Apply();
                    _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                }
                return _white;
            }
        }
    }

    /// <summary>Prevents camera zoom while scrolling a UI list.</summary>
    public sealed class ScrollBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData e) { TAP.Game.FlightCamera.UiBlocksScroll = true; }
        public void OnPointerExit(PointerEventData e) { TAP.Game.FlightCamera.UiBlocksScroll = false; }
        private void OnDisable() { TAP.Game.FlightCamera.UiBlocksScroll = false; }
    }

    /// <summary>
    /// Drag handle for a window (put it on the title): dragging moves <see cref="Target"/>, keeping at least part of it
    /// on screen; <see cref="Moved"/> reports the new anchored position when the drag ends.
    /// </summary>
    public sealed class DragWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Target;
        public Action<Vector2> Moved;
        private Canvas _canvas;
        private readonly Vector3[] _corners = new Vector3[4];

        public void OnBeginDrag(PointerEventData e) { _canvas = GetComponentInParent<Canvas>(); }

        public void OnDrag(PointerEventData e)
        {
            if (Target == null) return;
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            Target.anchoredPosition += e.delta / scale;
            // Keep the title reachable: 80 px of the window inside the screen horizontally, its top edge on screen.
            Target.GetWorldCorners(_corners);
            float keep = 80f * scale;
            Vector2 shift = Vector2.zero;
            if (_corners[2].x < keep) shift.x = keep - _corners[2].x;
            else if (_corners[0].x > Screen.width - keep) shift.x = Screen.width - keep - _corners[0].x;
            if (_corners[1].y > Screen.height) shift.y = Screen.height - _corners[1].y;
            else if (_corners[1].y < keep) shift.y = keep - _corners[1].y;
            Target.anchoredPosition += shift / scale;
        }

        public void OnEndDrag(PointerEventData e) { if (Target != null) Moved?.Invoke(Target.anchoredPosition); }
    }

    /// <summary>Hover tooltip source.</summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Func<string> Text;
        public void OnPointerEnter(PointerEventData e) { TooltipView.Show(this); }
        public void OnPointerExit(PointerEventData e) { TooltipView.Hide(this); }
        private void OnDisable() { TooltipView.Hide(this); }
    }

    /// <summary>Single floating tooltip that follows the mouse.</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        private static TooltipView _instance;
        private static TooltipTrigger _current;
        private RectTransform _rt;
        private TextMeshProUGUI _text;
        private Canvas _canvas;
        private float _shownAt;

        public static void Ensure()
        {
            if (_instance != null) return;
            var canvas = UIKit.CreateCanvas("TooltipCanvas", 500);
            _instance = canvas.gameObject.AddComponent<TooltipView>();
            _instance._canvas = canvas;
            var panel = UIKit.Panel(canvas.transform, "Tooltip", new Color(0.03f, 0.04f, 0.06f, 0.96f));
            panel.raycastTarget = false;
            _instance._rt = panel.rectTransform;
            _instance._rt.pivot = new Vector2(0, 1);
            _instance._text = UIKit.Label(panel.transform, "", 15, UIKit.TextColor);
            _instance._text.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Stretch(_instance._text.rectTransform, 10, 10, 8, 8);
            panel.gameObject.SetActive(false);
        }

        public static void Show(TooltipTrigger t) { Ensure(); _current = t; _instance._shownAt = Time.unscaledTime; }
        public static void Hide(TooltipTrigger t) { if (_current == t) _current = null; }

        private void Update()
        {
            bool show = _current != null && _current.isActiveAndEnabled && _current.Text != null && Time.unscaledTime - _shownAt > 0.35f;
            if (!show) { if (_rt.gameObject.activeSelf) _rt.gameObject.SetActive(false); return; }
            string s = _current.Text();
            if (string.IsNullOrEmpty(s)) { _rt.gameObject.SetActive(false); return; }
            if (!_rt.gameObject.activeSelf) _rt.gameObject.SetActive(true);
            _text.text = s;
            float scale = _canvas.scaleFactor;
            var pref = _text.GetPreferredValues(s, 380, 0);
            float w = Mathf.Min(400, pref.x + 22), h = _text.GetPreferredValues(s, w - 20, 0).y + 18;
            _rt.sizeDelta = new Vector2(w, h);
            Vector2 mp = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
            Vector2 pos = mp / scale + new Vector2(16, -16);
            Vector2 screen = new Vector2(Screen.width, Screen.height) / scale;
            if (pos.x + w > screen.x) pos.x = screen.x - w - 4;
            if (pos.y - h < 0) pos.y = h + 4;
            _rt.anchorMin = _rt.anchorMax = Vector2.zero;
            _rt.anchoredPosition = pos;
        }
    }
}
