#ifndef TAP_TERRAIN_MORPH_INCLUDED
#define TAP_TERRAIN_MORPH_INCLUDED

// LOD morphing (FND-18). A terrain chunk replaces its parent once the parent's distance falls under its split distance (arc x
// _TerrainMorphSplit, see PlanetTerrain.Visit). Each vertex carries its point on the parent's surface, and the parent's normal and
// colour there (TerrainChunkGenerator), so a new chunk starts out as the parent looked and settles into its own shape over
// 90% to 60% of that distance: no pop when it appears, and none when it hands back. Its own children only appear under 50%, once it
// has settled. Only PlanetTerrain's chunks carry that data, and only their material has _LodMorph = 1: a mesh without UV2/UV3 does
// not read them as zero (on D3D12 they hold another stream's data), so with _LodMorph = 0 they are never used, not even times 0.

float _TerrainMorphSplit; // global: SplitFactor x the player's terrain detail

// The vertex data (TerrainVertex, FND-21): uv2 = the move to the parent's surface (m) and the parent's arc (km; 0 for a root chunk),
// uv3 = the parent's normal (octahedral), uv4 = the parent's colour with the water flag.

// 1 = the parent's surface, 0 = the chunk's own.
float TerrainMorphFactor(float3 positionOS, float4 uv2)
{
    if (_LodMorph < 0.5) return 0;
    float end = uv2.w * 1000 * _TerrainMorphSplit;
    if (!(end > 0)) return 0;
    float d = length(TransformObjectToWorld(positionOS) - _WorldSpaceCameraPos);
    return saturate((d - 0.6 * end) / (0.3 * end));
}

// The point (or detail texture coordinate) moved towards the parent's surface: uv2.xyz is the whole move.
float3 TerrainMorphPosition(float3 p, float4 uv2, float k) { return k > 0 ? p + uv2.xyz * k : p; }

// The parent's normal, octahedral (ChunkGeometry.Octahedral).
float3 TerrainMorphNormal(float3 normalOS, float2 uv3, float k)
{
    if (k <= 0) return normalOS;
    float3 p = float3(uv3, 1 - abs(uv3.x) - abs(uv3.y));
    float t = saturate(-p.z);
    p.xy -= (step(0, p.xy) * 2 - 1) * t;
    return normalize(lerp(normalOS, normalize(p), k));
}

// The parent's colour and water flag.
float4 TerrainMorphColor(float4 color, float4 uv4, float k) { return k > 0 ? lerp(color, uv4, k) : color; }

#endif
