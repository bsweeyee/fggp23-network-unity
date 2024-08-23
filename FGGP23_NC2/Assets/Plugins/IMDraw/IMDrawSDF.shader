// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/IMDrawSDF"
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
        _LineStart("LineStart", Vector) = (0,0,0,1)
        _LineEnd("LineEnd", Vector) = (0,0,0,1)
        _Radius("Radius", Float) = 1.0
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
            fixed4 _LineStart;
            fixed4 _LineEnd;
            float _Radius;

            float sphereDistance (float3 p)
            {
                return distance(p,_LineStart.xyz) - _Radius;
            }

            float capsuleDistance(float3 p, float3 a, float3 b, float r)
            {
                float3 pa = p - a;
                float3 ba = b - a;
                float h = clamp(dot(pa,ba)/dot(ba,ba), 0.0, 1.0);
                return length(pa - ba*h) - r;               
            }
            
            fixed4 raymarch(float3 position, float3 direction)
            {
                for (int i=0; i<STEPS; i++)
                {
                    float distance = sphereDistance(position);
                    if (distance < MIN_DISTANCE)
                    {
                        // return i / (float) STEPS;
                        return 1;
                    }                                                       
                    position += direction * distance;
                }
                return 0;
            }

            bool raymarchCapsule(float3 position, float3 start, float3 end, float radius, float3 direction)
            {
                for (int i=0; i<STEPS; i++)
                {
                    float distance = capsuleDistance(position, start, end, radius);
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
                float3 viewDirection = normalize(i.worldPosition - _WorldSpaceCameraPos);                
                if (unity_OrthoParams.w > 0.01)
                {
                    viewDirection = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                }                
                // float rm = raymarch(worldPosition, viewDirection);  
                float rm = raymarchCapsule(worldPosition, _LineStart, _LineEnd, _Radius, viewDirection);              
                // if (rm <= 0) discard;                
                return rm;
                // return i.color;
            }
            ENDCG
        }
    }
}