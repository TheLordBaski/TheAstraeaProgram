using System;
using UnityEngine;

namespace TAP.Game
{
    public enum PlanetQuality { Low, Medium, High, Ultra }
    public enum PlanetCloudMode { Off, Layers, Volumetric }

    /// <summary>Presentation preferences deliberately independent of game saves and simulation time. They live in <see cref="GameSettings"/>.</summary>
    public static class PlanetGraphics
    {
        public static event Action Changed;
        public static PlanetQuality Quality => GameSettings.Current.graphics.planetQuality;
        public static PlanetCloudMode Clouds => GameSettings.Current.graphics.clouds;
        public static bool Scatter => GameSettings.Current.graphics.scatterDensity > 0;
        /// <summary>How far out the surface rocks reach, in grid cells around the camera.</summary>
        public static int ScatterExtent => new[] { 0, 5, 8, 12 }[GameSettings.Current.graphics.scatterDensity];
        public static int CloudSteps => Quality == PlanetQuality.Ultra ? 72 : Quality == PlanetQuality.High ? 48 : 32;
        // Samples concentrate at the limb, ground or camera, so a few suffice; Low still renders the full sky model.
        public static int AtmosphereSteps => Quality == PlanetQuality.Ultra ? 24 : Quality == PlanetQuality.High ? 16 : Quality == PlanetQuality.Medium ? 12 : 10;
        public static int TextureLimit => Quality == PlanetQuality.Low ? 1 : 0;

        static PlanetGraphics() { GameSettings.Changed += () => Changed?.Invoke(); }

        public static void SetQuality(PlanetQuality quality)
        {
            var g = GameSettings.Current.graphics;
            g.planetQuality = quality;
            g.clouds = quality >= PlanetQuality.High ? PlanetCloudMode.Volumetric : PlanetCloudMode.Layers;
            g.MatchPreset();
            GameSettings.Commit();
        }

        public static void SetClouds(PlanetCloudMode clouds)
        {
            var g = GameSettings.Current.graphics;
            g.clouds = clouds;
            g.MatchPreset();
            GameSettings.Commit();
        }
    }
}
