
Shader "DiceMaster/Lava Cartoon Dice"
{
    Properties
    {
        [Header(Volcanic Rock)]
        _CrustColor ("Crust Color", Color) = (0.045,0.025,0.025,1)
        _CrustTint ("Rock Highlights", Color) = (0.16,0.075,0.045,1)
        _RockSteps ("Rock Color Steps", Range(2,6)) = 3

        [Header(Molten Lava)]
        _LavaHotColor ("Lava Hot Color", Color) = (1,0.82,0.3,1)
        _LavaWarmColor ("Lava Warm Color", Color) = (1,0.28,0.025,1)
        _LavaDeepColor ("Lava Deep Color", Color) = (0.42,0.035,0.012,1)

        [Header(Lava Animation)]
        _FlowSpeed ("Flow Speed", Range(0,3)) = 0.5
        _FlowStrength ("Flow Movement", Range(0,1)) = 0.7
        _PatternRotationSpeed ("Pattern Rotation Speed", Range(-3,3)) = 0.15
        _PatternScale ("Pattern Scale", Range(0.5,12)) = 4
        _Distortion ("Distortion", Range(0,4)) = 1.2
        _LavaPulse ("Lava Pulse", Range(0,1)) = 0.2

        [Header(Cracks)]
        _CrackWidth ("Crack Width", Range(0.05,1)) = 0.38
        _CrackSharpness ("Crack Sharpness", Range(0.5,8)) = 3.8

        [Header(Emission)]
        _EmissionIntensity ("Emission Intensity", Range(0,8)) = 1.5
        _GlowSoftness ("Glow Softness", Range(0.1,3)) = 0.8

        [Header(Numbers)]
        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}

        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Number Color", Color) = (1,0.94,0.78,1)
        _NumberOutlineColor ("Number Outline", Color) = (0.04,0.015,0.012,1)
        _NumberOutlineWidth ("Number Outline Width", Range(0,0.04)) = 0.012

        [HideInInspector] _D4Values ("D4 Values", Vector) = (1,2,3,4)
        [HideInInspector] _PatternCenter ("Pattern Center", Vector) = (0,0,0,0)
        [HideInInspector] _PatternInvSize ("Pattern Inverse Size", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        #include "../DiceNumberAtlasUV.hlsl"
        #include "../DicePips.hlsl"
        #include "../D4CornerUV.hlsl"
        #include "../D4SelectValue.hlsl"

        TEXTURE2D(_NumberAtlas);
        SAMPLER(sampler_NumberAtlas);

        CBUFFER_START(UnityPerMaterial)

            float4 _CrustColor;
            float4 _CrustTint;

            float4 _LavaHotColor;
            float4 _LavaWarmColor;
            float4 _LavaDeepColor;

            float4 _InkColor;
            float4 _NumberOutlineColor;
            float4 _D4Values;
            float4 _PatternCenter;

            float _RockSteps;

            float _FlowSpeed;
            float _FlowStrength;
            float _PatternRotationSpeed;
            float _PatternScale;
            float _Distortion;
            float _LavaPulse;

            float _CrackWidth;
            float _CrackSharpness;

            float _EmissionIntensity;
            float _GlowSoftness;

            float _NumberMode;
            float _NumberOutlineWidth;
            float _PatternInvSize;

        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;

            float2 faceUV : TEXCOORD1;
            float2 faceValue : TEXCOORD2;
            float2 d4UV : TEXCOORD3;
            float4 d4Ids : TEXCOORD4;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float3 positionOS : TEXCOORD0;
            float3 positionWS : TEXCOORD1;

            half3 normalWS : TEXCOORD2;

            float4 face : TEXCOORD3;
            float4 d4Ids : TEXCOORD4;

            float3 valueAlphaFog : TEXCOORD5;
        };

        // ==========================================
        // PROCEDURAL NOISE
        // ==========================================

        float Hash(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);

            return frac((p.x + p.y) * p.z);
        }

        float3 Hash3(float3 p)
        {
            return float3(
                Hash(p + float3(19.1,3.7,8.3)),
                Hash(p + float3(4.2,27.3,11.5)),
                Hash(p + float3(15.7,8.4,31.2))
            );
        }

        float Noise(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);

            f = f * f * (3.0 - 2.0 * f);

            return lerp(
                lerp(
                    lerp(
                        Hash(i),
                        Hash(i + float3(1,0,0)),
                        f.x
                    ),
                    lerp(
                        Hash(i + float3(0,1,0)),
                        Hash(i + float3(1,1,0)),
                        f.x
                    ),
                    f.y
                ),
                lerp(
                    lerp(
                        Hash(i + float3(0,0,1)),
                        Hash(i + float3(1,0,1)),
                        f.x
                    ),
                    lerp(
                        Hash(i + float3(0,1,1)),
                        Hash(i + float3(1,1,1)),
                        f.x
                    ),
                    f.y
                ),
                f.z
            );
        }

        // ==========================================
        // NEW: PATTERN ROTATION
        // ==========================================

        float2 Rotate2D(float2 p, float angle)
        {
            float s = sin(angle);
            float c = cos(angle);

            return float2(
                p.x * c - p.y * s,
                p.x * s + p.y * c
            );
        }

        float3 RotatePatternSpace(float3 p)
        {
            // Rotation in local object space.
            // The mesh itself never rotates.
            float angle =
                _Time.y * _PatternRotationSpeed;

            // Rotate around the local Y axis.
            float2 rotatedXZ = Rotate2D(
                p.xz,
                angle
            );

            return float3(
                rotatedXZ.x,
                p.y,
                rotatedXZ.y
            );
        }

        // ==========================================
        // ROCK CRACKS
        // ==========================================

        float VoronoiEdge(float3 p)
        {
            float3 cell = floor(p);
            float3 localPos = frac(p);

            float nearest = 100.0;
            float second = 100.0;

            [unroll]
            for (int z = -1; z <= 1; z++)
            {
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float3 offset = float3(x,y,z);

                        float3 randomPoint =
                            Hash3(cell + offset);

                        float3 feature =
                            offset + 0.2 +
                            randomPoint * 0.6;

                        float3 delta = feature - localPos;

                        float dist = dot(delta, delta);

                        if (dist < nearest)
                        {
                            second = nearest;
                            nearest = dist;
                        }
                        else if (dist < second)
                        {
                            second = dist;
                        }
                    }
                }
            }

            return max(
                0.0,
                sqrt(second) - sqrt(nearest)
            );
        }

        // ==========================================
        // INTERNAL LAVA FLOW
        // ==========================================

        float FlowingHeat(float3 p, float time)
        {
            float first = Noise(
                p * 1.65 +
                float3(
                    time * 0.75,
                    -time * 0.40,
                    time * 0.25
                ) +
                float3(3.7, 0.2, 5.1)
            );

            float second = Noise(
                p * 3.2 +
                float3(
                    -time * 0.28,
                    time * 0.65,
                    time * 0.40
                ) +
                float3(12.6, 2.5, 1.6)
            );

            float ribbon =
                0.5 + 0.5 * sin(
                    dot(
                        p,
                        float3(1.9, -2.4, 1.35)
                    ) +
                    time * 2.8 +
                    first * 3.0
                );

            return saturate(
                first * 0.50 +
                second * 0.20 +
                ribbon * 0.30
            );
        }

        // ==========================================
        // LAVA PATTERN
        // ==========================================

        void LavaPattern(
            float3 p,
            out float lavaMask,
            out float hotMask,
            out float glowMask,
            out float crustVariation,
            out float flowValue
        )
        {
            float t = _Time.y * _FlowSpeed;

            // ======================================
            // NEW: ROTATE THE ENTIRE PATTERN
            // ======================================

            // All layers use the same transformed
            // coordinates, preserving their proportions.
            p = RotatePatternSpace(p);

            float3 q = p * _PatternScale;

            // Static domain distortion, evaluated
            // in the rotating pattern's coordinates.
            float3 warp = float3(
                Noise(
                    q * 0.65 +
                    float3(3.1,1.7,7.4)
                ),
                Noise(
                    q * 0.65 +
                    float3(8.2,5.3,2.1)
                ),
                Noise(
                    q * 0.65 +
                    float3(4.4,9.7,6.5)
                )
            ) - 0.5;

            float3 warped =
                q + warp * _Distortion * 0.65;

            // The crack network rotates with
            // the procedural pattern.
            float edge = VoronoiEdge(warped);

            float width = lerp(
                0.025,
                0.18,
                saturate(_CrackWidth)
            );

            float softness = max(
                fwidth(edge) * 1.5,
                width * 0.35 /
                max(_CrackSharpness, 0.5)
            );

            // Lava inside the rock cracks.
            lavaMask = 1.0 - smoothstep(
                max(0.0, width - softness),
                width + softness,
                edge
            );

            // Hot core inside each crack.
            float hotCore = 1.0 - smoothstep(
                width * 0.15,
                width * 0.85,
                edge
            );

            // Glow at the borders of the cracks.
            glowMask = 1.0 - smoothstep(
                width * 0.55,
                width * (1.0 + _GlowSoftness),
                edge
            );

            // Rock color variation.
            float rockNoise = Noise(
                warped * 0.55 +
                float3(7.2,3.9,1.5)
            );

            float steps = max(
                2.0,
                floor(_RockSteps)
            );

            crustVariation =
                floor(saturate(rockNoise) * steps)
                / max(1.0, steps - 1.0);

            crustVariation =
                saturate(crustVariation);

            // Optional internal molten movement.
            float movingHeat = FlowingHeat(
                warped,
                t
            );

            flowValue = lerp(
                0.5,
                movingHeat,
                saturate(_FlowStrength)
            );

            // Local heat pulsation.
            float localPulse =
                1.0 +
                _LavaPulse * 0.2 * sin(
                    t * 3.0 +
                    warped.x * 2.0 +
                    warped.y * 1.3
                );

            hotMask = saturate(
                hotCore *
                (0.12 + flowValue * 0.88) *
                localPulse
            );
        }

        // ==========================================
        // NUMBER ATLAS
        // ==========================================

        float SampleDigit(float2 uv, float value)
        {
            float2 tensUV;
            float2 unitsUV;
            float tensMask;
            float unitsMask;

            DiceNumberAtlasUV_float(
                uv,
                value,
                tensUV,
                unitsUV,
                tensMask,
                unitsMask
            );

            return max(
                SAMPLE_TEXTURE2D(
                    _NumberAtlas,
                    sampler_NumberAtlas,
                    tensUV
                ).r * tensMask,

                SAMPLE_TEXTURE2D(
                    _NumberAtlas,
                    sampler_NumberAtlas,
                    unitsUV
                ).r * unitsMask
            );
        }

        void Digits(
            float2 uv,
            float value,
            out float ink,
            out float outline
        )
        {
            ink = SampleDigit(uv, value);
            outline = ink;

            if (_NumberOutlineWidth > 0.00001)
            {
                float r = _NumberOutlineWidth;

                outline = max(
                    outline,
                    SampleDigit(
                        uv + float2(r,0),
                        value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv - float2(r,0),
                        value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv + float2(0,r),
                        value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv - float2(0,r),
                        value
                    )
                );
            }

            outline = saturate(outline - ink);
        }

        // ==========================================
        // VERTEX SHADER
        // ==========================================

        Varyings Vert(Attributes v)
        {
            Varyings o;

            VertexPositionInputs p =
                GetVertexPositionInputs(
                    v.positionOS.xyz
                );

            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;

            // Object-space pattern coordinates.
            o.positionOS =
                (
                    v.positionOS.xyz -
                    _PatternCenter.xyz
                ) * _PatternInvSize;

            o.normalWS =
                TransformObjectToWorldNormal(
                    v.normalOS
                );

            // Number coordinates remain unchanged.
            o.face = float4(
                v.faceUV,
                v.d4UV
            );

            o.d4Ids = v.d4Ids;

            o.valueAlphaFog = float3(
                v.faceValue.x,
                v.color.a,
                ComputeFogFactor(p.positionCS.z)
            );

            return o;
        }

        // ==========================================
        // FRAGMENT SHADER
        // ==========================================

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0.0;
            float outline = 0.0;

            // D4: numbers at triangle corners.
            if (_NumberMode > 1.5)
            {
                [unroll]
                for (int corner = 0; corner < 3; corner++)
                {
                    float2 uv;
                    float value;

                    D4CornerUV_float(
                        i.face.zw,
                        corner,
                        uv
                    );

                    D4SelectValue_float(
                        i.d4Ids[corner],
                        _D4Values,
                        value
                    );

                    float cornerInk;
                    float cornerOutline;

                    Digits(
                        uv,
                        value,
                        cornerInk,
                        cornerOutline
                    );

                    ink = max(
                        ink,
                        cornerInk
                    );

                    outline = max(
                        outline,
                        cornerOutline
                    );
                }

                ink *= i.d4Ids.a;
                outline *= i.d4Ids.a;
            }

            // D6: procedural pips.
            else if (_NumberMode > 0.5)
            {
                float aa = max(
                    length(fwidth(i.face.xy)),
                    0.002
                );

                float expanded;

                DicePips_float(
                    i.face.xy,
                    i.valueAlphaFog.x,
                    0.075,
                    aa,
                    ink
                );

                DicePips_float(
                    i.face.xy,
                    i.valueAlphaFog.x,
                    0.075 + _NumberOutlineWidth,
                    aa,
                    expanded
                );

                outline = saturate(
                    expanded - ink
                );

                ink *= i.valueAlphaFog.y;
                outline *= i.valueAlphaFog.y;
            }

            // D2, D8, D10, D12, D20.
            else
            {
                Digits(
                    i.face.xy,
                    i.valueAlphaFog.x,
                    ink,
                    outline
                );

                ink *= i.valueAlphaFog.y;
                outline *= i.valueAlphaFog.y;
            }

            // --------------------------------------
            // LAVA PATTERN
            // --------------------------------------

            float lavaMask;
            float hotMask;
            float glowMask;
            float crustVariation;
            float flowValue;

            LavaPattern(
                i.positionOS,
                lavaMask,
                hotMask,
                glowMask,
                crustVariation,
                flowValue
            );

            // --------------------------------------
            // LIGHTING
            // --------------------------------------

            half3 n = normalize(i.normalWS);

            Light light = GetMainLight(
                TransformWorldToShadowCoord(
                    i.positionWS
                )
            );

            half3 illumination =
                SampleSH(n) +
                light.color *
                saturate(
                    dot(n, light.direction)
                ) *
                light.shadowAttenuation;

            // --------------------------------------
            // VOLCANIC ROCK
            // --------------------------------------

            half3 rock = lerp(
                _CrustColor.rgb,
                _CrustTint.rgb,
                crustVariation
            );

            rock *=
                0.6 + illumination * 0.45;

            // --------------------------------------
            // MOLTEN LAVA
            // --------------------------------------

            float warmAmount = saturate(
                0.55 + flowValue * 0.75
            );

            half3 lava = lerp(
                _LavaDeepColor.rgb,
                _LavaWarmColor.rgb,
                warmAmount
            );

            lava = lerp(
                lava,
                _LavaHotColor.rgb,
                hotMask
            );

            half3 color = lerp(
                rock,
                lava,
                saturate(lavaMask)
            );

            // --------------------------------------
            // EMISSION
            // --------------------------------------

            float flowingEmission =
                0.15 + flowValue * 0.65;

            half3 emission =
                _LavaWarmColor.rgb *
                lavaMask *
                flowingEmission;

            emission +=
                _LavaHotColor.rgb *
                hotMask *
                (0.45 + flowValue * 0.9);

            emission +=
                _LavaDeepColor.rgb *
                glowMask *
                0.10;

            color +=
                emission * _EmissionIntensity;

            // --------------------------------------
            // NUMBER OUTLINE
            // --------------------------------------

            color = lerp(
                color,
                _NumberOutlineColor.rgb,
                saturate(outline)
            );

            // --------------------------------------
            // NUMBER GLYPH
            // --------------------------------------

            color = lerp(
                color,
                _InkColor.rgb,
                saturate(ink)
            );

            return half4(
                MixFog(
                    color,
                    i.valueAlphaFog.z
                ),
                1
            );
        }

        ENDHLSL

        // ==========================================
        // FORWARD PASS
        // ==========================================

        Pass
        {
            Name "Forward"

            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            ENDHLSL
        }

        // ==========================================
        // DEPTH PASS
        // ==========================================

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(
                    v.positionOS.xyz
                );
            }

            half4 DepthFrag() : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // ==========================================
        // SHADOW CASTER PASS
        // ==========================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVert(Attributes v) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(
                    v.positionOS.xyz
                );

                float3 n = TransformObjectToWorldNormal(
                    v.normalOS
                );

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    float3 direction = normalize(
                        _LightPosition - p
                    );

                #else

                    float3 direction = _LightDirection;

                #endif

                float4 clip = TransformWorldToHClip(
                    ApplyShadowBias(
                        p,
                        n,
                        direction
                    )
                );

                #if UNITY_REVERSED_Z

                    clip.z = min(
                        clip.z,
                        UNITY_NEAR_CLIP_VALUE
                    );

                #else

                    clip.z = max(
                        clip.z,
                        UNITY_NEAR_CLIP_VALUE
                    );

                #endif

                return clip;
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
