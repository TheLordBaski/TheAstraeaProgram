using System;
using System.Collections.Generic;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

    /// <summary>Key binding reference (also rendered by the in-game control guide).</summary>
    public static class Bindings
    {
        public static readonly (string keys, string action)[] Flight =
        {
            ("W / S", "Pitch down / up"),
            ("A / D", "Yaw left / right (D tilts east on the pad)"),
            ("Q / E", "Roll left / right"),
            ("Shift / Ctrl", "Throttle up / down"),
            ("Z / X", "Full throttle / cut throttle"),
            ("Space", "Activate next stage"),
            ("T", "Toggle SAS (stability assist)"),
            ("F (hold)", "Temporarily toggle SAS"),
            ("R", "Toggle RCS"),
            ("H / N", "RCS translate forward / back"),
            ("I / K", "RCS translate up / down"),
            ("J / L", "RCS translate left / right"),
            ("G", "Toggle landing legs"),
            ("1-0", "SAS: hold, pro, retro, normal, anti-normal, radial+, radial-, target, anti-target, maneuver"),
            ("M", "Toggle map view"),
            (", / .", "Time warp slower / faster"),
            ("Alt + .", "Physics warp (up to 4x)"),
            ("/", "Stop time warp"),
            ("V", "Camera mode (orbit / chase / locked)"),
            ("Right mouse", "Rotate camera, scroll to zoom"),
            ("[ / ]", "Switch to previous / next nearby vessel"),
            ("F5 / F9", "Quicksave / quickload"),
            ("Esc", "Pause menu"),
            ("F1", "Control guide"),
            ("F2", "Hide interface"),
            ("Alt+F12 or `", "Developer tools: cheats, teleports, zoom speed"),
            ("Right-click part", "Part actions (EVA, chutes, engines, undock...)"),
        };

        public static readonly (string keys, string action)[] Eva =
        {
            ("W A S D", "Walk / jetpack translate (camera relative)"),
            ("Shift", "Run  (jetpack: up)"),
            ("Ctrl", "Jetpack: down"),
            ("Space", "Jump"),
            ("R", "Toggle jetpack"),
            ("B", "Board nearby hatch (within 3 m, < 2.5 m/s)"),
            ("G", "Plant flag"),
        };

        public static readonly (string keys, string action)[] Map =
        {
            ("M", "Back to flight view"),
            ("Tab / Backspace", "Cycle focus / focus active vessel"),
            ("Click orbit line", "Add maneuver node / warp here"),
            ("Drag a node", "Move it along the orbit"),
            ("Drag node handles", "Prograde, normal, radial delta-v"),
            ("Right-click node / Del", "Delete the node (or its × button)"),
            ("Click body / vessel", "Select as target or focus"),
        };

        public static readonly (string keys, string action)[] Editor =
        {
            ("Click part list", "Pick up a new part"),
            ("Left click", "Attach held part / pick up a placed part (with attached parts; the root takes the whole craft)"),
            ("Click empty space", "Set the held parts aside (grey, not part of the craft)"),
            ("Alt + click", "Copy a placed part"),
            ("W A S D Q E", "Rotate held part (Shift: 5° steps), Space resets"),
            ("X / Shift+X", "Radial symmetry up / down (1, 2, 3, 4, 6, 8)"),
            ("C", "Angle snap on / off"),
            ("Del", "Delete the hovered or held part"),
            ("Esc", "Put picked-up parts back"),
            ("Right click", "Part details, thrust limiter, make root part"),
            ("Ctrl+Z / Ctrl+Y", "Undo / redo"),
            ("Ctrl+S", "Save craft"),
            ("Right drag / scroll", "Orbit / zoom camera; Shift+scroll or middle drag: up/down"),
            ("F", "Frame the craft"),
            ("V", "Show CoM / CoT / CoP markers"),
            ("F1", "Help"),
        };
    }

    /// <summary>Reads the keyboard and writes the active vessel's controls (or EVA inputs).</summary>
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
        /// <summary>When set (autopilot), player input does not touch the controls.</summary>
        public bool ControlsLocked;

        public const float ThrottleRate = 0.7f;
        private bool _fHeld;
        private double _f9Held;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || Sim == null) return;
            if (UiState.KeyboardCaptured) return;

            if (kb.escapeKey.wasPressedThisFrame) TogglePause?.Invoke();
            if (kb.f1Key.wasPressedThisFrame) ToggleHelp?.Invoke();
            if (kb.f2Key.wasPressedThisFrame) ToggleUi?.Invoke();
            if (kb.f5Key.wasPressedThisFrame) Quicksave?.Invoke();
            if (kb.f9Key.isPressed)
            {
                _f9Held += Time.unscaledDeltaTime;
                if (_f9Held > 0.35 && _f9Held < 10) { _f9Held = 100; Quickload?.Invoke(); }
            }
            else _f9Held = 0;
            if (UiState.Paused) return;

            if (kb.mKey.wasPressedThisFrame) ToggleMap?.Invoke();
            if (kb.vKey.wasPressedThisFrame) Camera?.CycleMode();
            if (kb.leftBracketKey.wasPressedThisFrame) Sim.CycleActive(-1);
            if (kb.rightBracketKey.wasPressedThisFrame) Sim.CycleActive(1);
            HandleWarp(kb);

            var v = Sim.ActiveVessel;
            if (v == null || ControlsLocked) return;
            if (v.IsEva) HandleEva(v, kb);
            else HandleVessel(v, kb);
        }

        private void HandleWarp(Keyboard kb)
        {
            bool alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
            if (kb.periodKey.wasPressedThisFrame)
            {
                if (alt || Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex + 1);
                else Sim.SetRailsWarp(Sim.Warp.RailsIndex + 1);
            }
            if (kb.commaKey.wasPressedThisFrame)
            {
                if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(Sim.Warp.PhysicsIndex - 1);
                else if (Sim.Warp.RailsIndex > 0) Sim.SetRailsWarp(Sim.Warp.RailsIndex - 1);
            }
            if (kb.slashKey.wasPressedThisFrame || kb.numpadDivideKey.wasPressedThisFrame) Sim.StopWarp();
        }

        private void HandleVessel(Vessel v, Keyboard kb)
        {
            var c = v.Ctrl;
            float dt = Time.unscaledDeltaTime;
            if (!UiState.MapActive || true)
            {
                c.Pitch = Axis(kb.wKey, kb.sKey);
                c.Yaw = Axis(kb.dKey, kb.aKey);
                c.Roll = Axis(kb.eKey, kb.qKey);
                c.TransY = Axis(kb.hKey, kb.nKey);
                c.TransZ = Axis(kb.kKey, kb.iKey); // +Z = belly/down
                c.TransX = Axis(kb.lKey, kb.jKey);
            }
            if (kb.leftShiftKey.isPressed) c.Throttle = Mathf.Clamp01(c.Throttle + ThrottleRate * dt);
            if (kb.leftCtrlKey.isPressed) c.Throttle = Mathf.Clamp01(c.Throttle - ThrottleRate * dt);
            if (kb.zKey.wasPressedThisFrame) c.Throttle = 1f;
            if (kb.xKey.wasPressedThisFrame) c.Throttle = 0f;
            if (kb.spaceKey.wasPressedThisFrame && !UiState.MapActive)
            {
                if (Sim.Warp.OnRails) Sim.StopWarp();
                v.ActivateNextStage();
            }
            if (kb.tKey.wasPressedThisFrame) { c.Sas = !c.Sas; v.Attitude.ResetHold(); Sim.Log("SAS " + (c.Sas ? "on" : "off")); }
            bool f = kb.fKey.isPressed;
            if (f != _fHeld) { c.Sas = !c.Sas; v.Attitude.ResetHold(); _fHeld = f; }
            if (kb.rKey.wasPressedThisFrame) { c.Rcs = !c.Rcs; Sim.Log("RCS " + (c.Rcs ? "on" : "off")); }
            if (kb.gKey.wasPressedThisFrame) { c.LegsDeployed = !c.LegsDeployed; Sim.Log("Landing legs " + (c.LegsDeployed ? "deploying" : "retracting")); }

            SasMode? mode = null;
            if (kb.digit1Key.wasPressedThisFrame) mode = SasMode.StabilityAssist;
            if (kb.digit2Key.wasPressedThisFrame) mode = SasMode.Prograde;
            if (kb.digit3Key.wasPressedThisFrame) mode = SasMode.Retrograde;
            if (kb.digit4Key.wasPressedThisFrame) mode = SasMode.Normal;
            if (kb.digit5Key.wasPressedThisFrame) mode = SasMode.AntiNormal;
            if (kb.digit6Key.wasPressedThisFrame) mode = SasMode.RadialOut;
            if (kb.digit7Key.wasPressedThisFrame) mode = SasMode.RadialIn;
            if (kb.digit8Key.wasPressedThisFrame) mode = SasMode.Target;
            if (kb.digit9Key.wasPressedThisFrame) mode = SasMode.AntiTarget;
            if (kb.digit0Key.wasPressedThisFrame) mode = SasMode.Maneuver;
            if (mode.HasValue) SetSasMode(v, mode.Value);
        }

        public static void SetSasMode(Vessel v, SasMode mode)
        {
            v.Ctrl.SasMode = mode;
            v.Ctrl.Sas = true;
            v.Attitude.ResetHold();
            FlightSim.Instance?.Log("SAS: " + mode);
        }

        private void HandleEva(Vessel v, Keyboard kb)
        {
            var eva = v.RootPart.GetModule<EvaModule>();
            if (eva == null) return;
            eva.MoveInput = new Vector2(Axis(kb.dKey, kb.aKey), Axis(kb.wKey, kb.sKey));
            eva.RunInput = kb.leftShiftKey.isPressed && !eva.JetpackOn;
            eva.VerticalInput = eva.JetpackOn ? Axis(kb.leftShiftKey, kb.leftCtrlKey) : 0f;
            if (kb.spaceKey.wasPressedThisFrame) eva.JumpPressed = true;
            if (kb.rKey.wasPressedThisFrame) { eva.JetpackOn = !eva.JetpackOn; Sim.Log("Jetpack " + (eva.JetpackOn ? "on" : "off")); }
            if (Camera != null) eva.CameraRotation = Camera.transform.rotation;
            if (kb.bKey.wasPressedThisFrame) Sim.RequestBoard(v);
            if (kb.gKey.wasPressedThisFrame) Sim.Enqueue(() => Sim.PlantFlag(v, $"{v.Record.evaCrew} was here. {Sim.Frame.Body.Name}, {TAP.Core.MathD.FormatUT(Sim.UT)}"));
        }

        private static float Axis(KeyControl pos, KeyControl neg) => (pos.isPressed ? 1f : 0f) - (neg.isPressed ? 1f : 0f);
    }
}
