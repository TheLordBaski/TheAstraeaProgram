Shader "TAP/Overlay"
{
    // Unlit translucent marker shading (assembly-building nodes, CoM/CoT/CoP markers).
    // _ZTest = 8 (Always) draws through geometry, 4 (LessEqual) respects depth.
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ZTest;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(ws);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float ndv = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float4 c = _Color;
                c.rgb *= 0.5 + 0.5 * ndv;
                c.a *= 0.75 + 0.25 * ndv;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
