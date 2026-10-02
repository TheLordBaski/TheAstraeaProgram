using System;
using UnityEngine;

namespace TAP.Game
{
    public enum PlanetQuality { Low, Medium, High, Ultra }
    public enum PlanetCloudMode { Off, Layers, Volumetric }

    /// <summary>Presentation preferences deliberately independent of game saves and simulation time.</summary>
    public static class PlanetGraphics
    {
        public static event Action Changed;
        public static PlanetQuality Quality { get; private set; } = (PlanetQuality)Mathf.Clamp(PlayerPrefs.GetInt("TAP.PlanetQuality", 2), 0, 3);
        public static PlanetCloudMode Clouds { get; private set; } = (PlanetCloudMode)Mathf.Clamp(PlayerPrefs.GetInt("TAP.PlanetClouds", 2), 0, 2);
        public static bool Scatter { get; private set; } = PlayerPrefs.GetInt("TAP.PlanetScatter", 1) != 0;
        public static int CloudSteps => Quality == PlanetQuality.Ultra ? 72 : Quality == PlanetQuality.High ? 48 : 32;
        // Samples concentrate at the limb, ground or camera, so a few suffice; Low still renders the full sky model.
        public static int AtmosphereSteps => Quality == PlanetQuality.Ultra ? 24 : Quality == PlanetQuality.High ? 16 : Quality == PlanetQuality.Medium ? 12 : 10;
        public static int TextureLimit => Quality == PlanetQuality.Low ? 1 : 0;

        public static void SetQuality(PlanetQuality quality)
        {
            Quality = quality;
            Clouds = quality >= PlanetQuality.High ? PlanetCloudMode.Volumetric : PlanetCloudMode.Layers;
            Save();
        }

        public static void SetClouds(PlanetCloudMode clouds) { Clouds = clouds; Save(); }
        public static void SetScatter(bool enabled) { Scatter = enabled; Save(); }
        private static void Save()
        {
            PlayerPrefs.SetInt("TAP.PlanetQuality", (int)Quality);
            PlayerPrefs.SetInt("TAP.PlanetClouds", (int)Clouds);
            PlayerPrefs.SetInt("TAP.PlanetScatter", Scatter ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
