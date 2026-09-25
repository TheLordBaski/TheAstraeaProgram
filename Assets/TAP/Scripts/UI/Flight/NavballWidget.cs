using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>
    /// Attitude indicator. The ball (shader) shows the local horizon frame (sky/ground, heading and
    /// pitch ladder) as seen from behind the vessel; markers are orthographic projections of
    /// directions onto the front hemisphere, so both are exactly consistent.
    /// </summary>
    public sealed class NavballWidget : MonoBehaviour
    {
        public FlightSim Sim;
        public RectTransform Root;
        public float Radius = 118f;
        private RawImage _ball;
        private Material _mat;
        private readonly Dictionary<string, Image> _markers = new Dictionary<string, Image>();
        private TextMeshProUGUI _heading;
        private Image _level;

        public static readonly Color Pro = new Color(0.9f, 0.95f, 0.2f);
        public static readonly Color Nrm = new Color(0.85f, 0.35f, 1f);
        public static readonly Color Rad = new Color(0.35f, 0.9f, 1f);
        public static readonly Color Man = new Color(0.25f, 0.55f, 1f);
        public static readonly Color Tgt = new Color(1f, 0.3f, 0.85f);

        public static NavballWidget Create(Transform parent, FlightSim sim, float size)
        {
            var rt = UIKit.Rect(parent, "Navball");
            rt.sizeDelta = new Vector2(size, size);
            var w = rt.gameObject.AddComponent<NavballWidget>();
            w.Sim = sim;
            w.Root = rt;
            w.Radius = size * 0.5f;
            // bezel
            var bezel = UIKit.Image(rt, UIKit.CircleSprite, new Color(0.08f, 0.09f, 0.11f, 1f), "Bezel");
            bezel.rectTransform.sizeDelta = new Vector2(size + 16, size + 16);
            var ballGo = UIKit.Rect(rt, "Ball");
            ballGo.sizeDelta = new Vector2(size, size);
            w._ball = ballGo.gameObject.AddComponent<RawImage>();
            var tex = Resources.Load<Texture2D>("Textures/Navball");
            w._ball.texture = tex;
            var baseMat = Resources.Load<Material>("Materials/NavballUI");
            if (baseMat != null) { w._mat = new Material(baseMat); w._ball.material = w._mat; }
            w._ball.raycastTarget = false;
            foreach (var (key, icon, color) in new[]
                     {
                         ("pro", "nav_prograde", Pro), ("retro", "nav_retrograde", Pro), ("nrm", "nav_normal", Nrm), ("anrm", "nav_antinormal", Nrm),
                         ("radout", "nav_radialout", Rad), ("radin", "nav_radialin", Rad), ("man", "nav_maneuver", Man),
                         ("tgt", "nav_target", Tgt), ("atgt", "nav_antitarget", Tgt),
                     })
            {
                var img = UIKit.Image(rt, UIKit.Icon(icon), color, key);
                img.rectTransform.sizeDelta = new Vector2(34, 34);
                w._markers[key] = img;
            }
            w._level = UIKit.Image(rt, UIKit.Icon("nav_level"), new Color(1f, 0.62f, 0.12f), "Level");
            w._level.rectTransform.sizeDelta = new Vector2(64, 64);
            w._heading = UIKit.Label(rt, "090°", 17, UIKit.TextColor, TextAlignmentOptions.Center);
            w._heading.rectTransform.anchoredPosition = new Vector2(0, size * 0.5f + 16);
            w._heading.rectTransform.sizeDelta = new Vector2(80, 22);
            return w;
        }

        private void LateUpdate()
        {
            var v = Sim != null ? Sim.ActiveVessel : null;
            if (v == null || v.IsEva) { SetAll(false); return; }
            Vector3 upW = (Vector3)v.TruePosition.normalized;
            Vector3 northW = Geo.North(upW);
            Vector3 eastW = Geo.East(upW);

            Quaternion ctrl = v.ControlRotation;
            Vector3 right = ctrl * Vector3.right, top = ctrl * Vector3.back, nose = ctrl * Vector3.up;
            // Camera frame (x right, y up, z forward=nose) -> horizon frame (x east, y up, z north)
            var m = Matrix4x4.identity;
            m.SetRow(0, new Vector4(Vector3.Dot(eastW, right), Vector3.Dot(eastW, top), Vector3.Dot(eastW, nose), 0));
            m.SetRow(1, new Vector4(Vector3.Dot(upW, right), Vector3.Dot(upW, top), Vector3.Dot(upW, nose), 0));
            m.SetRow(2, new Vector4(Vector3.Dot(northW, right), Vector3.Dot(northW, top), Vector3.Dot(northW, nose), 0));
            if (_mat != null) _mat.SetMatrix("_Rot", m);

            float heading = Mathf.Atan2(Vector3.Dot(nose, eastW), Vector3.Dot(nose, northW)) * Mathf.Rad2Deg;
            if (heading < 0) heading += 360;
            float pitch = 90f - Vector3.Angle(nose, upW);
            _heading.text = $"{heading:000}°  {pitch:+00;-00}°";

            // Markers
            Vector3 vel = (Vector3)v.SpeedModeVelocity;
            bool hasVel = vel.magnitude > 0.1f;
            Place("pro", hasVel ? vel.normalized : Vector3.zero, right, top, nose);
            Place("retro", hasVel ? -vel.normalized : Vector3.zero, right, top, nose);
            if (v.OrbitalSpeed > 1)
            {
                PatchedConics.OrbitalFrame(v.TruePosition, v.TrueVelocity, out var pr, out var nr, out var rd);
                Place("nrm", (Vector3)nr, right, top, nose);
                Place("anrm", -(Vector3)nr, right, top, nose);
                Place("radout", (Vector3)rd, right, top, nose);
                Place("radin", -(Vector3)rd, right, top, nose);
            }
            else { Hide("nrm"); Hide("anrm"); Hide("radout"); Hide("radin"); }
            if (v.TryGetBurnVector(out var burn) && burn.magnitude > 0.05) Place("man", (Vector3)burn.normalized, right, top, nose);
            else Hide("man");
            if (v.TryGetTargetState(Sim.UT, out var tp, out _))
            {
                Vector3 to = (Vector3)(tp - v.TruePosition).normalized;
                Place("tgt", to, right, top, nose);
                Place("atgt", -to, right, top, nose);
            }
            else { Hide("tgt"); Hide("atgt"); }
        }

        private void Place(string key, Vector3 dirWorld, Vector3 right, Vector3 top, Vector3 nose)
        {
            var img = _markers[key];
            if (dirWorld.sqrMagnitude < 1e-6f) { img.enabled = false; return; }
            float x = Vector3.Dot(dirWorld, right), y = Vector3.Dot(dirWorld, top), z = Vector3.Dot(dirWorld, nose);
            if (z < -0.02f)
            {
                img.enabled = false;
                return;
            }
            img.enabled = true;
            img.rectTransform.anchoredPosition = new Vector2(x, y) * (Radius - 4f);
        }

        private void Hide(string key) => _markers[key].enabled = false;

        private void SetAll(bool on)
        {
            foreach (var kv in _markers) kv.Value.enabled = on;
        }
    }
}
