
Shader "DiceMaster/Crystal Dice"
{
    Properties
    {
        [Header(Crystal Colors)]
        _CoreColor ("Deep Crystal Color", Color) = (0.055,0.22,0.38,1)
        _BodyColor ("Crystal Body Color", Color) = (0.28,0.70,0.85,1)
        _FacetColor ("Crystal Facet Color", Color) = (0.72,0.94,1,1)

        [HDR] _EdgeColor ("Crystal Edge Glow", Color) = (0.35,1.2,1.6,1)

        [Header(Crystal Interior)]
        _Translucency ("Fake Translucency", Range(0,1)) = 0.65
        _FacetScale ("Internal Facet Scale", Range(1,20)) = 5
        _FacetContrast ("Facet Contrast", Range(0,2)) = 0.75
        _ShardStrength ("Internal Shards", Range(0,1)) = 0.35
        _CloudStrength ("Internal Cloudiness", Range(0,1)) = 0.18

        [Header(Reflections)]
        _ReflectionStrength ("Reflection Strength", Range(0,3)) = 1.2
        _ReflectionSharpness ("Reflection Sharpness", Range(8,128)) = 54
        _FresnelPower ("Edge Fresnel Power", Range(1,8)) = 3
        _FresnelStrength ("Edge Glow Strength", Range(0,3)) = 0.9
        _TransmissionStrength ("Light Transmission", Range(0,2)) = 0.45

        [Header(Crystal Animation)]
        _ShimmerSpeed ("Internal Shimmer Speed", Range(0,3)) = 0.3
        _ShimmerStrength ("Shimmer Strength", Range(0,2)) = 0.22
        _EmissionIntensity ("Crystal Emission", Range(0,3)) = 0.28

        [Header(Numbers)]
        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}

        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Number Color", Color) = (0.015,0.085,0.17,1)
        _OutlineColor ("Number Outline Color", Color) = (0.75,0.96,1,1)
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

            float4 _CoreColor;
            float4 _BodyColor;
            float4 _FacetColor;
            float4 _EdgeColor;

            float4 _InkColor;
            float4 _OutlineColor;
            float4 _D4Values;
            float4 _PatternCenter;

            float _Translucency;
            float _FacetScale;
            float _FacetContrast;
            float _ShardStrength;
            float _CloudStrength;

            float _ReflectionStrength;
            float _ReflectionSharpness;
            float _FresnelPower;
            float _FresnelStrength;
            float _TransmissionStrength;

            float _ShimmerSpeed;
            float _ShimmerStrength;
            float _EmissionIntensity;

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
        // NOISE
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
        // INTERNAL CRYSTAL FACETS
        // Procedural layered crystalline planes.
        // Uses local 3D coordinates, not face UVs.
        // ==========================================

        void CrystalInterior(
            float3 p,
            out float facet,
            out float shards,
            out float clouds,
            out float shimmer
        )
        {
            float3 q = p * _FacetScale;

            float warp = Noise(
                q * 0.55 + float3(4.3,1.7,8.2)
            ) - 0.5;

            float3 w =
                q + warp * 0.45;

            // Three different crystal planes.
            float a = dot(
                w,
                normalize(float3(1.0,0.7,-0.4))
            );

            float b = dot(
                w,
                normalize(float3(-0.5,1.0,0.8))
            );

            float c = dot(
                w,
                normalize(float3(0.7,-0.3,1.0))
            );

            float planeA = frac(a * 0.62);
            float planeB = frac(b * 0.73);
            float planeC = frac(c * 0.51);

            // Polygon-like variations inside the die.
            float combined =
                planeA * 0.42 +
                planeB * 0.36 +
                planeC * 0.22;

            float steps = 5.0;

            facet =
                floor(saturate(combined) * steps)
                / steps;

            facet = saturate(
                0.5 +
                (facet - 0.5) * _FacetContrast
            );

            // Thin internal crystalline shards.
            float distA = abs(planeA - 0.5);
            float distB = abs(planeB - 0.5);
            float distC = abs(planeC - 0.5);

            float dist = min(
                distA,
                min(distB, distC)
            );

            float aa = max(
                fwidth(dist) * 1.2,
                0.004
            );

            shards = 1.0 - smoothstep(
                0.015,
                0.045 + aa,
                dist
            );

            shards *= _ShardStrength;

            // Milky internal clouds.
            float coarse = Noise(
                w * 0.75 + float3(2.3,7.1,4.4)
            );

            float fine = Noise(
                w * 1.9 + float3(8.1,3.6,5.2)
            );

            clouds = saturate(
                coarse * 0.75 +
                fine * 0.25
            );

            clouds *= _CloudStrength;

            // Slow light moving through the crystal.
            float t = _Time.y * _ShimmerSpeed;

            float movingNoise = Noise(
                w * 1.25 +
                float3(
                    t * 0.48,
                    -t * 0.31,
                    t * 0.25
                )
            );

            float movingBand =
                0.5 + 0.5 * sin(
                    a * 2.1 +
                    b * 1.3 -
                    t * 2.5 +
                    movingNoise * 2.3
                );

            shimmer = saturate(
                movingBand * 0.7 +
                movingNoise * 0.3
            );

            shimmer *= _ShimmerStrength;
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

            // Only the glyph outline.
            // No square mask around numbers.
            outline = saturate(outline - ink);
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

                outline = saturate(
                    expanded - ink
                );

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
            // INTERNAL FACETS
            // ======================================

            float facet;
            float shards;
            float clouds;
            float shimmer;

            CrystalInterior(
                i.positionOS,
                facet,
                shards,
                clouds,
                shimmer
            );

            // ======================================
            // LIGHTING AND VIEW DIRECTION
            // ======================================

            float3 n = normalize(i.normalWS);

            float3 viewDir =
                GetWorldSpaceNormalizeViewDir(
                    i.positionWS
                );

            Light light = GetMainLight(
                TransformWorldToShadowCoord(
                    i.positionWS
                )
            );

            float directLight = saturate(
                dot(n, light.direction)
            ) * light.shadowAttenuation;

            float3 ambient = SampleSH(n);

            float3 illumination =
                ambient +
                light.color * directLight;

            // ======================================
            // CRYSTAL BODY
            // ======================================

            float interiorAmount = saturate(
                facet * 0.75 +
                clouds * 0.50
            );

            float3 crystalColor = lerp(
                _CoreColor.rgb,
                _BodyColor.rgb,
                saturate(
                    0.25 + interiorAmount
                )
            );

            crystalColor = lerp(
                crystalColor,
                _FacetColor.rgb,
                saturate(shards * 0.65)
            );

            // Fake translucency:
            // brightens the internal material
            // without transparent render sorting.
            float3 translucentColor = lerp(
                _BodyColor.rgb,
                _FacetColor.rgb,
                interiorAmount * 0.5
            );

            crystalColor = lerp(
                crystalColor,
                translucentColor,
                _Translucency * 0.45
            );

            float3 color =
                crystalColor *
                (0.35 + illumination * 0.65);

            // ======================================
            // LIGHT TRANSMISSION
            // ======================================

            float backLight = pow(
                saturate(
                    dot(-n, light.direction)
                ),
                1.5
            );

            color +=
                _BodyColor.rgb *
                backLight *
                _TransmissionStrength *
                light.color *
                light.shadowAttenuation;

            // ======================================
            // FRESNEL / EDGES
            // ======================================

            float facing = saturate(
                dot(n, viewDir)
            );

            float fresnel = pow(
                1.0 - facing,
                _FresnelPower
            );

            // Cold light near silhouette.
            color +=
                _EdgeColor.rgb *
                fresnel *
                _FresnelStrength;

            // ======================================
            // SPECULAR REFLECTIONS
            // ======================================

            float3 halfDir = normalize(
                light.direction + viewDir
            );

            float specular = pow(
                saturate(
                    dot(n, halfDir)
                ),
                _ReflectionSharpness
            );

            specular *=
                _ReflectionStrength *
                light.shadowAttenuation;

            color +=
                _FacetColor.rgb *
                specular *
                light.color;

            // Additional stylized reflection.
            float3 reflected =
                reflect(-viewDir, n);

            float studioReflection = pow(
                saturate(
                    dot(
                        reflected,
                        normalize(
                            float3(0.35,0.8,0.45)
                        )
                    )
                ),
                28.0
            );

            color +=
                _FacetColor.rgb *
                studioReflection *
                _ReflectionStrength *
                0.45;

            // ======================================
            // ANIMATED INTERNAL LIGHT
            // ======================================

            color +=
                _EdgeColor.rgb *
                shimmer *
                _EmissionIntensity;

            color +=
                _FacetColor.rgb *
                shards *
                _EmissionIntensity *
                0.25;

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
