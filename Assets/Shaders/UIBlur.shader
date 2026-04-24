Shader "UI/Blur"
{
    Properties
    {
        _Color ("Tint Color", Color) = (0, 0, 0, 0.6)
        _BlurSize ("Blur Size", Range(0, 10)) = 3
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }

    Category
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        SubShader
        {
            // Horizontal blur pass
            GrabPass { "_GrabTexture" }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float4 grabPos : TEXCOORD0;
                };

                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _BlurSize;
                fixed4 _Color;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.grabPos = ComputeGrabScreenPos(o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 uv = i.grabPos.xy / i.grabPos.w;
                    float2 texelSize = _GrabTexture_TexelSize.xy * _BlurSize;

                    // 9-tap Gaussian blur (horizontal + vertical combined)
                    fixed4 col = fixed4(0, 0, 0, 0);
                    float weights[5] = { 0.227027, 0.194594, 0.121622, 0.054054, 0.016216 };

                    col += tex2D(_GrabTexture, uv) * weights[0];
                    for (int j = 1; j < 5; j++)
                    {
                        col += tex2D(_GrabTexture, uv + float2(texelSize.x * j, 0)) * weights[j];
                        col += tex2D(_GrabTexture, uv - float2(texelSize.x * j, 0)) * weights[j];
                        col += tex2D(_GrabTexture, uv + float2(0, texelSize.y * j)) * weights[j];
                        col += tex2D(_GrabTexture, uv - float2(0, texelSize.y * j)) * weights[j];
                    }

                    // Normalize (horizontal + vertical samples combined)
                    col /= (weights[0] + 2.0 * (weights[1] + weights[2] + weights[3] + weights[4]));

                    // Apply tint
                    col.rgb = lerp(col.rgb, _Color.rgb, _Color.a);
                    col.a = 1.0;
                    return col;
                }
                ENDCG
            }
        }
    }
}
