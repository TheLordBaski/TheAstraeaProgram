#ifndef TAP_PLANET_ATMOSPHERE
#define TAP_PLANET_ATMOSPHERE
// Shared physically based sky model: Rayleigh/Mie/ozone single scattering with a baked multiple-scattering term
// (Hillaire 2020), lit by sunlight from a transmittance lookup (Bruneton's parametrisation).
// Distances are metres relative to the body centre. Irradiance is in the renderer's light units times pi, so a
// white Lambertian surface (albedo * light * N.L) and the sky agree.

#define TAP_PI 3.14159265359

struct AtmosphereParams
{
    float bottom, top;          // planet radius and the top of the visual atmosphere
    float3 rayleigh; float rayleighHeight;
    float mie, mieHeight, mieG;
    float3 ozone;
    float3 sunDirection;        // towards the sun, body-relative world axes
    float3 sunIrradiance;       // linear, already multiplied by pi
};

// Texel-centre remap of a [0,1] coordinate onto a lookup of 'size' texels.
float LutCoord(float x, float size) { return 0.5 / size + x * (1 - 1 / size); }

// Covers only directions that reach the top of the atmosphere; directions into the ground are handled analytically.
float2 TransmittanceLutUV(float r, float mu, float bottom, float top)
{
    float H = sqrt(max(0, top * top - bottom * bottom));
    float rho = sqrt(max(0, r * r - bottom * bottom));
    float disc = r * r * (mu * mu - 1) + top * top;
    float d = max(0, -r * mu + sqrt(max(0, disc)));
    float dMin = top - r, dMax = rho + H;
    float xMu = (d - dMin) / max(1e-3, dMax - dMin);
    float xR = rho / max(1e-3, H);
    return float2(LutCoord(saturate(xMu), 256), LutCoord(saturate(xR), 64));
}

// The planet's shadow, softened over the sun's apparent radius instead of a hard edge.
float SunVisibility(float r, float mu, float bottom)
{
    float s = saturate(bottom / max(r, bottom));
    float horizon = -sqrt(max(0, 1 - s * s));
    return smoothstep(-0.0045, 0.0045, mu - horizon);
}

float3 SunTransmittance(Texture2D lut, SamplerState state, float r, float mu, float bottom, float top)
{
    r = clamp(r, bottom, top);
    float3 t = SAMPLE_TEXTURE2D_LOD(lut, state, TransmittanceLutUV(r, mu, bottom, top), 0).rgb;
    return t * SunVisibility(r, mu, bottom);
}

float3 MultipleScattering(Texture2D lut, SamplerState state, float r, float muS, float bottom, float top)
{
    float2 uv = float2(LutCoord(saturate(muS * 0.5 + 0.5), 32), LutCoord(saturate((r - bottom) / max(1, top - bottom)), 32));
    return SAMPLE_TEXTURE2D_LOD(lut, state, uv, 0).rgb;
}

float RayleighPhase(float c) { return 3.0 / (16.0 * TAP_PI) * (1 + c * c); }
// Cornette-Shanks: Henyey-Greenstein with a physically shaped backscatter, normalised over the sphere.
float MiePhase(float g, float c)
{
    float g2 = g * g;
    return 3.0 / (8.0 * TAP_PI) * ((1 - g2) * (1 + c * c)) / ((2 + g2) * pow(max(1e-4, 1 + g2 - 2 * g * c), 1.5));
}
float HenyeyGreenstein(float g, float c)
{
    float g2 = g * g;
    return (1 - g2) / (4 * TAP_PI * pow(max(1e-4, 1 + g2 - 2 * g * c), 1.5));
}

void AtmosphereDensity(AtmosphereParams a, float altitude, out float3 scattering, out float3 extinction, out float rayleighDensity, out float mieDensity)
{
    altitude = max(0, altitude);
    rayleighDensity = exp(-altitude / a.rayleighHeight);
    mieDensity = exp(-altitude / a.mieHeight);
    float ozone = max(0, 1 - abs(altitude - 25000) / 15000);
    scattering = a.rayleigh * rayleighDensity + a.mie * mieDensity;
    extinction = scattering + a.mie * mieDensity * 0.11 + a.ozone * ozone;
}

// Single + multiple scattering along [start,end]. Samples concentrate around the densest point of the ray: the
// tangent point of a limb ray, the ground at the end of a downward ray, or the camera when it is inside the air.
void IntegrateAtmosphere(AtmosphereParams a, Texture2D transmittanceLut, Texture2D multiLut, SamplerState state,
    float3 origin, float3 ray, float start, float end, int steps, out float3 scatter, out float3 trans)
{
    scatter = 0; trans = 1;
    if (end <= start || steps <= 0) return;
    float c = dot(ray, a.sunDirection);
    float phaseR = RayleighPhase(c), phaseM = MiePhase(a.mieG, c);
    float tangent = clamp(-dot(origin, ray), start, end);
    int nA = tangent - start > 1 ? (end - tangent > 1 ? (steps >> 1) : steps) : 0;
    int nB = steps - nA;
    [loop] for (int i = 0; i < steps; i++)
    {
        // Segment A [start,tangent] is sampled densely towards its end; segment B [tangent,end] towards its start.
        float t0, t1;
        if (i < nA) { float k0 = (float)(nA - i) / nA, k1 = (float)(nA - i - 1) / nA; t0 = tangent - (tangent - start) * k0 * k0; t1 = tangent - (tangent - start) * k1 * k1; }
        else { float k0 = (float)(i - nA) / nB, k1 = (float)(i - nA + 1) / nB; t0 = tangent + (end - tangent) * k0 * k0; t1 = tangent + (end - tangent) * k1 * k1; }
        float dt = t1 - t0; float3 p = origin + ray * (0.5 * (t0 + t1));
        float r = length(p);
        float3 sigmaS, sigmaT; float rd, md;
        AtmosphereDensity(a, r - a.bottom, sigmaS, sigmaT, rd, md);
        float muS = dot(p / r, a.sunDirection);
        float3 sun = SunTransmittance(transmittanceLut, state, r, muS, a.bottom, a.top);
        float3 ms = MultipleScattering(multiLut, state, r, muS, a.bottom, a.top);
        float3 source = a.sunIrradiance * ((a.rayleigh * rd * phaseR + a.mie * md * phaseM) * sun + sigmaS * ms);
        float3 segment = exp(-sigmaT * dt);
        scatter += trans * source * (1 - segment) / max(sigmaT, 1e-12);
        trans *= segment;
    }
}
#endif
