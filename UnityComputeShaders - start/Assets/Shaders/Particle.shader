Shader "Custom/Particle" {
    Properties     
    {         
        _PointSize("Point size", Float) = 5.0
        _ColorSpeed("Color Speed", Float) = 0.5
    }  

    SubShader {
       Pass {
          Tags{ "RenderType" = "Opaque" }
          LOD 200
          Blend SrcAlpha One

          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          #pragma target 5.0

          uniform float _PointSize;
          uniform float _ColorSpeed;

          #include "UnityCG.cginc"

          struct Particle
          {
             float3 position;
             float3 velocity;
             float life;
          };
          StructuredBuffer<Particle> particleBuffer;
          
          struct v2f
          {
             float4 position : SV_POSITION;
             float4 color    : COLOR;
             float  size     : PSIZE;
          };

          float3 AuroraShift(float t)
          {
             float cycle = frac(t);
             float phase = cycle * 3.0;
             int   seg   = (int)floor(phase);
             float f     = frac(phase);

             // Cubic smoothstep — no linear kinks
             f = f * f * (3.0 - 2.0 * f);

             float3 col;
             if      (seg == 0) col = lerp(float3(0.0, 0.6, 0.8),
                                           float3(0.2, 0.9, 0.6), f);
             else if (seg == 1) col = lerp(float3(0.2, 0.9, 0.6),
                                           float3(0.5, 0.2, 0.9), f);
             else               col = lerp(float3(0.5, 0.2, 0.9),
                                           float3(0.0, 0.6, 0.8), f);

             return col;
          }

          v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
          {
             v2f o = (v2f)0;

             float life    = sqrt(sin(particleBuffer[instance_id].position.x) + sin(particleBuffer[instance_id].position.y)) + sqrt(sin(particleBuffer[instance_id].position.y) + sin(particleBuffer[instance_id].position.x));
             float shimmer = (float)instance_id * 0.017;
             float t       = _Time.y * _ColorSpeed + shimmer;

             float3 rgb = AuroraShift(t);
             o.color    = float4(rgb, life * 0.25);

             o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].position, 1));
             o.size     = _PointSize;

             return o;
          }

          float4 frag(v2f i) : COLOR
          {
             return i.color;
          }

          ENDCG
       }
    }
    FallBack Off
}