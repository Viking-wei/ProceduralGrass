Shader "URPCustomShader/PerlinNoise"
{
    Properties
    {
        _CellNum("CellNum",Float) = 8
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" }

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


            CBUFFER_START(UnityPerMaterial)
            float _CellNum;
            CBUFFER_END

            float2 hash22(float2 p)
            {
                float2 res;
                res.x=p.x*127.1+p.y*311.7;
                res.y=p.x*269.5+p.y*183.3;

                float sin0=sin(res.x)*43758.5453123;
                float sin1=sin(res.y)*43758.5453123;
                res.x=(sin0-floor(sin0))*2-1;
                res.y=(sin1-floor(sin1))*2-1;

                return normalize(res);
            }

            float lerpFunc(float t)
            {
                //return 3*pow(t,2)-2*pow(t,3);
                return 6*pow(t,5)-15*pow(t,4)+10*pow(t,3);
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
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS=TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv=IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv=IN.uv*_CellNum;
                
                float2 p0=floor(uv);
                float2 p1=p0+float2(0,1);
                float2 p2=p0+float2(1,1);
                float2 p3=p0+float2(1,0);

                float2 gradP0=hash22(p0);
                float2 gradP1=hash22(p1);
                float2 gradP2=hash22(p2);
                float2 gradP3=hash22(p3);

                float2 p0pVec=(uv-p0);
                float2 p1pVec=(uv-p1);
                float2 p2pVec=(uv-p2);
                float2 p3pVec=(uv-p3);

                float product0=dot(gradP0,p0pVec);
                float product1=dot(gradP1,p1pVec);
                float product2=dot(gradP2,p2pVec);
                float product3=dot(gradP3,p3pVec);

                float d0=uv.x-p0.x;
                float t0=lerpFunc(d0);
                float n1=product1+t0*(product2-product1);
                float n0=product0+t0*(product3-product0);
                

                float d1=uv.y-p0.y;
                float t1=lerpFunc(d1);
                float n2=n0+t1*(n1-n0);

                n2=n2*0.5+0.5;
                return half4(1,1,1,1)*n2;
            }
            ENDHLSL
        }
    }
}
