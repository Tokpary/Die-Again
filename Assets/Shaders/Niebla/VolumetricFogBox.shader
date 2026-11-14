// Niebla volumétrica baja para URP. Se pone en un Cubo (el de Unity, de -0.5 a 0.5) que cubra la parte baja de la sala.
// Por cada píxel recorre el rayo de la cámara dentro del cubo, acumula niebla (más densa abajo, con ruido que se mueve)
// y se corta donde hay geometría, así envuelve las patas de la mesa, las paredes, etc.
// Necesita la Depth Texture activada en el URP Asset (o en la cámara).
Shader "DiceMaster/Volumetric Fog Box"
{
    Properties
    {
        [HDR] _FogColor ("Fog Color", Color) = (0.75, 0.8, 0.9, 1)
        _Density ("Density", Range(0, 5)) = 1
        _MaxOpacity ("Max Opacity", Range(0, 1)) = 0.85
        _HeightFalloff ("Height Falloff (más alto = más pegada al suelo)", Range(0.1, 8)) = 2
        _EdgeSoftness ("Edge Softness (fundido de los lados del cubo)", Range(0.01, 0.5)) = 0.15
        _NoiseScale ("Noise Scale", Float) = 0.6
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.7
        _Wind ("Wind (XYZ, unidades por segundo)", Vector) = (0.15, 0, 0.05, 0)
        _Steps ("Steps (calidad)", Range(4, 64)) = 24
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
            Name "VolumetricFog"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always    // la profundidad la comprobamos nosotros con la Depth Texture
            Cull Front      // se pintan las caras de dentro: funciona aunque la cámara esté dentro del cubo

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MAX_STEPS 64

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _Density;
                float _MaxOpacity;
                float _HeightFalloff;
                float _EdgeSoftness;
                float _NoiseScale;
                float _NoiseStrength;
                float4 _Wind;
                float _Steps;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // ---------------------------------------------------------------- Ruido

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

            // Dos capas de ruido: formas grandes y algo de detalle.
            float Fbm(float3 p)
            {
                return ValueNoise(p) * 0.6 + ValueNoise(p * 2.13 + 7.31) * 0.4;
            }

            // ---------------------------------------------------------------- Rayo contra el cubo

            // Entrada y salida del rayo en el cubo unidad (-0.5..0.5) en espacio local.
            bool IntersectBox(float3 origin, float3 dir, out float tNear, out float tFar)
            {
                float3 safeDir = (step(0.0, dir) * 2.0 - 1.0) * max(abs(dir), 1e-6);   // evita dividir entre 0
                float3 invDir = 1.0 / safeDir;
                float3 t0 = (-0.5 - origin) * invDir;
                float3 t1 = ( 0.5 - origin) * invDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                tNear = max(max(tMin.x, tMin.y), tMin.z);
                tFar = min(min(tMax.x, tMax.y), tMax.z);
                return tFar > max(tNear, 0.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 cameraWS = GetCameraPositionWS();
                float3 rayWS = normalize(input.positionWS - cameraWS);

                // Rayo en espacio local SIN normalizar: así "t" sigue midiendo distancia en el mundo.
                float3 originOS = TransformWorldToObject(cameraWS);
                float3 rayOS = mul((float3x3)GetWorldToObjectMatrix(), rayWS);

                float tNear, tFar;
                if (!IntersectBox(originOS, rayOS, tNear, tFar)) return half4(0, 0, 0, 0);
                tNear = max(tNear, 0.0);   // cámara dentro del cubo

                // La niebla acaba donde empieza la geometría (suelo, mesa, paredes...).
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS.xy);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
                float sceneDistance = sceneEyeDepth / max(dot(rayWS, cameraForward), 1e-4);
                tFar = min(tFar, sceneDistance);
                if (tFar <= tNear) return half4(0, 0, 0, 0);

                int steps = (int)_Steps;
                float stepLength = (tFar - tNear) / steps;

                // Desplazamiento distinto por píxel para que no se vean bandas entre pasos.
                float dither = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                float t = tNear + stepLength * dither;
                float transmittance = 1.0;

                [loop]
                for (int i = 0; i < MAX_STEPS; i++)
                {
                    if (i >= steps) break;

                    float3 pOS = originOS + rayOS * t;
                    float3 pWS = cameraWS + rayWS * t;

                    float height = saturate(0.5 - pOS.y);   // 1 en la base del cubo, 0 arriba
                    float heightFactor = pow(height, _HeightFalloff);
                    float edge = saturate((0.5 - abs(pOS.x)) / _EdgeSoftness) *
                                 saturate((0.5 - abs(pOS.z)) / _EdgeSoftness);
                    float noise = Fbm(pWS * _NoiseScale + _Wind.xyz * _Time.y);

                    float density = _Density * heightFactor * edge * lerp(1.0, noise, _NoiseStrength);
                    transmittance *= exp(-density * stepLength);
                    t += stepLength;
                }

                float alpha = (1.0 - transmittance) * _MaxOpacity;
                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}