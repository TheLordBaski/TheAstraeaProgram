using System.Collections.Generic;
using TAP.Core;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Attitude indicator. The ball (shader) shows the local horizon frame (sky/ground, heading and
    /// pitch ladder) as seen from behind the vessel; markers are orthographic projections of
    /// directions onto the front hemisphere, so both are exactly consistent. UI Toolkit can't run the ball's
    /// shader on an element, so it renders into a RenderTexture each frame that the ball element shows.
    /// </summary>
    public sealed class NavballWidget : MonoBehaviour
    {
        public FlightSim Sim;
        /// <summary>The widget's box (size × size), positioned by the caller's USS.</summary>
        public VisualElement Root;
        public float Radius = 118f;
        private VisualElement _ball;
        private RenderTexture _rt;
        private Material _mat;
        private Texture2D _tex;
        private readonly Dictionary<string, Image> _markers = new Dictionary<string, Image>();
        private Label _heading;
        private Image _level;
        private Matrix4x4 _rot = Matrix4x4.identity;

        // Flight Deck marker colours (not KSP's): green velocity, cyan orbit frame, magenta planned burn, amber target.
        public static readonly Color Pro = new Color32(72, 226, 124, 255);
        public static readonly Color Nrm = new Color32(94, 230, 242, 255);
        public static readonly Color Rad = new Color32(94, 230, 242, 255);
        public static readonly Color Man = new Color32(255, 90, 217, 255);
        public static readonly Color Tgt = new Color32(255, 181, 71, 255);

        /// <param name="root">An element sized size × size; the ball, markers and heading go inside it.</param>
        public static NavballWidget Create(GameObject host, VisualElement root, FlightSim sim, float size)
        {
            var w = host.AddComponent<NavballWidget>();
            w.Sim = sim;
            w.Root = root;
            w.Radius = size * 0.5f;
            root.AddToClassList("navball");
            root.pickingMode = PickingMode.Ignore;
            var bezel = UIKit.Element("navball__bezel");
            bezel.pickingMode = PickingMode.Ignore;
            root.Add(bezel);
            w._ball = UIKit.Element("navball__ball");
            w._ball.pickingMode = PickingMode.Ignore;
            root.Add(w._ball);
            w._tex = Resources.Load<Texture2D>("Textures/Navball");
            var baseMat = Resources.Load<Material>("Materials/NavballUI");
            if (baseMat != null) w._mat = new Material(baseMat);
            // Rendered at twice the reference size so it stays sharp on large screens.
            int px = Mathf.RoundToInt(size * 2);
            w._rt = new RenderTexture(px, px, 0, RenderTextureFormat.ARGB32) { name = "Navball", useMipMap = false, antiAliasing = 1 };
            w._rt.Create();
            w._ball.style.backgroundImage = Background.FromRenderTexture(w._rt);
            foreach (var (key, icon, color) in new[]
                     {
                         ("pro", "nav_prograde", Pro), ("retro", "nav_retrograde", Pro), ("nrm", "nav_normal", Nrm), ("anrm", "nav_antinormal", Nrm),
                         ("radout", "nav_radialout", Rad), ("radin", "nav_radialin", Rad), ("man", "nav_maneuver", Man),
                         ("tgt", "nav_target", Tgt), ("atgt", "nav_antitarget", Tgt),
                     })
            {
                var img = UIKit.Image(UIKit.Icon(icon), color, "navball__marker");
                root.Add(img);
                w._markers[key] = img;
            }
            w._level = UIKit.Image(UIKit.Icon("nav_level"), new Color32(255, 210, 74, 255), "navball__level");
            root.Add(w._level);
            w._heading = UIKit.Label("090°", "navball__heading no-loc");
            root.Add(w._heading);
            return w;
        }

        private void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_mat != null) Destroy(_mat);
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
            _rot.SetRow(0, new Vector4(Vector3.Dot(eastW, right), Vector3.Dot(eastW, top), Vector3.Dot(eastW, nose), 0));
            _rot.SetRow(1, new Vector4(Vector3.Dot(upW, right), Vector3.Dot(upW, top), Vector3.Dot(upW, nose), 0));
            _rot.SetRow(2, new Vector4(Vector3.Dot(northW, right), Vector3.Dot(northW, top), Vector3.Dot(northW, nose), 0));
            RenderBall();

            float heading = Mathf.Atan2(Vector3.Dot(nose, eastW), Vector3.Dot(nose, northW)) * Mathf.Rad2Deg;
            if (heading < 0) heading += 360;
            float pitch = 90f - Vector3.Angle(nose, upW);
            _heading.text = $"HDG {heading:000}°   PIT {pitch:+00;-00}°";

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

        private void RenderBall()
        {
            if (_mat == null || _rt == null) return;
            _mat.SetMatrix("_Rot", _rot);
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;
            Graphics.Blit(_tex, _rt, _mat);
            _ball.MarkDirtyRepaint();
        }

        private void Place(string key, Vector3 dirWorld, Vector3 right, Vector3 top, Vector3 nose)
        {
            var img = _markers[key];
            if (dirWorld.sqrMagnitude < 1e-6f) { Hide(key); return; }
            float x = Vector3.Dot(dirWorld, right), y = Vector3.Dot(dirWorld, top), z = Vector3.Dot(dirWorld, nose);
            if (z < -0.02f) { Hide(key); return; }
            img.visible = true;
            // Marker centres relative to the ball's centre; USS translate -50% centres each icon on its point.
            img.style.left = Radius + x * (Radius - 4f);
            img.style.top = Radius - y * (Radius - 4f);
        }

        private void Hide(string key) => _markers[key].visible = false;

        private void SetAll(bool on)
        {
            foreach (var kv in _markers) kv.Value.visible = on;
        }
    }
}
