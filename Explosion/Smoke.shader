Shader "Custom/Smoke"
{
	Properties
	{
		_BaseColor ("Color", Color) = (1,1,1,1)
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_ColorTex ("Color Gradient", 2D) = "white" {}
		_Offset("Offset", Range(0, 1)) = 0.5
		_Strength("Strength", Range(0, 10)) = 1
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
		}

		LOD 200
		Cull Off
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
		#pragma target 3.0

		sampler2D _NoiseTex;
		sampler2D _ColorTex;

		struct Input
		{
			float2 uv_NoiseTex;
			float4 vertexColor;
		};
		
		float height(sampler2D noise, float2 uv)
		{
			float4 t1 = float4(uv.x * 1, uv.y * 0.5, 0, 0);
			t1.y -= _Time.x * 5;
			t1.x -= uv.y * 0.25;

			float4 t2 = float4(uv.x * 1, uv.y * 0.4, 0, 0);
			t2.y -= _Time.x * 3;
			t2.x += uv.y * 0.25;

			float val1 = tex2Dlod(noise, t1).r;
			float val2 = tex2Dlod(noise, t2).r;
			float val = pow(max(1 - val1, 1 - val2), 16);

			float result = val;
			result = lerp(result, 0.5, pow(uv.y, 4));
			result = lerp(result, 0.5, pow(1 - uv.y, 4));

			return result;
		}
		
		float3 computeNormals(float h_A, float h_B, float h_C, float h_D, float h_N, float heightScale)
		{
			float3 va = { 0, 1, (h_A - h_N) * heightScale };
			float3 vb = { 1, 0, (h_B - h_N) * heightScale };
			float3 vc = { 0, -1, (h_C - h_N) * heightScale };
			float3 vd = { -1, 0, (h_D - h_N) * heightScale };

			float3 average_n = (cross(va, vb) + cross(vb, vc) + cross(vc, vd) + cross(vd, va)) * -0.25;
			return normalize(average_n);
		}

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			float h = height(_NoiseTex, v.texcoord.xy);

			v.vertex.xyz += v.normal * h * v.color.a * 1.5;
			o.vertexColor = v.color;
		}

		float4 _BaseColor;
		float _Offset;
		float _Strength;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float h = height(_NoiseTex, IN.uv_NoiseTex);
			
			// Calculate normal

			fixed2 uvA = IN.uv_NoiseTex + fixed2(0, +_Offset);
			fixed2 uvB = IN.uv_NoiseTex + fixed2(+_Offset, 0);
			fixed2 uvC = IN.uv_NoiseTex + fixed2(0, -_Offset);
			fixed2 uvD = IN.uv_NoiseTex + fixed2(-_Offset, 0);
			o.Normal = computeNormals(height(_NoiseTex, uvA),
						  height(_NoiseTex, uvB),
						  height(_NoiseTex, uvC),
						  height(_NoiseTex, uvD),
						  h, _Strength);

			// Calculate color

			h = lerp(h, h * pow(IN.uv_NoiseTex.y, 2), pow(IN.vertexColor.a, 0.25));
			h = lerp(h, 1, pow(IN.uv_NoiseTex.y, 4));

			float cut = pow(1 - IN.vertexColor.a, 0.5);
			float add = pow(1 - IN.vertexColor.a, 4);
			float sub = IN.vertexColor.a * 0.05;
			float c = saturate(h * cut + add - sub);

			clip(h - add);

			o.Albedo = _BaseColor * c;
			o.Emission = tex2D(_ColorTex, float2(c, 0)) / (c * 2 + 0.1);
			o.Smoothness = 0.15;
		}

		ENDCG
	}

	FallBack "Diffuse"
}