Shader "UI/StanceVignette"
{
    Properties
    {
        _Color      ("Tint Color", Color)           = (1, 0.78, 0.26, 1)
        _Intensity  ("Edge Intensity", Range(0, 1)) = 0.18
        _Radius     ("Clear Radius",  Range(0, 2))  = 0.65
        _Softness   ("Edge Softness", Range(0.01, 2)) = 0.75

        // Required by Unity UI masking
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil     ("Stencil ID",         Float) = 0
        _StencilOp   ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Overlay"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _Color;
            float  _Intensity;
            float  _Radius;
            float  _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Remap UV so center is (0,0), corners reach ~1.0
                float2 centered = (i.uv - 0.5) * 2.0;

                // Elliptical distance normalised so corners = 1.0
                float dist = length(centered) * 0.707; // 1/sqrt(2) normalisation

                // Smooth falloff: fully transparent inside _Radius,
                // ramps gently to _Intensity at the far edges
                float vignette = smoothstep(_Radius, _Radius + _Softness, dist);

                fixed4 col = _Color * i.color;
                col.a = vignette * _Intensity * i.color.a;

                return col;
            }
            ENDCG
        }
    }
}
