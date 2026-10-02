#ifndef TAP_PLANET_TRANSPARENT_TRANSMISSION
#define TAP_PLANET_TRANSPARENT_TRANSMISSION
#include "PlanetWeather.hlsl"
TEXTURE2D(_PlanetWeather); SAMPLER(sampler_PlanetWeather);
TEXTURE3D(_PlanetNoise); SAMPLER(sampler_PlanetNoise);
float4x4 _PlanetWorldToBody;
float3 _PlanetCamera,_PlanetBodyCenter;
float4 _PlanetRayleigh,_PlanetMie,_PlanetOzone,_PlanetClouds,_PlanetWind,_PlanetCloudShape;
float _PlanetEnabled,_PlanetScattering,_PlanetRadius,_PlanetCloudMode;
// _PlanetClouds: base, top, coverage scale, volume extinction (1/m); _PlanetCloudShape: shape scale (m), layer optical depth

float PlanetTransparentExtinction(float3 p,float footprint)
{
    float h=(length(p)-_PlanetRadius-_PlanetClouds.x)/(_PlanetClouds.y-_PlanetClouds.x);
    float result=0;
    if(h>=0 && h<=1 && _PlanetCloudMode>.5) {
        float3 bf=mul((float3x3)_PlanetWorldToBody,p);
        float2 uv=PlanetUV(bf);uv.x+=_PlanetWind.x;
        float4 w=SAMPLE_TEXTURE2D_LOD(_PlanetWeather,sampler_PlanetWeather,uv,0);
        float coverage=saturate(w.r*_PlanetClouds.z);
        if(_PlanetCloudMode<1.5) {
            // The 2D layer's optical depth spread over the shell thickness.
            result=pow(coverage,1.35)*_PlanetCloudShape.y*(.55+.45*w.g)/max(1,_PlanetClouds.y-_PlanetClouds.x);
        } else if(coverage>.004) {
            // Same shape model as the opaque volume, without the fine erosion (sub-step detail).
            float shape=CloudShape(_PlanetNoise,sampler_PlanetNoise,WeatherFrame(bf,_PlanetWind.x),footprint,_PlanetCloudShape.x);
            result=CloudDensityModel(shape,.5,coverage,w.g,h,0)*_PlanetClouds.w;
        }
    }
    return result;
}

// Transparent emission is added after the opaque sky/cloud composite. Only air/clouds in front of
// the actual fragment attenuate it, so a cloud behind a nearby engine cannot dim its exhaust.
float3 PlanetTransparentTransmission(float3 positionWS)
{
    float3 result=1;
    if(_PlanetEnabled>.5) {
    float3 origin=_PlanetCamera,endpoint=positionWS-_PlanetBodyCenter;
    float3 delta=endpoint-origin;float distance=length(delta),step=distance/8;
    float3 ray=delta/max(distance,.001),optical=0;
    if(_PlanetScattering>.5) {
      [loop] for(int i=0;i<8;i++) {
        float altitude=max(0,length(origin+ray*((i+.5)*step))-_PlanetRadius);
        float mie=exp(-altitude/_PlanetMie.y);
        optical+=(_PlanetRayleigh.rgb*exp(-altitude/_PlanetRayleigh.w)
            +_PlanetMie.x*mie*1.11
            +_PlanetOzone.rgb*max(0,1-abs(altitude-25000)/15000))*step;
      }
    }
    float2 outer=PlanetSphere(origin,ray,_PlanetRadius+_PlanetClouds.y);
    if(outer.y>0 && _PlanetCloudMode>.5) {
        float cs=max(0,outer.x),ce=min(distance,outer.y);
        float2 inner=PlanetSphere(origin,ray,_PlanetRadius+_PlanetClouds.x);
        if(inner.y>0 && inner.x>cs) ce=min(ce,inner.x);
        else if(inner.y>cs && length(origin)<_PlanetRadius+_PlanetClouds.x) cs=inner.y;
        if(ce>cs) {
            step=(ce-cs)/24;
            [loop] for(int s=0;s<24;s++) optical+=PlanetTransparentExtinction(origin+ray*(cs+(s+.5)*step),step)*step;
        }
    }
    result=exp(-optical);
    }
    return result;
}
#endif
