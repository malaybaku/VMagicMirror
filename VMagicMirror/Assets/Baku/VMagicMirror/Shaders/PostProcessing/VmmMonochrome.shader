Shader "Hidden/Vmm/Monochrome"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float _UseMonochrome;
        float _Division;
        float _WhiteThreshold;
        float _UseLevel;
        float4 _BlackColor;
        float4 _WhiteColor;
        float _BlockSize;
        float _Width;
        float _Height;

        float _UseColorReduction;
        float _ColorDivision;

        inline half3 GammaToLinearSpace(half3 sRGB)
        {
            return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
        }
    
        inline half3 LinearToGammaSpace(half3 linRGB)
        {
            linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
            return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);
        }
    
        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;
            if (_BlockSize > 0.0)
            {
                float2 scaled = uv.xy * _ScreenParams.xy;
                scaled.xy -= fmod(scaled.xy, _BlockSize);
                uv.xy = scaled / _ScreenParams.xy;
            }
            
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            if (_UseMonochrome > 0.0)
            {
                float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));

                if (_UseLevel > 0.0)
                {
                    //e.g. if _Division == 4 then value is one of [0, 0.333, 0.666, 1.0]
                    float leveled = floor(luminance * _Division) / (_Division - 1.0);
                    
                    //discretization for dark color
                    if (leveled * _Division <= _WhiteThreshold)
                    {
                        luminance = leveled;
                    }
                }

                color.rgb = lerp(_BlackColor.rgb, _WhiteColor.rgb, luminance.xxx);
            }

            if (_UseColorReduction > 0.0)
            {
                float3 scaled = color.rgb * _ColorDivision;
                color.rgb = (scaled - fmod(scaled, 1.0)) / (_ColorDivision - 1.0);
            }            
                        
            return color;
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