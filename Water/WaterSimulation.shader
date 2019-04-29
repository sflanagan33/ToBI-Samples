Shader "Custom/WaterSimulation"
{
  Properties
  {
    _Impulse ("Impulse", Vector) = (0,0,0)
    _Waves ("Waves", 2D) = "gray" {}
    [MaterialToggle] _ShouldSimulate("Should Simulate", Float) = 0
  }

  SubShader
  {
    Lighting Off
    Blend One Zero

    Pass
    {
      CGPROGRAM
      #include "UnityCustomRenderTexture.cginc"
      #pragma vertex CustomRenderTextureVertexShader
      #pragma fragment frag
      #pragma target 3.0

      float getTarget(sampler2D data, float2 uv)
      {
        float sum = 0;
        float weight = 0;

        for (int i = -2; i <= 2; i++)
        {
          for (int j = -2; j <= 2; j++)
          {
            float strength = 1 / (1 + sqrt(i * i + j * j));
            sum += tex2D(data, uv + float2(i, j) / 256).r * strength;
            weight += strength;
          }
        }

        weight += 0.5;

        return sum / weight;
      }

      float3 _Impulse;
      sampler2D _Waves;
      float _ShouldSimulate;

      float4 frag(v2f_customrendertexture IN) : COLOR
      {
        // Read data from texture

        float2 uv = IN.localTexcoord.xy;
        float2 c = tex2D(_SelfTexture2D, uv).rg;

        if (_ShouldSimulate < 0.5)
          return float4(c.r, c.g, 0, 0);

        float displacement = c.r;
        float velocity = c.g;
        
        // Find target (zero position) by averaging surrounding points

        float target = getTarget(_SelfTexture2D, uv);

        // Hooke's law

        velocity += (target - displacement) * 0.1;	// Acceleration towards target
        velocity *= 0.975;							// Deceleration (loss of energy)
        displacement += velocity;					// Velocity affects position
        
        // Apply the given impulse

        float dist = 1;
        for (int i = -1; i <= 1; i++)
          for (int j = -1; j <= 1; j++)
            dist = min(dist, distance(uv + float2(i, j), frac(_Impulse.xy)));

        float strength = 1 - step(0.01, dist);
        displacement += _Impulse.z * strength;

        // Apply the wave texture

        velocity += tex2D(_Waves, uv * 2 + _Time.x * 1.5).r * 0.0020;
        velocity += tex2D(_Waves, uv * 8 + _Time.x * 2.0).r * 0.0005;

        // Apply foam

        //foam = tex2D(_SelfTexture2D, uv - target.yz * 0.0005).b;
        //foam = saturate(foam + _Impulse.z * strength);
        //foam *= 0.975f;

        // Write data back into texture

        float r = displacement;
        float g = velocity;
        return float4(r, g, 0, 0);
      }

      ENDCG
    }
  }
}