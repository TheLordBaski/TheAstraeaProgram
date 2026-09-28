Shader "TAP/Star"
{
    // The star's disc in the far view: an emissive sphere, far above the bloom threshold, darker towards the limb.
    Properties
    {
        _Color ("Colour", Color) = (1, 0.96, 0.9, 1)
        _Intensity ("Intensity (HDR)", Float) = 24
        _LimbDarkening ("Limb Darkening", Range(0, 1)) = 0.55
        _Tint ("Tint (extinction in an atmosphere)", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _LimbDarkening;
                float4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 toEye = normalize(_WorldSpaceCameraPos - i.positionWS);
                float mu = saturate(dot(n, toEye));
                float limb = 1.0 - _LimbDarkening * (1.0 - sqrt(mu));
                return half4(_Color.rgb * _Tint.rgb * (_Intensity * limb), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
