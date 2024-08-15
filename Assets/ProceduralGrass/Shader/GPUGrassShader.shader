Shader "URPCustomShader/GPUGrassShader"
{
    Properties
    {
        _AlbedoTexture("AlbedoTexture",2D)="White" {}
        _GlossyTexture("GlossyTexture",2D)="White" {}
        _Smoothness("Smothness",Range(0,5))=1
        _GrassColor("GrassColor",Color)=(1,1,1,1)
        _GrassBottomColor("GrassButtomColor",Color)=(1,1,1,1)
        _TiltAmplitude("TiltAmplitude",Range(0,0.2))=0.1
        _WaveAmplitude("WaveAmplitude",Range(0,1))=0.1
        _WindSpeed("WindSpeed",float)=1
        _Disorder("Disorder",Range(0,2))=1
        _CurvedNormalAmount("CurvedNormalAmount",Range(0,2))=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name"GRASS"
            Tags{"LightMode"="UniversalForward"}
            
            Cull off
            
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "GrassUtilites.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            float _Smoothness;
            float4 _GrassColor;
            float4 _GrassBottomColor;
            float _WaveAmplitude;
            float _WindSpeed;
            float _CurvedNormalAmount;
            float _TiltAmplitude;
            float _Disorder;
            CBUFFER_END

            TEXTURE2D(_AlbedoTexture);
            SAMPLER(sampler_AlbedoTexture);
            TEXTURE2D(_GlossyTexture);
            SAMPLER(sampler_GlossyTexture);
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            struct GrassPropertiesStruct
            {
                float3 position;
                float angle;
                float height;
                float width;
                float bend;
                float hash;
                float windForce;
                float3 surfaceNorm;
                float3 motionVec;
            };
            
            StructuredBuffer<GrassPropertiesStruct>GrassProperties;
            StructuredBuffer<int> Triangles;
            StructuredBuffer<float4> Colors;
            StructuredBuffer<float2> Uvs;

            

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                float3 curvedNormalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor:TEXCOORD4;
            };

            Varyings vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
            {
                Varyings OUT;
                GrassPropertiesStruct grassProperties=GrassProperties[instance_id];

                int index=Triangles[vertex_id];
                float4 color=Colors[index];
                OUT.uv=Uvs[index];

                float t=color.x;
                float side=2*color.y-1;
                float height=grassProperties.height;
                float width=grassProperties.width;
                float bend=grassProperties.bend;
                float hash=grassProperties.hash;
                float windForce=grassProperties.windForce;
                
                bend+=grassProperties.motionVec.z+windForce;
                bend=bend>1.5?1.5:bend;
                
                float3 tangent=CurveTangent(bend,t);
                float3 normalWS=normalize(cross(float3(0,0,1),tangent));
                float3 curvedNormal=normalWS;
                curvedNormal.z+=side*_CurvedNormalAmount;
                curvedNormal=normalize(curvedNormal);
                
                float3 positionWS=ApproximateCurve(height,bend,t);
                positionWS.z+=side*width;

                float motionAngle=GetMotionAngle(grassProperties.motionVec.xy);
                float finalAngle=lerp(grassProperties.angle,motionAngle,grassProperties.motionVec.z);
                float3x3 matrix1=AngleAxis3x3(-0.6,float3(0,0,1));
                float3x3 rotMatrix=mul(AngleAxis3x3(finalAngle,float3(0,1,0)),matrix1);
                positionWS=mul(rotMatrix,positionWS);
                positionWS=positionWS+grassProperties.position;
                normalWS=mul(rotMatrix,normalWS);
                curvedNormal=mul(rotMatrix,curvedNormal);
                
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS=normalWS;
                OUT.curvedNormalWS=curvedNormal;
                OUT.positionWS=positionWS;
                OUT.shadowCoord=TransformWorldToShadowCoord(positionWS);
                OUT.fogFactor=0;
                return OUT;
            }
            
            half4 frag (Varyings IN,half facing : VFACE) : SV_Target
            {
                Light light=GetMainLight(IN.shadowCoord);
                float3 lightDir=light.direction;
                float3 normal=normalize(IN.normalWS);
                float3 curvedNormal=normalize(IN.curvedNormalWS);
                //curvedNormal=facing<0?curvedNormal:reflect(curvedNormal,normal);
                //return half4(normal,1);
                
                float3 positionWS=IN.positionWS;
                float3 viewDir=normalize(_WorldSpaceCameraPos-positionWS);
                float3 halfDir=normalize(lightDir+viewDir);
                float3 diffuse=_GrassColor.rgb*max(0,dot(curvedNormal,lightDir));
                float3 specular=_GrassColor.rgb*light.color.rgb*pow(max(0,dot(curvedNormal,halfDir)),200);
                float3 ambient=_GlossyEnvironmentColor.rgb*_GrassColor.rgb;
                //curvedNormal=curvedNormal*0.5+0.5;
                //return float4(curvedNormal,1);
                return float4(specular+diffuse+ambient,1);
            }
    
            ENDHLSL
        }
    }
}
