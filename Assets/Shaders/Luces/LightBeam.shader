// Haz de luz falso ("volumétrico") para URP. Va en el cono que genera LightBeamCone.
// Es aditivo: brilla más en el centro del haz, se apaga hacia el final y por los bordes,
// se funde suave donde toca la mesa o el suelo y tiene un poco de "polvo" que se mueve.
// Necesita la Depth Texture activada en el URP Asset (para el fundido con la geometría).
Shader "DiceMaster/Light Beam"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.85, 0.6, 1)
        _Intensity ("Intensity", Range(0, 5)) = 0.6
        _LengthFalloff ("Length Falloff (más alto = se apaga antes)", Range(0.1, 8)) = 1.5
        _EdgePower ("Edge Softness (más alto = bordes más suaves)", Range(0.1, 8)) = 2
        _SoftDistance ("Soft Intersection (fundido al tocar la geometría)", Range(0.01, 2)) = 0.3
        _CameraFade ("Camera Fade (se desvanece al acercarse la cámara)", Range(0, 5)) = 1
        _NoiseScale ("Dust Noise Scale", Float) = 4
        _NoiseStrength ("Dust Noise Strength", Range(0, 1)) = 0.3
        _NoiseSpeed ("Dust Noise Speed", Float) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LightBeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One   // aditivo: solo suma luz
            ZWrite Off
            Cull Off        // se ven las dos caras del cono (también desde dentro)

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _LengthFalloff;
                float _EdgePower;
                float _SoftDistance;
                float _CameraFade;
                float _NoiseScale;
                float _NoiseStrength;
                float _NoiseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;   // y: 0 en el origen del haz, 1 al final
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash(i + float3(0, 0, 0));
                float n100 = Hash(i + float3(1, 0, 0));
                float n010 = Hash(i + float3(0, 1, 0));
                float n110 = Hash(i + float3(1, 1, 0));
                float n001 = Hash(i + float3(0, 0, 1));
                float n101 = Hash(i + float3(1, 0, 1));
                float n011 = Hash(i + float3(0, 1, 1));
                float n111 = Hash(i + float3(1, 1, 1));

                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);

                // Centro del haz (la cara mira a la cámara) brillante; bordes de la silueta, apagados.
                float edge = pow(saturate(abs(dot(normalWS, viewDirWS))), _EdgePower);

                // Se apaga a lo largo del haz.
                float lengthFade = pow(saturate(1.0 - input.uv.y), _LengthFalloff);

                // Fundido suave donde el cono atraviesa la mesa o el suelo (en vez de un corte duro).
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS.xy);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragmentEyeDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float soft = saturate((sceneEyeDepth - fragmentEyeDepth) / _SoftDistance);

                // Que no "tape" la cámara si se mete dentro o pasa muy cerca.
                float cameraFade = _CameraFade > 0.0 ? saturate(fragmentEyeDepth / _CameraFade) : 1.0;

                // Polvo: ruido en el mundo que sube despacio.
                float3 noisePosition = input.positionWS * _NoiseScale + float3(0.0, -_Time.y * _NoiseSpeed, 0.0);
                float dust = lerp(1.0, ValueNoise(noisePosition), _NoiseStrength);

                float intensity = _Intensity * edge * lengthFade * soft * cameraFade * dust;
                return half4(_Color.rgb * intensity, 0.0);
            }
            ENDHLSL
        }
    }
}