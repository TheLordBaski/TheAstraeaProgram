using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAP.Game
{
    /// <summary>
    /// Puts the settings to work (FND-08): window, frame rate, the render pipeline, audio and the game's own detail levels.
    /// Runs at start and after every change; each part acts only when its own values changed. Writes the file a moment
    /// after the last change and when the game ends.
    /// </summary>
    public sealed class SettingsApply : MonoBehaviour
    {
        private static (int w, int h, FullScreenMode mode) _window;
        private static UniversalRenderPipelineAsset _pipeline;
        private static RenderPipelineAsset _original;
        private static int _originalVSync = -1, _originalAA;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            GameSettings.Forget();
            _window = default;
            _pipeline = null;
            GameSettings.Load();
            Apply(GameSettings.Current);
            var go = new GameObject("Settings");
            DontDestroyOnLoad(go);
            go.AddComponent<SettingsApply>();
            var g = GameSettings.Current.graphics;
            Debug.Log($"[Settings] {g.width}x{g.height} {g.windowMode}, preset {g.preset}, vsync {g.vsync}{(GameSettings.SafeMode ? ", safe mode: not saved" : "")}");
        }

        private System.Collections.IEnumerator Start()
        {
            // The window settles a frame or two after SetResolution; this is the size the player actually got.
            yield return null; yield return null; yield return null;
            Debug.Log($"[Settings] window is {Screen.width}x{Screen.height} {Screen.fullScreenMode}");
        }

        public static void Apply(GameSettings s)
        {
            var g = s.graphics;
            ApplyWindow(g);
            if (_originalVSync < 0) { _originalVSync = QualitySettings.vSyncCount; _originalAA = QualitySettings.antiAliasing; }
            QualitySettings.vSyncCount = g.vsync ? 1 : 0;
            Application.targetFrameRate = g.frameCap > 0 ? g.frameCap : -1;
            ApplyPipeline(g);
            PlanetTerrain.DetailScale = new[] { 1.4f, 1.8f, 2.2f, 3f }[g.terrainDetail] / 2.2f;
            AudioListener.volume = s.audio.master;
            SaveStorage.AutosaveMinutes = s.gameplay.autosaveMinutes;
        }

        private static void ApplyWindow(GraphicsOptions g)
        {
            if (Application.isEditor) return; // the editor's game view has no window of its own
            var mode = g.windowMode == WindowMode.Fullscreen ? FullScreenMode.ExclusiveFullScreen
                : g.windowMode == WindowMode.Borderless ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            int w = g.width, h = g.height;
            if (mode == FullScreenMode.FullScreenWindow) { w = Display.main.systemWidth; h = Display.main.systemHeight; }
            else if (mode == FullScreenMode.Windowed)
            {
                // A window bigger than the screen is cut down to it.
                var screen = Screen.currentResolution;
                if (w > screen.width || h > screen.height) { float k = Mathf.Min((float)screen.width / w, (float)screen.height / h); w = (int)(w * k); h = (int)(h * k); }
            }
            var want = (w, h, mode);
            if (want == _window) return;
            _window = want;
            Screen.SetResolution(w, h, mode);
        }

        /// <summary>
        /// Render scale, anti-aliasing and shadows live in the pipeline asset. A copy is edited, so the asset in the project
        /// (which the editor would keep) stays as it is.
        /// </summary>
        private static void ApplyPipeline(GraphicsOptions g)
        {
            if (_pipeline == null)
            {
                var source = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (source == null) return;
                _original = QualitySettings.renderPipeline;
                _pipeline = Instantiate(source);
                _pipeline.name = source.name + " (settings)";
                QualitySettings.renderPipeline = _pipeline;
            }
            _pipeline.renderScale = g.renderScale;
            _pipeline.msaaSampleCount = g.antiAliasing == AntiAliasing.Off ? 1 : g.antiAliasing == AntiAliasing.X2 ? 2 : g.antiAliasing == AntiAliasing.X4 ? 4 : 8;
            switch (g.shadows)
            {
                case ShadowSetting.Off: _pipeline.shadowDistance = 0f; break;
                case ShadowSetting.Low: _pipeline.shadowDistance = 120f; _pipeline.mainLightShadowmapResolution = 1024; _pipeline.shadowCascadeCount = 2; break;
                case ShadowSetting.Medium: _pipeline.shadowDistance = 250f; _pipeline.mainLightShadowmapResolution = 2048; _pipeline.shadowCascadeCount = 3; break;
                default: _pipeline.shadowDistance = 350f; _pipeline.mainLightShadowmapResolution = 2048; _pipeline.shadowCascadeCount = 4; break;
            }
        }

        private void Update()
        {
            if (GameSettings.DirtySince >= 0 && Time.unscaledTime - GameSettings.DirtySince > 0.6f) GameSettings.Save();
        }

        private void OnApplicationQuit()
        {
            if (GameSettings.DirtySince >= 0) GameSettings.Save();
            // The editor keeps what a script sets; leave its project as it was.
            if (Application.isEditor)
            {
                if (_original != null || _pipeline != null) QualitySettings.renderPipeline = _original;
                if (_originalVSync >= 0) { QualitySettings.vSyncCount = _originalVSync; QualitySettings.antiAliasing = _originalAA; }
                Application.targetFrameRate = -1;
            }
        }
    }
}
