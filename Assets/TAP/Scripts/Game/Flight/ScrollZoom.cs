using UnityEngine;

namespace TAP.Game
{
    /// <summary>Mouse-wheel zoom shared by the flight, map and assembly-building cameras.</summary>
    public static class ScrollZoom
    {
        /// <summary>User zoom speed multiplier (settings, developer window), 1 = default; saved with the settings.</summary>
        public static float Speed
        {
            get => GameSettings.Current.gameplay.zoomSpeed;
            set { GameSettings.Current.gameplay.zoomSpeed = value; GameSettings.Commit(); }
        }

        /// <summary>
        /// Wheel notches in one frame's scroll reading. The Input System reports ±120 per notch in the Windows editor
        /// but ±1 in a player build (and fractions from precision touchpads); treating everything as 120-based made one
        /// notch a 0.1 step in the build, so zooming there was very slow.
        /// </summary>
        public static float Notches(float raw) => Mathf.Abs(raw) >= 10f ? raw / 120f : raw;

        /// <summary>Distance multiplier for a scroll reading: each notch scales the distance by <paramref name="perNotch"/>.</summary>
        public static float Factor(float raw, float perNotch) => Mathf.Pow(perNotch, Notches(raw) * Speed);
    }
}
