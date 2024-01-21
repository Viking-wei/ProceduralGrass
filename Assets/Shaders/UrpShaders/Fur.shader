Shader "URPCustomShader/Fur"
{
     Properties
    {
        _FurNoiseTex("Fur Noise Texture", 2D) = "white" {}
        
        _FurRootColor ("Root Color", Color) = (1, 1, 1, 1)
        _FurSurfaceColor ("Surface Color", Color) = (1, 1, 1, 1)
        
        _LayerOffset ("Layer Offset", float) = 0.1
        _FurOffset("Fur Offset", Vector) = (0,0,0,0)
        _FurLength("Fur Length", float) = 0.5
        
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0,8)) = 5
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Fur"
            
            Cull off
            Blend SrcAlpha OneMinusSrcAlpha
            //Ztest always
            ZWrite On
            
            Tags
            { 
                "lightMode"="universalForward"
            }
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            
            CBUFFER_START(UnityPerMaterial)
            float4 _FurNoiseTex_ST;
            
            half4 _FurRootColor;
            half4 _FurSurfaceColor;
            float _LayerOffset;

            float4 _FurOffset;
            float _FurLength;
            half4 _RimColor;
            float _RimPower;
            CBUFFER_END
            
            TEXTURE2D(_FurNoiseTex);
            SAMPLER(sampler_FurNoiseTex);

            
            
            struct  Attributes
            {
                float4 positionOS: POSITION;
                float2 texCoord: TEXCOORD0;
                float3 normalOS: NORMAL;
            };

            struct Varyings
            {
                float4 positionCS: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 positionWS: TEXCOORD1;
                float3 normalWS: TEXCOORD2;
                float4 shadowCoord: TEXCOORD4;
            };
            
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS=IN.positionOS.xyz+IN.normalOS*_LayerOffset*_FurLength+_FurOffset.xyz;
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.positionWS=TransformObjectToWorld(posOS);
                OUT.normalWS=TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord=TransformWorldToShadowCoord(OUT.positionWS);
                
                OUT.uv.xy=TRANSFORM_TEX(IN.texCoord,_FurNoiseTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal=normalize(IN.normalWS);
                
                Light light=GetMainLight(IN.shadowCoord);
                half3 lightDir=normalize(light.direction);
                half3 viewDir=normalize(_WorldSpaceCameraPos.xyz-IN.positionWS);
                half NdotV=saturate(dot(normal,viewDir));
                half NdotL=saturate(dot(normal,lightDir));
                float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
                half NdotH=saturate(dot(normal,halfVec));

                half3 rim=_RimColor.rgb*pow(1-NdotV,_RimPower);
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz *_FurSurfaceColor.rgb;
                half3 specular=light.color.rgb*Pow4(NdotH)*light.shadowAttenuation;
                half3 diffuse=SampleSH(normal)*_FurSurfaceColor.rgb;
                
                half3 color=specular+diffuse+ambient+rim;
                half3 finalColor=lerp(_FurRootColor.rgb,color,_LayerOffset);
                
                half noise=SAMPLE_TEXTURE2D(_FurNoiseTex, sampler_FurNoiseTex, IN.uv).r;
                half alpha=saturate(noise-(_LayerOffset*_LayerOffset));
                
                return half4(finalColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
