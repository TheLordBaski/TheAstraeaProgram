Shader "TAP/NavballUI"
{
    // Renders the navball as an orthographic view of a textured sphere inside a UI RawImage.
    // Each pixel's camera-frame direction (front hemisphere) is rotated into the local horizon frame
    // (x = east, y = up, z = north) by _Rot and used to sample an equirectangular navball texture.
    Properties
    {
        [PerRendererData] _MainTex ("Navball Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float4 _Color;
            float4x4 _Rot;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r2 = dot(p, p);
                float edge = fwidth(sqrt(r2)) * 1.5 + 1e-4;
                float alpha = 1.0 - smoothstep(1.0 - edge, 1.0, sqrt(r2));
                if (alpha <= 0) discard;
                float z = sqrt(saturate(1.0 - r2));
                float3 dc = float3(p.x, p.y, z);
                float3 dh = mul((float3x3)_Rot, dc);
                float lon = atan2(dh.x, dh.z) / (2.0 * UNITY_PI);
                lon = lon < 0 ? lon + 1.0 : lon;
                float lat = asin(clamp(dh.y, -1.0, 1.0)) / UNITY_PI + 0.5;
                float4 col = tex2Dlod(_MainTex, float4(lon, lat, 0, 0));
                float shade = 0.55 + 0.45 * pow(z, 0.6);
                col.rgb *= shade;
                // rim
                col.rgb = lerp(col.rgb, float3(0.08, 0.08, 0.1), smoothstep(0.93, 1.0, sqrt(r2)) * 0.6);
                col.a = alpha;
                return col * i.color;
            }
            ENDCG
        }
    }
}
