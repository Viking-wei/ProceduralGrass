Shader "URPCustomShader/GPUGrassShader"
{
    Properties
    {
        _AlbedoTexture("AlbedoTexture",2D)="Green" {}
        _Smoothness("Smothness",Range(0,5))=1
        _GrassColor("GrassColor",Color)=(1,1,1,1)
        _TiltAmplitude("TiltAmplitude",Range(0,0.2))=0.1
        _WaveAmplitude("WaveAmplitude",Range(0,1))=0.1
        _WindSpeed("WindSpeed",float)=1
        _Disorder("Disorder",Range(0,2))=1
        _GrassHardness("GrassRigidy",Range(0,1))=1
        _GrassBendControl1("GrassBendControl1",Float)=1
        _GrassBendControl2("GrassBendControl2",Float)=1
        _CurvedNormalAmount("CurvedNormalAmount",Range(0,2))=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name"GRASS"
            Tags{"LightMode"="UniversalForward"}
            
            Cull Off
            
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            float _Smoothness;
            float4 _GrassColor;
            float _WaveAmplitude;
            float _WindSpeed;
            float _GrassHardness;
            float _GrassBendControl1;
            float _GrassBendControl2;
            float _CurvedNormalAmount;
            float _TiltAmplitude;
            float _Disorder;
            CBUFFER_END

            TEXTURE2D(_AlbedoTexture);
            SAMPLER(sampler_AlbedoTexture);

            float3x3 AngleAxis3x3(float angle, float3 axis)
	        {
		        float c, s;
		        sincos(angle, s, c);

		        float t = 1 - c;
		        float x = axis.x;
		        float y = axis.y;
		        float z = axis.z;
 
		        return float3x3(
			        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			        t * x * z - s * y, t * y * z + s * x, t * z * z + c
			        );
	        }

            float3 cubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t )
            {
                float3 a = lerp(p0, p1, t);
                float3 b = lerp(p2, p3, t);
                float3 c = lerp(p1, p2, t);
                float3 d = lerp(a, c, t);
                float3 e = lerp(c, b, t);
                return lerp(d,e,t); 
            }
            
            float3 bezierTangent(float3 p0, float3 p1, float3 p2, float3 p3, float t ){
            
                float omt = 1-t;
                float omt2 = omt*omt;
                float t2= t*t;

                float3 tangent = 
                    p0* (-omt2) +
                    p1 * (3 * omt2 - 2 *omt) +
                    p2 * (-3 * t2 + 2 * t) +
                    p3 * (t2);
                     
                return normalize(tangent);
            }

            float RemapNeg11_01(float value)
            {
                return value*0.5+0.5;
            }
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            struct GrassPropertiesStruct
            {
                float3 position;
                float angle;
                float height;
                float width;
                float tilt;
                float bend;
                float hash;
                float windForce;
                float3 surfaceNorm;
                float charaDistPower;
            };
            
            StructuredBuffer<GrassPropertiesStruct>GrassProperties;
            StructuredBuffer<int> Triangles;
            StructuredBuffer<float4> Colors;
            StructuredBuffer<float2> Uvs;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 curvedNormalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float test:TEXCOORD5;
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
                float tilt=grassProperties.tilt;
                float bend=grassProperties.bend;
                float hash=grassProperties.hash;
                float windForce=grassProperties.windForce;
                float characterDistancePower=grassProperties.charaDistPower;
                
                const float p1Weight=0.33f;
                const float p2Weight=0.66f;
                
                float3 p0=float3(0,0,0);
                float p3y=height*(1-tilt)*saturate(characterDistancePower/8+0.01);
                float p3x=sqrt(height*height-p3y*p3y);
                float3 p3=float3(p3x,p3y,0);
                float3 p1=p1Weight*p3;
                float3 p2=p2Weight*p3;

                float3 p3Norm=normalize(p3);
                float3 bladeVerticalDir=normalize(cross(float3(0,0,1),p3Norm));
                p1+=bend*bladeVerticalDir*_GrassBendControl1;
                p2+=bend*bladeVerticalDir*_GrassBendControl2;

                float amplitudeArg=RemapNeg11_01(sin(_Time.y*_WindSpeed*windForce+hash*_Disorder*PI));
                float p1Offset=pow(p1Weight,_GrassHardness)*_WaveAmplitude*amplitudeArg;
                float p2Offset=pow(p2Weight,_GrassHardness)*_WaveAmplitude*amplitudeArg;
                float p3Offset=_WaveAmplitude*amplitudeArg;
                
                p1+=p1Offset*bladeVerticalDir;
                p2+=p2Offset*bladeVerticalDir;
                p3+=p3Offset*bladeVerticalDir;

                float3 curvedTangent=bezierTangent(p0,p1,p2,p3,t);
                float3 normalWS=normalize(cross(float3(0,0,1),curvedTangent));
                float3 curvedNormal=normalWS;
                curvedNormal.z+=side*_CurvedNormalAmount;
                curvedNormal=normalize(curvedNormal);
                
                float3 positionWS=cubicBezier(p0,p1,p2,p3,t);
                positionWS.z+=side*width;
                
                float3x3 rotMatrix=AngleAxis3x3(grassProperties.angle,float3(0,1,0));
                positionWS=mul(rotMatrix,positionWS);
                positionWS=positionWS+grassProperties.position;
                normalWS=mul(rotMatrix,normalWS);
                curvedNormal=mul(rotMatrix,curvedNormal);
                
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS=normalWS;
                OUT.curvedNormalWS=curvedNormal;
                OUT.positionWS=positionWS;
                OUT.shadowCoord=TransformWorldToShadowCoord(positionWS);
                OUT.test=windForce;
                return OUT;
            }
            
            half4 frag (Varyings IN,half facing : VFACE) : SV_Target
            {
                Light mainLight=GetMainLight(IN.shadowCoord);
                half3 lightColor=mainLight.color;
                float3 lightDir=mainLight.direction;
                float3 normal=normalize(IN.normalWS);
                float3 curvedNormal=normalize(IN.curvedNormalWS);
                normal=facing<0?curvedNormal:reflect(curvedNormal,normal);
                float3 positionWS=IN.positionWS;
                float3 viewDir=normalize(_WorldSpaceCameraPos-positionWS);
                float3 halfDir=normalize(viewDir+lightDir);
                float NdotH=saturate(dot(normal,halfDir));

                half3 albedo=SAMPLE_TEXTURE2D(_AlbedoTexture,sampler_AlbedoTexture,IN.uv).rgb;
                half3 specular=_GrassColor.rgb*lightColor.rgb*saturate(pow(NdotH,_Smoothness))*mainLight.shadowAttenuation;
                half3 diffuse=_GrassColor.rgb*lightColor*SampleSH(normal);
                half4 finalColor=half4(specular*0.5+diffuse,1);
                half temp=IN.test;
                //return half4(albedo,1);
                return finalColor;
            }
    
            ENDHLSL
        }
    }
}
