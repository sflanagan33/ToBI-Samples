Shader "Custom/Water"
{
	Properties
	{
    _EdgeLength ("Tesselation Edge Length", Range(2,50)) = 50

    _SimulationTex ("Simulation Texture", 2D) = "gray" {}
    _SimulationStrength ("Simulation Displacement Strength", Float) = 0.3
		
    _ColorTex ("Water Palette", 2D) = "white" {}
		_HighlightThresholdMax ("Water Palette Depth", Float) = 1

		_NormalOffset ("Normal Offset", Range(0, 1)) = 0.5
		_NormalStrength ("Normal Strength", Range(0, 100)) = 1
    _NormalFarTex ("Normal Far Tex", 2D) = "bump" {}

    _CausticsTex ("Caustics Texture", 2D) = "gray" {}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
		}

		GrabPass
		{
			"_GrabBackground"
		}
		
		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows vertex:vert tessellate:tessEdge nolightmap alpha:fade
		#pragma target 4.6
		#include "UnityCG.cginc"
		#include "Tessellation.cginc"

		// =============================================================================

		float _EdgeLength;

		float4 tessEdge(appdata_full v0, appdata_full v1, appdata_full v2)
		{
			return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
		}
		
		// =============================================================================

		sampler2D _SimulationTex;
    float _SimulationStrength;

		void vert(inout appdata_full v)
		{
			float3 pos = mul(unity_ObjectToWorld, v.vertex).xyz;
			//float d = tex2Dlod(_SimulationTex, float4(v.texcoord.xy * 0.5 + _Time.xx * 2, 0, 0)).r * _SimulationStrength;
			float d = tex2Dlod(_SimulationTex, float4(pos.xz / 256 * 4, 0, 0)).r * _SimulationStrength;

			v.vertex.xyz += v.normal.xyz * d * sqrt(v.color.r);
			v.color.g = -UnityObjectToViewPos(v.vertex).z;
		}

		// =============================================================================

		float height(sampler2D tex, float2 uv)
		{
			return tex2D(tex, uv).r;
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
		
		// =============================================================================
		
		sampler2D _CameraDepthTexture;
		sampler2D _GrabBackground;
		
		sampler2D _ColorTex;
		float _HighlightThresholdMax;

		float _NormalOffset;
		float _NormalStrength;
    sampler2D _NormalFarTex;

    sampler2D _CausticsTex;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 viewDir;
			float4 screenPos;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			// Get normal and distortion displacement
			
			float s = sqrt(IN.color.r);
			half glancing = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
			s *= 1 - saturate(pow(glancing, 64) * 4);

			float fadeStart = 25;
			float fadeEnd = 150;
			float dist = distance(IN.worldPos, _WorldSpaceCameraPos);
			float fade = saturate((dist - fadeStart) / (fadeEnd - fadeStart));

			fixed3 closeRead = fixed3(0, 0, 0);
			fixed3 longRead = fixed3(0, 0, 0);

			// Close read (from simulation)
			
			if (fade < 1)
			{
				float2 normUV = IN.worldPos.xz / 256 * 4;
				float h = height(_SimulationTex, normUV);
				fixed2 uvA = normUV + fixed2(0, -_NormalOffset);
				fixed2 uvB = normUV + fixed2(-_NormalOffset, 0);
				fixed2 uvC = normUV + fixed2(0, +_NormalOffset);
				fixed2 uvD = normUV + fixed2(+_NormalOffset, 0);
				closeRead = computeNormals(height(_SimulationTex, uvA),
												  height(_SimulationTex, uvB),
												  height(_SimulationTex, uvC),
												  height(_SimulationTex, uvD),
												  h, _NormalStrength * s);
			}

			// Long read (from texture)

			if (fade > 0)
			{
				longRead += UnpackScaleNormal(tex2D(_NormalFarTex, IN.worldPos.xz / 1024 + fixed2(1, 1)       * _Time.x), s * 0.15);
				longRead += UnpackScaleNormal(tex2D(_NormalFarTex, IN.worldPos.xz / 128  + fixed2(0.55, 0.65) * _Time.x), s * 0.20);
				longRead += UnpackScaleNormal(tex2D(_NormalFarTex, IN.worldPos.xz / 96   + fixed2(2.55, 2.25) * _Time.x), s * 0.15);
			}

			// Blend and apply

			o.Normal = lerp(closeRead, longRead, fade);
			
			// Get the closeness of the current fragment to the fragment already in the depth buffer
			
			float4 screenUV = IN.screenPos + o.Normal.xyxy * 0.25;
			float sceneZ = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(screenUV)).r);
      float partZ = IN.color.g;
      float diff = saturate((abs(sceneZ - partZ)) / _HighlightThresholdMax);

			// Do caustics

			float3 viewDirection = normalize(IN.worldPos - _WorldSpaceCameraPos);
			float3 backgroundWorldPos = viewDirection * sceneZ + _WorldSpaceCameraPos;
			float causticDistort = tex2D(_SimulationTex, backgroundWorldPos.xz / 256 * 4).g;

			fixed3 sunDir = _WorldSpaceLightPos0.xyz;
			fixed3 uAxis = normalize(cross(sunDir, fixed3(0, 1, 0)));
			fixed3 vAxis = cross(sunDir, uAxis);
			fixed u = dot(backgroundWorldPos, uAxis);
			fixed v = dot(backgroundWorldPos, vAxis);
			fixed2 coords = fixed2(u, v) * 0.25;
			fixed causticStrength = saturate(diff * 10);
			float3 c1 = tex2D(_CausticsTex, coords * +0.8 + fixed2(_Time.x * +3, _Time.x * -2) + causticDistort).rgb;
			float3 c2 = tex2D(_CausticsTex, coords * -1.2 + fixed2(_Time.x * +2, _Time.x * +1) + causticDistort).rgb;

			o.Emission += causticStrength * pow(c1 * c2, 0.75) * 0.75 * pow(1 - diff, 0.5);

			// Water effect

      fixed3 waterColor = tex2D(_ColorTex, fixed2(diff, 0)).rgb;

			float waterMin = min(min(waterColor.r, waterColor.g), waterColor.b);
			float waterMax = max(max(waterColor.r, waterColor.g), waterColor.b);
			float waterSaturation = (waterMax - waterMin) / waterMax;

			fixed3 background = tex2Dproj(_GrabBackground, screenUV).rgb;	// should be grabPos

			float backgroundGray = dot(background.rgb, fixed3(0.2126, 0.7152, 0.0722));
			float3 backgroundColored = backgroundGray.rrr * waterColor;

			o.Albedo = lerp(background, backgroundColored, waterSaturation);
			o.Smoothness = saturate(diff * 1000) * 0.925;
			o.Alpha = saturate(diff * 100);

			// Edge foam

			o.Albedo = lerp(o.Albedo, fixed3(1.25, 1.25, 1.25), saturate(1 - diff * 25));
		}

		ENDCG
	}

	FallBack Off
}