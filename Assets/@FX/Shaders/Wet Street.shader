Shader "Blues With You/Rain/Wet Street"
{
    Properties
    {
        [MainColor] _BaseColor("Asphalt Color", Color) = (0.018, 0.028, 0.04, 1)
        [HideInInspector] _Color("Legacy Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor("Ambient Reflection", Color) = (0.004, 0.012, 0.025, 1)
        [HDR] _ReflectionTint("Wet Reflection Tint", Color) = (0.08, 0.18, 0.34, 1)
        [MainTexture] _RoadAlbedo("Road Albedo", 2D) = "gray" {}
        [Normal] _RoadNormal("Road Normal", 2D) = "bump" {}
        [NoScaleOffset] _PuddleMask("Puddle Distribution", 2D) = "white" {}
        [Normal] _WaterNormal("Flowing Water Normal", 2D) = "bump" {}
        [Normal] _RainPuddleNormal("Rain Puddle Normal", 2D) = "bump" {}
        [NoScaleOffset][Normal] _RainRippleNormal("Rain Ripple Sheet", 2D) = "bump" {}
        _Wetness("Wetness", Range(0, 1)) = 0.92
        _Metallic("Metallic", Range(0, 1)) = 0.08
        _Smoothness("Smoothness", Range(0, 1)) = 0.94
        _RippleScale("Ripple Density", Range(0.1, 8)) = 1.6
        _RippleSpeed("Ripple Frames Per Second", Range(0, 30)) = 20
        _RippleStrength("Ripple Normal Strength", Range(0, 0.3)) = 0.075
        _SurfaceTexScale("Surface Texture Scale", Range(0.01, 2)) = 0.12
        _PuddleTiling("Puddle Tiling", Range(0.02, 2)) = 0.18
        _WaterTiling("Water Normal Tiling", Range(0.05, 8)) = 1.35
        _WaterFlow("Water Flow Speed", Range(0, 1)) = 0.035
        _RoadNormalStrength("Road Normal Strength", Range(0, 2)) = 0.72
        _RainNormalStrength("Rain Normal Strength", Range(0, 2)) = 0.58
        _PuddleContrast("Puddle Contrast", Range(0.01, 1)) = 0.32
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FXCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half4 _EmissionColor;
                half4 _ReflectionTint;
                float4 _RoadAlbedo_ST;
                float4 _RoadNormal_ST;
                float4 _WaterNormal_ST;
                float4 _RainPuddleNormal_ST;
                half _Wetness;
                half _Metallic;
                half _Smoothness;
                half _RippleScale;
                half _RippleSpeed;
                half _RippleStrength;
                half _SurfaceTexScale;
                half _PuddleTiling;
                half _WaterTiling;
                half _WaterFlow;
                half _RoadNormalStrength;
                half _RainNormalStrength;
                half _PuddleContrast;
            CBUFFER_END

            TEXTURE2D(_RoadAlbedo);
            SAMPLER(sampler_RoadAlbedo);
            TEXTURE2D(_RoadNormal);
            SAMPLER(sampler_RoadNormal);
            TEXTURE2D(_PuddleMask);
            SAMPLER(sampler_PuddleMask);
            TEXTURE2D(_WaterNormal);
            SAMPLER(sampler_WaterNormal);
            TEXTURE2D(_RainPuddleNormal);
            SAMPLER(sampler_RainPuddleNormal);
            TEXTURE2D(_RainRippleNormal);
            SAMPLER(sampler_RainRippleNormal);
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) *
                    (input.tangentOS.w * GetOddNegativeScale());
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // YNL's Rain Ripple graph animates its normal sheet as a 4x4
            // flipbook. A per-cell phase keeps the road from pulsing in sync
            // while retaining one flush floor surface with no decal geometry.
            float RippleCellHash(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 RippleFlipbookUV(float2 positionXZ, float density, float phaseOffset)
            {
                float2 tiled = positionXZ * density;
                float2 cell = floor(tiled);
                float2 localUV = frac(tiled) * 0.92 + 0.04;
                float phase = _Time.y * _RippleSpeed + RippleCellHash(cell) * 16.0 + phaseOffset;
                float frame = fmod(floor(phase), 16.0);
                float2 tile = float2(fmod(frame, 4.0), 3.0 - floor(frame * 0.25));
                return (localUV + tile) * 0.25;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float waterTime = _Time.y * _WaterFlow;
                // The road is world-projected, but every visible texture field must still
                // respect the material inspector's Tiling and Offset values.
                float2 surfaceUv = input.positionWS.xz * _SurfaceTexScale;
                float2 roadAlbedoUv = surfaceUv * _RoadAlbedo_ST.xy + _RoadAlbedo_ST.zw;
                float2 roadNormalUv = surfaceUv * _RoadNormal_ST.xy + _RoadNormal_ST.zw;
                float2 puddleUv = input.positionWS.xz * _PuddleTiling;
                half3 roadAlbedo = SAMPLE_TEXTURE2D(_RoadAlbedo, sampler_RoadAlbedo, roadAlbedoUv).rgb;
                half puddleSource = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, puddleUv).r;
                half puddle = smoothstep(_PuddleContrast,
                    min(1.0h, _PuddleContrast + 0.24h), puddleSource);
                half wet = saturate(_Wetness * (0.58h + puddle * 0.42h));

                half3 roadNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RoadNormal, sampler_RoadNormal, roadNormalUv), _RoadNormalStrength);
                float2 waterUv = input.positionWS.xz * _WaterTiling;
                float2 flowA = waterUv * _WaterNormal_ST.xy + _WaterNormal_ST.zw +
                    float2(waterTime, waterTime * 0.37);
                float2 flowB = waterUv * (0.73 * _WaterNormal_ST.xy) + _WaterNormal_ST.zw +
                    float2(-waterTime * 0.41, waterTime * 0.61);
                half3 waterA = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaterNormal, sampler_WaterNormal, flowA), 0.55h);
                half3 waterB = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaterNormal, sampler_WaterNormal, flowB), 0.42h);
                half3 puddleNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RainPuddleNormal, sampler_RainPuddleNormal,
                        input.positionWS.xz * (0.5 * _RainPuddleNormal_ST.xy) +
                        _RainPuddleNormal_ST.zw), _RainNormalStrength);
                half3 rippleA = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RainRippleNormal, sampler_RainRippleNormal,
                        RippleFlipbookUV(input.positionWS.xz, _RippleScale, 0.0)),
                    _RippleStrength * 5.0h);
                half3 rippleB = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RainRippleNormal, sampler_RainRippleNormal,
                        RippleFlipbookUV(input.positionWS.xz + float2(2.71, 5.43), _RippleScale * 0.73, 7.0)),
                    _RippleStrength * 3.5h);
                half3 rippleNormal = normalize(half3(rippleA.xy + rippleB.xy * 0.62h, 1.0h));

                half2 combinedXY = roadNormal.xy;
                combinedXY += (waterA.xy + waterB.xy) * wet * 0.38h;
                combinedXY += puddleNormal.xy * puddle * 0.46h;
                combinedXY += rippleNormal.xy * puddle * 0.72h;
                half3 combinedNormalTS = normalize(half3(combinedXY,
                    sqrt(saturate(1.0h - dot(combinedXY, combinedXY)))));
                half3x3 tangentToWorld = half3x3(normalize(input.tangentWS),
                    normalize(input.bitangentWS), normalize(input.normalWS));
                half3 normalWS = normalize(TransformTangentToWorld(combinedNormalTS, tangentToWorld));
                half3 viewWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(saturate(1.0h - dot(normalWS, viewWS)), 5.0h);
                half sparkle = pow(saturate(rippleNormal.x * 0.5h + 0.5h), 12.0h) * puddle;

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = viewWS;
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.fogCoord = input.fogFactor;
                lighting.vertexLighting = VertexLighting(input.positionWS, normalWS);
                lighting.bakedGI = SampleSH(normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = roadAlbedo * _BaseColor.rgb * lerp(1.12h, 0.56h, wet);
                surface.metallic = _Metallic;
                surface.specular = half3(0.04h, 0.04h, 0.04h);
                surface.smoothness = lerp(_Smoothness * 0.42h, _Smoothness, wet);
                surface.normalTS = half3(0, 0, 1);
                // Reflections must come from the PBR BRDF, probes and actual
                // lights. The old broad fresnel/puddle emission made the whole
                // road glow blue even when no local light reached it.
                half rippleGlint = sparkle * (0.012h + fresnel * 0.018h);
                surface.emission = _EmissionColor.rgb + _ReflectionTint.rgb * wet * rippleGlint;
                surface.occlusion = lerp(0.82h, 1.0h, wet);
                surface.alpha = 1.0h;
                surface.clearCoatMask = saturate(wet * 0.58h + puddle * 0.34h);
                surface.clearCoatSmoothness = _Smoothness;

                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
