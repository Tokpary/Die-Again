
Shader "DiceMaster/Roulette Sectors"
{
    Properties
    {
        _BossFraction ("Boss fraction", Range(0,1)) = 0.01
        _NormalColor ("Normal", Color) = (0.07,0.28,0.24,1)
        _BossColor ("Boss", Color) = (0.7,0.06,0.12,1)
        _TrimColor ("Trim", Color) = (0.8,0.57,0.25,1)
        _SliceCount ("Number of slices", Range(1,20)) = 20
        _SliceLineWidth ("Slice line width", Range(0.001,0.02)) = 0.004

        [Header(Integrated VFX)]
        [HDR] _FXGoldColor ("Energy Gold", Color) = (1.7,0.85,0.22,1)
        [HDR] _FXRedColor ("Boss Energy", Color) = (1.6,0.10,0.16,1)
        _FXIntensity ("Energy Intensity", Range(0,4)) = 1.6
        _WinningGlow ("Winning Sector Glow", Range(0,3)) = 1.1

        [HideInInspector] _FXTime ("FX Time", Float) = 0
        [HideInInspector] _SpinEnergy ("Spin Energy", Float) = 0
        [HideInInspector] _PassingSlice ("Pointer sector", Float) = -1
        [HideInInspector] _PassingIntensity ("Pointer glow", Float) = 0.85
        [HideInInspector] _WinningSlice ("Winning Slice", Float) = -1
        [HideInInspector] _ResultIsBoss ("Result Is Boss", Float) = 0
        [HideInInspector] _RevealProgress ("Reveal Progress", Float) = 0
        [HideInInspector] _RevealEnergy ("Reveal Energy", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalColor;
                float4 _BossColor;
                float4 _TrimColor;
                float4 _FXGoldColor;
                float4 _FXRedColor;

                float _BossFraction;
                float _SliceCount;
                float _SliceLineWidth;
                float _FXIntensity;
                float _WinningGlow;

                float _FXTime;
                float _SpinEnergy;
                float _PassingSlice;
                float _PassingIntensity;
                float _WinningSlice;
                float _ResultIsBoss;
                float _RevealProgress;
                float _RevealEnergy;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float CircularDistance(float a, float b)
            {
                float d = abs(frac(a - b + 0.5) - 0.5);
                return d;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);

                clip(1.0 - radius);

                float angle = frac(
                    atan2(p.x, p.y) / 6.28318530718 + 1.0
                );

                float count = max(1.0, round(_SliceCount));
                float slicePosition = angle * count;
                float sliceIndex = floor(slicePosition);

                float sliceFraction = frac(slicePosition);
                float boundaryDistance = min(
                    sliceFraction, 1.0 - sliceFraction
                );

                // Preserve the original sector colors.
                half4 color = angle < _BossFraction
                    ? _BossColor
                    : _NormalColor;

                float lineDistance =
                    boundaryDistance * 6.28318530718 / count;

                if (lineDistance * radius < _SliceLineWidth &&
                    radius > 0.10 && radius < 0.96)
                {
                    color = _TrimColor;
                }

                if (radius > 0.97 || radius < 0.045)
                {
                    color = _TrimColor;
                }

                // ------------------------------------------
                // SPIN: faint sector-edge shimmer, no luminous ring.
                // ------------------------------------------

                float faceMask =
                    smoothstep(0.15, 0.24, radius) *
                    (1.0 - smoothstep(0.91, 0.97, radius));

                float edgeGlow = 1.0 - smoothstep(
                    0.006, 0.065, boundaryDistance
                );

                // Follow the real pointer angle, including the final slow ticks.
                float passing = step(0.0, _PassingSlice) *
                    (1.0 - step(0.5, abs(sliceIndex - _PassingSlice)));
                float3 passingColor = angle < _BossFraction
                    ? _FXRedColor.rgb : _FXGoldColor.rgb;
                color.rgb += passingColor * passing * faceMask *
                    (0.24 + edgeGlow * 0.65) * _PassingIntensity * _FXIntensity;

                // ------------------------------------------
                // RESULT: highlight the real winning wedge.
                // ------------------------------------------

                float validWinner = step(0.0, _WinningSlice);

                float selected =
                    validWinner *
                    (1.0 - step(
                        0.5,
                        abs(sliceIndex - _WinningSlice)
                    ));

                float3 resultColor = lerp(
                    _FXGoldColor.rgb,
                    _FXRedColor.rgb,
                    saturate(_ResultIsBoss)
                );

                float winnerArea =
                    smoothstep(0.12, 0.22, radius) *
                    (1.0 - smoothstep(0.89, 0.97, radius));

                float selectedEdge = 1.0 - smoothstep(
                    0.005, 0.09, boundaryDistance
                );

                float sustainedGlow =
                    selected * winnerArea *
                    _WinningGlow * (
                        0.30 +
                        _RevealEnergy * 0.65
                    );

                float borderGlow =
                    selected * selectedEdge *
                    winnerArea *
                    (0.35 + _RevealEnergy * 0.65);

                float resultGlow = sustainedGlow + borderGlow;

                color.rgb +=
                    resultColor *
                    resultGlow *
                    _FXIntensity;

                color.a = 1.0;
                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
