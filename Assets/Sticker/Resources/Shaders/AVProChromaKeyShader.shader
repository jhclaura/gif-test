// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Video/AVProChromaKeyShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
		_ThresholdSensitivity("Threshold Sensitivity", Range(0,5)) = 1
		_Smoothing("Smoothing", Range(0, 5)) = 1
		_KeyColor("Key Color", Color) = (1,1,1,1)

		[KeywordEnum(None, Top_Bottom, Left_Right)] Stereo("Stereo Mode", Float) = 0
		[Toggle(APPLY_GAMMA)] _ApplyGamma("Apply Gamma", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "IgnoreProjector"="False" "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		Lighting Off
		Cull Off

		Pass
		{
			Stencil {
				Ref 0
				Comp Equal
				Pass replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile MONOSCOPIC STEREO_TOP_BOTTOM STEREO_LEFT_RIGHT
			#pragma multi_compile __ APPLY_GAMMA

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
#if UNITY_VERSION >= 500
				UNITY_FOG_COORDS(1)
#endif
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;
			uniform float3 _cameraPosition;
			uniform float _ThresholdSensitivity;
			uniform float _Smoothing;
			uniform fixed4 _KeyColor;

			float chromakey(float lumaFactor, float3 p, float3 m)
			{
				if (m.x < 0.) {
					return 1.;
				}
				p.x *= lumaFactor;
				return distance(p, m);
			}

			float3 GammaToLinear(float3 col)
			{
				return pow(col, 2.2);
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);

//#if STEREO_TOP_BOTTOM | STEREO_LEFT_RIGHT
//				float4 scaleOffset = GetStereoScaleOffset(IsStereoEyeLeft(_cameraPosition, UNITY_MATRIX_V[0].xyz));
//				o.uv.xy *= scaleOffset.xy;
//				o.uv.xy += scaleOffset.zw;
//#endif

#if UNITY_VERSION >= 500
				UNITY_TRANSFER_FOG(o, o.vertex);
#endif

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv.xy) * _Color;
#if APPLY_GAMMA
				col.rgb = GammaToLinear(col.rgb);
#endif
				
#if UNITY_VERSION >= 500
				UNITY_APPLY_FOG(i.fogCoord, col);
#endif

				float blendValue = col.a;
				blendValue = min(
					blendValue,
					chromakey(_KeyColor.a, col, _KeyColor.rgb)
				);

				float s = _Smoothing * .16;
				float t = _ThresholdSensitivity * .2;
				float dropoff = clamp(t - s / 2., 0., 1.);
				float range = dropoff + s;

				blendValue = smoothstep(dropoff, range, blendValue);

				return fixed4(col.rgb, blendValue) * blendValue;
			}
			ENDCG
		}
	}
}
