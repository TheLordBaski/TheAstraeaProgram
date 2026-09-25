using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    /// <summary>
    /// Assembly building camera: orbits a pivot on the craft's vertical axis.
    /// Right drag rotates, scroll zooms, Shift+scroll or middle drag moves the pivot up/down.
    /// </summary>
    public sealed class EditorCamera : MonoBehaviour
    {
        public Camera Cam;
        public Func<bool> PointerOverUi = () => false;
        public float MinHeight = 0.5f, MaxHeight = 40f;
        public float MinDistance = 2.5f, MaxDistance = 120f;

        private float _yaw = 30f, _pitch = 12f, _dist = 16f, _height = 4f;
        private float _tYaw = 30f, _tPitch = 12f, _tDist = 16f, _tHeight = 4f;

        public static EditorCamera Create()
        {
            var go = new GameObject("EditorCamera");
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            cam.cullingMask = ~((1 << Core.Layers.Map) | (1 << Core.Layers.Scaled) | (1 << 30));
            go.tag = "MainCamera";
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var rig = go.AddComponent<EditorCamera>();
            rig.Cam = cam;
            return rig;
        }

        public void Frame(Bounds b, bool instant)
        {
            float size = Mathf.Max(b.extents.y * 2f, Mathf.Max(b.extents.x, b.extents.z) * 2f, 2f);
            _tHeight = Mathf.Clamp(b.center.y, MinHeight, MaxHeight);
            _tDist = Mathf.Clamp(size * 1.45f + 4f, MinDistance, MaxDistance);
            MaxHeight = Mathf.Max(8f, b.max.y + 2f);
            if (instant) { _height = _tHeight; _dist = _tDist; _yaw = _tYaw; _pitch = _tPitch; Apply(); }
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            float dt = Time.unscaledDeltaTime;
            bool overUi = PointerOverUi != null && PointerOverUi();
            bool typing = UiState.KeyboardCaptured;
            if (mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue();
                if (mouse.rightButton.isPressed)
                {
                    _tYaw += d.x * 0.22f;
                    _tPitch = Mathf.Clamp(_tPitch - d.y * 0.22f, -20f, 85f);
                }
                if (mouse.middleButton.isPressed) _tHeight -= d.y * _dist * 0.0022f;
                float scroll = mouse.scroll.ReadValue().y;
                if (!overUi && Mathf.Abs(scroll) > 0.01f && !FlightCamera.UiBlocksScroll)
                {
                    float steps = ScrollZoom.Notches(scroll);
                    bool shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
                    if (shift) _tHeight += steps * Mathf.Max(0.5f, _dist * 0.08f);
                    else _tDist *= ScrollZoom.Factor(scroll, 0.78f);
                }
            }
            if (kb != null && !typing)
            {
                // Arrow keys / page keys as an alternative to the mouse.
                float k = 70f * dt;
                if (kb.leftArrowKey.isPressed) _tYaw += k;
                if (kb.rightArrowKey.isPressed) _tYaw -= k;
                if (kb.upArrowKey.isPressed) _tPitch = Mathf.Clamp(_tPitch + k, -20f, 85f);
                if (kb.downArrowKey.isPressed) _tPitch = Mathf.Clamp(_tPitch - k, -20f, 85f);
                if (kb.pageUpKey.isPressed) _tHeight += _dist * 0.6f * dt;
                if (kb.pageDownKey.isPressed) _tHeight -= _dist * 0.6f * dt;
                if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) _tDist *= 1f - 1.2f * dt;
                if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) _tDist *= 1f + 1.2f * dt;
            }
            _tHeight = Mathf.Clamp(_tHeight, MinHeight, MaxHeight);
            _tDist = Mathf.Clamp(_tDist, MinDistance, MaxDistance);
            float a = 1f - Mathf.Exp(-14f * dt);
            _yaw = Mathf.LerpAngle(_yaw, _tYaw, a);
            _pitch = Mathf.Lerp(_pitch, _tPitch, a);
            _dist = Mathf.Lerp(_dist, _tDist, a);
            _height = Mathf.Lerp(_height, _tHeight, a);
            Apply();
        }

        private void Apply()
        {
            Quaternion q = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 pivot = new Vector3(0, _height, 0);
            Vector3 pos = pivot - q * Vector3.forward * _dist;
            if (pos.y < 0.3f) pos.y = 0.3f; // stay above the floor
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pivot - pos, Vector3.up));
        }
    }
}
