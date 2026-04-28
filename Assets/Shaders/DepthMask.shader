Shader "Custom/DepthMask"
{
    // Renders nothing visible but writes to the depth buffer,
    // preventing objects behind it from being drawn.
    // Used behind transparent effects (waterfalls, particles) to
    // occlude geometry that shouldn't be visible through them.
    SubShader
    {
        Tags { "Queue" = "Geometry-1" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            ZWrite On
            ColorMask 0
        }
    }
}
