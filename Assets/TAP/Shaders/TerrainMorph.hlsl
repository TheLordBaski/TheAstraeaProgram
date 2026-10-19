#ifndef TAP_TERRAIN_MORPH_INCLUDED
#define TAP_TERRAIN_MORPH_INCLUDED

// LOD morphing (FND-18). A terrain chunk replaces its parent once the parent's distance falls under its split distance (arc x
// _TerrainMorphSplit, see PlanetTerrain.Visit). Each vertex carries its point on the parent's surface, and the parent's normal and
// colour there (TerrainChunkGenerator), so a new chunk starts out as the parent looked and settles into its own shape over
// 90% to 60% of that distance: no pop when it appears, and none when it hands back. Its own children only appear under 50%, once it
// has settled. Only PlanetTerrain's chunks carry that data, and only their material has _LodMorph = 1: a mesh without UV2/UV3 does
// not read them as zero (on D3D12 they hold another stream's data), so with _LodMorph = 0 they are never used, not even times 0.

float _TerrainMorphSplit; // global: SplitFactor x the player's terrain detail

// 1 = the parent's surface, 0 = the chunk's own. uv2.w is the parent's arc (m); 0 for a root chunk.
float TerrainMorphFactor(float3 positionOS, float4 uv2)
{
    if (_LodMorph < 0.5) return 0;
    float end = uv2.w * _TerrainMorphSplit;
    if (!(end > 0)) return 0;
    float d = length(TransformObjectToWorld(positionOS) - _WorldSpaceCameraPos);
    return saturate((d - 0.6 * end) / (0.3 * end));
}

// The point (or detail texture coordinate) moved towards the parent's surface: uv2.xyz is the whole move.
float3 TerrainMorphPosition(float3 p, float4 uv2, float k) { return k > 0 ? p + uv2.xyz * k : p; }

// The parent's normal, octahedral in uv3.xy (TerrainChunkGenerator.Octahedral).
float3 TerrainMorphNormal(float3 normalOS, float4 uv3, float k)
{
    if (k <= 0) return normalOS;
    float3 p = float3(uv3.xy, 1 - abs(uv3.x) - abs(uv3.y));
    float t = saturate(-p.z);
    p.xy -= (step(0, p.xy) * 2 - 1) * t;
    return normalize(lerp(normalOS, normalize(p), k));
}

// The parent's colour: RGB bytes packed as r + 256 g + 65536 b in uv3.z (exact in a float), the water flag in uv3.w.
float4 TerrainMorphColor(float4 color, float4 uv3, float k)
{
    if (k <= 0) return color;
    float v = uv3.z;
    float b = floor(v / 65536);
    v -= b * 65536;
    float g = floor(v / 256);
    float4 parent = float4(float3(v - g * 256, g, b) / 255, uv3.w);
    return lerp(color, parent, k);
}

#endif
