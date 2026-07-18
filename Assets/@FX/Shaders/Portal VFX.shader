Shader "Blues With You/Portal VFX"
{
    Properties
    {
        [MainColor] _BaseColor("Glow Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Color("Legacy Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor("Emission Tint", Color) = (0, 0, 0, 1)
        _Intensity("Intensity", Range(0, 16)) = 2.4
        _PulseSpeed("Pulse Speed", Range(0, 15)) = 2
        _VertexWave("Vertex Wave", Range(0, 0.25)) = 0.015
        _NoiseScale("Noise Scale", Range(0.1, 12)) = 2.8
        _NoiseSpeed("Noise Speed", Range(0, 5)) = 0.65
        _FresnelPower("Fresnel Power", Range(0.5, 10)) = 2.4
        _EdgeSoftness("Particle Edge Softness", Range(0.02, 0.8)) = 0.32
        [NoScaleOffset] _FlowTex("Wind Flow Noise", 2D) = "gray" {}
        [IntRange] _StencilRef("Stencil ID", Range(0, 255)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
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

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_particles
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FXCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half4 _EmissionColor;
                half _Intensity;
                half _PulseSpeed;
                half _VertexWave;
                half _NoiseScale;
                half _NoiseSpeed;
                half _FresnelPower;
                half _EdgeSoftness;
            CBUFFER_END

            TEXTURE2D(_FlowTex);
            SAMPLER(sampler_FlowTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float phase = dot(positionWS, float3(2.7, 1.9, 2.1)) + _Time.y * _PulseSpeed;
                positionWS += normalWS * sin(phase) * _VertexWave;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(saturate(1.0h - abs(dot(normalWS, viewWS))), _FresnelPower);
                float2 noiseUV = input.positionWS.xz * _NoiseScale + _Time.y * _NoiseSpeed;
                half noise = (half)FXNoise2D(noiseUV);
                half flow = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex,
                    input.positionWS.xz * 0.16 + float2(_Time.y * 0.035, -_Time.y * 0.022)).r;
                noise = saturate(noise * 0.68h + flow * 0.32h);
                half pulse = 0.82h + 0.18h * sin(_Time.y * _PulseSpeed + input.positionWS.y * 2.1h);
                half radial = FXRadialMask(input.uv, _EdgeSoftness);
                half alpha = saturate(input.color.a * _BaseColor.a * radial * (0.58h + noise * 0.42h));
                half glow = _Intensity * pulse * (0.72h + fresnel * 1.35h + noise * 0.28h);
                half3 color = (input.color.rgb * _BaseColor.rgb + _EmissionColor.rgb) * glow;
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
