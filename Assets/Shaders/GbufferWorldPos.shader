Shader "CasualPRT/GbufferWorldPos"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : SEMANTIC_HELLO_WORLD;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.worldPos, 1.0);
            }
            ENDHLSL
        }
    }
}
