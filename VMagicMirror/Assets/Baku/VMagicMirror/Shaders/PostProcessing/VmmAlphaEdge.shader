Shader "Hidden/Vmm/AlphaEdge"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        float4 _EdgeColor;
        float _Thickness;
        float _Threshold;
        float _OutlineOverwriteAlpha;
        float _HighQualityMode;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            float4 result = original;

            float4 offsets = (_Thickness / 1.41421) * _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
            const float lu = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets.xw).a;
            const float rd = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets.zy).a;
            const float ru = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets.zw).a;
            const float ld = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets.xy).a;

            float d = length(float2(lu - rd, ld - ru));

            // 8方位に増やす: ちょっと見栄えがよくなる
            float3 offsets2 = _Thickness * _MainTex_TexelSize.xyx * float3(0.5, 0.5, 0.0);
            if (_HighQualityMode > 0.5)
            {
                const float up = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets2.zy).a;
                const float down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - offsets2.zy).a;
                const float left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + offsets2.xz).a;
                const float right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - offsets2.xz).a;
                d = length(float4(
                    lu - rd,
                    ld - ru,
                    left - right,
                    up - down
                    ));
            }

            // さらに頑張るパターン (ボツ) : 重たい + この方向で高解像度にしてもVRM自体のoutlineがギザギザだと見栄えが改善しないため
            // float count = 8.0 - 0.001;
            // float i2rad = 3.14159265 / count;
            // float d = 0.0;
            // for (float k = 0; k < count; k += 1.0)
            // {
            //     float angle = k * i2rad;
            //     float2 diff = (_Thickness * 0.5) * _MainTex_TexelSize.xy * float2(cos(angle), sin(angle));
            //     float a1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + diff).a;
            //     float a2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - diff).a;
            //     d = max(d, abs(a1 - a2));
            // }
            
            // NOTE: もともと半透明/不透明な場所はアウトライン上書きをしないことに注意
            const float outline = step(_Threshold, d) * step(original.a, _OutlineOverwriteAlpha);
            
            result.a = lerp(original.a, 1.0, outline);
            result.rgb = lerp(original.rgb, _EdgeColor.rgb, outline);

            // NOTE: 画面端が不透明なとき、その領域にはOutlineの色が適用される
            // これにより、バストアップ構図で背景透過していると下端にoutlineが効くようになる
            const float edge_factor =
                step(1.0 - offsets2.x, i.texcoord.x) +
                step(i.texcoord.x, offsets2.x) +
                step(1.0 - offsets2.y, i.texcoord.y) +
                step(i.texcoord.y, offsets2.y);

            const float apply_edge_outline =
                step(1.0, edge_factor) * step(_OutlineOverwriteAlpha, original.a);
            result.a = lerp(result.a, _EdgeColor.a, apply_edge_outline);
            result.rgb = lerp(result.rgb, _EdgeColor.rgb, apply_edge_outline);
            
            return result;
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