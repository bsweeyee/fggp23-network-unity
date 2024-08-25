// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "IMDraw/IMDrawTorusSDF"
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
        
        _MajorRadius("MajorRadius", Float) = 1.0
        _MinorRadius("MinorRadius", Float) = 0.5
        _Normal("Normal", Vector) = (0,0,0,1) 
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
            float _MajorRadius;
            float _MinorRadius; 
            float3 _Normal;
            float4x4 _RotationMatrix; // TODO: Find a way to apply rotation to torus SDF                                   
            
            float torusDistance(float3 p, float2 t)
            {
                float2 q = float2(length(p.xz) - t.x, p.y);
                return length(q) - t.y;
            }                            

            bool raymarch(float3 position, float majorRadius, float minorRadius, float3 direction)
            {
                for (int i=0; i<STEPS; i++)
                {                                        
                    float2 t = float2(majorRadius, minorRadius);                                                
                    float distance = torusDistance(position, t);                    
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
                // float3 worldPosition = mul(_RotationMatrix, float4(i.worldPosition, 1)).xyz;
                float3 viewDirection = normalize(i.worldPosition - _WorldSpaceCameraPos);                
                if (unity_OrthoParams.w > 0.01)
                {
                    viewDirection = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                }                
                
                float rm = raymarch(worldPosition, _MajorRadius, _MinorRadius, viewDirection);              
                if (rm <= 0) discard;                
                return rm;
                // return i.color;
            }
            ENDCG
        }
    }
}