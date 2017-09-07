Shader "Hidden/AnimationCaptureShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_OrgTex ("Original", 2D) = "white" {}
		_SrcWidth ("SrcWidth", Range(0.0, 1.0)) = 1
		_SrcHeight ("SrcHeight", Range(0.0, 1.0)) = 1
		_SrcX ("SrcX", Range(0.0, 1.0)) = 0.5
		_SrcY ("SrcY", Range(0.0, 1.0)) = 0.5
		_DstWidth ("DstWidth", Range(0.0, 1.0)) = 1
		_DstHeight ("DstHeight", Range(0.0, 1.0)) = 1
		_DstX ("DstX", Range(0.0, 1.0)) = 0.5
		_DstY ("DstY", Range(0.0, 1.0)) = 0.5
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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

			float _SrcWidth;
			float _SrcHeight;
			float _SrcX;
			float _SrcY;
			float _DstWidth;
			float _DstHeight;
			float _DstX;
			float _DstY;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv=  v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _OrgTex;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uvdst = float2((i.uv.x - _DstX) * (_SrcWidth/_DstWidth) + _SrcX, (i.uv.y - _DstY) * (_SrcHeight/_DstHeight) + _SrcY);
				if (abs(uvdst.x - _SrcX) < _SrcWidth/2 && abs(uvdst.y - _SrcY) < _SrcHeight/2) {
					fixed4 col = tex2D(_MainTex, uvdst);
					col.a = 1;
					return col;
				}else{
					fixed4 col = tex2D(_OrgTex, i.uv);
					return col;
				}
			}
			ENDCG
		}
	}
}
