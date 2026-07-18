Shader "Blues With You/Rain/Rain Streak"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color("Legacy Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 12)) = 1.8
        _StreakSharpness("Streak Sharpness", Range(0.5, 8)) = 2.4
        _SegmentFrequency("Reflection Segments", Range(0, 40)) = 11
        _ScrollSpeed("Scroll Speed", Range(-8, 8)) = 1.7
        _EdgeSoftness("End Softness", Range(0.01, 0.45)) = 0.14
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_particles
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half _Intensity;
                half _StreakSharpness;
                half _SegmentFrequency;
                half _ScrollSpeed;
                half _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half across = pow(saturate(1.0h - abs((half)input.uv.x * 2.0h - 1.0h)), _StreakSharpness);
                half head = smoothstep(0.0h, _EdgeSoftness, (half)input.uv.y);
                half tail = 1.0h - smoothstep(1.0h - _EdgeSoftness, 1.0h, (half)input.uv.y);
                half phase = sin((input.positionWS.z + input.uv.y) * _SegmentFrequency - _Time.y * _ScrollSpeed);
                half segments = lerp(1.0h, smoothstep(-0.15h, 0.72h, phase), saturate(_SegmentFrequency * 0.08h));
                half alpha = input.color.a * _BaseColor.a * across * head * tail * segments;
                half glint = 0.8h + 0.2h * smoothstep(0.65h, 1.0h, phase);
                half3 color = input.color.rgb * _BaseColor.rgb * (_Intensity * glint);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
