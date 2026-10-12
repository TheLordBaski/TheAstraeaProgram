using System;
using System.Collections.Generic;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// The flight HUD's panels as the player arranges them: drag a unit by its frame, drag its corner grip to scale it, × hides it,
    /// and the layout key lists them all (show, hide, reset). A position hugs the screen edge or corner the panel was dropped
    /// nearest (<see cref="HudPanelState"/>), so it holds at any aspect and HUD scale. Changes go to the settings file.
    /// </summary>
    public sealed class HudLayout
    {
        public static HudLayout Instance { get; private set; }

        private sealed class Entry
        {
            public string Id, Title;
            public VisualElement El, Grip, Close;
            public HudPanelState Default, Preview;
            public bool Wanted = true, CanHide, Dragging;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        /// <summary>A panel was shown, hidden, moved or reset (the layout menu redraws).</summary>
        public event Action Changed;

        public HudLayout()
        {
            Instance = this;
            GameSettings.Changed += ApplyAll;
        }

        public void Dispose()
        {
            GameSettings.Changed -= ApplyAll;
            if (Instance == this) Instance = null;
        }

        /// <summary>Puts a panel under the layout. <paramref name="el"/> must sit in a full-screen container.</summary>
        public void Add(string id, string title, VisualElement el, HudPanelState def, bool canHide = true)
        {
            var e = new Entry { Id = id, Title = title, El = el, Default = def, CanHide = canHide };
            el.AddToClassList("hud-panel");
            el.style.position = Position.Absolute;
            el.style.transformOrigin = new TransformOrigin(0, 0, 0);
            el.AddManipulator(new MoveDrag(this, e));
            e.Grip = UIKit.Element("hud-grip hud-chrome");
            e.Grip.AddManipulator(new ScaleDrag(this, e));
            UIKit.Tooltip(e.Grip, () => "Drag to resize");
            el.Add(e.Grip);
            if (canHide)
            {
                e.Close = UIKit.Button("×", () => SetHidden(id, true), "hud-close hud-chrome");
                UIKit.Tooltip(e.Close, () => "Hide this panel. The layout key (top right) brings it back.");
                el.Add(e.Close);
            }
            el.RegisterCallback<GeometryChangedEvent>(_ => Apply(e));
            el.parent?.RegisterCallback<GeometryChangedEvent>(_ => Apply(e));
            _entries.Add(e);
            Apply(e);
        }

        /// <summary>The game's own wish to show a panel (a burn strip needs a node); the player's choice to hide it comes on top.</summary>
        public void Want(string id, bool on)
        {
            var e = Find(id);
            if (e == null || e.Wanted == on) return;
            e.Wanted = on;
            Apply(e);
        }

        public bool IsShown(string id)
        {
            var e = Find(id);
            return e != null && e.Wanted && !State(e).hidden;
        }

        public IEnumerable<(string id, string title, bool hidden)> Panels()
        {
            foreach (var e in _entries) if (e.CanHide) yield return (e.Id, e.Title, State(e).hidden);
        }

        public void SetHidden(string id, bool hidden)
        {
            var e = Find(id);
            if (e == null) return;
            var s = State(e).Copy();
            s.hidden = hidden && e.CanHide;
            Save(e, s);
        }

        public void Reset()
        {
            GameSettings.Current.ui.hud.Clear();
            GameSettings.DirtySince = Time.unscaledTime;
            ApplyAll();
            Changed?.Invoke();
        }

        private Entry Find(string id) => _entries.Find(e => e.Id == id);

        private static HudPanelState State(Entry e) =>
            e.Preview ?? (GameSettings.Current.ui.hud.TryGetValue(e.Id, out var s) && s != null ? s : e.Default);

        private void Save(Entry e, HudPanelState s)
        {
            GameSettings.Current.ui.hud[e.Id] = s;
            GameSettings.DirtySince = Time.unscaledTime; // written by SettingsApply shortly; nothing else needs re-applying
            Apply(e);
            Changed?.Invoke();
        }

        private void ApplyAll()
        {
            foreach (var e in _entries) Apply(e);
        }

        private static float Scale(HudPanelState s) => GameSettings.Current.ui.hudScale * s.scale;

        private static void Apply(Entry e)
        {
            var s = State(e);
            bool show = e.Wanted && !s.hidden;
            if (UIKit.Shown(e.El) != show) UIKit.Show(e.El, show);
            if (!show || e.Dragging || e.El.parent == null) return;
            var size = e.El.layout.size;
            var screen = e.El.parent.layout.size;
            if (float.IsNaN(size.x) || size.x < 1 || float.IsNaN(screen.x) || screen.x < 1) return;
            float k = Scale(s);
            var tl = s.TopLeft(screen, size * k);
            if (Mathf.Abs(e.El.resolvedStyle.left - tl.x) > 0.5f) e.El.style.left = tl.x;
            if (Mathf.Abs(e.El.resolvedStyle.top - tl.y) > 0.5f) e.El.style.top = tl.y;
            if (Mathf.Abs(e.El.resolvedStyle.scale.value.x - k) > 1e-3f) e.El.style.scale = new Scale(new Vector3(k, k, 1));
            // The grip sits in the corner away from the anchored one, so dragging it grows the panel into the screen.
            bool gripLeft = s.ax == 2, gripTop = s.ay == 2;
            e.Grip.EnableInClassList("hud-grip--left", gripLeft);
            e.Grip.EnableInClassList("hud-grip--top", gripTop);
            e.Close?.EnableInClassList("hud-close--left", gripTop && !gripLeft);
        }

        /// <summary>Drags a panel by any part of it that is not a control.</summary>
        private sealed class MoveDrag : PointerManipulator
        {
            private readonly HudLayout _layout;
            private readonly Entry _e;
            private bool _down, _moving;
            private Vector2 _start, _origin;

            public MoveDrag(HudLayout layout, Entry e) { _layout = layout; _e = e; activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse }); }

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

            private bool OnControl(IEventHandler t)
            {
                for (var v = t as VisualElement; v != null && v != target; v = v.parent)
                    if (v is Button || v is Scroller || v is TextField || v.ClassListContains("hud-grip")) return true;
                return false;
            }

            private void OnDown(PointerDownEvent e)
            {
                if (!CanStartManipulation(e) || OnControl(e.target)) return;
                _down = true;
                _moving = false;
                _start = e.position;
                _origin = new Vector2(target.resolvedStyle.left, target.resolvedStyle.top);
                target.CapturePointer(e.pointerId);
            }

            private void OnMove(PointerMoveEvent e)
            {
                if (!_down || !target.HasPointerCapture(e.pointerId)) return;
                Vector2 d = (Vector2)e.position - _start;
                if (!_moving && d.magnitude < 4) return;
                _moving = true;
                _e.Dragging = true;
                var (tl, _, _) = Clamped(_origin + d);
                target.style.left = tl.x;
                target.style.top = tl.y;
            }

            private void OnUp(PointerUpEvent e)
            {
                if (!_down || !CanStopManipulation(e)) return;
                _down = false;
                target.ReleasePointer(e.pointerId);
                if (!_moving) return;
                _moving = false;
                _e.Dragging = false;
                var (tl, vis, screen) = Clamped(new Vector2(target.resolvedStyle.left, target.resolvedStyle.top));
                var s = HudPanelState.At(tl, vis, screen, State(_e).scale);
                _layout.Save(_e, s);
                e.StopPropagation();
            }

            private (Vector2 tl, Vector2 vis, Vector2 screen) Clamped(Vector2 p)
            {
                Vector2 screen = target.parent.layout.size, vis = target.layout.size * Scale(State(_e));
                p.x = Mathf.Clamp(p.x, 0, Mathf.Max(0, screen.x - vis.x));
                p.y = Mathf.Clamp(p.y, 0, Mathf.Max(0, screen.y - vis.y));
                return (p, vis, screen);
            }
        }

        /// <summary>Scales a panel about its anchored corner by dragging the grip in the opposite corner.</summary>
        private sealed class ScaleDrag : PointerManipulator
        {
            private readonly HudLayout _layout;
            private readonly Entry _e;
            private bool _down;
            private Vector2 _fixed;
            private float _d0, _s0;

            public ScaleDrag(HudLayout layout, Entry e) { _layout = layout; _e = e; activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse }); }

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
                var r = _e.El.worldBound;
                var s = State(_e);
                // the corner that stays put: the anchored one (the grip's opposite)
                _fixed = new Vector2(s.ax == 2 ? r.xMax : r.xMin, s.ay == 2 ? r.yMax : r.yMin);
                _d0 = Mathf.Max(20f, ((Vector2)e.position - _fixed).magnitude);
                _s0 = s.scale;
                _down = true;
                target.CapturePointer(e.pointerId);
                e.StopPropagation();
            }

            private void OnMove(PointerMoveEvent e)
            {
                if (!_down || !target.HasPointerCapture(e.pointerId)) return;
                var p = State(_e).Copy();
                p.scale = Mathf.Clamp(_s0 * ((Vector2)e.position - _fixed).magnitude / _d0, 0.5f, 2f);
                _e.Preview = p;
                Apply(_e);
            }

            private void OnUp(PointerUpEvent e)
            {
                if (!_down || !CanStopManipulation(e)) return;
                _down = false;
                target.ReleasePointer(e.pointerId);
                var p = _e.Preview;
                _e.Preview = null;
                if (p != null) _layout.Save(_e, p);
                e.StopPropagation();
            }
        }
    }
}
