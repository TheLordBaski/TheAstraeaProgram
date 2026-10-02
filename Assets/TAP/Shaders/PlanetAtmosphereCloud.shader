Shader "Hidden/TAP/PlanetAtmosphereCloud"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "PlanetWeather.hlsl"
        #include "PlanetAtmosphere.hlsl"
        TEXTURE2D_X(_Depth); TEXTURE2D_X(_SourceColor);
        TEXTURE2D_X(_Scatter); TEXTURE2D_X(_Extinction); TEXTURE2D_X(_FrontScatter); TEXTURE2D_X(_FrontExtinction);
        TEXTURE2D_X(_HistoryScatter); TEXTURE2D_X(_HistoryExtinction);
        TEXTURE2D(_Weather); SAMPLER(sampler_Weather);
        TEXTURE2D(_WeatherNormal); SAMPLER(sampler_WeatherNormal);
        TEXTURE2D(_TransmittanceLut); TEXTURE2D(_MultiScatterLut);
        TEXTURE3D(_CloudNoise); SAMPLER(sampler_CloudNoise);
        TEXTURE3D(_CloudDetail); SAMPLER(sampler_CloudDetail);
        float4x4 _InverseVP,_WorldToBody,_PreviousVP;
        float3 _CameraRelative,_SunDirection,_SunIrradiance,_CameraForward;
        float4 _Radii,_CloudParams,_CloudLayer,_CloudVolume,_Rayleigh,_Mie,_Ozone,_Wind;
        float4 _Weather_TexelSize;
        float4 _PreviousCamera,_BufferSize;
        float _Scale,_AtmosphereEnabled,_CloudMode,_CloudSteps,_AtmoSteps,_Jitter,_HistoryWeight,_VolumeBlend,_PixelAngle;
        float _CloudNormalEnabled,_LayerDetailStrength;
        // _CloudParams: base altitude, top altitude, coverage scale, volume extinction (1/m)
        // _CloudLayer: layer optical depth, detail strength, detail scale (m), ambient strength
        // _CloudVolume: shape scale (m), detail scale (m), erosion, light-march length (m)

        AtmosphereParams Atmosphere()
        {
            AtmosphereParams a;
            a.bottom=_Radii.x; a.top=_Radii.y;
            a.rayleigh=_Rayleigh.rgb; a.rayleighHeight=_Rayleigh.w;
            a.mie=_Mie.x; a.mieHeight=_Mie.y; a.mieG=_Mie.z;
            a.ozone=_Ozone.rgb; a.sunDirection=_SunDirection; a.sunIrradiance=_SunIrradiance;
            return a;
        }
        float3 Ray(float2 uv)
        {
            float4 p=mul(_InverseVP,ComputeClipSpacePosition(uv,1));
            return normalize(p.xyz/p.w);
        }
        float Distance(float2 uv,float3 ray)
        {
            float depth=SAMPLE_TEXTURE2D_X_LOD(_Depth,sampler_PointClamp,uv,0).r;
            // The flight camera's very large far/near ratio can make clear-depth linearisation infinite.
            #if UNITY_REVERSED_Z
            if(depth<=0) return _Radii.x*10000;
            #else
            if(depth>=1) return _Radii.x*10000;
            #endif
            return min(_Radii.x*10000,LinearEyeDepth(depth,_ZBufferParams)*_Scale/max(.0001,dot(ray,_CameraForward)));
        }
        // Visible interval of the ray inside the air: never through the planet, never behind geometry.
        void AirInterval(float3 origin,float3 ray,float limit,out float start,out float end)
        {
            float2 outer=PlanetSphere(origin,ray,_Radii.y);
            float2 ground=PlanetSphere(origin,ray,_Radii.x-100);
            start=max(0,outer.x); end=min(limit,outer.y);
            if(ground.y>0 && ground.x>0) end=min(end,ground.x);
        }
        // The 2D layer sits at the middle of the cloud shell; seen from above its near side, from below the side above.
        float LayerDistance(float3 origin,float3 ray)
        {
            float mid=_Radii.x+(_CloudParams.x+_CloudParams.y)*.5;
            float2 hit=PlanetSphere(origin,ray,mid);
            if(hit.x>hit.y) return -1;
            float t=length(origin)>mid?hit.x:hit.y;
            float2 ground=PlanetSphere(origin,ray,_Radii.x-100);
            if(ground.x<=ground.y && ground.x>0 && ground.x<t) return -1;
            return t;
        }
        float3 SkyAmbient(float muS,float3 sun)
        {
            // Skylight on a cloud: blue by day, warm and dim at sunset, gone at night.
            return _SunIrradiance*_CloudLayer.w*saturate(muS*1.4+.22)*(float3(.55,.70,1)*(.35+.65*sun));
        }

        // ---------------------------------------------------------------- volumes (near the cloud deck)
        float VolumeSample(float3 bf,float footprint,bool detail,out float type)
        {
            float r=length(bf); float h=(r-_Radii.x-_CloudParams.x)/(_CloudParams.y-_CloudParams.x);
            type=0;
            if(h<0 || h>1) return 0;
            float lod=WeatherMip(footprint,_Radii.x,_Weather_TexelSize.z);
            float2 uv=PlanetUV(bf); uv.x+=_Wind.x;
            float4 w=SAMPLE_TEXTURE2D_LOD(_Weather,sampler_Weather,uv,lod);
            float coverage=saturate(w.r*_CloudParams.z); type=w.g;
            if(coverage<.004) return 0;
            float3 q=WeatherFrame(bf,_Wind.x);
            float shape=CloudShape(_CloudNoise,sampler_CloudNoise,q,footprint,_CloudVolume.x);
            float d=.5;
            if(detail) {
                float lod2=max(0,log2(max(1,footprint)*64/_CloudVolume.y));
                float3 n=SAMPLE_TEXTURE3D_LOD(_CloudDetail,sampler_CloudDetail,q/_CloudVolume.y,lod2).rgb;
                d=n.r*.625+n.g*.25+n.b*.125;
            }
            return CloudDensityModel(shape,d,coverage,type,h,detail?_CloudVolume.z:0);
        }
        float VolumeLight(float3 bf,float3 sunBF)
        {
            // Short march towards the sun with growing steps; shape only (no erosion) beyond the first sample.
            float tau=0; float len=_CloudVolume.w;
            [unroll] for(int i=0;i<4;i++) {
                float a=(i+.5)/4; float d=len*a*a; float step=len*(2*i+1)/16;
                float type;
                tau+=VolumeSample(bf+sunBF*d,step,i==0,type)*step;
            }
            return tau*_CloudParams.w;
        }
        void MarchVolume(float3 origin,float3 ray,float cs,float ce,float mu,inout float3 cloudScatter,inout float cloudTrans,inout float meanDistance,inout float weight)
        {
            float3 sunBF=mul((float3x3)_WorldToBody,_SunDirection);
            bool inside=length(origin)<_Radii.x+_CloudParams.y;
            float phase=lerp(HenyeyGreenstein(.75,mu),HenyeyGreenstein(-.25,mu),.35);
            float steps=_CloudSteps;
            [loop] for(int s=0;s<(int)steps;s++) {
                float a=s/steps,b=(s+1)/steps;
                if(inside) { a*=a;b*=b; }
                float step=(ce-cs)*(b-a);
                float t=cs+(ce-cs)*lerp(a,b,_Jitter); float3 p=origin+ray*t;
                float3 bf=mul((float3x3)_WorldToBody,p);
                float type;
                // Filter the noise to the (half-resolution) pixel, not the step: jitter and history hide the steps,
                // while step-sized filtering blurred every billow away.
                float density=VolumeSample(bf,max(1,t*_PixelAngle*2),true,type);
                if(density<1e-4) continue;
                float sigma=density*_CloudParams.w;
                float segment=exp(-sigma*step),absorbed=cloudTrans*(1-segment);
                float r=length(p); float muS=dot(p/r,_SunDirection);
                float3 sun=SunTransmittance(_TransmittanceLut,sampler_LinearClamp,r,muS,_Radii.x,_Radii.y);
                float lightTau=VolumeLight(bf,sunBF);
                // Direct sunlight plus the diffuse light that multiple scattering carries deep into thick cloud
                // (two-stream: the diffuse flux at optical depth tau falls as 2/(2+(1-g)tau), g~0.8). A Beer-Lambert
                // light march alone leaves cloud bases black; real overcast bases are mid-grey.
                float direct=exp(-lightTau);
                float diffuse=max(0,2/(2+.2*lightTau)-direct);
                float powder=1-exp(-sigma*500);
                float h=saturate((r-_Radii.x-_CloudParams.x)/(_CloudParams.y-_CloudParams.x));
                float3 light=_SunIrradiance*sun*(direct*phase*2.5*lerp(.65,1,powder)+diffuse*.24*(.3+.7*saturate(muS)))
                    +SkyAmbient(muS,sun)*(.45+.55*h);
                cloudScatter+=absorbed*light; meanDistance+=t*absorbed; weight+=absorbed; cloudTrans*=segment;
                if(cloudTrans<.01) break;
            }
        }

        // ---------------------------------------------------------------- integration (half resolution)
        struct Buffers { float4 scatter:SV_Target0; float4 extinction:SV_Target1; float4 frontScatter:SV_Target2; float4 frontExtinction:SV_Target3; };
        Buffers Integrate(Varyings i)
        {
            float3 ray=Ray(i.texcoord), origin=_CameraRelative;
            float limit=Distance(i.texcoord,ray);
            float start,end; AirInterval(origin,ray,limit,start,end);
            AtmosphereParams atmo=Atmosphere();
            int steps=(int)_AtmoSteps;
            float layer=_CloudMode>.5?LayerDistance(origin,ray):-1;
            bool split=layer>start && layer<end;
            float3 scatter=0,trans=1,frontScatter=0,frontTrans=1;
            if(_AtmosphereEnabled>.5 && end>start) {
                if(split) {
                    // Exact aerial perspective in front of and behind the cloud layer.
                    float3 backScatter,backTrans;
                    int frontSteps=max(3,steps>>1);
                    IntegrateAtmosphere(atmo,_TransmittanceLut,_MultiScatterLut,sampler_LinearClamp,origin,ray,start,layer,frontSteps,frontScatter,frontTrans);
                    IntegrateAtmosphere(atmo,_TransmittanceLut,_MultiScatterLut,sampler_LinearClamp,origin,ray,layer,end,max(3,steps-frontSteps),backScatter,backTrans);
                    scatter=frontScatter+frontTrans*backScatter; trans=frontTrans*backTrans;
                } else {
                    IntegrateAtmosphere(atmo,_TransmittanceLut,_MultiScatterLut,sampler_LinearClamp,origin,ray,start,end,steps,scatter,trans);
                    frontScatter=scatter; frontTrans=trans;
                }
            }
            float3 cloudScatter=0; float cloudTrans=1; float meanDistance=0,weight=0;
            if(_CloudMode>1.5 && _VolumeBlend>.001) {
                float2 cloud=PlanetSphere(origin,ray,_Radii.x+_CloudParams.y);
                float2 inner=PlanetSphere(origin,ray,_Radii.x+_CloudParams.x);
                float2 ground=PlanetSphere(origin,ray,_Radii.x-100);
                float cs=max(0,cloud.x),ce=min(limit,cloud.y);
                if(ground.y>0 && ground.x>0) ce=min(ce,ground.x);
                // March only the visible interval of the shell, never across the empty interior of the planet.
                if(inner.y>0 && inner.x>cs) ce=min(ce,inner.x);
                else if(inner.y>cs && length(origin)<_Radii.x+_CloudParams.x) cs=inner.y;
                if(ce>cs) MarchVolume(origin,ray,cs,ce,dot(ray,_SunDirection),cloudScatter,cloudTrans,meanDistance,weight);
                cloudScatter*=_VolumeBlend; cloudTrans=lerp(1,cloudTrans,_VolumeBlend); weight*=_VolumeBlend; meanDistance*=_VolumeBlend;
                // Aerial perspective to the cloud centroid; the remaining atmosphere stays behind it.
                float fraction=weight>.001?saturate((meanDistance/weight-start)/max(1,end-start)):1;
                float3 cloudFront=pow(max(trans,1e-5),fraction);
                scatter=scatter*lerp(cloudTrans,1,fraction)+cloudScatter*cloudFront;
                trans*=cloudTrans;
                frontScatter=scatter; frontTrans=trans;
            }
            float distance=weight>.001?meanDistance/weight:max(0,min(end,_Radii.y*10));
            Buffers o;
            o.scatter=float4(scatter,distance/max(1,_Radii.x));
            o.extinction=float4(trans,min(limit/_Radii.x,10000));
            o.frontScatter=float4(frontScatter,0);
            o.frontExtinction=float4(frontTrans,0);
            // Reproject volumes in body-relative metres. History never stores scene-space origin coordinates.
            if(_HistoryWeight>0 && weight>.01) {
                float3 p=origin+ray*distance;
                float4 prev=mul(_PreviousVP,float4(p-_PreviousCamera.xyz,1));
                float2 uv=prev.xy/prev.w*.5+.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y=1-uv.y;
                #endif
                if(prev.w>0 && all(uv>0) && all(uv<1)) {
                    float4 hs=SAMPLE_TEXTURE2D_X_LOD(_HistoryScatter,sampler_LinearClamp,uv,0);
                    float4 ht=SAMPLE_TEXTURE2D_X_LOD(_HistoryExtinction,sampler_LinearClamp,uv,0);
                    float depthMatch=abs(ht.a-o.extinction.a)<max(.002,o.extinction.a*.01)?1:0;
                    float cloudMatch=abs(hs.a-o.scatter.a)<.02?1:0;
                    float w=_HistoryWeight*depthMatch*cloudMatch;
                    o.scatter.rgb=lerp(o.scatter.rgb,clamp(hs.rgb,o.scatter.rgb*.7-.01,o.scatter.rgb*1.3+.01),w);
                    o.extinction.rgb=lerp(o.extinction.rgb,clamp(ht.rgb,o.extinction.rgb-.08,o.extinction.rgb+.08),w);
                    o.frontScatter=o.scatter; o.frontExtinction.rgb=o.extinction.rgb;
                }
            }
            return o;
        }

        // ---------------------------------------------------------------- 2D cloud layer (full resolution)
        // Premultiplied radiance in rgb, transmittance in a. A slab of optical depth tau: two-stream reflectance from
        // above, diffuse transmission from below, a forward-scattering silver lining and blue/warm skylight.
        float4 CloudLayer(float3 origin,float3 ray,float t,float2 uv)
        {
            float3 p=origin+ray*t;
            float3 bf=mul((float3x3)_WorldToBody,p);
            float r=length(p); float3 up=p/r;
            float muV=abs(dot(ray,up));
            // Pixel footprint on the layer, stretched at grazing angles.
            float footprint=max(1,t*_PixelAngle/max(.08,muV));
            float lod=WeatherMip(footprint,_Radii.x,_Weather_TexelSize.z);
            float2 wuv=PlanetUV(bf); wuv.x+=_Wind.x;
            float4 w=SAMPLE_TEXTURE2D_LOD(_Weather,sampler_Weather,wuv,lod);
            float coverage=saturate(w.r*_CloudParams.z);
            float relief=1;
            if(_LayerDetailStrength>.01 && coverage>.002) {
                // Resolved billows: noise redistributes the coverage at the edges and inside thin cloud, keeping the
                // clear sky clear, opaque cores opaque and the mean coverage of the baked map.
                float3 q=WeatherFrame(bf,_Wind.x);
                float fadeA=_LayerDetailStrength*(1-smoothstep(_CloudLayer.z*.02,_CloudLayer.z*.12,footprint));
                float fadeB=_LayerDetailStrength*(1-smoothstep(_CloudLayer.z*.004,_CloudLayer.z*.03,footprint));
                if(fadeA>.01) {
                    float a=CloudShape(_CloudNoise,sampler_CloudNoise,q,footprint,_CloudLayer.z);
                    float b=fadeB>.01?CloudShape(_CloudNoise,sampler_CloudNoise,q+float3(3100,-1700,900),footprint,_CloudLayer.z*.23):.5;
                    float n=lerp(.5,a,fadeA)*.65+lerp(.5,b,fadeB)*.35;
                    coverage=saturate(coverage+(n-.5)*_CloudLayer.y*2*saturate(coverage*(1-coverage)*4+.15*coverage));
                    relief=lerp(1,.55+.9*n,fadeA);
                }
            }
            float tau=pow(coverage,1.35)*_CloudLayer.x*(.55+.45*w.g);
            if(tau<.001) return float4(0,0,0,1);
            float muS=dot(up,_SunDirection);
            float mid=_Radii.x+(_CloudParams.x+_CloudParams.y)*.5;
            float3 sun=SunTransmittance(_TransmittanceLut,sampler_LinearClamp,_Radii.x+_CloudParams.y,muS,_Radii.x,_Radii.y);
            float muR=muS;
            if(_CloudNormalEnabled>.5) {
                float3 radial=normalize(bf);
                float3 east=normalize(float3(radial.z,0,-radial.x)+float3(1e-6,0,0));
                float3 north=normalize(cross(radial,east));
                float3 dn=SAMPLE_TEXTURE2D_LOD(_WeatherNormal,sampler_WeatherNormal,wuv,lod).rgb*2-1;
                float3 sunBF=mul((float3x3)_WorldToBody,_SunDirection);
                muR=dot(normalize(east*dn.x+north*dn.y+radial*max(.05,dn.z)),sunBF);
            }
            float viewTrans=exp(-tau/max(.07,muV));
            float reflectance=.2*tau/(2+.2*tau);
            float direct=exp(-tau/max(.05,abs(muS)));
            float diffuse=saturate(1-reflectance-direct);
            bool above=length(origin)>mid;
            // Elevated clouds stay lit a little past the geometric terminator.
            float incidence=saturate(lerp(muS,muR,.7)+.12)/1.12;
            float3 E=_SunIrradiance*sun;
            float3 L=above?E*reflectance*incidence*relief/TAP_PI:E*diffuse*saturate(muS+.1)/TAP_PI*lerp(1,relief,.5);
            float c=dot(ray,_SunDirection);
            L+=E*HenyeyGreenstein(.6,c)*(1-exp(-tau))*exp(-tau*.35)*.6;
            L+=SkyAmbient(muS,sun)*(above?(.25+.75*reflectance):(.3+.4*diffuse))*(1-viewTrans);
            return float4(L,viewTrans);
        }

        float4 Composite(Varyings i):SV_Target
        {
            float3 ray=Ray(i.texcoord); float limit=Distance(i.texcoord,ray); float distance=limit/_Radii.x;
            float2 pixel=i.texcoord*_BufferSize.xy-.5,base=floor(pixel),f=frac(pixel);
            float3 scattering=0,trans=0,frontS=0,frontT=0; float sum=0;
            [unroll] for(int y=0;y<2;y++) [unroll] for(int x=0;x<2;x++) {
                float2 uv=(base+float2(x,y)+.5)*_BufferSize.zw;
                float4 s=SAMPLE_TEXTURE2D_X_LOD(_Scatter,sampler_PointClamp,uv,0),t=SAMPLE_TEXTURE2D_X_LOD(_Extinction,sampler_PointClamp,uv,0);
                float w=(x==0?1-f.x:f.x)*(y==0?1-f.y:f.y)/(1+abs(t.a-distance)*500);
                scattering+=s.rgb*w;trans+=t.rgb*w;sum+=w;
                frontS+=SAMPLE_TEXTURE2D_X_LOD(_FrontScatter,sampler_PointClamp,uv,0).rgb*w;
                frontT+=SAMPLE_TEXTURE2D_X_LOD(_FrontExtinction,sampler_PointClamp,uv,0).rgb*w;
            }
            float norm=1/max(sum,1e-5);
            scattering*=norm;trans*=norm;frontS*=norm;frontT*=norm;
            float3 source=SAMPLE_TEXTURE2D_X_LOD(_SourceColor,sampler_LinearClamp,i.texcoord,0).rgb;
            float layerWeight=_CloudMode<.5?0:_CloudMode<1.5?1:1-_VolumeBlend;
            if(layerWeight>.001) {
                float3 origin=_CameraRelative;
                float t=LayerDistance(origin,ray);
                if(t>0 && t<limit) {
                    float4 cloud=CloudLayer(origin,ray,t,i.texcoord);
                    cloud.rgb*=layerWeight; cloud.a=lerp(1,cloud.a,layerWeight);
                    float3 safeFront=max(frontT,1e-4);
                    float3 backT=saturate(trans/safeFront), backS=max(0,scattering-frontS)/safeFront;
                    float3 behind=source*backT+backS;
                    return float4((behind*cloud.a+cloud.rgb)*frontT+frontS,1);
                }
            }
            return float4(source*trans+scattering,1);
        }
        ENDHLSL
        Pass
        {
            Name "Spherical integration"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Integrate
            ENDHLSL
        }
        Pass
        {
            Name "Depth aware composition"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            ENDHLSL
        }
    }
}
