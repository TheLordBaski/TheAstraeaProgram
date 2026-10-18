#ifndef TAP_TERRAIN_MORPH_INCLUDED
#define TAP_TERRAIN_MORPH_INCLUDED

// LOD morphing (FND-18). A terrain chunk replaces its parent once the parent's distance falls under its split distance (arc x
// _TerrainMorphSplit, see PlanetTerrain.Visit). Each vertex carries its point on the parent's surface, and the parent's normal and
// colour there (TerrainChunkGenerator), so a new chunk starts out as the parent looked and settles into its own shape over
// 90% to 60% of that distance: no pop when it appears, and none when it hands back. Its own children only appear under 50%, once it
// has settled. Local terrain only: the far view draws root chunks, which carry no parent (arc 0) and never morph.

float _TerrainMorphSplit; // global: SplitFactor x the player's terrain detail

// 1 = the parent's surface, 0 = the chunk's own. uv2.w is the parent's arc (m).
float TerrainMorphFactor(float3 positionOS, float4 uv2)
{
    float end = uv2.w * _TerrainMorphSplit;
    if (end <= 0) return 0;
    float d = length(TransformObjectToWorld(positionOS) - _WorldSpaceCameraPos);
    return saturate((d - 0.6 * end) / (0.3 * end));
}

float3 TerrainMorphPosition(float3 positionOS, float4 uv2, float k) { return positionOS + uv2.xyz * k; }

// The parent's normal, octahedral in uv3.xy (TerrainChunkGenerator.Octahedral).
float3 TerrainMorphNormal(float3 normalOS, float4 uv3, float k)
{
    float3 p = float3(uv3.xy, 1 - abs(uv3.x) - abs(uv3.y));
    float t = saturate(-p.z);
    p.xy -= (step(0, p.xy) * 2 - 1) * t;
    return normalize(lerp(normalOS, normalize(p), k));
}

// The parent's colour: RGB bytes packed as r + 256 g + 65536 b in uv3.z (exact in a float), the water flag in uv3.w.
float4 TerrainMorphColor(float4 color, float4 uv3, float k)
{
    float v = uv3.z;
    float b = floor(v / 65536);
    v -= b * 65536;
    float g = floor(v / 256);
    float4 parent = float4(float3(v - g * 256, g, b) / 255, uv3.w);
    return lerp(color, parent, k);
}

#endif
