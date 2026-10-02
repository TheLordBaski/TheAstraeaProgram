using UnityEngine;
namespace TAP.Simulation
{
    [CreateAssetMenu(menuName = "TAP/Planet material library")]
    public sealed class PlanetVisualLibrary : ScriptableObject
    {
        public Texture2DArray Albedo, Normal, Material;
        /// <summary>128-cubed cloud shape noise: Perlin-Worley in R, Worley octaves in GBA (tiling).</summary>
        public Texture3D CloudNoise;
        /// <summary>32-cubed cloud erosion noise: Worley octaves in RGB (tiling).</summary>
        public Texture3D CloudDetail;
        /// <summary>Landscape texture ratios per satellite class (forest, grass, steppe, scrub, sand, rock).</summary>
        public Texture2DArray SatelliteDetail;
        public Texture2D TellusStone, LumaStone;
    }
}
