using TAP.Core;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    public enum CameraMode { Orbit, Chase, Locked }

    /// <summary>
    /// Flight camera orbiting the active vessel. Orbit mode keeps the local horizon level (up = away
    /// from the body), Chase follows the velocity vector, Locked rotates with the vessel.
    /// Right-mouse drag rotates, scroll zooms smoothly, Numpad/arrow-free.
    /// </summary>
    public sealed class FlightCamera : MonoBehaviour
    {
        public Camera Cam;
        public FlightSim Sim;
        public CameraMode Mode = CameraMode.Orbit;
        public float Distance = 25f;
        public float TargetDistance = 25f;
        public float Yaw = 20f; // measured from north
        public float Pitch = 12f;
        public bool InputEnabled = true;
        public Vector3 FocusOffset;

        private Quaternion _frame = Quaternion.identity;
        private Vector3 _lastUp = Vector3.up;
        private Vessel _lastVessel;

        public static FlightCamera Create(FlightSim sim)
        {
            var go = new GameObject("FlightCamera");
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.25f;
            cam.farClipPlane = 3.0e8f;
            cam.fieldOfView = 60f;
            // The map view, the assembly-editor layers and off-screen thumbnail renders are not part of the flight view.
            cam.cullingMask = ~((1 << Layers.Map) | (1 << Layers.EditorParts) | (1 << Layers.Ghost) | (1 << 30));
            cam.clearFlags = CameraClearFlags.SolidColor; // the sky is a background dome (SkyController)
            cam.backgroundColor = Color.black;
            cam.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            var fc = go.AddComponent<FlightCamera>();
            fc.Cam = cam;
            fc.Sim = sim;
            var data = go.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null) data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return fc;
        }

        public void CycleMode()
        {
            Mode = (CameraMode)(((int)Mode + 1) % 3);
            Sim?.Log($"Camera: {Mode}");
        }

        private void LateUpdate()
        {
            var v = Sim != null ? Sim.ActiveVessel : null;
            if (v == null) return;
            if (v != _lastVessel)
            {
                _lastVessel = v;
                float size = VesselSize(v);
                TargetDistance = v.IsEva ? 6f : Mathf.Clamp(size * 2.6f, 8f, 250f);
                Distance = TargetDistance;
            }

            if (InputEnabled) HandleInput();
            Distance = Mathf.Lerp(Distance, TargetDistance, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));

            Vector3 focus = v.Rb != null ? v.Rb.worldCenterOfMass : v.transform.position;
            if (v.Rb != null && v.Rb.interpolation != RigidbodyInterpolation.None)
            {
                // Use the interpolated transform + CoM offset for smooth rendering.
                focus = v.transform.TransformPoint(v.LocalCenterOfMass);
            }
            focus += FocusOffset;

            Vector3 up = v.GravityAccel.sqrMagnitude > 1e-6f ? -v.GravityAccel.normalized : _lastUp;
            _lastUp = up;
            Quaternion baseRot;
            switch (Mode)
            {
                case CameraMode.Chase:
                {
                    Vector3 vel = (Vector3)v.SpeedModeVelocity;
                    Vector3 fwd = vel.sqrMagnitude > 1f ? vel.normalized : v.ControlRotation * Vector3.up;
                    Vector3 u = Vector3.ProjectOnPlane(up, fwd);
                    if (u.sqrMagnitude < 1e-4f) u = v.ControlRotation * Vector3.back;
                    baseRot = Quaternion.LookRotation(fwd, u.normalized);
                    break;
                }
                case CameraMode.Locked:
                    baseRot = v.ControlRotation * Quaternion.Euler(-90, 0, 0);
                    break;
                default:
                {
                    // Horizon-level frame: forward = projected north, up = radial.
                    baseRot = Quaternion.LookRotation(Geo.North(up), up);
                    break;
                }
            }
            _frame = Quaternion.Slerp(_frame, baseRot, 1f - Mathf.Exp(-Time.unscaledDeltaTime * (Mode == CameraMode.Orbit ? 20f : 6f)));
            Quaternion orbit = _frame * Quaternion.Euler(Pitch, Yaw, 0);
            Vector3 offset = orbit * new Vector3(0, 0, -Distance);
            if (Mode == CameraMode.Chase) offset = _frame * Quaternion.Euler(Pitch, Yaw + 180, 0) * new Vector3(0, 0, Distance);
            Vector3 camPos = focus + offset;
            // Don't go below the ground.
            if (Physics.Raycast(focus, (camPos - focus).normalized, out RaycastHit hit, Distance, Layers.GroundMask, QueryTriggerInteraction.Ignore))
                camPos = hit.point + hit.normal * 0.6f;
            transform.position = camPos;
            transform.rotation = Quaternion.LookRotation(focus - camPos, _frame * Vector3.up);
            // Keep near plane appropriate for zoom level (depth precision).
            Cam.nearClipPlane = Mathf.Clamp(Distance * 0.01f, 0.1f, 5f);
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null) return;
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                Yaw += d.x * 0.25f;
                Pitch = Mathf.Clamp(Pitch - d.y * 0.25f, -89f, 89f);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !UiBlocksScroll)
            {
                TargetDistance = Mathf.Clamp(TargetDistance * ScrollZoom.Factor(scroll, 0.75f), 2.5f, 60000f);
            }
            if (kb != null)
            {
                float k = Time.unscaledDeltaTime * 60f;
                if (kb.numpad4Key.isPressed) Yaw -= k;
                if (kb.numpad6Key.isPressed) Yaw += k;
                if (kb.numpad8Key.isPressed) Pitch = Mathf.Clamp(Pitch + k, -89, 89);
                if (kb.numpad2Key.isPressed) Pitch = Mathf.Clamp(Pitch - k, -89, 89);
                float kz = 2.5f * ScrollZoom.Speed * Time.unscaledDeltaTime;
                if (kb.pageUpKey.isPressed) TargetDistance = Mathf.Max(2.5f, TargetDistance * Mathf.Exp(-kz));
                if (kb.pageDownKey.isPressed) TargetDistance = Mathf.Min(60000f, TargetDistance * Mathf.Exp(kz));
            }
        }

        /// <summary>Set by the UI when the pointer is over a scrollable panel.</summary>
        public static bool UiBlocksScroll;

        public static float VesselSize(Vessel v)
        {
            var b = new Bounds(v.transform.position, Vector3.zero);
            foreach (var r in v.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                b.Encapsulate(r.bounds);
            }
            return b.size.magnitude;
        }
    }
}
