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

            float3 ApproximateCurve(float h, float b, float t)
            {
                float c=2*h/(PI*b);
                t*=PI*b/2;
                float x=c*-cos(t)+c;
                float y=c*sin(t);
                return float3(x,y,0);
            }

            float3 CurveTangent(float b, float t)
            {
				t*=PI*b/2;
                return t<0.001? float3(0,1,0):normalize(float3(1,cos(t)/sin(t),0));
            }

            float RemapNeg11_01(float value)
            {
                return value*0.5+0.5;
            }

            float GetMotionAngle(float2 motionVec)
            {
                motionVec=motionVec*2-1;
                return atan2(-motionVec.y,motionVec.x);
            }