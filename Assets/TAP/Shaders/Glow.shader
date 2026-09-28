Shader "TAP/Glow"
{
    // Additive glow on a camera-facing quad, shaped by the angle from the quad's centre in units of _Radius (radians):
    // the star's corona (a bright ring fading out plus a wide faint halo) and points of light (a small gaussian dot).
    Properties
    {
        _Color ("Colour (linear HDR, not converted)", Vector) = (1, 1, 1, 1)
        _Radius ("Angular Radius (rad)", Float) = 0.01
        _Extent ("Quad Half-Size (in radii)", Float) = 10
        _Near ("Ring Brightness", Float) = 0
        _NearFalloff ("Ring Falloff", Float) = 6
        _Far ("Halo Brightness", Float) = 0
        _FarPower ("Halo Power", Float) = 2
        _Gauss ("Dot Brightness", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Radius;
                float _Extent;
                float _Near;
                float _NearFalloff;
                float _Far;
                float _FarPower;
                float _Gauss;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 centre = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float3 rd = normalize(i.positionWS - _WorldSpaceCameraPos);
                float3 cd = normalize(centre - _WorldSpaceCameraPos);
                // atan2 of the cross and dot products keeps small angles exact (acos of a dot near 1 does not).
                float theta = atan2(length(cross(rd, cd)), dot(rd, cd));
                float x = theta / max(_Radius, 1e-9);
                float glow = _Near * exp(-_NearFalloff * max(x - 1.0, 0.0))
                           + _Far * pow(max(x, 1.0), -_FarPower)
                           + _Gauss * exp(-2.0 * x * x);
                // Fade out before the quad's edge so its square never shows.
                glow *= saturate((_Extent - x) / (_Extent * 0.3));
                return half4(_Color.rgb * glow, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
