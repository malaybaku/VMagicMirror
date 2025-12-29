Shader "Hidden/Vmm/Crop"
{
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        // Range(0, 1)
        float _Margin;
        float _BorderWidth;
        float _SquareRate;

        float4 _BorderColor;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;

            float2 screenPx = _ScreenParams.xy;
            float  screenSize = min(screenPx.x, screenPx.y);

            float shapeSize = screenSize * saturate(1.0 - _Margin);
            float halfShapeSize = 0.5 * shapeSize;

            // x0.5 of straight segment
            float halfStraightSegLength = halfShapeSize * _SquareRate;
            // Corner radius
            float radius = halfShapeSize * (1.0 - _SquareRate);

            // Border width
            float borderPx = max(0.0, screenSize * _BorderWidth);
            borderPx = min(borderPx, halfShapeSize);

            // Pixel space around screen center
            float2 pos = (uv - 0.5) * screenPx;

            float2 d = abs(pos) - float2(halfStraightSegLength, halfStraightSegLength);
            float sd = length(max(d, 0.0)) + min(max(d.x, d.y), 0) - radius;

            if (sd > 0.0)
                return float4(0.0, 0.0, 0.0, 0.0);

            if (borderPx > 0.0 && sd >= -borderPx)
                return _BorderColor;

            float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            return float4(col.r, col.g, col.b, 1.0);
        }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}