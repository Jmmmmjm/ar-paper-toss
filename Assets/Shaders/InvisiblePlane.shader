Shader "AR/InvisiblePlane"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-100" }
        Pass
        {
            ZWrite Off
            ColorMask 0
            Cull Off
        }
    }
}
