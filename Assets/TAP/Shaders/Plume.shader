Shader "TAP/Plume"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.6, 0.25, 1)
        _CoreColor ("Core Colour", Color) = (1, 0.95, 0.85, 1)
        _Intensity ("Intensity", Float) = 1
        _Falloff ("Length Falloff", Float) = 1.6
        _Flicker ("Flicker", Float) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _CoreColor;
                float _Intensity;
                float _Falloff;
                float _Flicker;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.uv = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float along = saturate(i.uv.y);                      // 0 at nozzle, 1 at tail
                float fres = abs(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float soft = pow(fres, 1.5);                         // soft edges
                float fade = pow(1.0 - along, _Falloff);
                float flick = 1.0 + _Flicker * sin(_Time.y * 60.0 + along * 20.0);
                float3 col = lerp(_Color.rgb, _CoreColor.rgb, pow(fres, 6.0) * (1.0 - along));
                float a = soft * fade * _Intensity * flick;
                return half4(col * a, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
