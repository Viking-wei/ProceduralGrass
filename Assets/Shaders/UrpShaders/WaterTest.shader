Shader "URPCustomShader/WaterTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            CBUFFER_END
            
            const float _CausticSpeed = 0.131; // _151._m29
            const float _WorldPosXY_Speed1X = -0.02;  // _151._m44
            const float _WorldPosXY_Speed1Y = -0.01;  // _151._m45
            const float _CausticScale = 0.25;  // _151._m28
            const float _CausticNormalDisturbance = 0.096;   // _151._m33

            float GenshipCaustic(float3 _lookThroughAtTerrainWorldPos)
            {
                float3 _causticPos3DInput;
                    _causticPos3DInput.xy = (_Time.x * _CausticSpeed * float2(_WorldPosXY_Speed1X, _WorldPosXY_Speed1Y) * 25.0) + _lookThroughAtTerrainWorldPos.xz * _CausticScale;
                    // shadertoy 这里没有水面法线信息，屏蔽这个
                    // _causticPos3DInput.xy += _terrainToSurfLength * _CausticNormalDisturbance * _surfNormal.xz; 
                    _causticPos3DInput.z  = _Time.x * _CausticSpeed;

                float3 _step1;
                _step1.x = dot(_causticPos3DInput, float3(-2.0, 3.0, 1.0));
                _step1.y = dot(_causticPos3DInput, float3(-1.0, -2.0, 2.0));
                _step1.z = dot(_causticPos3DInput, float3(2.0, 1.0, 2.0));

                float3 _step2;
                _step2.x = dot(_step1, float3(-0.8, 1.2, 0.4));
                _step2.y = dot(_step1, float3(-0.4, -0.8, 0.8));
                _step2.z = dot(_step1, float3(0.8, 0.4, 0.8));

                float3 _step3;
                _step3.x = dot(_step2, float3(-0.6, 0.9, 0.3));
                _step3.y = dot(_step2, float3(-0.3, -0.6, 0.6));
                _step3.z = dot(_step2, float3(0.6, 0.3, 0.6));

                float3 _hnf1 = 0.5 - frac(_step1);
                float3 _hnf2 = 0.5 - frac(_step2);
                float3 _hnf3 = 0.5 - frac(_step3);
                
                float _min_dot_result = min(dot(_hnf3, _hnf3), min(dot(_hnf2, _hnf2), dot(_hnf1, _hnf1)));

                float _local_127 = (_min_dot_result * _min_dot_result * 7.0);
                float _causticNoise3DResult = _local_127 * _local_127;
                
                return _causticNoise3DResult;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };



            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS=TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS=TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv=IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half res=GenshipCaustic(IN.positionWS);
                return half4(res,res,res,1);
            }
            ENDHLSL
        }
    }
}
