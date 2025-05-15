Shader "Custom/HeightBasedTerrain"
{
    Properties
    {
        _MinHeight ("Min Height", Range(0,20)) = 0
        _MidHeight ("Mid (Max) Height", Range(10,100)) = 20

        _LowColor ("Low Altitude Color", Color) = (0.4, 0.26, 0.13, 1)
        _LowMidColor ("Low-Mid Altitude Color", Color) = (0.25, 0.35, 0.12, 1)
        _MidColor ("Mid Altitude Color", Color) = (0.1, 0.5, 0.1, 1)
        _HighMidColor ("High-Mid Altitude Color", Color) = (0.6, 0.6, 0.6, 1)
        _HighColor ("High Altitude Color", Color) = (0.8, 0.8, 0.8, 1)

        [Toggle(_USE_GRADIENT)] _UseGradient ("Use Gradient Blending", Float) = 1
        _GradientIntensity ("Gradient Intensity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_GRADIENT
            #include "UnityCG.cginc"

            float _MinHeight;
            float _MidHeight;

            fixed4 _LowColor;
            fixed4 _LowMidColor;
            fixed4 _MidColor;
            fixed4 _HighMidColor;
            fixed4 _HighColor;

            float _UseGradient;
            float _GradientIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.height = v.vertex.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Vertex yüksekliği
                float h = i.height;

                // Height değerini [MinHeight, MidHeight] aralığında kısıtla
                h = clamp(h, _MinHeight, _MidHeight);

                fixed3 baseColor;

                if (_UseGradient < 0.5)
                {
                    // Gradient kapalı: katmanlar arası keskin geçişler
                    float range = _MidHeight - _MinHeight;
                    float normalizedHeight = (h - _MinHeight) / range;

                    if (normalizedHeight < 0.2) baseColor = _LowColor.rgb;
                    else if (normalizedHeight < 0.4) baseColor = _LowMidColor.rgb;
                    else if (normalizedHeight < 0.6) baseColor = _MidColor.rgb;
                    else if (normalizedHeight < 0.8) baseColor = _HighMidColor.rgb;
                    else baseColor = _HighColor.rgb;
                }
                else
                {
                    // Gradient açık: katmanlar arası yumuşak geçişler
                    float range = _MidHeight - _MinHeight;
                    float t = saturate((h - _MinHeight) / range);

                    fixed3 colorLowMid;
                    if (t < 0.25)
                        colorLowMid = lerp(_LowColor.rgb, _LowMidColor.rgb, t / 0.25);
                    else if (t < 0.5)
                        colorLowMid = lerp(_LowMidColor.rgb, _MidColor.rgb, (t - 0.25) / 0.25);
                    else if (t < 0.75)
                        colorLowMid = lerp(_MidColor.rgb, _HighMidColor.rgb, (t - 0.5) / 0.25);
                    else
                        colorLowMid = lerp(_HighMidColor.rgb, _HighColor.rgb, (t - 0.75) / 0.25);

                    // En yakın katman rengini hesapla (keskin geçişteki gibi)
                    fixed3 nearestColor;
                    if (t < 0.2) nearestColor = _LowColor.rgb;
                    else if (t < 0.4) nearestColor = _LowMidColor.rgb;
                    else if (t < 0.6) nearestColor = _MidColor.rgb;
                    else if (t < 0.8) nearestColor = _HighMidColor.rgb;
                    else nearestColor = _HighColor.rgb;

                    // GradientIntensity ile blend işlemi
                    baseColor = lerp(nearestColor, colorLowMid, _GradientIntensity);
                }

                return fixed4(baseColor, 1.0);
            }
            ENDCG
        }
    }
}
