Shader "UI/ShimmerWipe"
{
    // Wipes a captured scene texture using a diagonal shimmer that matches
    // the main menu's MenuShimmerController exactly:
    //   - Same sweep angle (25 degrees)
    //   - Same band width (very wide, 1.2x diagonal)
    //   - Same alpha intensity (0.025 — barely perceptible)
    //   - Same gaussian * smoothstep^2 gradient profile
    // Left of the boundary: transparent (new scene visible).
    // Right of the boundary: captured old scene.
    Properties
    {
        [PerRendererData] _MainTex ("Captured Scene", 2D) = "white" {}
        _Progress ("Wipe Progress", Range(0, 1)) = 0

        // Very wide band matching menu's BAND_WIDTH_RATIO = 1.2 relative to diagonal
        _BandWidth ("Band Width (normalized)", Range(0.1, 2.0)) = 0.85

        // Same 25-degree angle as the menu shimmer
        _SweepAngle ("Sweep Angle (degrees)", Range(-45, 45)) = 25

        _ShimmerColor1 ("Shimmer Color 1", Color) = (0.961, 0.784, 0.259, 1)
        _ShimmerColor2 ("Shimmer Color 2", Color) = (0.353, 0.706, 0.941, 1)

        // Matches menu's MAX_ALPHA = 0.025
        _ShimmerIntensity ("Shimmer Additive Intensity", Range(0, 0.2)) = 0.025
        _ShimmerAlpha ("Shimmer Peak Alpha", Range(0, 0.1)) = 0.025

        // Unity UI stencil support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;

            float  _Progress;
            float  _BandWidth;
            float  _SweepAngle;
            float4 _ShimmerColor1;
            float4 _ShimmerColor2;
            float  _ShimmerIntensity;
            float  _ShimmerAlpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color    = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── Diagonal sweep coordinate (25 degrees, same as menu) ──
                float rad  = radians(_SweepAngle);
                float cosA = cos(rad);
                float sinA = sin(rad);
                float coord = i.texcoord.x * cosA + i.texcoord.y * sinA;
                float maxC  = abs(cosA) + abs(sinA);
                coord /= maxC; // normalize to ~0..1

                // Wipe boundary — generous margin so band fully enters/exits
                float margin  = _BandWidth * 1.5;
                float wipePos = lerp(-margin, 1.0 + margin, _Progress);

                float dist     = coord - wipePos;
                float bandHalf = _BandWidth * 0.5;

                // ── Old scene visibility ──
                float sceneAlpha = smoothstep(-bandHalf * 0.6, bandHalf * 0.6, dist);

                // Sample captured scene
                fixed4 sceneTex = tex2D(_MainTex, i.texcoord);

                // ── Shimmer glow ──
                float nd       = clamp(dist / bandHalf, -1.0, 1.0);
                float bell     = exp(-4.0 * nd * nd);
                float edgeFade = smoothstep(0.0, 1.0, 1.0 - abs(nd));
                float shimmer  = bell * edgeFade * edgeFade;

                // Two-color variation along the band
                float colorMix = smoothstep(0.3, 0.7, frac(coord * 2.0 + _Progress));
                float3 shimCol = lerp(_ShimmerColor1.rgb, _ShimmerColor2.rgb, colorMix);

                // ── Compose ──
                float3 finalRGB   = sceneTex.rgb * sceneAlpha
                                  + shimCol * shimmer * _ShimmerIntensity;
                float  finalAlpha = max(sceneAlpha, shimmer * _ShimmerAlpha);

                fixed4 col = fixed4(finalRGB, finalAlpha);
                col *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
