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
            Color (1, 0, 0, 1)
        }
    }
}
