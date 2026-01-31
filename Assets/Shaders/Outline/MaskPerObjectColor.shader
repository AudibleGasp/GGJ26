Shader "Hidden/Custom/MaskPerObject"
{
    Properties
    {
        _OutlineID("OutlineID", Range(0, 255)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "MaskWrite"
            Tags { "LightMode"="UniversalForward" } // so it can be drawn with URP passes

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
CBUFFER_START(UnityPerMaterial)
            half _OutlineID; // per-object
CBUFFER_END

            // Global
            half _OutlineGroupCount; // total number of outline groups

            // Simple hash to generate pseudo-random float between 0 and 1
            float HashFloat(float x)
            {
                return frac(sin(x) * 43758.5453);
            }

            // This technically supports 255 different shaded objects before
            float3 HashMatrix(float4x4 m)
            {
                // float seed = m._m00 + m._m11 + m._m22; // sum of diagonal as a simple seed
                float seed = m._m03 + m._m13 + m._m23; // world pos
                float r = HashFloat(seed);
                float g = frac(sin(seed * 37.719) * 43758.5453);
                // float b = (_OutlineID + 0.5) / _OutlineGroupCount;
                return float3(r, g, 0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // return _ObjectColor;
                float3 col = HashMatrix(unity_ObjectToWorld); 
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
