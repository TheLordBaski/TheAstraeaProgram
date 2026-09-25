Shader "TAP/Sky"
{
    Properties
    {
        _SkyColor ("Zenith Colour", Color) = (0.3, 0.55, 0.95, 1)
        _HorizonColor ("Horizon Colour", Color) = (0.75, 0.85, 1, 1)
        _SunsetColor ("Sunset Colour", Color) = (1, 0.5, 0.2, 1)
        _Atmosphere ("Atmosphere Amount", Range(0, 1)) = 1
        _SunDir ("Sun Direction", Vector) = (0, 1, 0, 0)
        _UpDir ("Local Up", Vector) = (0, 1, 0, 0)
        _StarTex ("Stars (equirect)", 2D) = "black" {}
        _StarBrightness ("Star Brightness", Float) = 1
        _SunSize ("Sun Angular Radius (rad)", Float) = 0.012
        _HorizonDip ("Horizon Dip (rad)", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest [_ZTest]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            TEXTURE2D(_StarTex);
            SAMPLER(sampler_StarTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyColor;
                float4 _HorizonColor;
                float4 _SunsetColor;
                float _Atmosphere;
                float4 _SunDir;
                float4 _UpDir;
                float4 _StarTex_ST;
                float _StarBrightness;
                float _SunSize;
                float _HorizonDip;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 up = normalize(_UpDir.xyz);
                float3 sun = normalize(_SunDir.xyz);
                float elev = dot(d, up) + _HorizonDip;
                float sunElev = dot(sun, up);
                float sunDot = dot(d, sun);

                // Rayleigh-ish gradient: bright pale horizon, deeper zenith.
                float h = 1.0 - saturate(elev);
                float3 sky = lerp(_SkyColor.rgb, _HorizonColor.rgb, pow(h, 5.0));
                // Below the geometric horizon the sky fades (terrain normally covers it).
                sky = lerp(sky, _HorizonColor.rgb * 0.6, saturate(-elev * 4.0));
                // Daylight factor and sunset tint.
                float day = saturate(sunElev * 3.0 + 0.25);
                float sunset = saturate(1.0 - abs(sunElev) * 5.0) * pow(saturate(sunDot * 0.5 + 0.5), 3.0);
                sky = sky * day + _SunsetColor.rgb * sunset * pow(h, 3.0) * 0.9;
                // Forward scattering glow around the sun.
                sky += _HorizonColor.rgb * pow(saturate(sunDot), 12.0) * 0.35 * day;

                float atm = _Atmosphere;
                float3 col = sky * atm;

                // Stars (hidden by a bright daytime sky).
                float lon = atan2(d.x, d.z) / (2.0 * PI) + 0.5;
                float lat = asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5;
                float3 stars = SAMPLE_TEXTURE2D_LOD(_StarTex, sampler_StarTex, float2(lon, lat), 0).rgb;
                float starVis = saturate(1.0 - atm * (0.3 + day * 1.5));
                col += stars * _StarBrightness * starVis;

                // Sun disc
                float sunDisc = smoothstep(cos(_SunSize * 1.3), cos(_SunSize), sunDot);
                col += float3(1.0, 0.96, 0.88) * sunDisc * 6.0;
                col += float3(1.0, 0.9, 0.75) * pow(saturate(sunDot), 800.0) * 1.5 * (1.0 - atm * 0.5);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
