Shader "Custom/HeightBasedTerrainWithMidHeightAndGradientIntensity"
{
    Properties
    {
        _MinHeight ("Min Height", Float) = 0
        _MidHeight ("Mid Height", Float) = 5
        _MaxHeight ("Max Height", Float) = 10

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
            float _MaxHeight;

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
                float h = i.height;

                fixed3 baseColor;

                // Keskin katmanlı renkler (gradient kapalı)
                if (_UseGradient < 0.5)
                {
                    if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.2) baseColor = _LowColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.4) baseColor = _LowMidColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.6) baseColor = _MidColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.8) baseColor = _HighMidColor.rgb;
                    else baseColor = _HighColor.rgb;
                }
                else
                {
                    // Gradient açık - katmanlar arası yumuşak geçişler
                    fixed3 colorLowMid;
                    fixed3 colorHighMid;
                    float t;

                    if (h < _MidHeight)
                    {
                        t = saturate((h - _MinHeight) / (_MidHeight - _MinHeight));
                        if (t < 0.25)
                            colorLowMid = lerp(_LowColor.rgb, _LowMidColor.rgb, t / 0.25);
                        else if (t < 0.5)
                            colorLowMid = lerp(_LowMidColor.rgb, _MidColor.rgb, (t - 0.25) / 0.25);
                        else if (t < 0.75)
                            colorLowMid = lerp(_MidColor.rgb, _HighMidColor.rgb, (t - 0.5) / 0.25);
                        else
                            colorLowMid = lerp(_HighMidColor.rgb, _HighColor.rgb, (t - 0.75) / 0.25);
                        baseColor = colorLowMid;
                    }
                    else
                    {
                        t = saturate((h - _MidHeight) / (_MaxHeight - _MidHeight));
                        if (t < 0.25)
                            colorHighMid = lerp(_LowColor.rgb, _LowMidColor.rgb, t / 0.25);
                        else if (t < 0.5)
                            colorHighMid = lerp(_LowMidColor.rgb, _MidColor.rgb, (t - 0.25) / 0.25);
                        else if (t < 0.75)
                            colorHighMid = lerp(_MidColor.rgb, _HighMidColor.rgb, (t - 0.5) / 0.25);
                        else
                            colorHighMid = lerp(_HighMidColor.rgb, _HighColor.rgb, (t - 0.75) / 0.25);
                        baseColor = colorHighMid;
                    }

                    // GradientIntensity ile blend yapıyoruz:
                    // baseColor ile en yakın katman rengini karıştıracağız

                    // En yakın katman rengi:
                    fixed3 nearestColor;
                    if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.2) nearestColor = _LowColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.4) nearestColor = _LowMidColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.6) nearestColor = _MidColor.rgb;
                    else if (h < _MinHeight + (_MidHeight - _MinHeight) * 0.8) nearestColor = _HighMidColor.rgb;
                    else nearestColor = _HighColor.rgb;

                    baseColor = lerp(nearestColor, baseColor, _GradientIntensity);
                }

                return fixed4(baseColor, 1.0);
            }
            ENDCG
        }
    }
}
