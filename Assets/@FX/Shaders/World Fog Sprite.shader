Shader "Blues With You/Rain/World Fog Sprite"
{
    Properties
    {
        [MainColor] _BaseColor("Fog Tint", Color) = (0.34, 0.42, 0.52, 0.08)
        _NoiseScale("Noise Scale", Range(1, 12)) = 4
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.36
        _EdgeSoftness("Edge Softness", Range(0.05, 0.48)) = 0.32
        _DepthFade("Surface Depth Fade", Range(0.1, 8)) = 1.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "WorldFogForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _NoiseScale;
                half _NoiseStrength;
                half _EdgeSoftness;
                half _DepthFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);
                output.eyeDepth = -TransformWorldToView(positionInputs.positionWS).z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half2 centered = input.uv * 2.0h - 1.0h;
                half radial = 1.0h - smoothstep(1.0h - _EdgeSoftness, 1.0h, length(centered));
                half noise = lerp(1.0h, (half)Hash21(floor(input.uv * _NoiseScale)), _NoiseStrength);
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                half depthFade = saturate((sceneEyeDepth - input.eyeDepth) * _DepthFade);
                half4 color = _BaseColor * input.color;
                color.a *= radial * noise * depthFade;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
