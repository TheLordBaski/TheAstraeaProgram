using System.Collections.Generic;

namespace TAP.Game
{
    public enum ControlKind { Button, Axis }

    /// <summary>One control of the game: what it is called, which keys and sticks it starts on, and where the guide lists it.</summary>
    public sealed class ControlDef
    {
        /// <summary>"Map/Name": the identity code uses and the saved rebinds refer to.</summary>
        public string Id;
        public string Map, Name;
        /// <summary>The line in the control guide.</summary>
        public string Label;
        public ControlKind Kind;
        /// <summary>Default bindings. A path is Input System syntax; "Alt+&lt;Keyboard&gt;/x" is a key with modifier keys held.</summary>
        public string[] Defaults;
        /// <summary>The guide's heading.</summary>
        public string Section;
    }

    /// <summary>
    /// Every control of the game in one place (FND-09): keyboard, gamepad and joystick defaults, and the lines of the F1
    /// guide. The guide, the rebinding screen and <see cref="Controls"/> are all made from this table, so none can be out of date.
    /// Mouse buttons are not part of it: a few of them are listed as plain text in the guide.
    /// </summary>
    public static class ControlTable
    {
        public const string Flight = "FLIGHT", Eva = "EVA", MapView = "MAP VIEW", Assembly = "ASSEMBLY BUILDING", Developer = "DEVELOPER";

        public static readonly List<ControlDef> All = new List<ControlDef>();

        /// <summary>Guide lines for the mouse, which is not rebindable: (section, keys, action).</summary>
        public static readonly (string section, string keys, string action)[] MouseRows =
        {
            (Flight, "Right mouse", "Rotate the camera, scroll to zoom"),
            (Flight, "Right-click part", "Part actions (EVA, chutes, engines, undock...)"),
            (MapView, "Click orbit line", "Add maneuver node / warp here"),
            (MapView, "Drag a node", "Move it along the orbit"),
            (MapView, "Drag node handles", "Prograde, normal, radial delta-v"),
            (MapView, "Right-click node", "Delete the node"),
            (MapView, "Click body / vessel", "Select as target or focus"),
            (Assembly, "Click part list", "Pick up a new part"),
            (Assembly, "Left click", "Attach held part / pick up a placed part (with attached parts; the root takes the whole craft)"),
            (Assembly, "Click empty space", "Set the held parts aside (grey, not part of the craft)"),
            (Assembly, "Right click", "Part details, thrust limiter, make root part"),
            (Assembly, "Right drag / scroll", "Orbit / zoom camera; Shift+scroll or middle drag: up/down"),
        };

