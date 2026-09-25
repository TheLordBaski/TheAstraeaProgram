using UnityEngine;

namespace TAP.Game
{
    /// <summary>Mouse-wheel zoom shared by the flight, map and assembly-building cameras.</summary>
    public static class ScrollZoom
    {
        private const string PrefKey = "TAP.ZoomSpeed";
        private static float _speed = -1f;

        /// <summary>User zoom speed multiplier (developer window), 1 = default; saved between sessions.</summary>
        public static float Speed
        {
            get
            {
                if (_speed < 0f) _speed = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, 1f), 0.25f, 4f);
                return _speed;
            }
            set
            {
                _speed = Mathf.Clamp(value, 0.25f, 4f);
                PlayerPrefs.SetFloat(PrefKey, _speed);
                PlayerPrefs.Save();
            }
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
