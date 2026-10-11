using System;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Global UI flags consulted by input handlers.</summary>
    public static class UiState
    {
        public static bool KeyboardCaptured;
        public static bool PointerOverUi;
        public static bool MapActive;
        public static bool Paused;
        public static bool HelpOpen;
    }

    /// <summary>
    /// Reads the controls (keyboard, gamepad, joystick; see <see cref="ControlTable"/>) and writes the active vessel's controls
    /// or the EVA inputs.
    /// </summary>
    public sealed class FlightInput : MonoBehaviour
    {
        public FlightSim Sim;
        public FlightCamera Camera;
        public Action ToggleMap;
        public Action TogglePause;
        public Action Quicksave;
        public Action Quickload;
        public Action ToggleHelp;
        public Action ToggleUi;
        /// <summary>An action-group key was pressed (1 to 10). What a group does is FLT-07.</summary>
        public Action<int> ActionGroup;
        /// <summary>When set (autopilot), player input does not touch the controls.</summary>
        public bool ControlsLocked;

        public const float ThrottleRate = 0.7f;
        /// <summary>How fast trim builds up while the modifier is held, per second (full trim in 5 s).</summary>
        public const float TrimRate = 0.2f;
        /// <summary>Precision controls scale the attitude inputs to this much.</summary>
        public const float PrecisionScale = 0.25f;
        /// <summary>Fine attitude control is on (Caps Lock by default).</summary>
        public bool Precision;
        private bool _fHeld;
        private double _f9Held;

        private static readonly (string id, SasMode mode)[] SasKeys =
        {
            ("Flight/SasHold", SasMode.StabilityAssist), ("Flight/SasPrograde", SasMode.Prograde), ("Flight/SasRetrograde", SasMode.Retrograde),
            ("Flight/SasNormal", SasMode.Normal), ("Flight/SasAntiNormal", SasMode.AntiNormal), ("Flight/SasRadialOut", SasMode.RadialOut),
            ("Flight/SasRadialIn", SasMode.RadialIn), ("Flight/SasTarget", SasMode.Target), ("Flight/SasAntiTarget", SasMode.AntiTarget),
            ("Flight/SasManeuver", SasMode.Maneuver),
        };

        private void Update()
        {
            if (Sim == null) return;
            if (UiState.KeyboardCaptured) return;

            if (Controls.Down("Common/Cancel")) TogglePause?.Invoke();
            if (Controls.Down("Common/Help")) ToggleHelp?.Invoke();
            if (Controls.Down("Common/HideUi")) ToggleUi?.Invoke();
            if (Controls.Down("Common/Quicksave")) Quicksave?.Invoke();
            if (Controls.Held("Common/Quickload"))
            {
                _f9Held += Time.unscaledDeltaTime;
                if (_f9Held > 0.35 && _f9Held < 10) { _f9Held = 100; Quickload?.Invoke(); }
            }
            else _f9Held = 0;
            if (UiState.Paused) return;

            if (Controls.Down("Flight/ToggleMap")) ToggleMap?.Invoke();
            if (Controls.Down("Flight/CameraMode")) Camera?.CycleMode();
            if (Controls.Down("Flight/PreviousVessel")) Sim.CycleActive(-1);
            if (Controls.Down("Flight/NextVessel")) Sim.CycleActive(1);
            HandleWarp();

            var v = Sim.ActiveVessel;
            if (v == null || ControlsLocked) return;
            if (v.IsEva) HandleEva(v);
            else HandleVessel(v);
        }

        private void HandleWarp()
        {
            bool physics = Controls.Held("Flight/Modifier");
            if (Controls.Down("Flight/WarpFaster"))
            {
                if (physics || Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex + 1);
                else Sim.SetRailsWarp(Sim.Warp.RailsIndex + 1);
            }
            if (Controls.Down("Flight/WarpSlower"))
            {
                if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex - 1);
                else if (Sim.Warp.RailsIndex > 0) Sim.SetRailsWarp(Sim.Warp.RailsIndex - 1);
            }
            if (Controls.Down("Flight/WarpStop")) Sim.StopWarp();
        }

        private void HandleVessel(Vessel v)
        {
            var c = v.Ctrl;
            float dt = Time.unscaledDeltaTime;
            bool modifier = Controls.Held("Flight/Modifier");

            // Attitude: keys, then sticks. With the modifier held the keys trim instead of steering.
            float pitch = Dir("Flight/PitchDown", "Flight/PitchUp") + Controls.Axis("Flight/PitchAxis");
            float yaw = Dir("Flight/YawRight", "Flight/YawLeft") + Controls.Axis("Flight/YawAxis");
            float roll = Dir("Flight/RollRight", "Flight/RollLeft") + Controls.Axis("Flight/RollAxis");
            if (Controls.Down("Flight/TrimReset")) { c.TrimPitch = c.TrimYaw = c.TrimRoll = 0; Sim.Log("Trim reset"); }
            if (Controls.Down("Flight/PrecisionToggle")) { Precision = !Precision; Sim.Log("Precision controls " + (Precision ? "on" : "off")); }
            ApplyAttitude(c, pitch, yaw, roll, modifier, Precision, dt);
            c.TransY = Dir("Flight/TransForward", "Flight/TransBack");
            c.TransZ = Dir("Flight/TransDown", "Flight/TransUp"); // +Z = belly/down
            c.TransX = Dir("Flight/TransRight", "Flight/TransLeft");

            // Throttle: keys and triggers change it; a throttle lever sets it where it stands.
            if (Controls.Held("Flight/ThrottleUp")) c.Throttle = Mathf.Clamp01(c.Throttle + ThrottleRate * dt);
            if (Controls.Held("Flight/ThrottleDown")) c.Throttle = Mathf.Clamp01(c.Throttle - ThrottleRate * dt);
            var lever = Controls.Action("Flight/ThrottleAxis");
            if (lever.activeControl != null)
            {
                if (GameSettings.Current.input.throttleLever)
                {
                    var o = GameSettings.Current.input.Axis("ThrottleAxis");
                    float raw = lever.ReadValue<float>() * (o.invert ? -1f : 1f);
                    c.Throttle = Mathf.Clamp01((raw + 1f) / 2f);
                }
                else c.Throttle = Mathf.Clamp01(c.Throttle + Controls.Axis("Flight/ThrottleAxis") * ThrottleRate * dt);
            }
            if (Controls.Down("Flight/ThrottleFull")) c.Throttle = 1f;
            if (Controls.Down("Flight/ThrottleCut") && !modifier) c.Throttle = 0f;
            if (Controls.Down("Flight/Stage") && !UiState.MapActive)
            {
                if (Sim.Warp.OnRails) Sim.StopWarp();
                v.ActivateNextStage();
            }
            if (Controls.Down("Flight/ToggleSas")) { c.Sas = !c.Sas; v.Attitude.ResetHold(); Sim.Log("SAS " + (c.Sas ? "on" : "off")); }
            bool f = Controls.Held("Flight/HoldSas");
            if (f != _fHeld) { c.Sas = !c.Sas; v.Attitude.ResetHold(); _fHeld = f; }
            if (Controls.Down("Flight/ToggleRcs")) { c.Rcs = !c.Rcs; Sim.Log("RCS " + (c.Rcs ? "on" : "off")); }
            if (Controls.Down("Flight/ToggleLegs")) { c.LegsDeployed = !c.LegsDeployed; Sim.Log("Landing legs " + (c.LegsDeployed ? "deploying" : "retracting")); }

            if (!modifier)
                foreach (var (id, mode) in SasKeys)
                    if (Controls.Down(id)) SetSasMode(v, mode);
            for (int i = 1; i <= 10; i++)
                if (Controls.Down("Flight/ActionGroup" + i)) ActionGroup?.Invoke(i);
        }

        /// <summary>
        /// Turns what the pilot is giving (-1 to 1 each) into the vessel's attitude input: with the modifier held it builds trim
        /// instead of steering, precision controls cut the input to a quarter, and trim is always added, so it holds a
        /// pitch, yaw or roll with the keys released.
        /// </summary>
        public static void ApplyAttitude(VesselControl c, float pitch, float yaw, float roll, bool trimming, bool precision, float dt)
        {
            if (trimming)
            {
                c.TrimPitch = Mathf.Clamp(c.TrimPitch + pitch * TrimRate * dt, -1f, 1f);
                c.TrimYaw = Mathf.Clamp(c.TrimYaw + yaw * TrimRate * dt, -1f, 1f);
                c.TrimRoll = Mathf.Clamp(c.TrimRoll + roll * TrimRate * dt, -1f, 1f);
                pitch = yaw = roll = 0;
            }
            float k = precision ? PrecisionScale : 1f;
            c.Pitch = Mathf.Clamp(pitch * k + c.TrimPitch, -1f, 1f);
            c.Yaw = Mathf.Clamp(yaw * k + c.TrimYaw, -1f, 1f);
            c.Roll = Mathf.Clamp(roll * k + c.TrimRoll, -1f, 1f);
        }

        public static void SetSasMode(Vessel v, SasMode mode)
        {
            v.Ctrl.SasMode = mode;
            v.Ctrl.Sas = true;
            v.Attitude.ResetHold();
            FlightSim.Instance?.Log("SAS: " + mode);
        }

        private void HandleEva(Vessel v)
        {
            var eva = v.RootPart.GetModule<EvaModule>();
            if (eva == null) return;
            eva.MoveInput = new Vector2(Dir("Eva/Right", "Eva/Left"), Dir("Eva/Forward", "Eva/Back"));
            eva.RunInput = Controls.Held("Eva/Up") && !eva.JetpackOn;
            eva.VerticalInput = eva.JetpackOn ? Dir("Eva/Up", "Eva/Down") : 0f;
            if (Controls.Down("Eva/Jump")) eva.JumpPressed = true;
            if (Controls.Down("Eva/Jetpack")) { eva.JetpackOn = !eva.JetpackOn; Sim.Log("Jetpack " + (eva.JetpackOn ? "on" : "off")); }
            if (Camera != null) eva.CameraRotation = Camera.transform.rotation;
            if (Controls.Down("Eva/Board")) Sim.RequestBoard(v);
            if (Controls.Down("Eva/PlantFlag")) Sim.Enqueue(() => Sim.PlantFlag(v, $"{v.Record.evaCrew} was here. {Sim.Frame.Body.Name}, {TAP.Core.MathD.FormatUT(Sim.UT)}"));
        }

        /// <summary>+1 while the first control is held, -1 for the second.</summary>
        private static float Dir(string positive, string negative) => (Controls.Held(positive) ? 1f : 0f) - (Controls.Held(negative) ? 1f : 0f);
    }
}
