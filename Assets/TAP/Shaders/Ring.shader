Shader "TAP/Ring"
{
    // The rings of a planet: a flat annulus in its equatorial plane. Opacity, colour and ringlets come from the body's
    // ring definitions (a 4096x2 profile texture: row 0 colour and opacity, row 1 ringlet strength) plus fine ringlets
    // made here, so they stay sharp up close. Lit by the star, dimmed in the planet's shadow, seen from both sides.
    Properties
    {
        _Profile ("Ring profile", 2D) = "white" {}
        _RingRadii ("Inner, outer, planet radius (m)", Vector) = (1, 2, 1, 0)
        _DistanceScale ("Metres per unit", Float) = 1
        _RingNormal ("Ring plane normal (world)", Vector) = (0, 1, 0, 0)
        _BodyCenter ("Body centre (world)", Vector) = (0, 0, 0, 0)
        _SunDir ("Direction to the star", Vector) = (0, 1, 0, 0)
        _SunColor ("Star light (linear); w = angular radius (rad)", Vector) = (1, 1, 1, 0.002)
        _Ambient ("Ambient (linear)", Vector) = (0.012, 0.013, 0.016, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Profile); SAMPLER(sampler_Profile);

            CBUFFER_START(UnityPerMaterial)
                float4 _RingRadii;
                float _DistanceScale;
                float4 _RingNormal;
                float4 _BodyCenter;
                float4 _SunDir;
                float4 _SunColor;
                float4 _Ambient;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            float Hash(float x) { return frac(sin(x * 127.1 + 311.7) * 43758.5453); }

            // One-dimensional value noise in 0..1.
            float Noise1(float x)
            {
                float i = floor(x), f = frac(x);
                f = f * f * (3 - 2 * f);
                return lerp(Hash(i), Hash(i + 1), f);
            }

            // Ringlets: bands of different brightness at several scales, finer where the camera is closer.
            float Ringlets(float r, float width)
            {
                float n = Noise1(r / width * 90) * 0.45 + Noise1(r / width * 410) * 0.3 + Noise1(r / width * 1900) * 0.15
                        + Noise1(r / width * 9000) * 0.1;
                return n;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 rel = (i.positionWS - _BodyCenter.xyz) * _DistanceScale;          // metres from the planet's centre
                float3 normal = normalize(_RingNormal.xyz);                              // the ring plane's normal: the spin axis, tilted
                float r = length(rel - normal * dot(rel, normal));                       // distance from the centre within the plane
                float inner = _RingRadii.x, outer = _RingRadii.y, width = outer - inner;
                if (r < inner || r > outer) discard;
                float u = (r - inner) / width;
                float4 prof = SAMPLE_TEXTURE2D_LOD(_Profile, sampler_Profile, float2(u, 0.25), 0);
                float ringletAmount = SAMPLE_TEXTURE2D_LOD(_Profile, sampler_Profile, float2(u, 0.75), 0).r;
                float n = Ringlets(r, width);
                float variation = 1 - ringletAmount * (0.15 + 0.85 * n);
                float opacity = saturate(prof.a * variation);
                if (opacity < 0.002) discard;
                float tauN = -log(max(1 - opacity, 1e-3));                               // optical depth at normal incidence

                float3 L = normalize(_SunDir.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                float nl = dot(normal, L), nv = dot(normal, V);
                float muS = max(abs(nl), 0.04), muV = max(abs(nv), 0.04);
                bool sameSide = nl * nv > 0;

                // The planet's shadow: a ray from here to the star, its closest approach to the centre against the radius,
                // with the penumbra of the star's disc.
                float tCa = -dot(rel, L);
                float shadow = 1;
                if (tCa > 0)
                {
                    float d = length(rel + L * tCa);
                    float pen = max(tCa * _SunColor.w, 1.0);
                    shadow = smoothstep(_RingRadii.z - pen, _RingRadii.z + pen, d);
                }

                // Light from the star scattered back (the lit side) or forward through the ring (the dark side).
                float intercepted = 1 - exp(-tauN / muS);
                float back = intercepted * 0.9;
                float forward = intercepted * exp(-0.55 * tauN / muS) * 0.55 + intercepted * 0.12;
                float scatter = sameSide ? back : forward;
                float3 albedo = prof.rgb * (0.8 + 0.4 * n);
                float3 lit = albedo * (_SunColor.rgb * scatter * shadow * abs(nl) + _Ambient.rgb);
                // Seen edge-on a ring is thin: the optical depth along the view ray gives its opacity.
                float alpha = 1 - exp(-tauN / muV);
                return half4(lit, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
