using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TMPro;
using UnityEngine;

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
        private RectTransform _root;
        private readonly Dictionary<CelestialBody, TextMeshProUGUI> _labels = new Dictionary<CelestialBody, TextMeshProUGUI>();

        public static BodyLabels Create(FlightSceneController scene)
        {
            var canvas = UIKit.CreateCanvas("BodyLabels", 1); // under the HUD's panels
            var l = canvas.gameObject.AddComponent<BodyLabels>();
            l.Scene = scene;
            l._root = (RectTransform)canvas.transform;
            scene.UiVisibilityChanged += visible => canvas.enabled = visible;
            return l;
        }

        private readonly List<(Vector2 screen, TextMeshProUGUI label)> _shown = new List<(Vector2, TextMeshProUGUI)>();

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
                    label = UIKit.Label(_root, fb.Body.Name, 13, UIKit.TextDim);
                    label.rectTransform.pivot = new Vector2(0, 0.5f);
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero;
                    label.rectTransform.sizeDelta = new Vector2(240, 18);
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
                if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
                if (!show) continue;
                label.text = fb.Body.Name;
                _shown.Add((s, label));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, s, null, out Vector2 local);
                // Just right of the point, in canvas units from the bottom-left corner.
                label.rectTransform.anchoredPosition = local + _root.rect.size * 0.5f + new Vector2(9, 0);
            }
        }
    }
}
