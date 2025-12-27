Shader "Hidden/Vmm/Crop"
{
    HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        // Range(0, 1)
        float _Margin;
        // Range(0, 1)
        float _BorderWidth;
        float4 _BorderColor;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;

            float2 screenPx = _ScreenParams.xy;
            float size = min(screenPx.x, screenPx.y);

            float radiusPx = 0.5 * size * saturate(1.0 - _Margin);

            float borderPx = max(0.0, size * _BorderWidth);
            borderPx = min(borderPx, radiusPx);

            float dist = length((uv - 0.5) * screenPx);

            if (dist > radiusPx)
                return float4(0.0, 0.0, 0.0, 0.0);

            if (borderPx > 0.0 && dist >= (radiusPx - borderPx))
                return _BorderColor;

            float4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            return src;
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