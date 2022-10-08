Shader "CasualPRT/SHDebug"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/SH.hlsl"

            // 使用定点数存储小数, 因为 compute shader 的 InterlockedAdd 不支持 float
            // array size: 3x9=27
            CBUFFER_START(UnityPerMaterial)
                StructuredBuffer<int> _coefficientSH9; 
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.normal = normalize(o.normal);
                return o;
            }

            // for validate
            /*
            float3 Evaluate(float3 dir, float3 _SH[9]) {
                const float c1 = 0.42904276540489171563379376569857;
                const float c2 = 0.51166335397324424423977581244463;
                const float c3 = 0.24770795610037568833406429782001;
                const float c4 = 0.88622692545275801364908374167057;
                
                return  max(float3(0,0,0), 
                    +  c4 * _SH[0]                                                                           //   c4  L00 
                    +  c2 * 2.0 * (_SH[3] * dir.x + _SH[1]* dir.y + _SH[2]* dir.z)                           // 2 c2 (L11 x + L1-1 y + L10 z)
                    +  c1 * 2.0 * (_SH[4] * dir.x * dir.y + _SH[7] * dir.x * dir.z + _SH[5] * dir.y * dir.z) // 2 c1 (L2-2 xy + L21 xz + L2-1 yz)
                    + (c1 * (dir.x * dir.x - dir.y * dir.y)) * _SH[8]                                        //   c1 L22 (x²-y²)
                    + (c3 * (3.0 * dir.z * dir.z - 1.0)) * _SH[6]                                            //   c3 L20 (3.z² - 1)
                );
            }
            */

            float4 frag (v2f i) : SV_Target
            {
                float3 dir = i.normal;

                // decode sh
                
                float3 c[9];
                for(int i=0; i<9; i++)
                {
                    c[i].x = DecodeFloatFromInt(_coefficientSH9[i*3+0]);
                    c[i].y = DecodeFloatFromInt(_coefficientSH9[i*3+1]);
                    c[i].z = DecodeFloatFromInt(_coefficientSH9[i*3+2]);
                }
                
                /*
                // for validate
                float3 c[9] = {
                    float3( 0.7953949,  0.4405923,  0.5459412 ),
                    float3( 0.3981450,  0.3526911,  0.6097158 ),
                    float3(-0.3424573, -0.1838151, -0.2715583 ),
                    float3(-0.2944621, -0.0560606,  0.0095193 ),
                    float3(-0.1123051, -0.0513088, -0.1232869 ),
                    float3(-0.2645007, -0.2257996, -0.4785847 ),
                    float3(-0.1569444, -0.0954703, -0.1485053 ),
                    float3( 0.5646247,  0.2161586,  0.1402643 ),
                    float3( 0.2137442, -0.0547578, -0.3061700 )
                };*/

                // decode irradiance
                float3 irradiance = IrradianceSH9(c, dir);
                float3 Lo = irradiance / PI;

                //Lo = SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, dir, 0).rgb;

                //return float4(Evaluate(dir.xzy, c), 1.0);
                return float4(Lo, 1.0);
            }
            ENDHLSL
        }
    }
}
