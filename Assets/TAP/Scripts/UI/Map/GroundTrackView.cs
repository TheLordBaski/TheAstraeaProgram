using System.Collections.Generic;
using TAP.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// The map's ground-track picture: the body's flat surface map (longitude −180° to 180° left to right, north on top), a
    /// 30° grid, the track of an orbit as lines, and icons for the vessel and its next apsides. The pointer reads latitude
    /// and longitude off it.
    /// </summary>
    public sealed class GroundTrackView : VisualElement
    {
        public const float Width = 520, Height = 260;
        private static readonly Color Track = new Color32(255, 214, 64, 255);
        private static readonly Color Grid = new Color(1, 1, 1, 0.14f), Axis = new Color(1, 1, 1, 0.3f);

        public readonly Image Vessel, Apoapsis, Periapsis;
        private readonly Image _map;
        private readonly VisualElement _lines;
        private List<GroundPoint> _track;
        private Vector2? _pointer; // latitude, longitude under the pointer

        public GroundTrackView()
        {
            style.width = Width;
            style.height = Height;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = (Color)new Color32(4, 6, 7, 255);
            pickingMode = PickingMode.Position;
            // The surface texture has geographic north in its first row, which a texture shows at the bottom.
            _map = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill, tintColor = new Color(0.8f, 0.8f, 0.8f) };
            _map.style.position = Position.Absolute;
            _map.style.left = _map.style.top = _map.style.right = _map.style.bottom = 0;
            _map.style.scale = new Scale(new Vector3(1, -1, 1));
            Add(_map);
            _lines = new VisualElement { pickingMode = PickingMode.Ignore };
            _lines.style.position = Position.Absolute;
            _lines.style.left = _lines.style.top = _lines.style.right = _lines.style.bottom = 0;
            _lines.generateVisualContent += Draw;
            Add(_lines);
            Vessel = Mark("map_vessel", UIKit.Good, 22);
            Apoapsis = Mark("map_ap", UIKit.TextColor, 16);
            Periapsis = Mark("map_pe", UIKit.TextColor, 16);
            RegisterCallback<PointerMoveEvent>(e =>
            {
                var s = layout.size;
                _pointer = new Vector2(90f - e.localPosition.y / s.y * 180f, e.localPosition.x / s.x * 360f - 180f);
            });
            RegisterCallback<PointerLeaveEvent>(_ => _pointer = null);
        }

        private Image Mark(string icon, Color tint, float size)
        {
            var img = UIKit.Image(UIKit.Icon(icon), tint, "gt-mark");
            img.style.width = img.style.height = size;
            UIKit.Show(img, false);
            Add(img);
            return img;
        }

        /// <summary>The body's surface map; null draws the grid on plain ground.</summary>
        public void SetMap(Texture texture)
        {
            _map.image = texture;
            UIKit.Show(_map, texture != null);
        }

        /// <summary>The lines to draw (the list is kept, not copied); null shows none.</summary>
        public void SetTrack(List<GroundPoint> track)
        {
            _track = track;
            _lines.MarkDirtyRepaint();
        }

        /// <summary>Puts an icon at a position, or hides it when <paramref name="on"/> is false.</summary>
        public static void Place(Image icon, bool on, double lat, double lon)
        {
            UIKit.Show(icon, on);
            icon.style.left = Length.Percent((float)((lon + 180) / 360 * 100));
            icon.style.top = Length.Percent((float)((90 - lat) / 180 * 100));
        }

        /// <summary>Latitude and longitude (degrees) under the pointer; false while it is elsewhere.</summary>
        public bool Pointer(out double lat, out double lon)
        {
            lat = _pointer?.x ?? 0;
            lon = _pointer?.y ?? 0;
            return _pointer.HasValue;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = _lines.contentRect;
            if (r.width < 4) return;
            var g = ctx.painter2D;
            Vector2 At(double lat, double lon) => new Vector2((float)((lon + 180) / 360 * r.width), (float)((90 - lat) / 180 * r.height));

            g.lineWidth = 1f;
            for (int lon = -180; lon <= 180; lon += 30)
            {
                g.strokeColor = lon == 0 ? Axis : Grid;
                g.BeginPath(); g.MoveTo(At(90, lon)); g.LineTo(At(-90, lon)); g.Stroke();
            }
            for (int lat = -90; lat <= 90; lat += 30)
            {
                g.strokeColor = lat == 0 ? Axis : Grid;
                g.BeginPath(); g.MoveTo(At(lat, -180)); g.LineTo(At(lat, 180)); g.Stroke();
            }
            if (_track == null || _track.Count < 2) return;
            g.lineWidth = 2f;
            g.lineJoin = LineJoin.Round;
            g.strokeColor = Track;
            g.BeginPath();
            foreach (var p in _track)
            {
                if (p.Start) g.MoveTo(At(p.Lat, p.Lon)); else g.LineTo(At(p.Lat, p.Lon));
            }
            g.Stroke();
        }
    }
}
