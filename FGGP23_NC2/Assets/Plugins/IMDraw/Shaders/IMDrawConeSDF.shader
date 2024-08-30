// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "IMDraw/IMDrawConeSDF"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _SrcBlend ("SrcBlend", Int) = 5.0 // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10.0 // OneMinusSrcAlpha
        _ZWrite ("ZWrite", Int) = 1.0 // On
        _ZTest ("ZTest", Int) = 4.0 // LEqual
        _Cull ("Cull", Int) = 0.0 // Off
        _ZBias ("ZBias", Float) = 0.0

        _Angle("Angle", Float) = 1.0
        _H("H", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Offset [_ZBias], [_ZBias]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "UnityCG.cginc"
            #define STEPS 64
            #define STEP_SIZE 0.01
            #define MIN_DISTANCE 0.01

            struct appdata_t {                
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
                float3 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _Color;
            fixed4 _Origin;
            
            float _Angle;
            float _H;
            float4x4 _InverseTransformMatrix; // TODO: Find a way to apply rotation to SDF                                   
            float4x4 _TranslationMatrix;

            float coneDistance( float3 p, float2 c, float h )
            {
                // c is the sin/cos of the angle, h is height
                // Alternatively pass q instead of (c,h),
                // which is the point at the base in 2D
                float2 q = h*float2(c.x/c.y,-1.0);
                    
                // float2 w = float2( length(p.xz), p.y - (h/2.0) );
                float2 w = float2( length(p.xz), p.y - (h/2.0) );
                float2 a = w - q*clamp( dot(w,q)/dot(q,q), 0.0, 1.0 );
                float2 b = w - q*float2( clamp( w.x/q.x, 0.0, 1.0 ), 1.0 );
                float k = sign( q.y );
                float d = min(dot( a, a ),dot(b, b));
                float s = max( k*(w.x*q.y-w.y*q.x),k*(w.y-q.y)  );
                return sqrt(d)*sign(s);
            }

            float raymarch(float3 position, float3 direction)
            {
                for (int i=0; i<STEPS; i++)
                {                                        
                    float distance = coneDistance(position, float2(sin(_Angle), cos(_Angle)), _H);                
                    if (distance < MIN_DISTANCE)
                    {
                        return 1;
                    }
                    position += direction * distance;
                }
                return 0;
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPosition = i.worldPosition;
                worldPosition = mul(_TranslationMatrix, float4(worldPosition, 1)).xyz;
                worldPosition = mul(_InverseTransformMatrix, float4(worldPosition, 1)).xyz;

                float3 viewDirection = normalize(i.worldPosition - _WorldSpaceCameraPos);                
                if (unity_OrthoParams.w > 0.01)
                {
                    viewDirection = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                }
                viewDirection = mul(_InverseTransformMatrix, float4(viewDirection, 1)).xyz;                
                
                float rm = raymarch(worldPosition, viewDirection);              
                if (rm <= 0) discard;                
                return rm;
                // return i.color;
            }
            ENDCG
        }
    }
}