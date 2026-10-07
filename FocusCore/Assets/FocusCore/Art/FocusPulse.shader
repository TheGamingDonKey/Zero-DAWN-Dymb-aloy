Shader "Focus/UnlitPulse"
{
    Properties
    {
        _BaseColor("Colour", Color) = (0.615,0.388,1,0.14)
        _Progress("Finite scan progress", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            // The lattice triangles face inward. Keep only the inner shell:
            // the wearer is inside it; an outside preview avoids doubled front/back grids.
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 facet : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 facet : TEXCOORD0;
                float3 direction : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Progress;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.facet = input.facet;
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 width = max(fwidth(input.facet.xyz), .0001);
                float3 interior = smoothstep(width*.35, width*1.45, input.facet.xyz);
                float edge = 1-min(interior.x,min(interior.y,interior.z));
                float tip = max(input.facet.x,max(input.facet.y,input.facet.z));
                float node = smoothstep(.97,.999,tip) * step(.79,input.facet.w);
                float sweepY = lerp(-1.15,1.15,_Progress);
                float band = 1-smoothstep(.03,.22,abs(input.direction.y-sweepY));
                float shimmer = .5+.5*sin(input.facet.w*39+_Progress*5);
                float facet = .015 + .055*band*shimmer;
                half3 colour = lerp(_BaseColor.rgb,half3(.76,.69,1),band*.42+node*.18);
                half alpha = _BaseColor.a * saturate(edge*(.46+.36*band)+node*.42+facet);
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
