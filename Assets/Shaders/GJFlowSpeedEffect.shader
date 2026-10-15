// Full screen Flow speed effect: radial (zoom) blur + procedural speed lines.
// Used by a Full Screen Pass Renderer Feature (Fetch Color Buffer on, After Rendering Post Processing).
// All values are global shader properties set by GJPostProcessManager every frame, so the material asset is never modified.
// With _GJFlowSpeedIntensity = 0 (default) the pass outputs the screen unchanged.
Shader "GameJam/FlowSpeedEffect"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "GJFlowSpeedEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define BLUR_SAMPLE_COUNT 10

            // 0 = off, 1 = full Flow look, above 1 = dash burst.
            float _GJFlowSpeedIntensity;
            // Effect center in viewport space (0..1), usually the player.
            float4 _GJFlowSpeedCenter;

            // Radial blur
            // Fraction of the distance to the center that one pixel is smeared over.
            float _GJFlowBlurStrength;
            // Distance (in screen heights) from the center that stays sharp, and the fade width after it.
            float _GJFlowBlurInnerRadius;
            float _GJFlowBlurFalloff;

            // Speed lines
            float _GJFlowLineCount;
            // Chance (0..1) that a line slot is drawn in the current flicker slot.
            float _GJFlowLineDensity;
            // Line width as a fraction of one line slot (0..0.5).
            float _GJFlowLineWidth;
            float _GJFlowLineInnerRadius;
            float _GJFlowLineFalloff;
            float _GJFlowLineOpacity;
            float4 _GJFlowLineColor;
            // How many times per second the line pattern is re-rolled.
            float _GJFlowLineFlickerRate;

            float Hash(float value)
            {
                return frac(sin(value * 12.9898) * 43758.5453);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float intensity = _GJFlowSpeedIntensity;
                if (intensity <= 0.0001)
                {
                    return baseColor;
                }

                float2 center = _GJFlowSpeedCenter.xy;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                // Aspect corrected offset so distances and angles are round, not stretched.
                float2 offset = uv - center;
                offset.x *= aspect;
                float dist = length(offset);

                // Radial blur: average samples toward the center. The area around the center stays sharp.
                float blurMask = smoothstep(_GJFlowBlurInnerRadius, _GJFlowBlurInnerRadius + _GJFlowBlurFalloff, dist);
                float blurAmount = _GJFlowBlurStrength * intensity * blurMask;
                float2 toCenter = center - uv;
                float3 color = 0;
                [unroll]
                for (int i = 0; i < BLUR_SAMPLE_COUNT; i++)
                {
                    float t = i / (float)(BLUR_SAMPLE_COUNT - 1);
                    color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + toCenter * blurAmount * t).rgb;
                }
                color /= BLUR_SAMPLE_COUNT;

                // Speed lines: split the full circle into line slots and randomly light some of them.
                float angle01 = atan2(offset.y, offset.x) / TWO_PI + 0.5;
                float slotPosition = angle01 * _GJFlowLineCount;
                float slot = floor(slotPosition);
                float flickerSlot = floor(_Time.y * _GJFlowLineFlickerRate);
                float lineOn = step(1.0 - _GJFlowLineDensity, Hash(slot + flickerSlot * 17.13));
                float lineWidth = _GJFlowLineWidth * (0.5 + Hash(slot * 3.71 + flickerSlot));
                float lineShape = 1.0 - smoothstep(0.0, max(lineWidth, 0.0001), abs(frac(slotPosition) - 0.5));
                // Each line starts at a slightly different distance so the ring edge is not a perfect circle.
                float lineStart = _GJFlowLineInnerRadius + Hash(slot * 7.31 + flickerSlot * 5.17) * _GJFlowLineFalloff;
                float lineMask = smoothstep(lineStart, lineStart + _GJFlowLineFalloff, dist);
                float lineAlpha = saturate(lineOn * lineShape * lineMask * _GJFlowLineOpacity * intensity);
                color = lerp(color, _GJFlowLineColor.rgb, lineAlpha);

                return float4(color, baseColor.a);
            }
            ENDHLSL
        }
    }
}
