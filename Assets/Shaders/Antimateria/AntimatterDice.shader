
Shader "DiceMaster/Antimatter Dice"
{
    Properties
    {
        _BaseColor ("Black Color", Color) = (0.003,0.003,0.005,1)
        _WaveSpeed ("Wave Speed", Range(0,3)) = 0.22
        _WaveScale ("Wave Scale", Range(0.5,12)) = 4
        _Distortion ("Distortion", Range(0,4)) = 2.2
        _EffectIntensity ("White Effect Intensity", Range(0,5)) = 1
        _WaveWidth ("Flame Reach From Edges", Range(0.05,0.8)) = 0.35
        _EdgeWidth ("White Edge Width", Range(0,0.2)) = 0.035

        // NEW: Animated mesh deformation
        _DeformAmount ("Mesh Deformation", Range(0,0.05)) = 0.012
        _DeformFrequency ("Deformation Frequency", Range(0.5,8)) = 2

        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}
        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Number Color", Color) = (1,1,1,1)
        _NumberClearance ("Black Clearance Around Numbers", Range(0,0.15)) = 0.035

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
            float4 _InkColor;
            float4 _D4Values;
            float4 _PatternCenter;

            float _WaveSpeed;
            float _WaveScale;
            float _Distortion;
            float _EffectIntensity;
            float _WaveWidth;
            float _NumberMode;
            float _NumberClearance;
            float _PatternInvSize;
            float _EdgeWidth;

            // NEW
            float _DeformAmount;
            float _DeformFrequency;

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
            float4 edgeA : TEXCOORD5;
            float4 edgeB : TEXCOORD6;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float3 positionOS : TEXCOORD0;
            float3 positionWS : TEXCOORD1;

            half3 normalWS : TEXCOORD2;

            float4 face : TEXCOORD3;
            float4 d4Ids : TEXCOORD4;
            float4 edgeA : TEXCOORD5;
            float4 edgeB : TEXCOORD6;

            float3 valueAlphaFog : TEXCOORD7;
        };

        // ------------------------------------------
        // NOISE
        // ------------------------------------------

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

            f = f * f * (3 - 2 * f);

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

        // ------------------------------------------
        // NEW: ANIMATED MESH DEFORMATION
        // ------------------------------------------

        float3 DeformPositionOS(float3 positionOS)
        {
            if (_DeformAmount <= 0.00001)
                return positionOS;

            float invSize = (_PatternInvSize > 0.0001)
                ? _PatternInvSize
                : 1.0;

            float3 p =
                (positionOS - _PatternCenter.xyz) * invSize;

            float t = _Time.y * _WaveSpeed;

            // Animated organic noise
            float n = Noise(
                p * (_DeformFrequency * 4.0)
                + float3(
                    t,
                    -t * 0.8,
                    t * 0.45
                )
            );

            // Animated wave movement
            float wave = sin(
                dot(
                    p,
                    float3(1.7, 2.3, -1.4)
                )
                * _DeformFrequency * 5.0
                - t * 3.0
                + n * 3.0
            );

            // Combine wave and noise
            float displacement =
                (
                    wave * 0.65
                    + (n * 2.0 - 1.0) * 0.35
                )
                * _DeformAmount
                / invSize;

            // Radial displacement avoids gaps
            // between vertices shared by faces.
            float3 radial =
                positionOS - _PatternCenter.xyz;

            float3 direction =
                radial / max(length(radial), 0.0001);

            return positionOS + direction * displacement;
        }

        // ------------------------------------------
        // VERTEX SHADER
        // ------------------------------------------

        Varyings Vert(Attributes v)
        {
            Varyings o;

            // NEW: Deform visual geometry
            float3 deformedPosition =
                DeformPositionOS(v.positionOS.xyz);

            VertexPositionInputs p =
                GetVertexPositionInputs(deformedPosition);

            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;

            // Keep original coordinates for
            // the animated white wave pattern.
            o.positionOS =
                (v.positionOS.xyz - _PatternCenter.xyz)
                * _PatternInvSize;

            o.normalWS =
                TransformObjectToWorldNormal(v.normalOS);

            // Preserve existing number rendering
            o.face = float4(v.faceUV, v.d4UV);
            o.d4Ids = v.d4Ids;

            o.edgeA = v.edgeA;
            o.edgeB = v.edgeB;

            o.valueAlphaFog = float3(
                v.faceValue.x,
                v.color.a,
                ComputeFogFactor(p.positionCS.z)
            );

            return o;
        }

        // ------------------------------------------
        // ANIMATED WHITE WAVES
        // ------------------------------------------

        float Waves(float3 p, float edgeDistance)
        {
            float t = _Time.y * _WaveSpeed;

            float3 q = p * _WaveScale * 7;

            float bend = Noise(
                q * 0.23 + float3(0, -t, 7.1)
            ) - 0.5;

            q += float3(
                bend,
                -bend * 0.6,
                bend * 0.8
            ) * _Distortion;

            q += float3(0.7, -1, 0.3)
                * (t * 2 - edgeDistance * 1.7);

            float coarse = Noise(q * 0.55);
            float fine = Noise(q * 1.63 + 19.3);

            float tongue = pow(
                saturate(
                    coarse * 0.8 + fine * 0.2
                ),
                2.4
            );

            float reach =
                _EdgeWidth
                + _WaveWidth * tongue * 2;

            float aa = max(
                fwidth(edgeDistance - reach),
                0.004
            );

            return 1 - smoothstep(
                reach - aa,
                reach + aa,
                edgeDistance
            );
        }

        // ------------------------------------------
        // NUMBER RENDERING
        // ------------------------------------------

        float Rectangle(
            float2 p,
            float2 center,
            float2 extent
        )
        {
            float2 d = abs(p - center) - extent;
            float sd = max(d.x, d.y);

            return 1 - smoothstep(
                0,
                max(fwidth(sd), 0.001),
                sd
            );
        }

        float Digits(
            float2 uv,
            float value,
            out float clearance
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

            float ink = max(
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

            float2 p = (uv - 0.05) / 0.90;

            clearance = value >= 10
                ? Rectangle(
                    p,
                    float2(0.5,0.5),
                    float2(0.18,0.25)
                        + _NumberClearance
                )
                : Rectangle(
                    p,
                    float2(0.5,0.5),
                    float2(0.125,0.32)
                        + _NumberClearance
                );

            return ink;
        }

        // ------------------------------------------
        // FRAGMENT SHADER
        // ------------------------------------------

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0;
            float clearance = 0;

            if (_NumberMode > 1.5)
            {
                [unroll]
                for (int corner = 0; corner < 3; corner++)
                {
                    float2 uv;
                    float value;
                    float blank;

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

                    ink = max(
                        ink,
                        Digits(uv, value, blank)
                    );

                    clearance = max(
                        clearance,
                        blank
                    );
                }

                ink *= i.d4Ids.a;
                clearance *= i.d4Ids.a;
            }
            else if (_NumberMode > 0.5)
            {
                float aa = max(
                    length(fwidth(i.face.xy)),
                    0.002
                );

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
                    0.075 + _NumberClearance,
                    aa,
                    clearance
                );

                ink *= i.valueAlphaFog.y;
                clearance *= i.valueAlphaFog.y;
            }
            else
            {
                ink = Digits(
                    i.face.xy,
                    i.valueAlphaFog.x,
                    clearance
                ) * i.valueAlphaFog.y;

                clearance *= i.valueAlphaFog.y;
            }

            // Lighting
            half3 n = normalize(i.normalWS);

            Light light = GetMainLight(
                TransformWorldToShadowCoord(i.positionWS)
            );

            half3 illumination =
                SampleSH(n)
                + light.color
                * saturate(
                    dot(n, light.direction)
                )
                * light.shadowAttenuation;

            half3 color =
                _BaseColor.rgb * (0.2 + illumination);

            // Original animated Heatwave effect
            float4 nearest = min(
                i.edgeA,
                i.edgeB
            );

            float edgeDistance = max(
                0,
                min(
                    min(nearest.x, nearest.y),
                    min(nearest.z, nearest.w)
                )
            );

            color +=
                Waves(i.positionOS, edgeDistance)
                * (1 - saturate(clearance))
                * _EffectIntensity;

            // Keep numbers readable
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

        // ------------------------------------------
        // FORWARD PASS
        // ------------------------------------------

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

        // ------------------------------------------
        // DEPTH PASS
        // ------------------------------------------

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
                float3 pos =
                    DeformPositionOS(v.positionOS.xyz);

                return TransformObjectToHClip(pos);
            }

            half4 DepthFrag() : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // ------------------------------------------
        // SHADOW PASS
        // ------------------------------------------

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
            #pragma fragment DepthFrag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVert(Attributes v) : SV_POSITION
            {
                // NEW: Apply same geometry deformation
                float3 deformedPosition =
                    DeformPositionOS(v.positionOS.xyz);

                float3 p =
                    TransformObjectToWorld(deformedPosition);

                float3 n =
                    TransformObjectToWorldNormal(v.normalOS);

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

            half4 DepthFrag() : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
