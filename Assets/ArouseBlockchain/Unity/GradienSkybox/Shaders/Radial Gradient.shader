Shader "Gradient/Radial"
{
	Properties
	{
		_Color1("Color A", Color) = (1, 1, 1, 0)
		_Color2("Color B", Color) = (1, 1, 1, 0)
		_Scale("Scale", Float) = 1.0
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		Tags
		{
			"IgnoreProjector" = "True"
			"Queue" = "Background"
			"RenderType" = "Opaque"
			"PreviewType" = "Skybox"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			float4 _Color1;
			float4 _Color2;
			float  _Scale;
			struct a2v
			{
				float4 vertex : POSITION;
			};
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float4 position : TEXCOORD0;
			};
			struct f2g
			{
				float4 color : SV_TARGET;
			};
			void Vert(a2v i, out v2f o)
			{
				o.vertex = o.position = UnityObjectToClipPos(i.vertex);
			}
			void Frag(v2f i, out f2g o)
			{
				o.color = lerp(_Color1, _Color2, length(i.position.xy / i.position.w) * _Scale);
			}
			ENDCG
		}
	}
}