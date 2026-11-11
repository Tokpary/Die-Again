
Shader "DiceMaster/Toxic Dice Unified"
{
    Properties
    {
        [Header(Toxic Colors)]
        _DiceColor ("Dice Color Tint", Color) = (1,1,1,1)
        _AcidDark ("Acid Dark", Color) = (0.43,0.80,0.25,1)
        _AcidBright ("Acid Bright", Color) = (0.70,1,0.22,1)

        [Header(Liquid Surface)]
        _LiquidMetallic ("Liquid Metallic", Range(0,1)) = 0
        _LiquidSmoothness ("Liquid Smoothness", Range(0,1)) = 0.8
        _EmissionStrength ("Emission Strength", Range(0,3)) = 0.82

        [Header(Animated Toxic Fluid)]
        _FluidSpeed ("Fluid Speed", Range(0,3)) = 0.05
        _Distortion ("Distortion", Range(0,2)) = 0.12
        _FluidScale ("Fluid Scale", Range(0.5,12)) = 4
        _PatternContrast ("Pattern Contrast", Range(0.25,3)) = 1.2
        _PatternSoftness ("Pattern Softness", Range(0.01,0.3)) = 0.08

        [Header(Slime Vertex Animation)]
        _SlimeWobbleSpeed ("Wobble Speed", Range(0,10)) = 3
        _SlimeSquash ("Idle Squash Strength", Range(0,0.15)) = 0.035
        _SlimeWaveAmplitude ("Jelly Wave Amplitude", Range(0,0.08)) = 0.012
        _SlimeWaveFrequency ("Jelly Wave Frequency", Range(0.5,12)) = 3.5

        [Header(Slime Collision Response)]
        _SlimeImpactSquash ("Impact Squash Strength", Range(0,0.3)) = 0.12
        _SlimeImpactFrequency ("Impact Oscillation Frequency", Range(1,50)) = 23

        [HideInInspector] _SlimeImpact ("Slime Impact", Float) = 0
        [HideInInspector] _SlimeImpactAge ("Slime Impact Age", Float) = 0
        [HideInInspector] _SlimeImpactAxisOS ("Slime Impact Axis OS", Vector) = (0,1,0,0)

        [Header(Numbers)]
        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}

        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode", Float) = 0

        _InkColor ("Number Color", Color) = (0.015,0.035,0.012,1)
        _NumberOutlineColor ("Number Outline", Color) = (0.78,1,0.45,1)
        _NumberOutlineWidth ("Number Outline Width", Range(0,0.04)) = 0.004

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

            float4 _DiceColor;
            float4 _AcidDark;
            float4 _AcidBright;

            float4 _InkColor;
            float4 _NumberOutlineColor;

            float4 _D4Values;
            float4 _PatternCenter;

            float4 _SlimeImpactAxisOS;

            float _LiquidMetallic;
            float _LiquidSmoothness;
            float _EmissionStrength;

            float _FluidSpeed;
            float _Distortion;
            float _FluidScale;
            float _PatternContrast;
            float _PatternSoftness;

            float _SlimeWobbleSpeed;
            float _SlimeSquash;
            float _SlimeWaveAmplitude;
            float _SlimeWaveFrequency;

            float _SlimeImpactSquash;
            float _SlimeImpactFrequency;
            float _SlimeImpact;
            float _SlimeImpactAge;

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

            float3 patternOS : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            half3 normalWS : TEXCOORD2;

            float4 face : TEXCOORD3;
            float4 d4Ids : TEXCOORD4;

            float3 valueAlphaFog : TEXCOORD5;
        };

        // ==========================================
        // SLIME VERTEX DEFORMATION
        // Only changes rendered vertex positions.
        // Does not modify MeshCollider or Rigidbody.
        // ==========================================

        float3 DeformSlimeOS(float3 originalOS)
        {
            float inverseSize = max(
                _PatternInvSize,
                0.00001
            );

            float meshSize = 1.0 / inverseSize;

            float3 center = _PatternCenter.xyz;
            float3 relative = originalOS - center;
            float3 normalizedP = relative * inverseSize;

            // World vertical converted to object space.
            // The squash remains gravity-oriented
            // even while the die rotates.
            float3 upOS = normalize(
                TransformWorldToObjectDir(
                    float3(0,1,0)
                )
            );

            float t =
                _Time.y * _SlimeWobbleSpeed;

            // --------------------------------------
            // IDLE BREATHING / SQUASH
            // --------------------------------------

            float idlePulse =
                0.5 + 0.5 * sin(t * 1.15);

            float idleSquash =
                _SlimeSquash * idlePulse;

            float height = dot(
                relative,
                upOS
            );

            float3 vertical =
                upOS * height;

            float3 horizontal =
                relative - vertical;

            // Volume-preserving approximation:
            // compress vertically, expand sideways.
            float3 deformed =
                vertical * (1.0 - idleSquash) +
                horizontal * (1.0 + idleSquash * 0.5);

            // --------------------------------------
            // COLLISION SQUASH / STRETCH
            // --------------------------------------

            float3 impactAxis = normalize(
                _SlimeImpactAxisOS.xyz +
                float3(0.00001,0.00001,0.00001)
            );

            // First compress, then stretch.
            // The C# component reduces _SlimeImpact
            // over time to create a damped bounce.
            float impactOscillation =
                cos(
                    _SlimeImpactAge *
                    _SlimeImpactFrequency
                );

            float impactSquash =
                _SlimeImpact *
                _SlimeImpactSquash *
                impactOscillation;

            float impactHeight = dot(
                deformed,
                impactAxis
            );

            float3 impactVertical =
                impactAxis * impactHeight;

            float3 impactHorizontal =
                deformed - impactVertical;

            deformed =
                impactVertical *
                (1.0 - impactSquash) +
                impactHorizontal *
                (1.0 + impactSquash * 0.5);

            // --------------------------------------
            // JELLY SURFACE WAVES
            // --------------------------------------

            // Same original position always produces
            // the same displacement, even when mesh
            // vertices have different face normals.
            // This avoids opening seams between faces.

            float waveA = sin(
                normalizedP.x *
                _SlimeWaveFrequency * 4.0 +
                normalizedP.y * 3.7 +
                t * 1.5
            );

            float waveB = sin(
                normalizedP.z *
                _SlimeWaveFrequency * 3.0 -
                normalizedP.x * 5.1 -
                t * 1.15
            );

            float waveC = sin(
                dot(
                    normalizedP,
                    float3(6.3,4.7,5.9)
                ) - t * 1.8
            );

            float wave =
                waveA * waveB * 0.65 +
                waveC * 0.35;

            // Additional visible wobble after impacts.
            float waveStrength =
                _SlimeWaveAmplitude *
                (1.0 + _SlimeImpact * 1.2);

            float radius =
                length(normalizedP);

            float3 radialDirection =
                normalizedP /
                max(radius,0.00001);

            float radialEnvelope =
                smoothstep(
                    0.0,
                    0.15,
                    radius
                );

            float3 surfaceDisplacement =
                radialDirection *
                wave *
                waveStrength *
                radialEnvelope *
                meshSize;

            return center +
                deformed +
                surfaceDisplacement;
        }

        // ==========================================
        // DEFORMED VERTEX NORMAL
        //
        // Approximates derivatives of the vertex
        // displacement field to keep highlights
        // consistent with the new surface.
        // ==========================================

        float3 DeformedNormalOS(
            float3 originalOS,
            float3 normalOS
        )
        {
            float3 n = normalize(normalOS);

            float3 referenceAxis =
                abs(n.y) < 0.9
                ? float3(0,1,0)
                : float3(1,0,0);

            float3 tangent = normalize(
                cross(referenceAxis,n)
            );

            float3 bitangent = normalize(
                cross(n,tangent)
            );

            float meshSize =
                1.0 / max(
                    _PatternInvSize,
                    0.00001
                );

            float epsilon =
                meshSize * 0.005;

            float3 p0 =
                DeformSlimeOS(originalOS);

            float3 pt =
                DeformSlimeOS(
                    originalOS +
                    tangent * epsilon
                );

            float3 pb =
                DeformSlimeOS(
                    originalOS +
                    bitangent * epsilon
                );

            return normalize(
                cross(
                    pt - p0,
                    pb - p0
                )
            );
        }

        // ==========================================
        // PROCEDURAL NOISE
        // ==========================================

        float Hash(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p,p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
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
        // ANIMATED TOXIC PATTERN
        // ==========================================

        void ToxicPattern(
            float3 p,
            out float acidMask,
            out float smallDetail
        )
        {
            float time =
                _Time.y * _FluidSpeed;

            float3 q =
                p * _FluidScale * 2.0;

            float3 drift = float3(
                time * 0.85,
                -time * 0.58,
                time * 0.42
            );

            float3 warp = float3(
                Noise(
                    q * 0.48 +
                    drift * 0.65 +
                    float3(3.1,7.4,1.8)
                ),
                Noise(
                    q * 0.48 -
                    drift * 0.48 +
                    float3(8.6,2.2,5.1)
                ),
                Noise(
                    q * 0.48 +
                    drift * 0.35 +
                    float3(1.6,5.7,9.3)
                )
            ) - 0.5;

            float warpStrength =
                0.85 + _Distortion * 4.0;

            float3 warped =
                q + warp * warpStrength;

            float coarse = Noise(
                warped * 0.72 + drift
            );

            float fine = Noise(
                warped * 1.48 -
                drift * 0.65 +
                float3(12.4,3.6,8.1)
            );

            float ribbon = 0.5 + 0.5 * sin(
                warped.x * 0.75 +
                warped.y * 0.42 -
                warped.z * 0.31 +
                coarse * 4.2 -
                time * 1.4
            );

            float field =
                coarse * 0.72 +
                fine * 0.16 +
                ribbon * 0.12;

            field = saturate(
                0.5 +
                (field - 0.5) *
                _PatternContrast
            );

            float softness = max(
                _PatternSoftness,
                fwidth(field) * 1.5
            );

            acidMask = smoothstep(
                0.5 - softness,
                0.5 + softness,
                field
            );

            smallDetail = Noise(
                warped * 2.1 +
                drift * 0.7 +
                float3(6.3,9.7,2.4)
            );
        }

        // ==========================================
        // NUMBER ATLAS
        // ==========================================

        float SampleDigit(
            float2 uv,
            float value
        )
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
            ink = SampleDigit(uv,value);
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

            outline = saturate(
                outline - ink
            );
        }

        // ==========================================
        // VERTEX SHADER
        // ==========================================

        Varyings Vert(Attributes v)
        {
            Varyings o;

            // These vertices are modified for
            // visual rendering only.
            float3 deformedOS =
                DeformSlimeOS(
                    v.positionOS.xyz
                );

            float3 normalOS =
                DeformedNormalOS(
                    v.positionOS.xyz,
                    v.normalOS
                );

            VertexPositionInputs p =
                GetVertexPositionInputs(
                    deformedOS
                );

            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;

            o.normalWS =
                TransformObjectToWorldNormal(
                    normalOS
                );

            // The toxic pattern is anchored to
            // original mesh coordinates.
            o.patternOS =
                (v.positionOS.xyz -
                _PatternCenter.xyz) *
                _PatternInvSize;

            // UVs remain attached to the vertices.
            // Numbers deform with the mesh.
            o.face = float4(
                v.faceUV,
                v.d4UV
            );

            o.d4Ids = v.d4Ids;

            o.valueAlphaFog = float3(
                v.faceValue.x,
                v.color.a,
                ComputeFogFactor(
                    p.positionCS.z
                )
            );

            return o;
        }

        // ==========================================
        // FRAGMENT SHADER
        // ==========================================

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0;
            float outline = 0;

            // D4: corner numbers.
            if (_NumberMode > 1.5)
            {
                [unroll]
                for (
                    int corner = 0;
                    corner < 3;
                    corner++
                )
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

            // ======================================
            // TOXIC LIQUID
            // ======================================

            float acidMask;
            float detail;

            ToxicPattern(
                i.patternOS,
                acidMask,
                detail
            );

            half3 acid = lerp(
                _AcidDark.rgb,
                _AcidBright.rgb,
                acidMask
            );

            acid *= _DiceColor.rgb;

            acid *= 1.0 +
                (detail - 0.5) * 0.065;

            // ======================================
            // LIGHTING
            // ======================================

            half3 n = normalize(
                i.normalWS
            );

            Light light = GetMainLight(
                TransformWorldToShadowCoord(
                    i.positionWS
                )
            );

            half3 ambient = SampleSH(n);

            float directLight = saturate(
                dot(n,light.direction)
            ) * light.shadowAttenuation;

            float ambientFactor = saturate(
                dot(
                    ambient,
                    half3(0.30,0.59,0.11)
                )
            );

            float lightFactor =
                0.72 +
                ambientFactor * 0.12 +
                directLight * 0.16;

            half3 color =
                acid * lightFactor;

            color +=
                acid *
                _EmissionStrength *
                0.22;

            // ======================================
            // WET SLIME REFLECTIONS
            // ======================================

            float3 viewDir =
                GetWorldSpaceNormalizeViewDir(
                    i.positionWS
                );

            float3 halfDir = normalize(
                light.direction + viewDir
            );

            float smoothness = saturate(
                _LiquidSmoothness
            );

            float glossPower = lerp(
                12.0,
                108.0,
                smoothness
            );

            float specular = pow(
                saturate(
                    dot(n,halfDir)
                ),
                glossPower
            );

            float3 reflectionColor = lerp(
                float3(1,1,1),
                acid,
                saturate(_LiquidMetallic)
            );

            color +=
                reflectionColor *
                specular *
                (0.12 + smoothness * 0.32) *
                light.color *
                light.shadowAttenuation;

            float fresnel = pow(
                1.0 - saturate(
                    dot(n,viewDir)
                ),
                4.0
            );

            color +=
                _AcidBright.rgb *
                fresnel *
                smoothness *
                0.055;

            // ======================================
            // NUMBERS
            // ======================================

            color = lerp(
                color,
                _NumberOutlineColor.rgb,
                saturate(outline)
            );

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
        // FORWARD
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
        // DEPTH ONLY
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

            float4 DepthVert(
                Attributes v
            ) : SV_POSITION
            {
                float3 deformedOS =
                    DeformSlimeOS(
                        v.positionOS.xyz
                    );

                return TransformObjectToHClip(
                    deformedOS
                );
            }

            half4 DepthFrag() : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // ==========================================
        // SHADOW CASTER
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

            float4 ShadowVert(
                Attributes v
            ) : SV_POSITION
            {
                float3 deformedOS =
                    DeformSlimeOS(
                        v.positionOS.xyz
                    );

                float3 deformedNormalOS =
                    DeformedNormalOS(
                        v.positionOS.xyz,
                        v.normalOS
                    );

                float3 p =
                    TransformObjectToWorld(
                        deformedOS
                    );

                float3 n =
                    TransformObjectToWorldNormal(
                        deformedNormalOS
                    );

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    float3 direction = normalize(
                        _LightPosition - p
                    );

                #else

                    float3 direction =
                        _LightDirection;

                #endif

                float4 clip =
                    TransformWorldToHClip(
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
