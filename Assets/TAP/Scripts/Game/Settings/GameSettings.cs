using System;
using System.IO;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    public enum WindowMode { Fullscreen, Borderless, Windowed }
    public enum ShadowSetting { Off, Low, Medium, High }
    public enum AntiAliasing { Off, X2, X4, X8 }
    public enum GraphicsPreset { Low, Medium, High, Ultra, Custom }
    public enum UnitSystem { Metric, Imperial }
    public enum AudioBus { Music, Effects, Interface, Ambience }

    [Serializable]
    public class GraphicsOptions
    {
        public int width = 1920, height = 1080;
        public WindowMode windowMode = WindowMode.Windowed;
        public bool vsync = true;
        /// <summary>Frames per second the game stops at; 0 = no cap. Ignored while vsync is on.</summary>
        public int frameCap;
        public GraphicsPreset preset = GraphicsPreset.High;
        public ShadowSetting shadows = ShadowSetting.High;
        public AntiAliasing antiAliasing = AntiAliasing.X4;
        public float renderScale = 1f;
        /// <summary>How soon the terrain splits into finer chunks, 0 (coarse) to 3 (fine).</summary>
        public int terrainDetail = 2;
        /// <summary>Surface rocks around the camera: 0 off, 1 to 3 more.</summary>
        public int scatterDensity = 2;
        public PlanetQuality planetQuality = PlanetQuality.High;
        public PlanetCloudMode clouds = PlanetCloudMode.Volumetric;

        /// <summary>The settings a preset stands for (everything below the window and frame rate).</summary>
        public void ApplyPreset(GraphicsPreset p)
        {
            if (p == GraphicsPreset.Custom) { preset = p; return; }
            preset = p;
            switch (p)
            {
                case GraphicsPreset.Low: Set(ShadowSetting.Low, AntiAliasing.Off, 0.75f, 0, 1, PlanetQuality.Low); break;
                case GraphicsPreset.Medium: Set(ShadowSetting.Medium, AntiAliasing.X2, 1f, 1, 2, PlanetQuality.Medium); break;
                case GraphicsPreset.High: Set(ShadowSetting.High, AntiAliasing.X4, 1f, 2, 2, PlanetQuality.High); break;
                default: Set(ShadowSetting.High, AntiAliasing.X8, 1f, 3, 3, PlanetQuality.Ultra); break;
            }
        }

        private void Set(ShadowSetting s, AntiAliasing aa, float scale, int terrain, int scatter, PlanetQuality planet)
        {
            shadows = s; antiAliasing = aa; renderScale = scale; terrainDetail = terrain; scatterDensity = scatter;
            planetQuality = planet;
            clouds = planet >= PlanetQuality.High ? PlanetCloudMode.Volumetric : PlanetCloudMode.Layers;
        }

        /// <summary>After a single setting changed: the preset the settings still match, or Custom.</summary>
        public void MatchPreset()
        {
            foreach (var p in new[] { GraphicsPreset.Low, GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra })
            {
                var t = (GraphicsOptions)MemberwiseClone();
                t.ApplyPreset(p);
                if (t.shadows == shadows && t.antiAliasing == antiAliasing && Mathf.Approximately(t.renderScale, renderScale)
                    && t.terrainDetail == terrainDetail && t.scatterDensity == scatterDensity && t.planetQuality == planetQuality && t.clouds == clouds)
                {
                    preset = p;
                    return;
                }
            }
            preset = GraphicsPreset.Custom;
        }
    }

    [Serializable]
    public class AudioOptions
    {
        public float master = 1f, music = 0.7f, effects = 1f, interfaceSounds = 1f, ambience = 0.8f;

        public float Volume(AudioBus bus)
        {
            float v = bus == AudioBus.Music ? music : bus == AudioBus.Effects ? effects : bus == AudioBus.Interface ? interfaceSounds : ambience;
            return v * master;
        }
    }

    [Serializable]
    public class GameplayOptions
    {
        /// <summary>Minutes between autosaves in flight; 0 = off.</summary>
        public int autosaveMinutes = 5;
        public bool tooltips = true;
        public float zoomSpeed = 1f;
        public UnitSystem units = UnitSystem.Metric;
    }

    [Serializable]
    public class InterfaceOptions
    {
        public float uiScale = 1f, hudScale = 1f;
    }

    /// <summary>A stick, twist or lever: how much of its travel to ignore, how strongly it acts, and which way it points.</summary>
    [Serializable]
    public class AxisOptions
    {
        public float deadzone = 0.1f, sensitivity = 1f;
        public bool invert;

        /// <summary>The reading (-1 to 1) after the dead zone is cut out and the rest stretched back to full range.</summary>
        public float Apply(float v)
        {
            float a = Mathf.Abs(v);
            if (a <= deadzone) return 0f;
            float stretched = deadzone >= 1f ? 0f : (a - deadzone) / (1f - deadzone);
            return Mathf.Clamp(Mathf.Sign(v) * stretched * sensitivity, -1f, 1f) * (invert ? -1f : 1f);
        }
    }

    [Serializable]
    public class InputOptions
    {
        /// <summary>Per axis ("PitchAxis", "YawAxis", "RollAxis", "ThrottleAxis"); an axis not listed has the defaults.</summary>
        public System.Collections.Generic.Dictionary<string, AxisOptions> axes = new System.Collections.Generic.Dictionary<string, AxisOptions>();
        /// <summary>A throttle lever (HOTAS) sets the throttle where it stands; off, it changes the throttle like the keys do.</summary>
        public bool throttleLever;

        public AxisOptions Axis(string name)
        {
            if (!axes.TryGetValue(name, out var a)) axes[name] = a = new AxisOptions { deadzone = name == "ThrottleAxis" ? 0.02f : 0.1f };
            return a;
        }
    }

    /// <summary>
    /// The player's settings (FND-08): one JSON file in the user folder next to the saves, never the registry. <see cref="Current"/>
    /// is what the game reads; <see cref="Commit"/> applies a change and schedules the write. Command-line overrides
    /// (<c>-safe-mode</c>, <c>-screen-width</c>, <c>-screen-height</c>, <c>-window-mode</c>) last for the session and are not saved.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public int version = 1;
        public GraphicsOptions graphics = new GraphicsOptions();
        public AudioOptions audio = new AudioOptions();
        public GameplayOptions gameplay = new GameplayOptions();
        public InterfaceOptions ui = new InterfaceOptions();
        public InputOptions input = new InputOptions();

        public const string FileName = "settings.json";
        public static string FilePath => System.IO.Path.Combine(SaveStorage.Root, FileName);

        /// <summary>The refresh rate and sizes below which the window is clamped.</summary>
        public const int MinWidth = 640, MinHeight = 360;

        private static GameSettings _current;
        private static GameSettings _disk;
        private static bool _safeMode, _resolutionOverride;

        /// <summary>What the game is using. Outside play (tests, tools) it is the defaults, so nothing reads a player's file by accident.</summary>
        public static GameSettings Current
        {
            get
            {
                if (_current == null) { if (Application.isPlaying) Load(); else _current = new GameSettings(); }
                return _current;
            }
        }

        public static bool SafeMode => _safeMode;
        public static event Action Changed;
        /// <summary>Set by <see cref="Commit"/>; the runner writes the file shortly after the last change.</summary>
        public static float DirtySince = -1f;

        // ------------------------------------------------------------------ load and save

        /// <summary>Reads the file (a first run migrates the old PlayerPrefs, a damaged file is kept aside) and applies the command line.</summary>
        public static void Load(string[] args = null)
        {
            GameSettings disk = null;
            string path = FilePath;
            if (File.Exists(path))
            {
                try { disk = SaveStorage.FromJson<GameSettings>(File.ReadAllText(path)); }
                catch (Exception e)
                {
                    Debug.LogWarning($"Settings file {path} is damaged ({e.Message}); starting from the defaults. The file is kept as {FileName}.damaged.");
                    try { File.Copy(path, path + ".damaged", true); } catch { /* the notice above is enough */ }
                }
            }
            else
            {
                disk = new GameSettings();
                if (disk.MigratePlayerPrefs())
                {
                    try { Directory.CreateDirectory(Path.GetDirectoryName(path)); SaveStorage.WriteAtomic(path, SaveStorage.ToJson(disk)); }
                    catch (Exception e) { Debug.LogWarning("Could not write the settings file: " + e.Message); }
                }
            }
            disk = disk ?? new GameSettings();
            disk.Sanitize();
            _disk = disk;
            _current = Clone(disk);
            _safeMode = _resolutionOverride = false;
            ApplyCommandLine(_current, args ?? Environment.GetCommandLineArgs());
            _current.Sanitize();
        }

        /// <summary>Writes the file now. Not under <c>-safe-mode</c>, and a window size from the command line is not kept.</summary>
        public static void Save()
        {
            DirtySince = -1f;
            if (_safeMode || _current == null) return;
            var write = Clone(_current);
            if (_resolutionOverride)
            {
                write.graphics.width = _disk.graphics.width;
                write.graphics.height = _disk.graphics.height;
                write.graphics.windowMode = _disk.graphics.windowMode;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                SaveStorage.WriteAtomic(FilePath, SaveStorage.ToJson(write));
                _disk = write;
            }
            catch (Exception e) { Debug.LogError("Could not save the settings: " + e.Message); }
        }

        /// <summary>A setting changed: keep it sane, apply it, tell the screens, and write the file soon.</summary>
        public static void Commit()
        {
            Current.Sanitize();
            DirtySince = Time.unscaledTime;
            SettingsApply.Apply(Current);
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            _current = new GameSettings();
            if (_safeMode) ApplyCommandLine(_current, Environment.GetCommandLineArgs());
            Commit();
        }

        /// <summary>For tests: forget everything loaded.</summary>
        public static void Forget() { _current = _disk = null; _safeMode = _resolutionOverride = false; DirtySince = -1f; }

        private static GameSettings Clone(GameSettings s) => SaveStorage.FromJson<GameSettings>(SaveStorage.ToJson(s));

        // ------------------------------------------------------------------ rules

        public void Sanitize()
        {
            graphics = graphics ?? new GraphicsOptions(); audio = audio ?? new AudioOptions();
            gameplay = gameplay ?? new GameplayOptions(); ui = ui ?? new InterfaceOptions(); input = input ?? new InputOptions();
            foreach (var a in input.axes.Values) { a.deadzone = Mathf.Clamp(a.deadzone, 0f, 0.9f); a.sensitivity = Mathf.Clamp(a.sensitivity, 0.1f, 3f); }
            graphics.width = Mathf.Clamp(graphics.width, MinWidth, 16384);
            graphics.height = Mathf.Clamp(graphics.height, MinHeight, 16384);
            graphics.frameCap = graphics.frameCap <= 0 ? 0 : Mathf.Clamp(graphics.frameCap, 10, 1000);
            graphics.renderScale = Mathf.Clamp(graphics.renderScale, 0.5f, 1.5f);
            graphics.terrainDetail = Mathf.Clamp(graphics.terrainDetail, 0, 3);
            graphics.scatterDensity = Mathf.Clamp(graphics.scatterDensity, 0, 3);
            audio.master = Mathf.Clamp01(audio.master); audio.music = Mathf.Clamp01(audio.music); audio.effects = Mathf.Clamp01(audio.effects);
            audio.interfaceSounds = Mathf.Clamp01(audio.interfaceSounds); audio.ambience = Mathf.Clamp01(audio.ambience);
            gameplay.autosaveMinutes = Mathf.Clamp(gameplay.autosaveMinutes, 0, 60);
            gameplay.zoomSpeed = Mathf.Clamp(gameplay.zoomSpeed, 0.25f, 4f);
            ui.uiScale = Mathf.Clamp(ui.uiScale, 0.75f, 1.5f);
            ui.hudScale = Mathf.Clamp(ui.hudScale, 0.75f, 1.3f);
        }

        /// <summary>
        /// First run after the settings moved out of PlayerPrefs (the registry on Windows): takes the old values over and
        /// removes the keys. Returns whether there was anything to take.
        /// </summary>
        private bool MigratePlayerPrefs()
        {
            bool any = false;
            if (PlayerPrefs.HasKey("TAP.ZoomSpeed")) { gameplay.zoomSpeed = PlayerPrefs.GetFloat("TAP.ZoomSpeed", 1f); any = true; }
            if (PlayerPrefs.HasKey("TAP.AutosaveMinutes")) { gameplay.autosaveMinutes = PlayerPrefs.GetInt("TAP.AutosaveMinutes", 5); any = true; }
            if (PlayerPrefs.HasKey("TAP.PlanetQuality")) { graphics.planetQuality = (PlanetQuality)Mathf.Clamp(PlayerPrefs.GetInt("TAP.PlanetQuality", 2), 0, 3); any = true; }
            if (PlayerPrefs.HasKey("TAP.PlanetClouds")) { graphics.clouds = (PlanetCloudMode)Mathf.Clamp(PlayerPrefs.GetInt("TAP.PlanetClouds", 2), 0, 2); any = true; }
            if (PlayerPrefs.HasKey("TAP.PlanetScatter")) { graphics.scatterDensity = PlayerPrefs.GetInt("TAP.PlanetScatter", 1) != 0 ? 2 : 0; any = true; }
            if (!any) return false;
            graphics.MatchPreset();
            foreach (var key in new[] { "TAP.ZoomSpeed", "TAP.AutosaveMinutes", "TAP.PlanetQuality", "TAP.PlanetClouds", "TAP.PlanetScatter" }) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return true;
        }

        // ------------------------------------------------------------------ command line

        private static void ApplyCommandLine(GameSettings s, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i], next = i + 1 < args.Length ? args[i + 1] : null;
                if (a == "-safe-mode")
                {
                    // Runs on any machine: a small window, low quality, and nothing written back.
                    _safeMode = true;
                    s.graphics.width = 1280; s.graphics.height = 720; s.graphics.windowMode = WindowMode.Windowed;
                    s.graphics.vsync = true; s.graphics.frameCap = 0;
                    s.graphics.ApplyPreset(GraphicsPreset.Low);
                }
                else if (a == "-screen-width" && int.TryParse(next, out int w)) { s.graphics.width = w; _resolutionOverride = true; }
                else if (a == "-screen-height" && int.TryParse(next, out int h)) { s.graphics.height = h; _resolutionOverride = true; }
                else if (a == "-window-mode" && next != null && Enum.TryParse(next, true, out WindowMode m)) { s.graphics.windowMode = m; _resolutionOverride = true; }
                else if (a == "-screen-fullscreen" && next != null) { s.graphics.windowMode = next == "0" ? WindowMode.Windowed : WindowMode.Fullscreen; _resolutionOverride = true; }
            }
        }
    }
}
