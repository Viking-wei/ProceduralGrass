Shader "URPCustomShader/Voronoi"
{
    Properties
    {
        _CellNum("CellNum",Float)=50
        _Disorder("Disorder",Float)=7
        _ClumpNum("ClumpNum",Float)=1
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
            float _Disorder;
            float _ClumpNum;
            CBUFFER_END

            float2 N22(float2 p)
            {
                float3 a = frac(p.xyx*float3(123.34,234.34,345.65));
                a += dot(a, a+34.45);
                return frac(float2(a.x*a.y,a.y*a.z));
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
                half2 extendUV=IN.uv*_CellNum;
                half2 uv=frac(extendUV);
                half2 id=floor(extendUV);
                half clumpId=0;

                float minDist=1000;
                half2 clumpCenter=half2(0,0);

                for(float y=-1;y<=1;y++)
                {
                    for(float x=-1;x<=1;x++)
                    {
                        half2 offset=float2(x,y);
                        float2 currentId=id+offset;
                        half2 random=N22(currentId);
                        half2 p=offset+sin(random*_Disorder)*0.3;
                        half2 temp=uv-p;
                        half dist=length(temp);
                        if(dist<minDist)
                        {
                            minDist=dist;
                            clumpCenter=IN.uv-temp/_CellNum;
                            clumpId=fmod((int)(clumpCenter.x+clumpCenter.y)*10,_ClumpNum);
                        }
                    }
                }
                return half4(clumpId,clumpCenter,1);
            }
            ENDHLSL
        }
    }
}
