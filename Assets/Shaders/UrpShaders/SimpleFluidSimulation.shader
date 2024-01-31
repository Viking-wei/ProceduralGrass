Shader "URPCustomShader/SimpleFluidSimulation"
{
    Properties
    {
        _VelocityTexNew ("Velocity Texture", 2D) = "white" {}
        _VelocityTexOld ("Velocity Texture", 2D) = "white" {}
        _PressureTex ("Pressure Texture", 2D) = "white" {}
        _DivergenceTex ("Divergence Texture", 2D) = "white" {}
        _AdvectSpeed ("Advect Speed", Float) = 1
        _InputPosAForceDir ("Input Position", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 0.01
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        //#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define dx 1.0
        #define dx2 (dx*dx)
        #define dt unity_DeltaTime.z
        #define halfrdx (0.5/dx)
        
        TEXTURE2D(_VelocityTexNew);
        TEXTURE2D(_VelocityTexOld);
        TEXTURE2D(_PressureTex);
        TEXTURE2D(_DivergenceTex);
 

        float4 _TexelSize;
        float _Viscosity;

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float _AdvectSpeed;
        float4 _InputPosAForceDir;
        float _Radius;
        CBUFFER_END

        struct MyAttributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            
        };

        struct VaryingsSimple
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        struct VaryingsSurround
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 lr: TEXCOORD1;
            float4 ud: TEXCOORD2;
        };

        VaryingsSimple vert_simple (MyAttributes IN)
        {
            VaryingsSimple OUT;
            OUT.positionHCS=TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv=IN.uv;
            return OUT;
        }

        VaryingsSurround vert_surround (MyAttributes IN)
        {
            VaryingsSurround OUT;
            OUT.positionHCS=TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv=IN.uv;
            OUT.lr=IN.uv.xyxy+float4(-1,0,1,0)*_TexelSize.xyxy;
            OUT.ud=IN.uv.xyxy+float4(0,1,0,-1)*_TexelSize.xyxy;
            return OUT;
        }
        
        
        half4 frag_advect (VaryingsSimple IN) : SV_Target
        {
            half2 currentVelocity=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.uv).xy;
            half2 previousPosition=IN.uv-currentVelocity*dt*_AdvectSpeed;
            half4 currentColor=SAMPLE_TEXTURE2D(_VelocityTexOld,sampler_LinearClamp,previousPosition);
            return currentColor;
        }

        half4 frag_diffusion (VaryingsSurround IN) : SV_Target
        {
            half4 L=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.lr.xy);
            half4 R=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.lr.zw);
            half4 U=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.ud.xy);
            half4 D=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.ud.zw);

            half4 Bij=SAMPLE_TEXTURE2D(_VelocityTexOld,sampler_LinearClamp,IN.uv);
            half alpha=dx2/(_Viscosity*dt);
            half beta=4+alpha;

            return (L+R+U+D+Bij*alpha)/beta;
        }

        half4 frag_force(VaryingsSimple IN) : SV_Target
        {
            half2 velocity=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.uv).xy;
            half2 temp=_InputPosAForceDir.xy-IN.uv;
            velocity+=_InputPosAForceDir.zw*dt*exp(-dot(temp,temp)/(_Radius))*200;
            return float4(velocity,0,1);
        }

        half4 frag_divergence (VaryingsSurround IN) : SV_Target
        {
            half4 L=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.lr.xy);
            half4 R=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.lr.zw);
            half4 U=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.ud.xy);
            half4 D=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.ud.zw);
            half4 C=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.uv);

            if(IN.lr.x<=0.0) L=-C;
            if(IN.lr.z>=1.0) R=-C;
            if(IN.ud.y>=1.0) U=-C;
            if(IN.ud.w<=0.0) D=-C;
            
            return halfrdx*(R.x-L.x+U.y-D.y);
        }

        half4 frag_pressure (VaryingsSurround IN) : SV_Target
        {
            half L=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.lr.xy).x;
            half R=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.lr.zw).x;
            half U=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.ud.xy).x;
            half D=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.ud.zw).x;
            
            half4 Bij=SAMPLE_TEXTURE2D(_DivergenceTex,sampler_LinearClamp,IN.uv);
            half alpha=-dx2;
            half beta=4;
            
            return (L+R+U+D+Bij*alpha)/beta;
        }

        half4 frag_gradient (VaryingsSurround IN) : SV_Target
        {
            half L=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.lr.xy).x;
            half R=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.lr.zw).x;
            half U=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.ud.xy).x;
            half D=SAMPLE_TEXTURE2D(_PressureTex,sampler_LinearClamp,IN.ud.zw).x;

            half4 Bij=SAMPLE_TEXTURE2D(_VelocityTexNew,sampler_LinearClamp,IN.uv);

            Bij.xy -= halfrdx * float2(R - L, U - D);
            return Bij;
        }
        
        ENDHLSL

        Pass
        {
            Name "ADVECT"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_simple
            #pragma fragment frag_advect
            ENDHLSL
        }

        Pass
        {
            Name "DIFFUSION"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_surround
            #pragma fragment frag_diffusion
            ENDHLSL
        }

        Pass
        {
            Name "FORCE"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_simple
            #pragma fragment frag_force
            ENDHLSL
        }

        Pass
        {
            Name "DIVERGENCE"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_surround
            #pragma fragment frag_divergence
            ENDHLSL
        }

        Pass
        {
            Name "PRESSURE"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_surround
            #pragma fragment frag_pressure
            ENDHLSL
        }

        Pass
        {
            Name "GRADIENT"
            Tags{"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert_surround
            #pragma fragment frag_gradient
            ENDHLSL
        }
    }
}
