using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Limits of the assembly building, edited in the Unity editor on Assets/TAP/Resources/Settings/AssemblyBuilding.asset
    /// (menu TAP → Assembly Building Settings). Building upgrades will raise them later.
    /// </summary>
    public sealed class AssemblyBuildingSettings : ScriptableObject
    {
        public const string ResourcePath = "Settings/AssemblyBuilding";

        [Tooltip("Working height in metres above the floor: a craft can be raised until its top reaches it. A taller craft " +
                 "stands on the floor and the engineer's report says so. The hall itself is 64 m high.")]
        [Min(5f)]
        public float maxCraftHeight = 60f;

        /// <summary>The settings asset, or the defaults when it is missing.</summary>
        public static AssemblyBuildingSettings Load()
        {
            var s = Resources.Load<AssemblyBuildingSettings>(ResourcePath);
            return s != null ? s : CreateInstance<AssemblyBuildingSettings>();
        }
    }
}
