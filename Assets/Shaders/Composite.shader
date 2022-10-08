Shader "CasualPRT/Composite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/SH.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            TEXTURE2D_X(_CameraDepthTexture);
            TEXTURE2D_X_HALF(_GBuffer0);
            TEXTURE2D_X_HALF(_GBuffer1);
            TEXTURE2D_X_HALF(_GBuffer2);
            float4x4 _ScreenToWorld[2];
            SamplerState my_point_clamp_sampler;

            float _coefficientVoxelGridSize;
            float4 _coefficientVoxelCorner;
            float4 _coefficientVoxelSize;
            StructuredBuffer<int> _coefficientVoxel; 
            StructuredBuffer<int> _lastFrameCoefficientVoxel;

            float _GIIntensity;

            float4 GetFragmentWorldPos(float2 screenPos)
            {
                float sceneRawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, my_point_clamp_sampler, screenPos);
                float4 ndc = float4(screenPos.x * 2 - 1, screenPos.y * 2 - 1, sceneRawDepth, 1);
                #if UNITY_UV_STARTS_AT_TOP
                    ndc.y *= -1;
                #endif
                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndc);
                worldPos /= worldPos.w;

                return worldPos;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);

                // decode from gbuffer
                float4 worldPos = GetFragmentWorldPos(i.uv);
                float3 albedo = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, my_point_clamp_sampler, i.uv, 0).xyz;
                float3 normal = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, i.uv, 0).xyz;

                float3 gi = SampleSHVoxel(
                    worldPos, 
                    albedo, 
                    normal,
                    _coefficientVoxel,
                    _coefficientVoxelGridSize,
                    _coefficientVoxelCorner,
                    _coefficientVoxelSize
                );
                color.rgb += gi * _GIIntensity;
                
                return color;
            }
            ENDHLSL
        }
    }
}
