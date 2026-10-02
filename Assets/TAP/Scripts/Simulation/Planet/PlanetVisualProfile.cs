using UnityEngine;

namespace TAP.Simulation
{
    public enum WeatherSourceMode
    {
        /// <summary>Seeded regional banks, patches and fronts; an optional source image only textures their interiors.</summary>
        Regional,
        /// <summary>The source image is a whole-planet cloud map, rotated by the seed and suppressed over the poles.</summary>
        Global,
    }

    /// <summary>Art parameters only. Never changes a body's physics, terrain or atmosphere.</summary>
    [CreateAssetMenu(menuName = "TAP/Planet visual profile")]
    public sealed class PlanetVisualProfile : ScriptableObject
    {
        public string BodyId;
        public int Version = 1;
        public bool Lunar;
        public Texture2D Surface, SurfaceNormal, SurfaceMask, Weather, WeatherNormal;
        public Texture2D Transmittance, MultiScattering;
        [Header("Sky")]
        public Color Rayleigh = new Color(5.8e-6f, 13.5e-6f, 33.1e-6f, 1);
        public float RayleighHeight = 5600f;
        public float Mie = 12e-6f, MieHeight = 1400f, MieAnisotropy = 0.76f;
        public Color Ozone = new Color(0.65e-6f, 1.88e-6f, 0.085e-6f, 1);
        [Min(0), Tooltip("Brightness of scattered skylight relative to physically correct sunlight (1). Live.")]
        public float SkyBrightness = 1;
        [Header("Cloud weather")]
        public float CloudBase = 2800, CloudTop = 6500, CloudCoverage = 0.30f;
        [Tooltip("Extinction of near volumetric cloud at full density (1/m). Live.")]
        public float CloudDensity = 0.006f;
        public float WindSpeed = 18, CloudShadow = 0.35f;
        public int WeatherSeed = 424242;
        [Tooltip("Regional: seeded weather systems. Global: the source image is used as a whole-planet cloud map. Requires Bake weather only.")]
        public WeatherSourceMode WeatherMode = WeatherSourceMode.Regional;
        [Min(0), Tooltip("Number of large cloud banks across the whole planet (Regional). Change outside Play mode, then run TAP > Planet visuals > Bake weather only.")]
        public int LargeCloudBanks = 7;
        [Min(0), Tooltip("Number of smaller broken cloud patches across the whole planet (Regional). Requires Bake weather only.")]
        public int SmallCloudPatches = 8;
        [Min(0), Tooltip("Number of faint, wispy cloud fronts across the whole planet (Regional). Requires Bake weather only.")]
        public int ThinCloudFronts = 6;
        [Tooltip("Optional project-relative PNG/JPEG cloud source used only by the offline weather bake. Empty keeps procedural weather.")]
        public string WeatherSourceFile;
        [HideInInspector] public string WeatherSourceHash;
        [Min(0), Tooltip("Apparent cloud-top relief in metres, used to bake lighting normals for distant clouds. Requires Bake weather only.")]
        public float CloudRelief = 1400;
        [Range(0,1),Tooltip("Fine optical variation in texture clouds when individual billows are resolved. Fades with pixel footprint. Live.")]
        public float CloudLayerDetail = .5f;
        [Min(0), Tooltip("Vertical optical depth of a fully covered 2D cloud layer: ~5 thin stratus, ~20 dense cumulus. Live.")]
        public float CloudOpticalDepth = 20;
        [Min(500), Tooltip("Size of the noise that breaks layer clouds and shapes volumes into billows (m). Live.")]
        public float CloudBillowScale = 14000;
        [Header("Surface appearance")]
        [Range(0,1)] public float SurfaceSaturation = 1;
        public Color SurfaceTint = Color.white;
        [Range(.1f,3)] public float SurfaceNormalStrength = 1;
        [Range(0,1)] public float OceanColourBlend;
        [Tooltip("Deep water albedo (sRGB). Real open ocean is dark; the atmosphere adds the blue seen from orbit.")]
        public Color OceanColour = new Color(.15f,.31f,.47f,1);
        [Tooltip("Water over shallow shelves (sRGB), blending to the deep colour by 140 m depth.")]
        public Color OceanShallowColour = new Color(.18f,.55f,.58f,1);
        public float GroundDetail = 0.72f, OceanRoughness = 0.12f;
        [Range(0,1),Tooltip("Photographic material variation at regional/orbital distances. High and Ultra.")]
        public float RegionalDetail;
        [Min(1000)] public float RegionalDetailScale = 18000;
        [Tooltip("Paint the terrain's colour rules with real landscape textures (satellite exemplars) in an 8K orbital map. Requires Bake all.")]
        public bool SatelliteAlbedo;
        [Range(0,1),Tooltip("Satellite landscape detail between orbit and the ground materials. Live.")]
        public float SatelliteDetail = .7f;
        public const int SatelliteAlbedoWidth = 8192;
        public string TerrainHash;
        public string SurfaceBakeKey,WeatherBakeKey,AtmosphereBakeKey;
        public string ExpectedSurfaceKey(TAP.Core.CelestialBody body,int width=4096) => Key(3,Version,width,SatelliteAlbedo,body.Radius,body.HasOcean,body.HasAtmosphere,TAP.Core.TerrainFingerprint.Hash(body.Def.terrain));
        public string ExpectedWeatherKey() => Key(7,Version,4096,2048,WeatherSeed,CloudCoverage,(int)WeatherMode,Mathf.Max(0,LargeCloudBanks),Mathf.Max(0,SmallCloudPatches),Mathf.Max(0,ThinCloudFronts),WeatherSourceFile??"",WeatherSourceHash??"",CloudRelief);
        public string ExpectedAtmosphereKey(TAP.Core.CelestialBody body) => Key(3,Version,AtmosphereLuts.TransmittanceWidth,AtmosphereLuts.TransmittanceHeight,AtmosphereLuts.MultiSize,body.Radius,body.Atmosphere?.Height??0,Rayleigh.r,Rayleigh.g,Rayleigh.b,RayleighHeight,Mie,MieHeight,Ozone.r,Ozone.g,Ozone.b);
        static string Key(params object[] values)
        {
            var parts=System.Array.ConvertAll(values,v=>v is System.IFormattable f?f.ToString(null,System.Globalization.CultureInfo.InvariantCulture):v.ToString());
            return Hash128.Compute(string.Join("|",parts)).ToString();
        }

