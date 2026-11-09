
Shader "DiceMaster/Roulette Sectors"
{
    Properties
    {
        _BossFraction ("Boss fraction", Range(0, 1)) = 0.01
        _NormalColor ("Normal", Color) = (0.07, 0.28, 0.24, 1)
        _BossColor ("Boss", Color) = (0.7, 0.06, 0.12, 1)
        _TrimColor ("Trim", Color) = (0.8, 0.57, 0.25, 1)

        _SliceCount ("Number of slices", Range(4, 60)) = 20
        _SliceLineWidth ("Slice line width", Range(0.001, 0.02)) = 0.004
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry+1"
        }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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

                float _BossFraction;
                float _SliceCount;
                float _SliceLineWidth;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);

                clip(1.0 - radius);

                float angle = frac(
                    atan2(p.x, p.y) / 6.28318530718 + 1.0
                );

                // Original boss probability logic.
                half4 color = angle < _BossFraction
                    ? _BossColor
                    : _NormalColor;

                // Decorative radial divisions.
                float slicePosition = angle * max(1.0, round(_SliceCount));
                float distanceToBoundary =
                    min(frac(slicePosition), 1.0 - frac(slicePosition));

                float lineDistance =
                    distanceToBoundary * 6.28318530718
                    / max(1.0, round(_SliceCount));

                if (lineDistance * radius < _SliceLineWidth
                    && radius > 0.10
                    && radius < 0.96)
                {
                    color = _TrimColor;
                }

                // Keep only the outer golden ring and central decoration.
                if (radius > 0.97 || radius < 0.045)
                {
                    color = _TrimColor;
                }

                return color;
            }

            ENDHLSL
        }
    }
}
