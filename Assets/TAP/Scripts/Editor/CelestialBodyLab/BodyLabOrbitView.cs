using System;
using System.Collections.Generic;
using System.Linq;
using TAP.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.EditorTools
{
    public sealed class BodyLabOrbitFrame
    {
        public sealed class Entry
        {
            public CelestialBody Body;
            public Vector3d Position;
            public Vector3d[] Path;
        }
        public Entry[] Entries;
        public double Extent;
        public CelestialBody Focus;

        public static BodyLabOrbitFrame Sample(CelestialSystem system, string draftId, double ut, bool parentView)
        {
            var draft = system.Get(draftId);
            var focus = parentView ? draft.Parent ?? draft : system.Root;
            var origin = focus.GetPositionAtUT(ut);
            var bodies = parentView ? system.Bodies.Where(b => b == focus || b.Parent == focus) : system.Bodies;
            var entries = new List<Entry>();
            double extent = Math.Max(1, focus.Radius * 2);
            foreach (var body in bodies)
            {
                var position = body.GetPositionAtUT(ut) - origin;
                var points = body.Orbit != null && body != focus ? new Vector3d[129] : Array.Empty<Vector3d>();
                if (points.Length > 0)
                {
                    var parentPosition = body.Parent.GetPositionAtUT(ut) - origin;
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = parentPosition + body.Orbit.PositionAtTrueAnomaly(MathD.TwoPi * i / (points.Length - 1));
                        extent = Math.Max(extent, points[i].magnitude);
                    }
                    points[points.Length - 1] = points[0];
                }
                extent = Math.Max(extent, position.magnitude + body.Radius);
                entries.Add(new Entry { Body = body, Position = position, Path = points });
            }
            return new BodyLabOrbitFrame { Entries = entries.ToArray(), Extent = extent, Focus = focus };
        }
    }

    /// <summary>Double-precision system positions, projected only at the final drawing step.</summary>
    public sealed class BodyLabOrbitView : VisualElement
    {
        private CelestialSystem _system;
        private string _draftId;
        private double _ut, _scale = 1;
        private bool _parentView;
        private float _yaw, _pitch = 90, _zoom = 1;
        private Vector2 _pointer;
        private BodyLabOrbitFrame _frame;
        public event Action<string> Hovered;
        public BodyLabOrbitFrame Frame => _frame;

        public BodyLabOrbitView()
        {
            name = "orbitPreview";
            AddToClassList("body-lab-orbits");
            tooltip = "Drag to tilt the orbital planes; scroll to zoom. Markers use enlarged sizes. Hover a body for orbital details.";
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                _pointer = e.localPosition; this.CapturePointer(e.pointerId); e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (this.HasPointerCapture(e.pointerId))
                {
                    var delta = (Vector2)e.localPosition - _pointer; _pointer = e.localPosition;
                    _yaw += delta.x * 0.5f; _pitch = Mathf.Clamp(_pitch - delta.y * 0.5f, -90, 90); MarkDirtyRepaint();
                }
                else Hover(e.localPosition);
            });
            RegisterCallback<PointerUpEvent>(e => { if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId); });
            RegisterCallback<PointerLeaveEvent>(_ => Hovered?.Invoke("Hover a body for its parent, period and apsides."));
            RegisterCallback<WheelEvent>(e => { _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-e.delta.y * 0.08f), 0.25f, 10000); MarkDirtyRepaint(); e.StopPropagation(); });
        }

        public void SetSystem(CelestialSystem system, string draftId)
        {
            bool changedBody = _draftId != draftId;
            _system = system; _draftId = draftId;
            RefreshFrame();
            if (changedBody) Fit();
        }
        public void SetTime(double ut) { _ut = ut; RefreshFrame(); }
        public void SetParentView(bool parentView) { _parentView = parentView; RefreshFrame(); Fit(); }
        public void Fit()
        {
            _zoom = 1; _yaw = 0; _pitch = 90; _scale = _frame?.Extent ?? 1; MarkDirtyRepaint();
        }
        private void RefreshFrame()
        {
            if (_system != null) _frame = BodyLabOrbitFrame.Sample(_system, _draftId, _ut, _parentView);
            _scale = _frame?.Extent ?? 1;
            MarkDirtyRepaint();
        }
        public Vector2 Project(Vector3d position)
        {
            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            var right = (Vector3d)(rotation * Vector3.right); var up = (Vector3d)(rotation * Vector3.up);
            double pixels = Math.Min(contentRect.width, contentRect.height) * 0.4 * _zoom / Math.Max(1, _scale);
            return contentRect.center + new Vector2((float)(Vector3d.Dot(position, right) * pixels), (float)(-Vector3d.Dot(position, up) * pixels));
        }
        private void Hover(Vector2 pointer)
        {
            var entry = _frame?.Entries.OrderBy(e => (Project(e.Position) - pointer).sqrMagnitude).FirstOrDefault();
            if (entry == null || Vector2.Distance(Project(entry.Position), pointer) > 14) return;
            var b = entry.Body;
            string details = b.Name + (b.Id == _draftId ? " · draft" : "");
            details += b.Parent == null ? " · system root" : $" · orbits {b.Parent.Name} · period {b.Orbit.Period:N0} s\nPeriapsis altitude {b.Orbit.PeriapsisRadius - b.Parent.Radius:N0} m · apoapsis altitude {b.Orbit.ApoapsisRadius - b.Parent.Radius:N0} m";
            Hovered?.Invoke(details);
        }
        private void Draw(MeshGenerationContext context)
        {
            if (_frame == null || contentRect.width < 1 || contentRect.height < 1) return;
            var painter = context.painter2D;
            foreach (var entry in _frame.Entries)
            {
                bool draft = entry.Body.Id == _draftId;
                var rgb = entry.Body.Def.mapColor;
                Color color = draft ? new Color(1, 0.72f, 0.25f) : new Color(rgb[0], rgb[1], rgb[2]);
                painter.strokeColor = new Color(color.r, color.g, color.b, draft ? 1 : 0.65f); painter.lineWidth = draft ? 2 : 1;
                if (entry.Path.Length > 0)
                {
                    painter.BeginPath(); painter.MoveTo(Project(entry.Path[0]));
                    for (int i = 1; i < entry.Path.Length; i++) painter.LineTo(Project(entry.Path[i]));
                    painter.Stroke();
                }
                var point = Project(entry.Position);
                if (!contentRect.Contains(point)) continue;
                painter.fillColor = color; painter.BeginPath();
                painter.Arc(point, draft ? 5 : entry.Body == _frame.Focus ? 6 : 3, Angle.Degrees(0), Angle.Degrees(360)); painter.Fill();
                context.DrawText(entry.Body.Name + (draft ? " (draft)" : ""), point + new Vector2(8, -8), 11, color, null);
            }
        }
    }
}
