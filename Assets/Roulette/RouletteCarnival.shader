Shader "DiceMaster/Roulette Carnival"
{
    Properties
    {
        _BaseColor ("Original material color", Color) = (0.78,0.59,0.31,1)
        _Metallic ("Metallic", Range(0,1)) = 0.233
        _Smoothness ("Smoothness", Range(0,1)) = 0.552
        [HDR] _GlowColor ("Bulb color", Color) = (1,0.42,0.07,1)
        _Idle ("Idle emission", Float) = 0.6
        _Intensity ("Chase emission", Float) = 5
        _ChaseSize ("Group size", Float) = 3
        [HideInInspector] _ChasePhase ("Phase", Float) = 0
        [HideInInspector] _Chasing ("Chasing", Float) = 0
        [HideInInspector] _Reveal ("Reveal", Float) = 0
        [HideInInspector] _Facing ("Facing", Float) = 1
        [HideInInspector] _PointerMode ("Pointer", Float) = 0
        [HideInInspector] _Bounce ("Bounce", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _GlowColor;
            float _Metallic, _Smoothness, _Idle, _Intensity, _ChaseSize;
            float _ChasePhase, _Chasing, _Reveal, _Facing, _PointerMode, _Bounce;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 originalOS : TEXCOORD2;
                half fog : TEXCOORD3;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.originalOS = v.positionOS.xyz;
                // Pivot at the top of the actual triangular pointer. Logical marker stays fixed.
                float angle = radians(_Bounce * _PointerMode);
                float sn, cs; sincos(angle, sn, cs);
                float2 pivot = float2(0, 0.00267497);
                float2 p = v.positionOS.xy - pivot;
                v.positionOS.xy = mul(float2x2(cs,-sn,sn,cs), p) + pivot;
                v.normalOS.xy = mul(float2x2(cs,-sn,sn,cs), v.normalOS.xy);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.originalOS.xy - float2(0,-0.00884);
                float radius = length(p);
                // Rim studs: 15 spheres at radius .01115, z .00295. Excludes hub and support bolts.
                float mask = step(0.01065, radius) * step(radius, 0.01165) * step(0.00245, i.originalOS.z) * (1-_PointerMode);
                float slot = floor(frac(atan2(p.x * _Facing, p.y) / TWO_PI + 1) * 16 + 0.5);
                // Slot zero is deliberately missing under the pointer; chase skips that gap.
                float index = clamp(slot - 1, 0, 14);
                float distanceBehind = fmod(_ChasePhase - index + 30, 15);
                float chase = (1-smoothstep(max(0,_ChaseSize-1), _ChaseSize, distanceBehind)) * _Chasing;
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = _BaseColor.rgb;
                surface.alpha = 1;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.emission = _GlowColor.rgb * mask * (_Idle + chase * _Intensity + _Reveal);
                InputData input = (InputData)0;
                input.positionWS = i.positionWS;
                input.normalWS = normalize(i.normalWS);
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI = SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask = half4(1,1,1,1);
                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, i.fog);
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
