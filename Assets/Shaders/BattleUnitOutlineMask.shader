Shader "Battle/UnitOutlineMask"
{
    Properties
    {
        _StencilRef ("Stencil Ref", Float) = 1
    }

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
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
        }
    }
}