        public static PlanetVisualProfile Fallback(TAP.Core.CelestialBody body)
        {
            var p = CreateInstance<PlanetVisualProfile>();
            p.name = "Generated visuals " + body.Id;
            p.BodyId = body.Id;
            p.Lunar = !body.HasOcean && !body.HasAtmosphere;
            if (body.Atmosphere != null)
            {
                p.RayleighHeight = (float)body.Def.atmosphere.scaleHeight;
                // Scattering from the body's own air: optical depth scales with the air column (Earth: 1.225 kg/m3
                // over 8 km gives blue 0.265), tinted by the authored sky colour, so a thin dusty-red world keeps a
                // thin reddish sky instead of an Earth-blue one.
                double column = body.Atmosphere.SeaLevelDensity * Mathf.Max(1, p.RayleighHeight) / (1.225 * 8000);
                var sky = body.Def.atmosphere.skyColor;
                float r = sky != null && sky.Length >= 3 ? sky[0] : .32f, g = sky != null && sky.Length >= 3 ? sky[1] : .56f, b = sky != null && sky.Length >= 3 ? sky[2] : .95f;
                float peak = Mathf.Max(.05f, Mathf.Max(r, Mathf.Max(g, b)));
                float scale = (float)(.265 * column) / Mathf.Max(1, p.RayleighHeight) / peak;
                p.Rayleigh = new Color(r * scale, g * scale, b * scale, 1);
                p.Mie = (float)(6e-6 * column);
                p.CloudBase = Mathf.Min(2800, (float)body.Atmosphere.Height * 0.08f);
                p.CloudTop = Mathf.Min(6500, (float)body.Atmosphere.Height * 0.18f);
                // Custom bodies remain cloud-free until their author supplies a weather profile.
                p.CloudCoverage = 0;
            }
            else p.CloudCoverage = 0;
            return p;
        }

        /// <summary>
        /// Sky lookup tables computed on the spot for a body without a current bake, so custom worlds with air still
        /// get scattering. Returns false for airless bodies. The textures belong to the caller.
        /// </summary>
        public bool GenerateSkyTables(TAP.Core.CelestialBody body)
        {
            if (body.Atmosphere == null || body.Atmosphere.Height <= 0) return false;
            var a = AtmosphereLuts.Params.Of(body, this);
            var t = AtmosphereLuts.Transmittance(a);
            Transmittance = AtmosphereLuts.ToTexture(t, AtmosphereLuts.TransmittanceWidth, AtmosphereLuts.TransmittanceHeight, BodyId + " transmittance");
            MultiScattering = AtmosphereLuts.ToTexture(AtmosphereLuts.MultiScattering(a, t), AtmosphereLuts.MultiSize, AtmosphereLuts.MultiSize, BodyId + " multiple scattering");
            AtmosphereBakeKey = ExpectedAtmosphereKey(body);
            return true;
        }
    }
}
