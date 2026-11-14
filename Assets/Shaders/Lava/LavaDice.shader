Shader "DiceMaster/Lava Dice"
{
    Properties
    {
        _CrustColor ("Crust Color", Color) = (0.05, 0.03, 0.025, 1)
        _CrustTint ("Crust Tint", Color) = (0.16, 0.08, 0.04, 1)

        _LavaHotColor ("Lava Hot Color", Color) = (1.0, 0.86, 0.42, 1)
        _LavaWarmColor ("Lava Warm Color", Color) = (1.0, 0.34, 0.06, 1)
        _LavaDeepColor ("Lava Deep Color", Color) = (0.55, 0.05, 0.01, 1)

        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.45
        _PatternScale ("Pattern Scale", Range(0.5, 12)) = 4.6
        _Distortion ("Distortion", Range(0, 4)) = 1.35

        _CrackWidth ("Crack Width", Range(0.05, 1.0)) = 0.42
        _CrackSharpness ("Crack Sharpness", Range(0.5, 8.0)) = 3.4
        _EmissionIntensity ("Emission Intensity", Range(0, 8)) = 2.8
        _GlowSoftness ("Glow Softness", Range(0.1, 3.0)) = 1.2

        _NumberAtlas ("Existing Number Atlas", 2D) = "black" {}
        [Enum(Numbers,0,Pips,1,D4Corners,2)]
        _NumberMode ("Number Mode (automatic at spawn)", Float) = 0

        _InkColor ("Number Color", Color) = (1,1,1,1)
        _NumberClearance ("Clearance Around Numbers", Range(0,0.15)) = 0.035

        [HideInInspector] _D4Values ("D4 Values", Vector) = (1,2,3,4)
        [HideInInspector] _PatternCenter ("Pattern Center", Vector) = (0,0,0,0)
        [HideInInspector] _PatternInvSize ("Pattern Inverse Size", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
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
            float4 _D4Values;
            float4 _PatternCenter;

            float _FlowSpeed;
            float _PatternScale;
            float _Distortion;

            float _CrackWidth;
            float _CrackSharpness;
            float _EmissionIntensity;
            float _GlowSoftness;

            float _NumberMode;
            float _NumberClearance;
            float _PatternInvSize;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 color      : COLOR;

            float2 faceUV     : TEXCOORD1;
            float2 faceValue  : TEXCOORD2;
            float2 d4UV       : TEXCOORD3;
            float4 d4Ids      : TEXCOORD4;
        };

        struct Varyings
        {
            float4 positionCS     : SV_POSITION;
            float3 positionOS     : TEXCOORD0;
            float3 positionWS     : TEXCOORD1;
            half3 normalWS        : TEXCOORD2;
            float4 face           : TEXCOORD3;
            float4 d4Ids          : TEXCOORD4;
            float3 valueAlphaFog  : TEXCOORD5;
        };

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
                    lerp(Hash(i), Hash(i + float3(1,0,0)), f.x),
                    lerp(Hash(i + float3(0,1,0)), Hash(i + float3(1,1,0)), f.x),
                    f.y
                ),
                lerp(
                    lerp(Hash(i + float3(0,0,1)), Hash(i + float3(1,0,1)), f.x),
                    lerp(Hash(i + float3(0,1,1)), Hash(i + float3(1,1,1)), f.x),
                    f.y
                ),
                f.z
            );
        }

        float FBM(float3 p)
        {
            float value = 0.0;
            float amplitude = 0.5;
            float frequency = 1.0;

            [unroll(5)]
            for (int i = 0; i < 5; i++)
            {
                value += Noise(p * frequency) * amplitude;
                frequency *= 2.0;
                amplitude *= 0.5;
            }

            return value;
        }

        float Ridged(float3 p)
        {
            float value = 0.0;
            float amplitude = 0.5;
            float frequency = 1.0;

            [unroll(4)]
            for (int i = 0; i < 4; i++)
            {
                float n = Noise(p * frequency);
                n = 1.0 - abs(n * 2.0 - 1.0);
                value += n * amplitude;
                frequency *= 2.1;
                amplitude *= 0.55;
            }

            return value;
        }

        void LavaPattern(float3 p, out float lavaMask, out float hotMask, out float glowMask, out float crustVariation)
        {
            float t = _Time.y * _FlowSpeed;

            float3 q = p * _PatternScale;

            float3 flowA = q + float3(t * 0.45, -t * 0.18, t * 0.12);
            float3 flowB = q * 1.73 + float3(-t * 0.27, t * 0.21, 4.7);
            float3 flowC = q * 0.82 + float3(t * 0.11, t * 0.09, 9.3);

            float warpX = FBM(flowB + float3(7.1, 2.3, 0.4)) - 0.5;
            float warpY = FBM(flowC + float3(1.8, 6.2, 3.1)) - 0.5;
            float warpZ = FBM(flowA + float3(5.2, 0.7, 8.5)) - 0.5;

            float3 warped = flowA + float3(warpX, warpY, warpZ) * _Distortion;

            float baseField = FBM(warped);
            float detailField = Ridged(warped * 1.35 + float3(2.4, 5.6, 1.7));
            float fineField = FBM(warped * 2.4 + float3(8.2, 3.9, 6.4));

            float combined = saturate(baseField * 0.55 + detailField * 0.35 + fineField * 0.10);

            float veins = 1.0 - abs(combined * 2.0 - 1.0);
            veins = pow(saturate(veins), _CrackSharpness);

            lavaMask = smoothstep(1.0 - _CrackWidth, 1.0, veins);
            hotMask = smoothstep(1.0 - _CrackWidth * 0.38, 1.0, veins);
            glowMask = smoothstep(1.0 - _CrackWidth * _GlowSoftness, 1.0, veins);

            crustVariation = saturate(baseField * 0.75 + fineField * 0.25);
        }

        float Rectangle(float2 p, float2 center, float2 extent)
        {
            float2 d = abs(p - center) - extent;
            float sd = max(d.x, d.y);
            return 1.0 - smoothstep(0.0, max(fwidth(sd), 0.001), sd);
        }

        float Digits(float2 uv, float value, out float clearance)
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
                SAMPLE_TEXTURE2D(_NumberAtlas, sampler_NumberAtlas, tensUV).r * tensMask,
                SAMPLE_TEXTURE2D(_NumberAtlas, sampler_NumberAtlas, unitsUV).r * unitsMask
            );

            float2 p = (uv - 0.05) / 0.90;

            clearance = value >= 10
                ? Rectangle(p, float2(0.5,0.5), float2(0.18,0.25) + _NumberClearance)
                : Rectangle(p, float2(0.5,0.5), float2(0.125,0.32) + _NumberClearance);

            return ink;
        }

        Varyings Vert(Attributes v)
        {
            Varyings o;

            VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);

            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;
            o.positionOS = (v.positionOS.xyz - _PatternCenter.xyz) * _PatternInvSize;
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);

            o.face = float4(v.faceUV, v.d4UV);
            o.d4Ids = v.d4Ids;
            o.valueAlphaFog = float3(
                v.faceValue.x,
                v.color.a,
                ComputeFogFactor(p.positionCS.z)
            );

            return o;
        }

        half4 Frag(Varyings i) : SV_Target
        {
            float ink = 0.0;
            float clearance = 0.0;

            if (_NumberMode > 1.5)
            {
                [unroll]
                for (int corner = 0; corner < 3; corner++)
                {
                    float2 uv;
                    float value;
                    float blank;

                    D4CornerUV_float(i.face.zw, corner, uv);
                    D4SelectValue_float(i.d4Ids[corner], _D4Values, value);

                    ink = max(ink, Digits(uv, value, blank));
                    clearance = max(clearance, blank);
                }

                ink *= i.d4Ids.a;
                clearance *= i.d4Ids.a;
            }
            else if (_NumberMode > 0.5)
            {
                float aa = max(length(fwidth(i.face.xy)), 0.002);

                DicePips_float(i.face.xy, i.valueAlphaFog.x, 0.075, aa, ink);
                DicePips_float(i.face.xy, i.valueAlphaFog.x, 0.075 + _NumberClearance, aa, clearance);

                ink *= i.valueAlphaFog.y;
                clearance *= i.valueAlphaFog.y;
            }
            else
            {
                ink = Digits(i.face.xy, i.valueAlphaFog.x, clearance) * i.valueAlphaFog.y;
                clearance *= i.valueAlphaFog.y;
            }

            float lavaMask;
            float hotMask;
            float glowMask;
            float crustVariation;

            LavaPattern(i.positionOS, lavaMask, hotMask, glowMask, crustVariation);

            half3 n = normalize(i.normalWS);
            Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

            half3 illumination =
                SampleSH(n) +
                light.color * saturate(dot(n, light.direction)) * light.shadowAttenuation;

            half3 crustColor = lerp(_CrustColor.rgb, _CrustTint.rgb, crustVariation);
            half3 litCrust = crustColor * (0.22 + illumination);

            half3 lavaColor = lerp(_LavaDeepColor.rgb, _LavaWarmColor.rgb, lavaMask);
            lavaColor = lerp(lavaColor, _LavaHotColor.rgb, hotMask);

            half3 surfaceColor = lerp(litCrust, lavaColor * (0.35 + illumination * 0.25), lavaMask);

            half3 emission =
                lerp(_LavaWarmColor.rgb, _LavaHotColor.rgb, hotMask) *
                glowMask *
                _EmissionIntensity *
                (1.0 - saturate(clearance) * 0.7);

            half3 finalColor = surfaceColor + emission;

            finalColor = lerp(finalColor, _InkColor.rgb, saturate(ink));

            return half4(MixFog(finalColor, i.valueAlphaFog.z), 1);
        }

        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(Attributes v) : SV_POSITION
            {
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half4 DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
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
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float3 n = TransformObjectToWorldNormal(v.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 direction = normalize(_LightPosition - p);
                #else
                    float3 direction = _LightDirection;
                #endif

                float4 clip = TransformWorldToHClip(ApplyShadowBias(p, n, direction));

                #if UNITY_REVERSED_Z
                    clip.z = min(clip.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z = max(clip.z, UNITY_NEAR_CLIP_VALUE);
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