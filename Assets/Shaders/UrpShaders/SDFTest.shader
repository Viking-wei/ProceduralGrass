Shader "URPCustomShader/SDFTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SurfaceColor ("Surface Color", Color) = (1,1,1,1)
        _SlicedTex ("Sliced Texture", 2D) = "white" {}
        _Edge ("Edge", Range(-0.5,0.5)) = 0.0
        _CircleRadius ("Circle Radius", Range(0.0,0.5)) = 0.35
        _CircleColor ("Circle Color", Color) = (1,1,1,1)
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" }

        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            
            Cull Off
            
            HLSLPROGRAM
            #define MAX_MARCHING_STEPS 50
            #define MAX_DISTANCE 10.0
            #define SURFACE_DISTANCE 0.001
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SlicedTex);
            SAMPLER(sampler_SlicedTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _SlicedTex_ST;
            float _Edge;
            float4 _SurfaceColor;
            float _CircleRadius;
            float4 _CircleColor;
            CBUFFER_END

            float PlaneSDF(float3 rayPos)
            {
                float plane=rayPos.y-_Edge;
                return plane;
            }

            float SphereCasting(float3 rayOrigin,float3 rayDir)
            {
                float distOrigin=0;
                for(int i=0;i<MAX_MARCHING_STEPS;++i)
                {
                    float3 rayPos=rayOrigin+rayDir*distOrigin;
                    float distScene=PlaneSDF(rayPos);
                    distOrigin+=distScene;

                    if(distScene<SURFACE_DISTANCE||distOrigin>MAX_DISTANCE)
                        break;
                }
                return distOrigin;
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
                float3 positionOS : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS=TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv=IN.uv;
                OUT.positionOS=IN.positionOS.xyz;
                return OUT;
            }

            half4 frag (Varyings IN,bool face:SV_isFrontFace) : SV_Target
            {
                if(IN.positionOS.y>_Edge)
                    discard;
                
                float3 rayOrigin=TransformWorldToObject(_WorldSpaceCameraPos);
                float3 rayDir=normalize(IN.positionOS-rayOrigin);
                float t=SphereCasting(rayOrigin,rayDir);

                half3 slicedColor=_SurfaceColor.rgb;
                half circleColor=0;
                if(t<MAX_DISTANCE)
                {
                    float3 castPositionOS=rayOrigin+t*rayDir;
                    float2 slicedUV=castPositionOS.xz;

                    float len=length(slicedUV);
                    float currentRadius=sqrt(0.25-pow(_Edge,2));
                    circleColor=smoothstep(len-0.01,len+0.01,currentRadius-_CircleRadius);
                    slicedUV=TRANSFORM_TEX(slicedUV,_SlicedTex)*0.5/currentRadius+0.5f;
                    slicedColor=SAMPLE_TEXTURE2D(_SlicedTex,sampler_SlicedTex,slicedUV).rgb*circleColor+_CircleColor.rgb*(1-circleColor);
                }
                
                return face?_SurfaceColor:half4(slicedColor,1);
            }
            ENDHLSL
        }
    }
}
