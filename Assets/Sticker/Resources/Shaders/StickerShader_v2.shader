Shader "Sticker/Unlit_2"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BlendTex ("_BlendTex", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		//Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		//ZWrite Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _BlendTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 col2 = tex2D(_BlendTex, i.uv);
				float4 bg = lerp(col2, col, col.a);
				//float4 result = col+bg;
				return bg;
			}
			ENDCG
		}
	}
}
