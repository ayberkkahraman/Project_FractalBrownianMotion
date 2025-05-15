Shader "Custom/TerrainShader"
{
    Properties
    {
        _MinHeight("Min Height", Range(0, 100)) = 0
        _MaxHeight("Max Height", Range(0, 100)) = 20
        _GradientActive("Gradient Active", Float) = 1
        _GradientIntensity("Gradient Intensity", Range(0.1, 10.0)) = 1.0

        _LowColor("Low Color", Color) = (0.2, 0.4, 0.2, 1)
        _MidLowColor("Mid Low Color", Color) = (0.4, 0.3, 0.1, 1)
        _MidColor("Mid Color", Color) = (0.6, 0.5, 0.3, 1)
        _MidHighColor("Mid High Color", Color) = (0.8, 0.8, 0.6, 1)
        _HighColor("High Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _MinHeight;
            float _MaxHeight;
            float _GradientActive;
            float _GradientIntensity;

            float4 _LowColor;
            float4 _MidLowColor;
            float4 _MidColor;
            float4 _MidHighColor;
            float4 _HighColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float height = IN.positionWS.y;
                float t = saturate((height - _MinHeight) / (_MaxHeight - _MinHeight));
                float3 color;

                if (_GradientActive > 0.5)
                {
                    t = pow(t, _GradientIntensity);

                    if (t < 0.25)
                        color = lerp(_LowColor.rgb, _MidLowColor.rgb, t / 0.25);
                    else if (t < 0.5)
                        color = lerp(_MidLowColor.rgb, _MidColor.rgb, (t - 0.25) / 0.25);
                    else if (t < 0.75)
                        color = lerp(_MidColor.rgb, _MidHighColor.rgb, (t - 0.5) / 0.25);
                    else
                        color = lerp(_MidHighColor.rgb, _HighColor.rgb, (t - 0.75) / 0.25);
                }
                else
                {
                    if (t < 0.2)
                        color = _LowColor.rgb;
                    else if (t < 0.4)
                        color = _MidLowColor.rgb;
                    else if (t < 0.6)
                        color = _MidColor.rgb;
                    else if (t < 0.8)
                        color = _MidHighColor.rgb;
                    else
                        color = _HighColor.rgb;
                }

                // Aydınlatma
                Light mainLight = GetMainLight();
                float3 normal = normalize(IN.normalWS);
                float NdotL = max(0, dot(normal, mainLight.direction));
                float3 litColor = color * mainLight.color.rgb * NdotL;

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
}
