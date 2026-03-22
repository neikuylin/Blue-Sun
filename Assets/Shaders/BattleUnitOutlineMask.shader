Shader "Battle/UnitOutlineMask"
{
    SubShader
    {
        Tags { "Queue" = "Geometry+5" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        ColorMask 0
        ZWrite Off
        ZTest LEqual
        Cull Back

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}
