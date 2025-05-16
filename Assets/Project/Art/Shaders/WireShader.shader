Shader "Unlit/WireShader"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Lighting Off
            Fog { Mode Off }
            Color (256, 256, 256, 1)
        }
    }
}
