Shader "Custom/URP_WorldOriginFog"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        [Header(Fog Colors)]
        _FogColor ("Fog Color", Color) = (0.5, 0.6, 0.7, 1)

        [Header(Vertical Fog Settings)]
        _FogHeight ("Fog Max Height (Y)", Float) = 2.0
        _HeightFalloff ("Height Falloff", Float) = 5.0

        [Header(Circular Fog Settings)]
        _StartDistance ("Start Distance", Float) = 10.0
        _EndDistance ("Full Fog Distance", Float) = 50.0
    }

    SubShader
    {
        // THIS TAG IS CRITICAL FOR URP
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP Core Library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            // Texture needs to be outside the CBUFFER
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Variables inside CBUFFER for SRP Batcher compatibility
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FogColor;
                float _FogHeight;
                float _HeightFalloff;
                float _StartDistance;
                float _EndDistance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // URP specific helper for Object Space -> World Space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS; // Clip Space

                // Standard UV tiling/offset
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture using URP macros
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // --- LOGIC 1: CIRCULAR FOG (XZ Plane) ---
                // Distance from (0,0,0) ignoring height
                float dist = length(input.positionWS.xz);
                float radialFogFactor = saturate((dist - _StartDistance) / (_EndDistance - _StartDistance));

                // --- LOGIC 2: VERTICAL FOG (Y Axis) ---
                // Difference between fog height and pixel height
                float heightFogFactor = saturate((_FogHeight - input.positionWS.y) / _HeightFalloff);

                // --- COMBINE ---
                float finalFogFactor = max(radialFogFactor, heightFogFactor);

                // --- APPLY ---
                return lerp(col, _FogColor, finalFogFactor);
            }
            HLSLPROGRAM
        }
    }
}