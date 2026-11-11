Shader "DiceMaster/Roulette Rim Trails"
{
    Properties { _Emission ("HDR Emission", Range(1, 6)) = 3 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Emission;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.color=v.color; o.uv=v.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half soft=pow(saturate(1-abs(i.uv.y*2-1)),2);
                return half4(i.color.rgb * _Emission, i.color.a*soft*smoothstep(0,0.12,i.uv.x));
            }
            ENDHLSL
        }
    }
}
