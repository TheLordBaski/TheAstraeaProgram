Shader "TAP/Terrain"
{
    Properties
    {
        _DetailTex ("Detail Noise (tileable)", 2D) = "gray" {}
        _DetailScaleNear ("Detail Scale Near (1/m)", Float) = 0.22
        _DetailScaleFar ("Detail Scale Far (1/m)", Float) = 0.012
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.45
        _WaterSpecular ("Water Specular", Float) = 1.3
        _AmbientBoost ("Ambient Boost", Float) = 1.0
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float4 _DetailTex_ST;
                float _DetailScaleNear;
                float _DetailScaleFar;
                float _DetailStrength;
                float _WaterSpecular;
                float _AmbientBoost;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
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

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float water = i.color.a;
                float3 albedo = i.color.rgb;
                float dist = length(i.positionWS - _WorldSpaceCameraPos);
                float3 nOS = normalize(i.normalOS);
                float dNear = Triplanar(i.detailPos, nOS, _DetailScaleNear);
                float dFar = Triplanar(i.detailPos, nOS, _DetailScaleFar);
                float nearFade = saturate(1.0 - dist / 450.0);
                float farFade = saturate(1.0 - dist / 9000.0);
                float detail = lerp(1.0, lerp(0.72, 1.28, dNear), nearFade * _DetailStrength)
                             * lerp(1.0, lerp(0.8, 1.2, dFar), farFade * _DetailStrength);
                albedo *= lerp(detail, 1.0, water);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = saturate(dot(n, mainLight.direction));
                float3 ambient = SampleSH(n) * _AmbientBoost;
                float3 lit = albedo * (mainLight.color * ndl * mainLight.shadowAttenuation + ambient);

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float3 h = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(n, h)), 180.0) * water * _WaterSpecular;
                float fres = pow(1.0 - saturate(dot(n, viewDir)), 5.0) * water * 0.25;
                lit += mainLight.color * spec * mainLight.shadowAttenuation + fres * ambient;

                lit = MixFog(lit, i.fogFactor);
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
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }
    FallBack Off
}
