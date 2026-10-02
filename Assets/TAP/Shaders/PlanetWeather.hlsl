#ifndef TAP_PLANET_WEATHER
#define TAP_PLANET_WEATHER
float2 PlanetUV(float3 d) { d=normalize(d); return float2(atan2(-d.z,d.x)/6.28318530718+.5,asin(clamp(d.y,-1,1))/3.14159265359+.5); }
float3 WeatherFrame(float3 p,float wind)
{
    float s,c;sincos(wind*6.28318530718,s,c);
    return float3(c*p.x+s*p.z,p.y,-s*p.x+c*p.z);
}
// The baker stores continuous optical coverage before building mips, preserving thin clouds and clear gaps.
float WeatherMask(float value,float coverage) {return saturate(value)*step(.0001,coverage);}
float WeatherMip(float footprint,float radius,float resolution) {return max(0,log2(max(1,footprint)*resolution/(6.28318530718*radius)));}
// The periodic longitude offset is supplied from double-precision simulation UT.
float WeatherCoverage(Texture2D weather, SamplerState state, float3 direction, float wind, float coverage,float lod)
{
    float2 uv=PlanetUV(direction); uv.x+=wind;
    float w=weather.SampleLevel(state,uv,lod).r;
    return WeatherMask(w,coverage);
}
float WeatherCoverage(Texture2D weather, SamplerState state, float3 direction, float wind, float coverage) {return WeatherCoverage(weather,state,direction,wind,coverage,0);}
float CloudRemap(float v,float lo,float hi,float newLo,float newHi) {return newLo+(v-lo)/max(1e-4,hi-lo)*(newHi-newLo);}

// Volume cloud density (Schneider/Nubis style). Body-fixed, wind-rotated position in metres; h is the normalised
// height in the cloud shell; coverage/type come from the weather map. Shape: 128-cubed Perlin-Worley + Worley
// octaves; detail: 32-cubed Worley octaves that erode the edges (wispy at the base, billowy at the top).
// Both the opaque composite and transparent transmission use this one definition.
float CloudHeightProfile(float h,float type)
{
    float top=lerp(.42,1,type);
    return saturate(h/.07)*(1-smoothstep(top*.55,top,h));
}
float CloudShape(Texture3D noise,SamplerState state,float3 p,float footprint,float shapeScale)
{
    float lod=max(0,log2(max(1,footprint)*128/shapeScale));
    float4 n=noise.SampleLevel(state,p/shapeScale,lod);
    float worley=n.g*.625+n.b*.25+n.a*.125;
    // The raw Perlin-Worley remap spans only ~0.68-0.95 (median 0.83); stretch it to 0-1 with a 0.5 median so
    // coverage thresholds carve distinct billows instead of a uniform blanket.
    return saturate((CloudRemap(n.r,worley-1,1,0,1)-.70)/.25);
}
float CloudDensityModel(float shape,float detail,float coverage,float type,float h,float erosion)
{
    float base=shape*CloudHeightProfile(h,type);
    // Coverage raises the cloud amount without changing the optical weight of the remaining cloud.
    base=saturate(CloudRemap(base,1-coverage,1,0,1))*coverage;
    if(base<=0) return 0;
    float d=lerp(detail,1-detail,saturate(h*3.5));
    return saturate(CloudRemap(base,d*erosion,1,0,1));
}
float2 PlanetSphere(float3 origin,float3 ray,float radius)
{
    float b=dot(origin,ray);
    // Closest approach avoids subtracting two distance-squared values far from the body.
    float3 perpendicular=origin-ray*b;
    float disc=radius*radius-dot(perpendicular,perpendicular);
    float s=sqrt(max(0,disc));
    return disc<0?float2(1,-1):float2(-b-s,-b+s);
}
#endif
