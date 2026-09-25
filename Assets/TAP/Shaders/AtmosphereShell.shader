Shader "TAP/AtmosphereShell"
{
    Properties
    {
        _Color ("Scatter Colour", Color) = (0.35, 0.6, 1, 1)
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _Intensity ("Intensity", Float) = 1.2
        _PlanetCenter ("Planet Centre (world)", Vector) = (0, 0, 0, 0)
        _PlanetRadius ("Planet Radius", Float) = 600000
        _AtmoRadius ("Atmosphere Radius", Float) = 670000
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Front

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
                float4 _SunDir;
                float _Intensity;
                float4 _PlanetCenter;
                float _PlanetRadius;
                float _AtmoRadius;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            // Approximate optical depth of the view ray through the shell (analytic chord lengths).
            half4 frag(Varyings i) : SV_Target
            {
                float3 ro = _WorldSpaceCameraPos - _PlanetCenter.xyz;
                float3 rd = normalize(i.positionWS - _WorldSpaceCameraPos);
                float b = dot(ro, rd);
                float c = dot(ro, ro) - _AtmoRadius * _AtmoRadius;
                float disc = b * b - c;
                if (disc <= 0) return 0;
                float sq = sqrt(disc);
                float t0 = max(-b - sq, 0.0);
                float t1 = -b + sq;
                // stop at the planet surface if the ray hits it
                float cp = dot(ro, ro) - _PlanetRadius * _PlanetRadius;
                float discP = b * b - cp;
                if (discP > 0)
                {
                    float tp = -b - sqrt(discP);
                    if (tp > 0) t1 = min(t1, tp);
                }
                float len = max(t1 - t0, 0.0);
                float thickness = _AtmoRadius - _PlanetRadius;
                float depth = len / (thickness * 6.0);
                // closest approach altitude -> denser near the limb
                float3 mid = ro + rd * ((t0 + t1) * 0.5);
                float midAlt = saturate((length(mid) - _PlanetRadius) / thickness);
                float density = exp(-midAlt * 3.0);
                float3 n = normalize(mid);
                float sunLit = saturate(dot(n, normalize(_SunDir.xyz)) * 1.2 + 0.25);
                float a = saturate(depth * density) * sunLit * _Intensity;
                return half4(_Color.rgb * a, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
