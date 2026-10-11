using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Names beside the bodies too small to make out in the flight view: the points of light of the far view (FND-03).
    /// Hidden with the rest of the interface, and for a body behind a nearer one.
    /// </summary>
    [DefaultExecutionOrder(1100)] // after the far view has placed the bodies this frame
    public sealed class BodyLabels : MonoBehaviour
    {
        public FlightSceneController Scene;
        private VisualElement _root;
        private readonly Dictionary<CelestialBody, Label> _labels = new Dictionary<CelestialBody, Label>();

        public static BodyLabels Create(FlightSceneController scene)
        {
            var doc = UIKit.CreateDocument("BodyLabels", 1, null, "FlightPanels"); // under the HUD's panels
            var l = doc.gameObject.AddComponent<BodyLabels>();
            l.Scene = scene;
            l._root = doc.rootVisualElement;
            scene.UiVisibilityChanged += visible => UIKit.Show(l._root, visible);
            return l;
        }

        private readonly List<(Vector2 screen, Label label)> _shown = new List<(Vector2, Label)>();

        private void LateUpdate()
        {
            var far = Scene != null ? Scene.FarView : null;
            if (far == null) return;
            bool on = far.Cam.enabled;
            _shown.Clear();
            // Parents come before their moons, so a moon right next to its planet joins the planet's label.
            foreach (var fb in far.Bodies)
            {
                if (!_labels.TryGetValue(fb.Body, out var label))
                {
                    label = UIKit.Label(fb.Body.Name, "body-label");
                    _root.Add(label);
                    _labels[fb.Body] = label;
                }
                Vector3 s = fb.Screen;
                bool show = on && fb.PointAlpha > 0.5f && !fb.Occluded && s.z > 0
                            && s.x >= 0 && s.y >= 0 && s.x <= Screen.width && s.y <= Screen.height;
                if (show)
                    foreach (var other in _shown)
                        if (Vector2.Distance(other.screen, s) < 12f)
                        {
                            other.label.text += " · " + fb.Body.Name;
                            show = false;
                            break;
                        }
                UIKit.Show(label, show);
                if (!show) continue;
                label.text = fb.Body.Name;
                _shown.Add((s, label));
                // Just right of the point.
                UIKit.Place(label, UIKit.ScreenToPanel(s) + new Vector2(9, 0));
            }
        }
    }
}
