Shader "Hidden/Vmm/AlphaEdge"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        float4 _MainTex_TexelSize;

        float4 _EdgeColor;
        float _Thickness;
        float _Threshold;
        float _OutlineOverwriteAlpha;
        float _HighQualityMode;

        float SampleAlpha(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
        }
        
        float GetAlphaBasedD(VaryingsDefault i)
        {
            float4 offsets = (_Thickness / 1.41421) * _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
            const float lu = SampleAlpha(i.texcoord + offsets.xw);
            const float rd = SampleAlpha(i.texcoord + offsets.zy);
            const float ru = SampleAlpha(i.texcoord + offsets.zw);
            const float ld = SampleAlpha(i.texcoord + offsets.xy);

            float d;

            // optionalに8方位に増やす: ちょっと見栄えがよくなる
            float3 offsets2 = _Thickness * _MainTex_TexelSize.xyx * float3(0.5, 0.5, 0.0);
            if (_HighQualityMode > 0.5)
            {
                const float up = SampleAlpha(i.texcoord + offsets2.zy);
                const float down = SampleAlpha(i.texcoord - offsets2.zy);
                const float left = SampleAlpha(i.texcoord + offsets2.xz);
                const float right = SampleAlpha(i.texcoord - offsets2.xz);
                d = max(max(max(
                    abs(lu - rd),
                    abs(ld - ru)),
                    abs(left - right)),
                    abs(up - down)
                    );
                // d = length(float4(
                //     lu - rd,
                //     ld - ru,
                //     left - right,
                //     up - down
                //     ));
            }
            else
            {
                d = max(abs(lu - rd), abs(ld - ru));
                // d = length(float2(lu - rd, ld - ru));
            }

            return d;

            // (ボツ) さらに頑張るパターン : 重たいし、この方向で高解像度にしてもVRM自体のoutlineがギザギザだと見栄えが改善しない
            // float count = 8.0 - 0.001;
            // float i2rad = 3.14159265 / count;
            // float d = 0.0;
            // for (float k = 0; k < count; k += 1.0)
            // {
            //     float angle = k * i2rad;
            //     float2 diff = (_Thickness * 0.5) * _MainTex_TexelSize.xy * float2(cos(angle), sin(angle));
            //     float a1 = SampleAlpha(i.texcoord + diff);
            //     float a2 = SampleAlpha(_MainTex, sampler_MainTex, i.texcoord - diff).a;
            //     d = max(d, abs(a1 - a2));
            // }
        }

        // NOTE: Depthでエッジ検出する方法も試したが、とくにShadowBoardの実装と相性が悪いのを重く見て不採用にしている
        float SampleDepth01(float2 uv)
        {
            return Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
        }

        float GetDepthBasedD(VaryingsDefault i)
        {
            //NOTE: 最終的にDepthTextureのtexelSizeにしたほうがいいかも
            float4 offsets = (_Thickness / 1.41421) * _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
            const float lu = SampleDepth01(i.texcoord + offsets.xw);
            const float rd = SampleDepth01(i.texcoord + offsets.zy);
            const float ru = SampleDepth01(i.texcoord + offsets.zw);
            const float ld = SampleDepth01(i.texcoord + offsets.xy);

            float d;

            // optionalに8方位に増やす: ちょっと見栄えがよくなる
            float3 offsets2 = _Thickness * _MainTex_TexelSize.xyx * float3(0.5, 0.5, 0.0);
            if (_HighQualityMode > 0.5)
            {
                const float up = SampleDepth01(i.texcoord + offsets2.zy);
                const float down = SampleDepth01(i.texcoord - offsets2.zy);
                const float left = SampleDepth01(i.texcoord + offsets2.xz);
                const float right = SampleDepth01(i.texcoord - offsets2.xz);
                d = length(float4(
                    lu - rd,
                    ld - ru,
                    left - right,
                    up - down
                    ));
            }
            else
            {
                d = length(float2(lu - rd, ld - ru));
            }
            return d;
        }
        
        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            float4 result = original;

            //test: write depth
            // result.rgb = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord));
            // return result;

            float d = GetAlphaBasedD(i);
            //float d = GetDepthBasedD(i);

            // NOTE: もともと半透明/不透明な場所はアウトライン上書きをしない…というのが第2項
            const float outline = step(_Threshold, d) * step(original.a, _OutlineOverwriteAlpha);
            
            result.a = lerp(original.a, 1.0, outline);
            result.rgb = lerp(original.rgb, _EdgeColor.rgb, outline);
            float2 xyOffsets = (_Thickness * 0.5) * _MainTex_TexelSize.xy;

            // NOTE: 画面端が不透明なとき、その領域にはOutlineの色が適用される
            // これにより、バストアップ構図で背景透過していると下端にoutlineが効くようになる
            const float edge_factor =
                step(1.0 - xyOffsets.x, i.texcoord.x) +
                step(i.texcoord.x, xyOffsets.x) +
                step(1.0 - xyOffsets.y, i.texcoord.y) +
                step(i.texcoord.y, xyOffsets.y);
            
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