        static ControlTable()
        {
            const string kb = "<Keyboard>/", gp = "<Gamepad>/", js = "<Joystick>/";

            // ---- every screen
            Section = Flight;
            Button("Common", "Cancel", "Pause menu / close the dialog on top / put picked-up parts back", kb + "escape", gp + "start");
            Button("Common", "Help", "Control guide / assembly help", kb + "f1");
            Button("Common", "HideUi", "Hide interface", kb + "f2");
            Button("Common", "Quicksave", "Quicksave", kb + "f5");
            Button("Common", "Quickload", "Quickload (hold)", kb + "f9");

            // ---- flight: attitude
            Button("Flight", "PitchDown", "Pitch down", kb + "w");
            Button("Flight", "PitchUp", "Pitch up", kb + "s");
            Button("Flight", "YawLeft", "Yaw left", kb + "a");
            Button("Flight", "YawRight", "Yaw right (tilts east on the pad)", kb + "d");
            Button("Flight", "RollLeft", "Roll left", kb + "q");
            Button("Flight", "RollRight", "Roll right", kb + "e");
            Axis("Flight", "PitchAxis", "Pitch (stick: forward is nose down)", gp + "leftStick/y", js + "stick/y");
            Axis("Flight", "YawAxis", "Yaw (stick)", gp + "leftStick/x", js + "stick/x");
            Axis("Flight", "RollAxis", "Roll (stick, twist)", gp + "rightStick/x");
            Button("Flight", "PrecisionToggle", "Precision controls on / off (fine attitude control)", kb + "capsLock");
            Button("Flight", "Modifier", "Modifier: hold with W A S D Q E to trim, with the warp key for physics warp", kb + "alt");
            Button("Flight", "TrimReset", "Reset trim", "Alt+" + kb + "x");

            // ---- flight: engines and staging
            Button("Flight", "ThrottleUp", "Throttle up", kb + "leftShift", gp + "rightTrigger");
            Button("Flight", "ThrottleDown", "Throttle down", kb + "leftCtrl", gp + "leftTrigger");
            Button("Flight", "ThrottleFull", "Full throttle", kb + "z", gp + "rightShoulder");
            Button("Flight", "ThrottleCut", "Cut throttle", kb + "x", gp + "leftShoulder");
            Axis("Flight", "ThrottleAxis", "Throttle (HOTAS lever sets it, others change it)");
            Button("Flight", "Stage", "Activate next stage", kb + "space", gp + "buttonSouth", js + "trigger");

            // ---- flight: systems
            Button("Flight", "ToggleSas", "Toggle SAS (stability assist)", kb + "t", gp + "buttonWest");
            Button("Flight", "HoldSas", "Temporarily toggle SAS (hold)", kb + "f");
            Button("Flight", "ToggleRcs", "Toggle RCS", kb + "r", gp + "buttonNorth");
            Button("Flight", "TransForward", "RCS translate forward", kb + "h");
            Button("Flight", "TransBack", "RCS translate back", kb + "n");
            Button("Flight", "TransUp", "RCS translate up", kb + "i");
            Button("Flight", "TransDown", "RCS translate down", kb + "k");
            Button("Flight", "TransLeft", "RCS translate left", kb + "j");
            Button("Flight", "TransRight", "RCS translate right", kb + "l");
            Button("Flight", "ToggleLegs", "Toggle landing legs", kb + "g", gp + "buttonEast");

            // ---- flight: SAS modes
            Button("Flight", "SasHold", "SAS: hold attitude", kb + "1");
            Button("Flight", "SasPrograde", "SAS: prograde", kb + "2");
            Button("Flight", "SasRetrograde", "SAS: retrograde", kb + "3");
            Button("Flight", "SasNormal", "SAS: normal", kb + "4");
            Button("Flight", "SasAntiNormal", "SAS: anti-normal", kb + "5");
            Button("Flight", "SasRadialOut", "SAS: radial out", kb + "6");
            Button("Flight", "SasRadialIn", "SAS: radial in", kb + "7");
            Button("Flight", "SasTarget", "SAS: target", kb + "8");
            Button("Flight", "SasAntiTarget", "SAS: anti-target", kb + "9");
            Button("Flight", "SasManeuver", "SAS: maneuver node", kb + "0");

            // ---- flight: view, vessels, time
            Button("Flight", "ToggleMap", "Toggle map view", kb + "m");
            Button("Flight", "CameraMode", "Camera mode (orbit / chase / locked)", kb + "v", gp + "dpad/up");
            Button("Flight", "PreviousVessel", "Switch to the previous nearby vessel", kb + "leftBracket");
            Button("Flight", "NextVessel", "Switch to the next nearby vessel", kb + "rightBracket");
            Button("Flight", "WarpFaster", "Time warp faster", kb + "period", gp + "dpad/right");
            Button("Flight", "WarpSlower", "Time warp slower", kb + "comma", gp + "dpad/left");
            Button("Flight", "WarpStop", "Stop time warp", kb + "slash", kb + "numpadDivide", gp + "dpad/down");

            // ---- flight: action groups (their effect is FLT-07)
            string[] digits = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            for (int i = 0; i < digits.Length; i++)
                Button("Flight", "ActionGroup" + (i + 1), "Action group " + (i + 1), "Alt+" + kb + digits[i]);

            // ---- flight camera (keyboard)
            Button("Camera", "OrbitLeft", "Camera: turn left", kb + "numpad4");
            Button("Camera", "OrbitRight", "Camera: turn right", kb + "numpad6");
            Button("Camera", "OrbitUp", "Camera: tilt up", kb + "numpad8");
            Button("Camera", "OrbitDown", "Camera: tilt down", kb + "numpad2");
            Button("Camera", "ZoomIn", "Camera: zoom in", kb + "pageUp");
            Button("Camera", "ZoomOut", "Camera: zoom out", kb + "pageDown");

            // ---- EVA
            Section = Eva;
            Button("Eva", "Forward", "Walk / jetpack forward (camera relative)", kb + "w");
            Button("Eva", "Back", "Walk / jetpack back", kb + "s");
            Button("Eva", "Left", "Walk / jetpack left", kb + "a");
            Button("Eva", "Right", "Walk / jetpack right", kb + "d");
            Button("Eva", "Up", "Run  (jetpack: up)", kb + "leftShift");
            Button("Eva", "Down", "Jetpack: down", kb + "leftCtrl");
            Button("Eva", "Jump", "Jump", kb + "space");
            Button("Eva", "Jetpack", "Toggle jetpack", kb + "r");
            Button("Eva", "Board", "Board nearby hatch (within 3 m, < 2.5 m/s)", kb + "b");
            Button("Eva", "PlantFlag", "Plant flag", kb + "g");

            // ---- map view
            Section = MapView;
            Button("Map", "CycleFocus", "Cycle focus", kb + "tab");
            Button("Map", "FocusActive", "Focus on the active vessel", kb + "backspace");
            Button("Map", "DeleteNode", "Delete the selected maneuver node", kb + "delete");

            // ---- assembly building
            Section = Assembly;
            Button("Assembly", "Undo", "Undo", "Ctrl+" + kb + "z");
            Button("Assembly", "Redo", "Redo", "Ctrl+" + kb + "y", "Ctrl+Shift+" + kb + "z");
            Button("Assembly", "SaveCraft", "Save craft", "Ctrl+" + kb + "s");
            Button("Assembly", "SymmetryUp", "Radial symmetry up (1, 2, 3, 4, 6, 8)", kb + "x");
            Button("Assembly", "SymmetryDown", "Radial symmetry down", "Shift+" + kb + "x");
            Button("Assembly", "AngleSnap", "Angle snap on / off", kb + "c");
            Button("Assembly", "FrameCraft", "Frame the craft", kb + "f", kb + "home");
            Button("Assembly", "Markers", "Show CoM / CoT / CoP markers", kb + "v");
            Button("Assembly", "RotatePitchUp", "Rotate the held part (away from you)", kb + "w");
            Button("Assembly", "RotatePitchDown", "Rotate the held part (towards you)", kb + "s");
            Button("Assembly", "RotateLeft", "Rotate the held part left", kb + "a");
            Button("Assembly", "RotateRight", "Rotate the held part right", kb + "d");
            Button("Assembly", "RotateRollLeft", "Roll the held part left", kb + "q");
            Button("Assembly", "RotateRollRight", "Roll the held part right", kb + "e");
            Button("Assembly", "CopyModifier", "Hold with a click to copy a placed part", kb + "alt");
            Button("Assembly", "FineRotation", "Hold for 5° rotation steps", kb + "leftShift");
            Button("Assembly", "ResetRotation", "Reset the held part's rotation", kb + "space");
            Button("Assembly", "Delete", "Delete the hovered or held part", kb + "delete", kb + "backspace");
            Button("EditorCamera", "HeightModifier", "Hold with the mouse wheel to move the camera up and down", kb + "shift");
            Button("EditorCamera", "OrbitLeft", "Camera: turn left", kb + "leftArrow");
            Button("EditorCamera", "OrbitRight", "Camera: turn right", kb + "rightArrow");
            Button("EditorCamera", "OrbitUp", "Camera: tilt up", kb + "upArrow");
            Button("EditorCamera", "OrbitDown", "Camera: tilt down", kb + "downArrow");
            Button("EditorCamera", "Raise", "Camera: up", kb + "pageUp");
            Button("EditorCamera", "Lower", "Camera: down", kb + "pageDown");
            Button("EditorCamera", "ZoomIn", "Camera: zoom in", kb + "equals", kb + "numpadPlus");
            Button("EditorCamera", "ZoomOut", "Camera: zoom out", kb + "minus", kb + "numpadMinus");

            // ---- developer
            Section = Developer;
            Button("Dev", "Window", "Developer tools: cheats, teleports, zoom speed", "Alt+" + kb + "f12", kb + "backquote");
            Button("Dev", "Overlay", "Developer overlay: raw simulation state", kb + "f3");
            Button("Dev", "ReloadContent", "Reload part and body data", kb + "f8");
        }

        private static string Section;

        private static void Button(string map, string name, string label, params string[] defaults) => Add(map, name, label, ControlKind.Button, defaults);
        private static void Axis(string map, string name, string label, params string[] defaults) => Add(map, name, label, ControlKind.Axis, defaults);

        private static void Add(string map, string name, string label, ControlKind kind, string[] defaults)
            => All.Add(new ControlDef { Id = map + "/" + name, Map = map, Name = name, Label = label, Kind = kind, Defaults = defaults, Section = Section });

        public static ControlDef Find(string id) => All.Find(c => c.Id == id);
    }
}
