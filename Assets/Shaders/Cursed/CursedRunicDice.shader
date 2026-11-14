
Shader "DiceMaster/Cursed Runic Dice"
{
    Properties
    {
        [Header(Aged Cursed Bone)]
        _BoneColor ("Bone Color", Color) = (0.69,0.63,0.57,1)
        _BoneLightColor ("Bone Highlights", Color) = (0.84,0.80,0.73,1)
        _BoneStainColor ("Aged Bone Stains", Color) = (0.33,0.28,0.32,1)

        _BoneGrainScale ("Bone Texture Scale", Range(1,60)) = 15
        _BoneGrainStrength ("Bone Texture Strength", Range(0,1)) = 0.28
        _StainStrength ("Age and Stains", Range(0,1)) = 0.30
        _EdgeAging ("Darkened Edges", Range(0,1)) = 0.25

        [Header(Fractures)]
        _CrackColor ("Deep Crack Color", Color) = (0.055,0.025,0.075,1)
        _CrackScale ("Crack Scale", Range(1,10)) = 3.2
        _CrackWidth ("Crack Width", Range(0.004,0.08)) = 0.018
        _CrackCoverage ("Crack Coverage", Range(0,1)) = 0.32
        _CrackDistortion ("Crack Irregularity", Range(0,2)) = 0.85
        _CrackHalo ("Crack Shadow Depth", Range(0,1)) = 0.45
        _BranchStrength ("Small Branches", Range(0,1)) = 0.30

        [Header(Cursed Energy)]
        [HDR] _EnergyColor ("Purple Energy", Color) = (0.45,0.10,1.1,1)
        _EnergyGlow ("Energy Intensity", Range(0,4)) = 0.65
        _EnergySpeed ("Energy Flow Speed", Range(0,3)) = 0.55
        _EnergyPulse ("Energy Pulse", Range(0,1)) = 0.70

        [Header(Numbers)]
        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}

        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Number Color", Color) = (0.11,0.055,0.15,1)
        _OutlineColor ("Number Outline", Color) = (0.84,0.78,0.72,1)
        _OutlineWidth ("Number Outline Width", Range(0,0.04)) = 0.008

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

            float4 _BoneColor;
            float4 _BoneLightColor;
            float4 _BoneStainColor;
            float4 _CrackColor;
            float4 _EnergyColor;

            float4 _InkColor;
            float4 _OutlineColor;
            float4 _D4Values;
            float4 _PatternCenter;

            float _BoneGrainScale;
            float _BoneGrainStrength;
            float _StainStrength;
            float _EdgeAging;

            float _CrackScale;
            float _CrackWidth;
            float _CrackCoverage;
            float _CrackDistortion;
            float _CrackHalo;
            float _BranchStrength;

            float _EnergyGlow;
            float _EnergySpeed;
            float _EnergyPulse;

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
        // PROCEDURAL NOISE
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
        // SPARSE ORGANIC FRACTURES
        // No Voronoi mosaic or runic symbols.
        // ==========================================

        void FracturePattern(
            float3 p,
            out float crackMask,
            out float shadowMask,
            out float coreMask,
            out float flowMask
        )
        {
            float3 q = p * _CrackScale;

            // Static distortion of the fracture paths.
            float3 warp = float3(
                Noise(q * 1.25 + float3(3.1,7.2,1.8)),
                Noise(q * 1.40 + float3(8.4,2.7,5.3)),
                Noise(q * 1.15 + float3(1.6,5.1,9.7))
            ) - 0.5;

            float3 w =
                q + warp * _CrackDistortion;

            // Irregular primary fracture contour.
            float broad = Noise(
                w * 0.90 + float3(2.3,4.7,8.1)
            );

            float fine = Noise(
                w * 2.30 + float3(6.2,1.9,3.5)
            );

            float field =
                broad + (fine - 0.5) * 0.13;

            float distanceToLine =
                abs(field - 0.53);

            // Sparse regional mask.
            float region = Noise(
                q * 0.70 + float3(12.4,5.8,7.1)
            );

            float threshold =
                lerp(0.78, 0.39, _CrackCoverage);

            float gate = smoothstep(
                threshold,
                threshold + 0.16,
                region
            );

            float aa = max(
                fwidth(distanceToLine) * 1.2,
                0.002
            );

            float width = max(
                _CrackWidth, 0.004
            );

            float mainLine = 1.0 - smoothstep(
                width * 0.20,
                width + aa,
                distanceToLine
            );

            float primaryCore = 1.0 - smoothstep(
                width * 0.06,
                width * 0.40 + aa,
                distanceToLine
            );

            float mainShadow = 1.0 - smoothstep(
                width * 0.40,
                width * 2.8 + aa,
                distanceToLine
            );

            // Smaller, occasional branches.
            float branchField = Noise(
                w * 1.95 + float3(9.3,3.4,6.7)
            );

            float branchDistance =
                abs(branchField - 0.56);

            float branchLine = 1.0 - smoothstep(
                width * 0.10,
                width * 0.58 + aa,
                branchDistance
            );

            float branchRegion = Noise(
                q * 0.95 + float3(5.3,13.1,2.4)
            );

            float branchGate = smoothstep(
                0.62,
                0.84,
                branchRegion
            );

            float branches =
                branchLine *
                branchGate *
                gate *
                _BranchStrength;

            crackMask = saturate(
                max(
                    mainLine * gate,
                    branches
                )
            );

            shadowMask = saturate(
                max(
                    mainShadow * gate,
                    branches * 1.8
                )
            );

            coreMask =
                primaryCore * gate;

            // Moving cursed energy, not moving rock.
            float t = _Time.y * _EnergySpeed;

            float traveling = 0.5 + 0.5 * sin(
                dot(
                    w,
                    float3(2.1,-1.6,2.8)
                ) * 3.0
                - t * 4.0
                + fine * 3.0
            );

            float movingNoise = Noise(
                w * 1.65 +
                float3(
                    t * 0.45,
                    -t * 0.31,
                    t * 0.22
                )
            );

            float flow =
                traveling * 0.75 +
                movingNoise * 0.25;

            flowMask = lerp(
                0.55,
                smoothstep(0.43,0.82,flow),
                _EnergyPulse
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

            if (_OutlineWidth > 0.00001)
            {
                float r = _OutlineWidth;

                outline = max(
                    outline,
                    SampleDigit(
                        uv + float2(r,0),value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv - float2(r,0),value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv + float2(0,r),value
                    )
                );

                outline = max(
                    outline,
                    SampleDigit(
                        uv - float2(0,r),value
                    )
                );
            }

            outline = saturate(
                outline - ink
            );
        }

        // ==========================================
        // VERTEX
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

            o.positionOS =
                (v.positionOS.xyz -
                _PatternCenter.xyz) *
                _PatternInvSize;

            o.normalWS =
                TransformObjectToWorldNormal(
                    v.normalOS
                );

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
        // FRAGMENT
        // ==========================================

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0.0;
            float outline = 0.0;

            // D4: corner numbers.
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

                    float digitInk;
                    float digitOutline;

                    Digits(
                        uv,
                        value,
                        digitInk,
                        digitOutline
                    );

                    ink = max(
                        ink,
                        digitInk
                    );

                    outline = max(
                        outline,
                        digitOutline
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
                    0.075 + _OutlineWidth,
                    aa,
                    expanded
                );

                outline = saturate(
                    expanded - ink
                );

                ink *= i.valueAlphaFog.y;
                outline *= i.valueAlphaFog.y;
            }

            // D2, D8, D10, D12 and D20.
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
            // AGED BONE SURFACE
            // ======================================

            float3 p = i.positionOS;

            float coarseNoise = Noise(
                p * (_BoneGrainScale * 0.35)
                + float3(5.7,2.1,8.3)
            );

            float fineNoise = Noise(
                p * _BoneGrainScale
                + float3(2.4,7.3,4.1)
            );

            float largeStains = Noise(
                p * 3.2
                + float3(9.2,1.7,6.5)
            );

            float variation = saturate(
                0.62
                + (coarseNoise - 0.5) * 0.35
                + (fineNoise - 0.5)
                    * _BoneGrainStrength
            );

            half3 bone = lerp(
                _BoneColor.rgb,
                _BoneLightColor.rgb,
                variation
            );

            float stains = smoothstep(
                0.54,
                0.81,
                largeStains
            ) * _StainStrength;

            bone = lerp(
                bone,
                _BoneStainColor.rgb,
                stains
            );

            // Small weathering marks.
            float speckles = smoothstep(
                0.74,
                0.89,
                Noise(
                    p * (_BoneGrainScale * 2.5)
                    + float3(4.1,6.3,9.5)
                )
            );

            bone = lerp(
                bone,
                _BoneStainColor.rgb,
                speckles *
                _BoneGrainStrength * 0.24
            );

            // ======================================
            // LIGHTING
            // ======================================

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
                    dot(n,light.direction)
                ) *
                light.shadowAttenuation;

            half3 color = bone *
                (0.52 + illumination * 0.52);

            float3 viewDir =
                GetWorldSpaceNormalizeViewDir(
                    i.positionWS
                );

            // Darken grazing angles to suggest
            // an old, worn surface.
            float fresnel = pow(
                1.0 - saturate(
                    dot(n,viewDir)
                ),
                3.0
            );

            color *= 1.0 -
                fresnel * _EdgeAging;

            // Soft, restrained surface sheen.
            float3 halfDir = normalize(
                light.direction + viewDir
            );

            float sheen = pow(
                saturate(dot(n,halfDir)),
                28.0
            );

            color +=
                light.color *
                sheen *
                0.07 *
                light.shadowAttenuation;

            // ======================================
            // FRACTURES
            // ======================================

            float crack;
            float crackShadow;
            float crackCore;
            float flow;

            FracturePattern(
                p,
                crack,
                crackShadow,
                crackCore,
                flow
            );

            // Recessed shadow around each crack.
            color *= 1.0 -
                crackShadow *
                _CrackHalo *
                0.65;

            // Deep, dark fracture line.
            color = lerp(
                color,
                _CrackColor.rgb,
                saturate(crack * 0.96)
            );

            // Thin purple energy within the
            // deepest parts of selected cracks.
            float energy =
                crackCore *
                flow *
                _EnergyGlow;

            color +=
                _EnergyColor.rgb *
                energy;

            // A tiny reflected violet glow
            // near the strongest energy.
            float reflectedGlow =
                max(0.0,crackShadow - crack) *
                flow *
                _EnergyGlow *
                0.10;

            color +=
                _EnergyColor.rgb *
                reflectedGlow;

            // ======================================
            // NUMBER OUTLINE
            // ======================================

            color = lerp(
                color,
                _OutlineColor.rgb,
                saturate(outline)
            );

            // ======================================
            // NUMBER GLYPH
            // ======================================

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
                    float3 direction = _LightDirection;
                #endif

                float4 clip =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            p,n,direction
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
