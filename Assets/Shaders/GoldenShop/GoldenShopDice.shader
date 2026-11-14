
Shader "DiceMaster/Golden Shop Dice"
{
    Properties
    {
        [Header(Gold Colors)]
        _BaseColor ("Antique Gold", Color) = (0.72,0.42,0.09,1)
        _BrightColor ("Polished Gold", Color) = (1,0.75,0.22,1)
        [HDR] _HighlightColor ("Golden Reflections", Color) = (1.5,1.15,0.62,1)

        [Header(Real Metal)]
        _Metallic ("Metallic", Range(0,1)) = 0.95
        _Smoothness ("Smoothness", Range(0,1)) = 0.85
        _StudioReflection ("Studio Reflection Strength", Range(0,3)) = 0.65
        _DarkBandStrength ("Dark Reflection Contrast", Range(0,1)) = 0.25
        _GoldFill ("Minimum Gold Brightness", Range(0,0.5)) = 0.08

        [Header(Surface Detail)]
        _MetalContrast ("Metal Contrast", Range(0,2)) = 1.1
        _GlossIntensity ("Reflection Strength", Range(0,3)) = 0.7
        _GlossPower ("Reflection Sharpness", Range(8,128)) = 36
        _FresnelStrength ("Edge Reflection", Range(0,2)) = 0.35
        _GrainStrength ("Metal Texture", Range(0,1)) = 0.07
        _GrainScale ("Metal Texture Scale", Range(2,80)) = 24

        [Header(Animated Shine)]
        _ShineSpeed ("Shine Speed", Range(0,3)) = 0.20
        _ShineIntensity ("Shine Intensity", Range(0,3)) = 0.30
        _ShineWidth ("Shine Width", Range(0.05,0.8)) = 0.18

        [Header(Numbers)]
        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}

        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Engraved Number Color", Color) = (0.09,0.04,0.005,1)
        _OutlineColor ("Number Border Color", Color) = (1,0.85,0.43,1)
        _OutlineWidth ("Number Border Width", Range(0,0.04)) = 0.012

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
            float4 _BaseColor;
            float4 _BrightColor;
            float4 _HighlightColor;

            float4 _InkColor;
            float4 _OutlineColor;
            float4 _D4Values;
            float4 _PatternCenter;

            float _Metallic;
            float _Smoothness;
            float _StudioReflection;
            float _DarkBandStrength;
            float _GoldFill;

            float _MetalContrast;
            float _GlossIntensity;
            float _GlossPower;
            float _FresnelStrength;
            float _GrainStrength;
            float _GrainScale;

            float _ShineSpeed;
            float _ShineIntensity;
            float _ShineWidth;

            float _NumberMode;
            float _OutlineWidth;
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
        // METAL GRAIN
        // ==========================================

        float Hash(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);

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

            if (_OutlineWidth > 0.00001)
            {
                float r = _OutlineWidth;

                outline = max(
                    outline,
                    SampleDigit(uv + float2(r,0), value)
                );

                outline = max(
                    outline,
                    SampleDigit(uv - float2(r,0), value)
                );

                outline = max(
                    outline,
                    SampleDigit(uv + float2(0,r), value)
                );

                outline = max(
                    outline,
                    SampleDigit(uv - float2(0,r), value)
                );
            }

            outline = saturate(outline - ink);
        }

        // ==========================================
        // VERTEX
        // ==========================================

        Varyings Vert(Attributes v)
        {
            Varyings o;

            VertexPositionInputs p =
                GetVertexPositionInputs(v.positionOS.xyz);

            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;

            o.positionOS =
                (v.positionOS.xyz - _PatternCenter.xyz)
                * _PatternInvSize;

            o.normalWS =
                TransformObjectToWorldNormal(v.normalOS);

            o.face = float4(v.faceUV, v.d4UV);
            o.d4Ids = v.d4Ids;

            o.valueAlphaFog = float3(
                v.faceValue.x,
                v.color.a,
                ComputeFogFactor(p.positionCS.z)
            );

            return o;
        }

        // ==========================================
        // METALLIC STUDIO REFLECTIONS
        // ==========================================

        float3 StudioHighlights(
            float3 normalWS,
            float3 viewDirWS,
            float3 positionOS
        )
        {
            // Reflections change with camera direction.
            float3 reflected =
                reflect(-viewDirWS, normalWS);

            float3 panelDirectionA =
                normalize(float3(0.35, 0.82, 0.44));

            float3 panelDirectionB =
                normalize(float3(-0.72, 0.51, 0.46));

            // Bright studio reflection.
            float brightPanel = pow(
                saturate(
                    dot(reflected, panelDirectionA)
                ),
                max(8.0, _GlossPower * 0.5)
            );

            // Secondary reflection from another angle.
            float secondaryPanel = pow(
                saturate(
                    dot(reflected, panelDirectionB)
                ),
                max(8.0, _GlossPower * 0.35)
            );

            float3 highlights =
                _HighlightColor.rgb *
                (brightPanel + secondaryPanel * 0.55) *
                _StudioReflection *
                _GlossIntensity;

            // Animated narrow reflection moving
            // through the die's local coordinates.
            float sweep = sin(
                _Time.y * _ShineSpeed
            ) * 0.75;

            float position = dot(
                positionOS,
                normalize(float3(0.8, 0.45, 0.35))
            );

            float distance =
                (position - sweep)
                / max(_ShineWidth, 0.01);

            float movingShine = exp(
                -distance * distance * 2.5
            );

            highlights +=
                _HighlightColor.rgb *
                movingShine *
                _ShineIntensity *
                0.35;

            // Subtle edge glint.
            float fresnel = pow(
                1.0 - saturate(
                    dot(normalWS, viewDirWS)
                ),
                4.0
            );

            highlights +=
                _HighlightColor.rgb *
                fresnel *
                _FresnelStrength *
                0.18;

            return highlights;
        }

        // ==========================================
        // FRAGMENT
        // ==========================================

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0.0;
            float outline = 0.0;

            // --------------------------------------
            // D4 CORNER NUMBERS
            // --------------------------------------

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

                    ink = max(ink, cornerInk);
                    outline = max(
                        outline,
                        cornerOutline
                    );
                }

                ink *= i.d4Ids.a;
                outline *= i.d4Ids.a;
            }

            // --------------------------------------
            // D6 PIPS
            // --------------------------------------

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
                    0.075 + _OutlineWidth,
                    aa,
                    expanded
                );

                outline = saturate(expanded - ink);

                ink *= i.valueAlphaFog.y;
                outline *= i.valueAlphaFog.y;
            }

            // --------------------------------------
            // D2, D8, D10, D12, D20
            // --------------------------------------

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
            // METALLIC SURFACE
            // ======================================

            float3 n = normalize(i.normalWS);

            float3 viewDir =
                GetWorldSpaceNormalizeViewDir(
                    i.positionWS
                );

            // Small surface variations.
            float grain = Noise(
                i.positionOS * _GrainScale
            );

            float grainAmount =
                (grain - 0.5) *
                _GrainStrength *
                _MetalContrast;

            float3 gold = lerp(
                _BaseColor.rgb,
                _BrightColor.rgb,
                saturate(0.52 + grainAmount)
            );

            // ======================================
            // URP PHYSICALLY BASED METAL
            // ======================================

            SurfaceData surfaceData =
                (SurfaceData)0;

            surfaceData.albedo = gold;
            surfaceData.metallic =
                saturate(_Metallic);

            surfaceData.smoothness = saturate(
                _Smoothness - grainAmount * 0.12
            );

            surfaceData.normalTS =
                half3(0, 0, 1);

            surfaceData.occlusion = 1.0;
            surfaceData.alpha = 1.0;

            // Small fill so dark environments
            // do not turn gold completely black.
            surfaceData.emission =
                gold * _GoldFill;

            // --------------------------------------
            // URP LIGHTING INPUT
            // --------------------------------------

            InputData inputData =
                (InputData)0;

            inputData.positionWS =
                i.positionWS;

            inputData.normalWS = n;

            inputData.viewDirectionWS =
                viewDir;

            inputData.shadowCoord =
                TransformWorldToShadowCoord(
                    i.positionWS
                );

            inputData.fogCoord =
                i.valueAlphaFog.z;

            inputData.vertexLighting =
                half3(0, 0, 0);

            inputData.bakedGI =
                SampleSH(n);

            inputData.normalizedScreenSpaceUV =
                GetNormalizedScreenSpaceUV(
                    i.positionCS
                );

            inputData.shadowMask =
                half4(1, 1, 1, 1);

            // Real metallic lighting and reflections.
            half4 result =
                UniversalFragmentPBR(
                    inputData,
                    surfaceData
                );

            // ======================================
            // STUDIO REFLECTIONS
            // ======================================

            float3 reflected =
                reflect(-viewDir, n);

            float darkPanel = pow(
                saturate(
                    dot(
                        reflected,
                        normalize(
                            float3(0.65, -0.18, -0.74)
                        )
                    )
                ),
                8.0
            );

            // Dark reflected bands create contrast.
            result.rgb *=
                1.0 - darkPanel *
                _DarkBandStrength;

            result.rgb += StudioHighlights(
                n,
                viewDir,
                i.positionOS
            );

            // ======================================
            // NUMBER BORDER
            // ======================================

            result.rgb = lerp(
                result.rgb,
                _OutlineColor.rgb,
                saturate(outline)
            );

            // ======================================
            // ENGRAVED NUMBER
            // ======================================

            result.rgb = lerp(
                result.rgb,
                _InkColor.rgb,
                saturate(ink)
            );

            result.rgb = MixFog(
                result.rgb,
                inputData.fogCoord
            );

            return half4(result.rgb, 1);
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

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            ENDHLSL
        }

        // ==========================================
        // DEPTH
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
                float3 p =
                    TransformObjectToWorld(
                        v.positionOS.xyz
                    );

                float3 n =
                    TransformObjectToWorldNormal(
                        v.normalOS
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
