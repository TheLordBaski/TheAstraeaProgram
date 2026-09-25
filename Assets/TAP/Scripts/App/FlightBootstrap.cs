using System;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAP.App
{
    /// <summary>Entry point of the Flight scene: builds the simulation, presentation and UI.</summary>
    public sealed class FlightBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            CommandLine.Apply();
            if (!GameSession.HasGame)
            {
                // Started directly from the Flight scene (editor testing): launch a starter rocket.
                // Automated runs use their own save so they never touch the player's games.
                GameSession.NewGame(string.IsNullOrEmpty(GameSession.AutoTest) ? "Sandbox" : "AutoTest");
                GameSession.PendingLaunch = string.IsNullOrEmpty(GameSession.AutoTest)
                    ? FindStarter("Luma Pathfinder")
                    : MissionAutopilot.CraftFor(GameSession.AutoTest);
                GameSession.Entry = FlightEntry.Launch;
            }
            var go = new GameObject("FlightScene");
            var ctrl = go.AddComponent<FlightSceneController>();
            PostFx.Create();
            ctrl.Build();
            go.AddComponent<DevOverlay>().Scene = ctrl;
            TAP.UI.FlightUI.Create(ctrl);
            if (!string.IsNullOrEmpty(GameSession.AutoTest))
                MissionAutopilot.Start(ctrl, GameSession.AutoTest, quitWhenDone: !Application.isEditor);
        }

        public static CraftDesign FindStarter(string name)
        {
            foreach (var c in SaveStorage.LoadStarterCraft()) if (c.name == name) return c;
            foreach (var c in StarterCraft.All(PartDatabase.Instance)) if (c.name == name) return c;
            return StarterCraft.Meridian(PartDatabase.Instance);
        }
    }

    /// <summary>Command line: -autotest &lt;orbit|lunar|suborbital|failures|persistence|ascent&gt; [-craft &lt;file or starter name&gt;] -report &lt;path&gt;</summary>
    public static class CommandLine
    {
        private static bool _applied;
        public static void Apply()
        {
            if (_applied) return;
            _applied = true;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autotest" && i + 1 < args.Length) GameSession.AutoTest = args[i + 1];
                if (args[i] == "-report" && i + 1 < args.Length) GameSession.AutoTestReportPath = args[i + 1];
                if (args[i] == "-craft" && i + 1 < args.Length) GameSession.AutoTestCraft = args[i + 1];
            }
            // Editor/automation hook: one-shot test request stored in PlayerPrefs.
            string pref = PlayerPrefs.GetString("TAP.AutoTest", "");
            if (!string.IsNullOrEmpty(pref))
            {
                GameSession.AutoTest = pref;
                PlayerPrefs.DeleteKey("TAP.AutoTest");
                string craft = PlayerPrefs.GetString("TAP.AutoTestCraft", "");
                if (!string.IsNullOrEmpty(craft)) GameSession.AutoTestCraft = craft;
                PlayerPrefs.DeleteKey("TAP.AutoTestCraft");
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>Global post-processing (bloom for plumes/sun, tonemapping, slight vignette).</summary>
    public static class PostFx
    {
        public static void Create()
        {
            var go = new GameObject("PostProcessing");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.15f);
            ca.contrast.Override(8f);
            ca.saturation.Override(6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.18f);
            vol.sharedProfile = profile;
        }
    }

    /// <summary>Developer overlay (F3): raw simulation state for debugging and verification.</summary>
    public sealed class DevOverlay : MonoBehaviour
    {
        public FlightSceneController Scene;
        public bool Show;
        private GUIStyle _style;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) Show = !Show;
        }

        private void OnGUI()
        {
            if (!Show || Scene == null || Scene.Sim == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            var sim = Scene.Sim;
            var v = sim.ActiveVessel;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"UT {sim.UT:F2}  warp {(sim.Warp.OnRails ? "rails x" + sim.Warp.Rate : "phys x" + sim.Warp.Rate)}  frame body {sim.Frame.Body.Name}");
            sb.AppendLine($"origin {sim.Frame.Origin.ToString("F1")}  |O| {sim.Frame.Origin.magnitude:F1}");
            sb.AppendLine($"frameVel {sim.Frame.Velocity.ToString("F2")}");
            sb.AppendLine($"loaded {sim.LoadedVessels.Count}  handles {sim.Handles.Count}  FPS {1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):F0}");
            if (v != null)
            {
                sb.AppendLine($"<b>{v.VesselName}</b> [{v.Kind}] {v.Situation} parts {v.Parts.Count} stage {v.CurrentStage}  hold {v.PadHold}  locked {v.KeplerLocked}");
                sb.AppendLine($"mass {v.TotalMass:F0} kg  alt {v.Altitude:F1} m  AGL {v.AltitudeAGL:F1}  vs {v.VerticalSpeed:F2}");
                sb.AppendLine($"surf {v.SurfaceSpeed:F2} m/s  orb {v.OrbitalSpeed:F2} m/s  mach {v.Mach:F2}  q {v.DynamicPressure / 1000:F2} kPa  g {v.GForce:F2}");
                sb.AppendLine($"unity pos {v.Rb.worldCenterOfMass.ToString("F2")}  vel {v.Rb.linearVelocity.ToString("F3")}");
                if (v.Orbit != null) sb.AppendLine($"orbit: Ap {(v.Orbit.ApoapsisRadius - v.MainBody.Radius) / 1000:F2} km  Pe {(v.Orbit.PeriapsisRadius - v.MainBody.Radius) / 1000:F2} km  e {v.Orbit.Eccentricity:F4}  i {v.Orbit.Inclination * MathD.Rad2Deg:F2}");
                sb.AppendLine($"throttle {v.Ctrl.Throttle:F2}  SAS {v.Ctrl.Sas} {v.Ctrl.SasMode}  cmd {v.Ctrl.TorqueCommand.ToString("F2")}  auth {v.TorqueAuthority.ToString("F0")}");
                foreach (var p in v.Parts)
                {
                    if (p.SkinTemp > 600 || p.SmoothedJointLoad > 0.3)
                        sb.AppendLine($"  {p.Def.id}: skin {p.SkinTemp:F0}K int {p.InternalTemp:F0}K load {p.SmoothedJointLoad:P0} {p.LastLoadDescription} exp {p.Exposure:F2}");
                }
            }
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(8, 120, 720, 16 + 17 * sb.ToString().Split('\n').Length), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(14, 124, 710, 600), sb.ToString(), _style);
        }
    }
}
