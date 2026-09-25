Shader "TAP/OrbitLine"
{
    // Screen-space constant-width lines. Each segment is a quad whose 4 vertices carry the segment start
    // (POSITION), end (NORMAL), a side sign (TEXCOORD0.x) and an end selector (TEXCOORD0.y).
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _Width ("Width (px)", Float) = 2.5
        _DashLength ("Dash (0=solid)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+50" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 otherOS : NORMAL;
                float4 uv : TEXCOORD0;   // x: side (-1/+1), y: 0 = this is start, 1 = this is end, z: distance along line, w: alpha
                float4 color : COLOR;
            };
            struct Varyings { float4 positionCS : SV_POSITION; float side : TEXCOORD0; float along : TEXCOORD1; float4 color : COLOR; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Width;
                float _DashLength;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 a = TransformObjectToHClip(v.positionOS.xyz);
                float4 b = TransformObjectToHClip(v.otherOS);
                // Guard against points behind the camera.
                float wa = max(a.w, 1e-4);
                float wb = max(b.w, 1e-4);
                float2 sa = a.xy / wa;
                float2 sb = b.xy / wb;
                float2 dir = (sb - sa) * _ScreenParams.xy;
                if (v.uv.y > 0.5) dir = -dir;
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(1, 0);
                float2 nrm = float2(-dir.y, dir.x);
                float2 offset = nrm * v.uv.x * _Width / _ScreenParams.xy;
                o.positionCS = a;
                o.positionCS.xy += offset * a.w;
                o.side = v.uv.x;
                o.along = v.uv.z;
                o.color = v.color * _Color;
                o.color.a *= v.uv.w;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float edge = 1.0 - smoothstep(0.6, 1.0, abs(i.side));
                float4 c = i.color;
                c.a *= edge;
                if (_DashLength > 0)
                {
                    float d = frac(i.along / _DashLength);
                    c.a *= step(d, 0.55);
                }
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
