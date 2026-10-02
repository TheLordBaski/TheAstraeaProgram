Shader "TAP/Terrain"
{
    Properties
    {
        _BaseMap ("Orbital albedo", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _SurfaceNormal ("Orbital normal", 2D) = "bump" {}
        _SurfaceMask ("Surface material/ocean/depth", 2D) = "black" {}
        _Weather ("Cloud weather", 2D) = "black" {}
        _GroundAlbedo ("Ground albedo array", 2DArray) = "" {}
        _GroundNormal ("Ground normal array", 2DArray) = "" {}
        _GroundMaterial ("Ground material array", 2DArray) = "" {}
        _SatelliteDetail ("Satellite landscape detail array", 2DArray) = "" {}
        _SatelliteStrength ("Satellite detail strength", Range(0,1)) = 0
        _OrbitalNear ("Orbital colour up close", Float) = 0
        _OceanShallow ("Shallow water colour", Color) = (.18,.55,.58,1)
        _SurfaceEnabled ("Detailed surface", Float) = 0
        _BiomeMode ("Biome diagnostic", Float) = 0
        _MapMode ("Map surface", Float) = 0
        _Radius ("Physical body radius", Float) = 600000
        _CloudEnabled ("Cloud shadow", Float) = 0
        _CloudParams ("Base, coverage, shadow, wind", Vector) = (2800,.52,.55,0)
        _OceanTime ("Wave time", Float) = 0
        _OceanRoughness ("Ocean roughness", Range(.04,1)) = .12
        _SurfaceSaturation ("Surface saturation", Range(0,1)) = 1
        _SurfaceTint ("Surface tint", Color) = (1,1,1,1)
        _SurfaceNormalStrength ("Terrain relief", Range(.1,3)) = 1
        _OceanColour ("Deep ocean colour", Color) = (.15,.31,.47,1)
        _OceanColourBlend ("Deep ocean colour blend", Range(0,1)) = 0
        _RegionalDetail ("Regional strength and scale", Vector) = (0,18000,0,0)
        _TextureBias ("Ground mip bias", Float) = 0
        _DetailTex ("Detail Noise (tileable)", 2D) = "gray" {}
        _DetailScaleNear ("Detail Scale Near (1/m)", Float) = 0.22
        _DetailScaleFar ("Detail Scale Far (1/m)", Float) = 0.012
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.45
        _WaterSpecular ("Water Specular", Float) = 1.3
        _AmbientBoost ("Ambient Boost", Float) = 1.0
        // Ambient: the scene's (sky) ambient weighted by _SkyAmbient, else the faint ambient of space that keeps night
        // sides readable. The far view always uses the latter; nearby terrain uses the sky's inside an atmosphere.
        _SpaceAmbient ("Ambient in Space", Color) = (0.2, 0.215, 0.265, 1)
        _SkyAmbient ("Sky Ambient Weight", Range(0, 1)) = 1
        // Far view (FND-03): metres per world unit, the body's own sunlight (w = 1 to use it instead of the scene's
        // sun light, whose direction is the camera's) and the air between a camera inside an atmosphere and the body.
        _DistanceScale ("Metres per Unit", Float) = 1
        _BodySunDir ("Body Sun Direction (w = use)", Vector) = (0, 1, 0, 0)
        _BodySunColor ("Body Sun Colour (linear, not converted)", Vector) = (1, 1, 1, 1)
        _HazeColor ("Haze (added)", Color) = (0, 0, 0, 0)
        _Transmittance ("Transmittance", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PlanetWeather.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                float3 detailPos : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SurfaceNormal); SAMPLER(sampler_SurfaceNormal);
            TEXTURE2D(_SurfaceMask); SAMPLER(sampler_SurfaceMask);
            TEXTURE2D(_Weather); SAMPLER(sampler_Weather);
            TEXTURE2D_ARRAY(_GroundAlbedo); SAMPLER(sampler_GroundAlbedo);
            TEXTURE2D_ARRAY(_GroundNormal); SAMPLER(sampler_GroundNormal);
            TEXTURE2D_ARRAY(_GroundMaterial); SAMPLER(sampler_GroundMaterial);
            TEXTURE2D_ARRAY(_SatelliteDetail); SAMPLER(sampler_SatelliteDetail);

            CBUFFER_START(UnityPerMaterial)
                float4 _DetailTex_ST;
                float _DetailScaleNear;
                float _DetailScaleFar;
                float _DetailStrength;
                float _WaterSpecular;
                float _AmbientBoost;
                float4 _SpaceAmbient;
                float _SkyAmbient;
                float _DistanceScale;
                float4 _BodySunDir;
                float4 _BodySunColor;
                float4 _HazeColor;
                float _Transmittance;
                float4 _BaseColor, _CloudParams;
                float _SurfaceEnabled, _BiomeMode, _MapMode, _Radius, _CloudEnabled, _OceanTime, _OceanRoughness;
                float _SurfaceSaturation,_SurfaceNormalStrength,_OceanColourBlend;
                float4 _SurfaceTint,_OceanColour;
                float4 _RegionalDetail;
                float4 _OceanShallow;
                float _SatelliteStrength, _OrbitalNear;
                float _TextureBias;
                float4 _BodyCenter;
                float4x4 _WorldToBody;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                // Terrain roots use uniform scale. Remove that scale before normalization:
                // the far view's inverse scale produced zero normals and NaNs on the GPU.
                float3 direction = mul((float3x3)GetObjectToWorldMatrix(), v.normalOS);
                direction /= max(max(abs(direction.x), max(abs(direction.y), abs(direction.z))), 1e-20);
                o.normalWS = SafeNormalize(direction);
                o.normalOS = v.normalOS;
                o.color = v.color;
                o.detailPos = float3(v.uv0.x, v.uv0.y, v.uv1.x);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float Triplanar(float3 pos, float3 n, float scale)
            {
                float3 w = abs(n);
                w = w * w * w * w;
                w /= (w.x + w.y + w.z + 1e-5);
                float a = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, pos.yz * scale).r;
                float b = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, pos.xz * scale).r;
                float c = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, pos.xy * scale).r;
                return a * w.x + b * w.y + c * w.z;
            }

            void Ground(float3 pos,float3 normal,float layer,out float3 color,out float3 bump,out float rough)
            {
                float3 w=pow(abs(normal),4); w/=max(dot(w,1),1e-5);
                float2 x=pos.yz/16, y=pos.xz/16,z=pos.xy/16;
                color=SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundAlbedo,sampler_GroundAlbedo,x,layer,_TextureBias).rgb*w.x
                    +SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundAlbedo,sampler_GroundAlbedo,y,layer,_TextureBias).rgb*w.y
                    +SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundAlbedo,sampler_GroundAlbedo,z,layer,_TextureBias).rgb*w.z;
                float3 nx=SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundNormal,sampler_GroundNormal,x,layer,_TextureBias).xyz*2-1;
                float3 ny=SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundNormal,sampler_GroundNormal,y,layer,_TextureBias).xyz*2-1;
                float3 nz=SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundNormal,sampler_GroundNormal,z,layer,_TextureBias).xyz*2-1;
                bump=float3(nx.z*sign(normal.x),nx.x,nx.y)*w.x+float3(ny.x,ny.z*sign(normal.y),ny.y)*w.y+float3(nz.x,nz.y,nz.z*sign(normal.z))*w.z;
                rough=SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundMaterial,sampler_GroundMaterial,x,layer,_TextureBias).g*w.x
                    +SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundMaterial,sampler_GroundMaterial,y,layer,_TextureBias).g*w.y
                    +SAMPLE_TEXTURE2D_ARRAY_BIAS(_GroundMaterial,sampler_GroundMaterial,z,layer,_TextureBias).g*w.z;
            }

            // Satellite landscape texture of one class as a ratio to its mean (stored x0.5): adds structure at finer
            // scales than the orbital map without changing its colour. Body-fixed triplanar, sharp blend.
            float3 SatelliteRatio(float3 bodyPos,float3 radial,float layer,float metresPerTexel)
            {
                float3 w=abs(radial);w*=w;w*=w*w;w/=max(dot(w,1),1e-5);
                float3 q=bodyPos/(metresPerTexel*1024);
                // Broad continuous distortion avoids matching tiles along the projection's blend seams.
                q+=sin(bodyPos.yzx/41000+float3(1.7,4.3,2.1))*.35;
                float3 c=0;
                if(w.x>.02) c+=SAMPLE_TEXTURE2D_ARRAY_BIAS(_SatelliteDetail,sampler_SatelliteDetail,q.yz,layer,_TextureBias).rgb*w.x;
                if(w.y>.02) c+=SAMPLE_TEXTURE2D_ARRAY_BIAS(_SatelliteDetail,sampler_SatelliteDetail,q.zx,layer,_TextureBias).rgb*w.y;
                if(w.z>.02) c+=SAMPLE_TEXTURE2D_ARRAY_BIAS(_SatelliteDetail,sampler_SatelliteDetail,q.xy,layer,_TextureBias).rgb*w.z;
                return c*2/max(w.x*(w.x>.02)+w.y*(w.y>.02)+w.z*(w.z>.02),1e-3);
            }
            float GGX(float nh,float a) { float a2=a*a; float d=nh*nh*(a2-1)+1; return a2/(PI*d*d); }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = SafeNormalize(i.normalWS);
                float water = i.color.a;
                float3 albedo = i.color.rgb;
                float dist = length(i.positionWS - _WorldSpaceCameraPos) * _DistanceScale;
                float3 nOS = normalize(i.normalOS);
                float dNear = Triplanar(i.detailPos, nOS, _DetailScaleNear);
                float dFar = Triplanar(i.detailPos, nOS, _DetailScaleFar);
                float nearFade = saturate(1.0 - dist / 450.0);
                float farFade = saturate(1.0 - dist / 9000.0);
                float detail = lerp(1.0, lerp(0.72, 1.28, dNear), nearFade * _DetailStrength)
                             * lerp(1.0, lerp(0.8, 1.2, dFar), farFade * _DetailStrength);
                albedo *= lerp(detail, 1.0, water);

                float3 bodyPos=mul((float3x3)_WorldToBody,(i.positionWS-_BodyCenter.xyz)*_DistanceScale);
                float3 radial=normalize(bodyPos);
                float2 geo=PlanetUV(radial);
                // Seamless gradients (Tarini): the longitude wraps at the dateline, where plain derivatives jump a
                // whole turn and pick the smallest mip, drawing a line down the planet.
                float2 geoAlt=float2(frac(geo.x+.5)-.5,geo.y);
                float2 gdx=ddx(geo),gdy=ddy(geo),adx=ddx(geoAlt),ady=ddy(geoAlt);
                if(abs(gdx.x)+abs(gdy.x)>abs(adx.x)+abs(ady.x)) { gdx.x=adx.x; gdy.x=ady.x; }
                float footprint=length(ddx(bodyPos))+length(ddy(bodyPos));
                float roughness=.85;
                float depth=0;
                if(_SurfaceEnabled>.5)
                {
                    float4 mask=SAMPLE_TEXTURE2D_GRAD(_SurfaceMask,sampler_SurfaceMask,geo,gdx,gdy);
                    water=_MapMode>.5 ? mask.g : water;
                    depth=mask.b*5100;
                    float3 orbital=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,geo,gdx,gdy).rgb*_BaseColor.rgb;
                    // A satellite-textured orbital map is the better colour at every distance; ground materials and
                    // landscape detail supply the close structure. Otherwise vertex colours keep fine painted detail.
                    albedo=_MapMode>.5 || _OrbitalNear>.5 ? orbital : lerp(i.color.rgb,orbital,smoothstep(500,6000,dist));
                    if(_BiomeMode<.5)
                    {
                        float3 east=normalize(float3(radial.z,0,-radial.x)+float3(1e-6,0,0));
                        float3 north=normalize(cross(radial,east));
                        float3 on=SAMPLE_TEXTURE2D_GRAD(_SurfaceNormal,sampler_SurfaceNormal,geo,gdx,gdy).xyz*2-1;
                        on.xy*=_SurfaceNormalStrength;
                        float3 macro=normalize(east*on.x+north*on.y+radial*on.z);
                        n=normalize(lerp(n,mul(transpose((float3x3)_WorldToBody),macro),smoothstep(1000,10000,dist)));
                        // Satellite landscape detail where the orbital map runs out of resolution, handing over to the
                        // photographed ground materials near the camera.
                        // Three octaves, each fading in once the previous one is magnified past a few pixels per texel.
                        // Near the camera a faint satellite layer remains to break up the ground materials' tiling.
                        float satellite=_SatelliteStrength*(1-water)*step(mask.a,.99)*lerp(.35,1,saturate(dist/1200-.3))*(1-_MapMode);
                        if(satellite>.01) {
                            float layer=round(mask.a*255/40);
                            float fA=satellite*(1-smoothstep(450,1300,footprint));
                            float fB=satellite*.8*(1-smoothstep(110,320,footprint));
                            float fC=satellite*.7*(1-smoothstep(25,80,footprint));
                            if(fA>.01) albedo*=lerp(1,pow(max(SatelliteRatio(bodyPos,radial,layer,105),0),1.3),fA);
                            if(fB>.01) albedo*=lerp(1,pow(max(SatelliteRatio(bodyPos,radial,layer,26),0),1.3),fB);
                            if(fC>.01) albedo*=lerp(1,pow(max(SatelliteRatio(bodyPos+3700,radial,layer,6.5),0),1.4),fC);
                        }
                        float near=saturate(1.25-dist/2000)*_DetailStrength*(1-water);
                        if(near>.01 && _MapMode<.5)
                        {
                            float layer=round(mask.r*255/40);
                            float3 nb=mul((float3x3)_WorldToBody,n),c1,c2,b1,b2; float r1,r2;
                            Ground(i.detailPos,nb,layer,c1,b1,r1); Ground(i.detailPos,nb,3,c2,b2,r2);
                            float slope=smoothstep(.9,.55,dot(nb,radial));
                            float3 c=lerp(c1,c2,slope), bn=normalize(lerp(b1,b2,slope));
                            // Keep the baked surface colour, with photographed small-scale structure: the ratio to the
                            // material's own average (its last mip) keeps light and dark detail, not only hue.
                            float3 average=lerp(SAMPLE_TEXTURE2D_ARRAY_LOD(_GroundAlbedo,sampler_GroundAlbedo,float2(.5,.5),layer,12).rgb,
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_GroundAlbedo,sampler_GroundAlbedo,float2(.5,.5),3,12).rgb,slope);
                            // Mostly light and shade from the photograph, a little of its hue: a full per-channel
                            // ratio oversaturated the baked colour into a lawn.
                            float3 ratio=clamp(c/max(average,.02),.3,2.2);
                            float lumRatio=dot(ratio*average,float3(.2126,.7152,.0722))/max(dot(average,float3(.2126,.7152,.0722)),.02);
                            albedo*=lerp(1,lerp(lumRatio.xxx,ratio,.3),near*.8);
                            n=normalize(lerp(n,mul(transpose((float3x3)_WorldToBody),bn),near*.65));
                            roughness=lerp(r1,r2,slope);
                        }
                        if(water>.01)
                        {
                            float waveFade=saturate(1-dist/22000)*(1-_MapMode);
                            float2 phase=i.detailPos.xz*(6.28318530718/16)+float2(_OceanTime,-_OceanTime*2);
                            float2 wave=float2(sin(phase.x+sin(phase.y)),cos(phase.y+sin(phase.x*2)))*.035*waveFade;
                            n=normalize(n+mul(transpose((float3x3)_WorldToBody),east*wave.x+north*wave.y)*water);
                            roughness=lerp(roughness,_OceanRoughness,water);
                            float foam=saturate(1-mask.b*100)*saturate(sin(phase.x*.5+phase.y)*.7+.25)*water*waveFade;
                            albedo=lerp(albedo,float3(.65,.72,.71),foam*.65);
                        }
                    }
                    else { n=normalize(i.normalWS); water=0; }
                }

                if(_SurfaceEnabled>.5 && _BiomeMode<.5) {
                    // Water colour from depth: bright turquoise over sandy shelves, navy offshore, darker over the abyss.
                    float3 ocean=lerp(_OceanShallow.rgb,_OceanColour.rgb,smoothstep(3,140,depth))*lerp(1,.6,smoothstep(300,3000,depth));
                    albedo=lerp(albedo,ocean,water*_OceanColourBlend);
                    float luminance=dot(albedo,float3(.2126,.7152,.0722));
                    albedo=lerp(luminance.xxx,albedo,_SurfaceSaturation)*_SurfaceTint.rgb;
                }
                float3 lightDir, lightColor;
                float shadow;
                if (_BodySunDir.w > 0.5)
                {
                    lightDir = normalize(_BodySunDir.xyz);
                    lightColor = _BodySunColor.rgb;
                    // Local terrain still receives nearby geometry shadows. Far/map views have
                    // separate scaled depth and must not sample the flight camera's shadow atlas.
                    shadow = _DistanceScale <= 1.0 ? MainLightRealtimeShadow(TransformWorldToShadowCoord(i.positionWS)) : 1.0;
                }
                else
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    lightDir = mainLight.direction;
                    lightColor = mainLight.color;
                    shadow = mainLight.shadowAttenuation;
                }
                float ndl = saturate(dot(n, lightDir));
                if(_CloudEnabled>.5 && _BiomeMode<.5 && ndl>.01)
                {
                    float3 sunBF=mul((float3x3)_WorldToBody,lightDir);
                    float2 hit=PlanetSphere(bodyPos,sunBF,_Radius+_CloudParams.x);
                    uint weatherWidth,weatherHeight;_Weather.GetDimensions(weatherWidth,weatherHeight);
                    float weatherLod=WeatherMip(length(ddx(bodyPos))+length(ddy(bodyPos)),_Radius,weatherWidth);
                    if(hit.y>0) shadow*=1-WeatherCoverage(_Weather,sampler_Weather,bodyPos+sunBF*hit.y,_CloudParams.w,_CloudParams.y,weatherLod)*_CloudParams.z;
                }
                float3 ambient = lerp(_SpaceAmbient.rgb, SampleSH(n), _SkyAmbient) * _AmbientBoost;
                float3 lit = albedo * (lightColor * ndl * shadow + ambient);

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float3 h = SafeNormalize(lightDir + viewDir);
                float nv=saturate(dot(n,viewDir))+1e-4,nh=saturate(dot(n,h)),vh=saturate(dot(viewDir,h));
                // GGX sun glint. Waves below the pixel widen it with distance: orbit sees a broad glitter patch.
                float alpha=min(.6,lerp(.05,.3,saturate(roughness))*lerp(1,2.4,smoothstep(2000,250000,dist)));
                float a2=alpha*alpha;
                float vis=.5/max(ndl*sqrt(nv*nv*(1-a2)+a2)+nv*sqrt(ndl*ndl*(1-a2)+a2),1e-4);
                float waterFresnel=.02+.98*pow(1-vh,5);
                float spec=GGX(nh,alpha)*vis*waterFresnel*ndl*PI*water*_WaterSpecular;
                float fres = pow(1.0 - saturate(dot(n, viewDir)), 5.0) * water * 0.25;
                lit += lightColor * min(spec,40) * shadow + fres * ambient;

                lit = lit * _Transmittance + _HazeColor.rgb;
                lit = MixFog(lit, i.fogFactor);
                if (_BiomeMode > .5) lit = albedo;
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 direction = mul((float3x3)GetObjectToWorldMatrix(), v.normalOS);
                direction /= max(max(abs(direction.x), max(abs(direction.y), abs(direction.z))), 1e-20);
                o.normalWS = SafeNormalize(direction); return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }
    FallBack Off
}
