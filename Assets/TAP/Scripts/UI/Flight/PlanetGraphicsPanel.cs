using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>Planet graphics settings (quality, clouds, surface rocks), opened from the pause menu.</summary>
    public sealed class PlanetGraphicsPanel : MonoBehaviour
    {
        private Button _preset, _clouds, _scatter;

        /// <param name="panel">The pause menu's "planet-graphics" dialog.</param>
        public static PlanetGraphicsPanel Bind(GameObject host, VisualElement panel)
        {
            var ui = host.AddComponent<PlanetGraphicsPanel>();
            ui._preset = panel.Q<Button>("quality");
            ui._preset.clicked += () => PlanetGraphics.SetQuality((PlanetQuality)(((int)PlanetGraphics.Quality + 1) % 4));
            ui._clouds = panel.Q<Button>("clouds");
            ui._clouds.clicked += () => PlanetGraphics.SetClouds((PlanetCloudMode)(((int)PlanetGraphics.Clouds + 1) % 3));
            ui._scatter = panel.Q<Button>("scatter");
            ui._scatter.clicked += () => PlanetGraphics.SetScatter(!PlanetGraphics.Scatter);
            panel.Q<Button>("graphics-back").clicked += () => UIKit.Show(panel, false);
            PlanetGraphics.Changed += ui.Refresh;
            ui.Refresh();
            return ui;
        }

        private void Refresh()
        {
            _preset.text = "Quality: " + PlanetGraphics.Quality;
            _clouds.text = "Clouds: " + PlanetGraphics.Clouds;
            _scatter.text = "Surface rocks: " + (PlanetGraphics.Scatter ? "On" : "Off");
        }

        private void OnDestroy() { PlanetGraphics.Changed -= Refresh; }
    }
}
