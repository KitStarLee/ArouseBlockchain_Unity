// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Nokdef/Alpha Uber Shader"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_MainTex("Main Texture", 2D) = "white" {}
		_Desaturate("Desaturate?", Range( 0 , 1)) = 0
		_MainTextureChannel("Main Texture Channel", Vector) = (1,1,1,0)
		_MainAlphaChannel("Main Alpha Channel", Vector) = (0,0,0,1)
		_MainTexturePanning("Main Texture Panning", Vector) = (0,0,0,0)
		_MainAlphaPanning("Main Alpha Panning", Vector) = (0,0,0,0)
		_AlphaOverride("Alpha Override", 2D) = "white" {}
		_AlphaOverridePanning("Alpha Override Panning", Vector) = (0,0,0,0)
		_AlphaOverrideChannel("Alpha Override Channel", Vector) = (1,0,0,0)
		_FlipbooksColumsRows("Flipbooks Colums & Rows", Vector) = (1,1,0,0)
		_DetailNoise("Detail Noise", 2D) = "white" {}
		_DetailNoisePanning("Detail Noise Panning", Vector) = (0,0,0,0)
		_DetailDistortionChannel("Detail Distortion Channel", Vector) = (1,0,0,0)
		_DistortionIntensity("Distortion Intensity", Float) = 0
		_DetailMultiplyChannel("Detail Multiply Channel", Vector) = (0,0,0,0)
		_DetailAdditiveChannel("Detail Additive Channel", Vector) = (0,0,0,0)
		[Toggle(_USESOFTALPHA_ON)] _UseSoftAlpha("UseSoftAlpha", Float) = 0
		_SoftFadeFactor("SoftFadeFactor", Range( 0.1 , 1)) = 0

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			Cull Off
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR
				#pragma shader_feature_local _USESOFTALPHA_ON


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					float4 ase_texcoord1 : TEXCOORD1;
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float2 _MainTexturePanning;
				uniform sampler2D _DetailNoise;
				uniform float2 _DetailNoisePanning;
				uniform float4 _DetailNoise_ST;
				uniform float4 _DetailDistortionChannel;
				uniform float _DistortionIntensity;
				uniform float2 _FlipbooksColumsRows;
				uniform float4 _MainTextureChannel;
				uniform float _Desaturate;
				uniform float4 _DetailAdditiveChannel;
				uniform float4 _DetailMultiplyChannel;
				uniform sampler2D _AlphaOverride;
				uniform float2 _AlphaOverridePanning;
				uniform float4 _AlphaOverride_ST;
				uniform float4 _AlphaOverrideChannel;
				uniform float2 _MainAlphaPanning;
				uniform float4 _MainAlphaChannel;
				uniform float4 _CameraDepthTexture_TexelSize;
				uniform float _SoftFadeFactor;


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
					float4 screenPos = ComputeScreenPos(ase_clipPos);
					o.ase_texcoord4 = screenPos;
					
					o.ase_texcoord3 = v.ase_texcoord1;

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 uv_DetailNoise = i.texcoord.xy * _DetailNoise_ST.xy + _DetailNoise_ST.zw;
					float2 panner80 = ( 1.0 * _Time.y * _DetailNoisePanning + uv_DetailNoise);
					float4 tex2DNode79 = tex2D( _DetailNoise, panner80 );
					float4 break2_g75 = ( tex2DNode79 * _DetailDistortionChannel );
					float4 appendResult11_g75 = (float4(break2_g75.x , break2_g75.y , break2_g75.z , break2_g75.w));
					float3 desaturateInitialColor190 = appendResult11_g75.xyz;
					float desaturateDot190 = dot( desaturateInitialColor190, float3( 0.299, 0.587, 0.114 ));
					float3 desaturateVar190 = lerp( desaturateInitialColor190, desaturateDot190.xxx, 1.0 );
					float3 DistortionNoise90 = desaturateVar190;
					float4 texCoord168 = i.ase_texcoord3;
					texCoord168.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult176 = (float2(texCoord168.z , 0.0));
					float2 appendResult182 = (float2(0.0 , 0.0));
					float2 LocalUVOffset184 = ( appendResult176 + appendResult182 );
					float2 uv_MainTex = i.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
					float3 UVFlipbookInput194 = ( ( DistortionNoise90 * _DistortionIntensity ) + float3( ( LocalUVOffset184 + uv_MainTex ) ,  0.0 ) );
					float temp_output_4_0_g119 = _FlipbooksColumsRows.x;
					float temp_output_5_0_g119 = _FlipbooksColumsRows.y;
					float2 appendResult7_g119 = (float2(temp_output_4_0_g119 , temp_output_5_0_g119));
					float totalFrames39_g119 = ( temp_output_4_0_g119 * temp_output_5_0_g119 );
					float2 appendResult8_g119 = (float2(totalFrames39_g119 , temp_output_5_0_g119));
					float4 texCoord75 = i.ase_texcoord3;
					texCoord75.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float clampResult42_g119 = clamp( texCoord75.x , 0.0001 , ( totalFrames39_g119 - 1.0 ) );
					float temp_output_35_0_g119 = frac( ( ( (float)0 + clampResult42_g119 ) / totalFrames39_g119 ) );
					float2 appendResult29_g119 = (float2(temp_output_35_0_g119 , ( 1.0 - temp_output_35_0_g119 )));
					float2 temp_output_15_0_g119 = ( ( UVFlipbookInput194.xy / appendResult7_g119 ) + ( floor( ( appendResult8_g119 * appendResult29_g119 ) ) / appendResult7_g119 ) );
					float2 temp_output_73_0 = temp_output_15_0_g119;
					float2 panner22 = ( 1.0 * _Time.y * _MainTexturePanning + temp_output_73_0);
					float4 break2_g143 = ( tex2D( _MainTex, panner22 ) * _MainTextureChannel );
					float4 appendResult11_g143 = (float4(break2_g143.x , break2_g143.y , break2_g143.z , break2_g143.w));
					float4 MainTexInfo25 = appendResult11_g143;
					float3 desaturateInitialColor166 = MainTexInfo25.xyz;
					float desaturateDot166 = dot( desaturateInitialColor166, float3( 0.299, 0.587, 0.114 ));
					float3 desaturateVar166 = lerp( desaturateInitialColor166, desaturateDot166.xxx, _Desaturate );
					float4 break156 = ( _DetailAdditiveChannel * tex2DNode79 );
					float4 appendResult155 = (float4(break156.x , break156.y , break156.z , break156.w));
					float3 desaturateInitialColor191 = appendResult155.xyz;
					float desaturateDot191 = dot( desaturateInitialColor191, float3( 0.299, 0.587, 0.114 ));
					float3 desaturateVar191 = lerp( desaturateInitialColor191, desaturateDot191.xxx, 1.0 );
					float3 AdditiveNoise91 = desaturateVar191;
					float4 texCoord71 = i.texcoord;
					texCoord71.xy = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float4 break2_g142 = ( tex2DNode79 * _DetailMultiplyChannel );
					float4 appendResult11_g142 = (float4(break2_g142.x , break2_g142.y , break2_g142.z , break2_g142.w));
					float4 temp_cast_11 = (1.0).xxxx;
					float4 temp_cast_12 = (1.0).xxxx;
					float4 ifLocalVar106 = 0;
					if( ( _DetailMultiplyChannel.x + _DetailMultiplyChannel.y + _DetailMultiplyChannel.z + _DetailMultiplyChannel.w ) <= 0.0 )
					ifLocalVar106 = temp_cast_12;
					else
					ifLocalVar106 = appendResult11_g142;
					float3 desaturateInitialColor189 = ifLocalVar106.xyz;
					float desaturateDot189 = dot( desaturateInitialColor189, float3( 0.299, 0.587, 0.114 ));
					float3 desaturateVar189 = lerp( desaturateInitialColor189, desaturateDot189.xxx, 1.0 );
					float3 MultiplyNoise92 = desaturateVar189;
					float4 break206 = ( i.color * float4( ( desaturateVar166 + AdditiveNoise91 ) , 0.0 ) * ( texCoord71.z + 1.0 ) * float4( MultiplyNoise92 , 0.0 ) );
					float2 uv_AlphaOverride = i.texcoord.xy * _AlphaOverride_ST.xy + _AlphaOverride_ST.zw;
					float temp_output_4_0_g118 = _FlipbooksColumsRows.x;
					float temp_output_5_0_g118 = _FlipbooksColumsRows.y;
					float2 appendResult7_g118 = (float2(temp_output_4_0_g118 , temp_output_5_0_g118));
					float totalFrames39_g118 = ( temp_output_4_0_g118 * temp_output_5_0_g118 );
					float2 appendResult8_g118 = (float2(totalFrames39_g118 , temp_output_5_0_g118));
					float clampResult42_g118 = clamp( texCoord75.x , 0.0001 , ( totalFrames39_g118 - 1.0 ) );
					float temp_output_35_0_g118 = frac( ( ( (float)0 + clampResult42_g118 ) / totalFrames39_g118 ) );
					float2 appendResult29_g118 = (float2(temp_output_35_0_g118 , ( 1.0 - temp_output_35_0_g118 )));
					float2 temp_output_15_0_g118 = ( ( ( LocalUVOffset184 + uv_AlphaOverride ) / appendResult7_g118 ) + ( floor( ( appendResult8_g118 * appendResult29_g118 ) ) / appendResult7_g118 ) );
					float2 panner44 = ( 1.0 * _Time.y * _AlphaOverridePanning + temp_output_15_0_g118);
					float4 break2_g121 = ( tex2D( _AlphaOverride, panner44 ) * _AlphaOverrideChannel );
					float AlphaOverride49 = saturate( ( break2_g121.x + break2_g121.y + break2_g121.z + break2_g121.w ) );
					float2 panner33 = ( 1.0 * _Time.y * _MainAlphaPanning + temp_output_73_0);
					float4 break2_g120 = ( tex2D( _MainTex, panner33 ) * _MainAlphaChannel );
					float MainAlpha30 = saturate( ( break2_g120.x + break2_g120.y + break2_g120.z + break2_g120.w ) );
					float temp_output_55_0 = ( AlphaOverride49 * MainAlpha30 );
					float4 texCoord60 = i.texcoord;
					texCoord60.xy = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float temp_output_3_0_g122 = ( texCoord60.w - ( 1.0 - temp_output_55_0 ) );
					float temp_output_40_0 = ( i.color.a * temp_output_55_0 * saturate( saturate( ( temp_output_3_0_g122 / fwidth( temp_output_3_0_g122 ) ) ) ) );
					float4 screenPos = i.ase_texcoord4;
					float4 ase_screenPosNorm = screenPos / screenPos.w;
					ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
					float screenDepth199 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
					float distanceDepth199 = abs( ( screenDepth199 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _SoftFadeFactor ) );
					#ifdef _USESOFTALPHA_ON
					float staticSwitch198 = ( temp_output_40_0 * saturate( distanceDepth199 ) );
					#else
					float staticSwitch198 = temp_output_40_0;
					#endif
					float4 appendResult207 = (float4(break206.r , break206.g , break206.b , staticSwitch198));
					

					fixed4 col = appendResult207;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18912
0;0;2560;1389;483.9343;1420.66;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;103;-853.4689,-2259.997;Inherit;False;1884.647;1001.187;Extra Noise Setup;22;91;191;155;156;87;157;85;90;190;158;79;83;80;84;81;92;189;86;105;162;108;106;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode;83;-814.2831,-2017.479;Inherit;True;Property;_DetailNoise;Detail Noise;10;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;81;-564.8421,-1844.278;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;84;-560.261,-1728.191;Inherit;False;Property;_DetailNoisePanning;Detail Noise Panning;11;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;80;-326.8439,-1769.278;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;173;1088,-1728;Inherit;False;869.2021;446.9999;UV Offset Controlled by custom vertex stream;6;184;179;180;176;182;168;;0.3699214,0.2971698,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;168;1120,-1664;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;79;-140.0461,-2013.979;Inherit;True;Property;_TextureSample3;Texture Sample 3;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;85;-116.0612,-1805.789;Inherit;False;Property;_DetailDistortionChannel;Detail Distortion Channel;12;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;180;1136,-1424;Inherit;False;Constant;_InitialOffset;Initial Offset;16;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;176;1328,-1600;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;158;163.2251,-2003.815;Inherit;False;Channel Picker;-1;;75;dc5f4cb24a8bdf448b40a1ec5866280e;0;2;5;FLOAT4;1,0,0,0;False;7;FLOAT4;0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;182;1408,-1424;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;179;1600,-1456;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DesaturateOpNode;190;190.6096,-1917.282;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;184;1760,-1456;Inherit;False;LocalUVOffset;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;90;172.7251,-1829.615;Inherit;False;DistortionNoise;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;197;1095.931,-2387.688;Inherit;False;853.4072;636.7309;Set UV Modifiers For Main Tex;8;93;96;186;7;174;95;94;194;;1,0.8279877,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;7;1152.569,-1914.957;Inherit;False;0;27;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;186;1185.564,-2043.374;Inherit;False;184;LocalUVOffset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;93;1149.931,-2337.688;Inherit;False;90;DistortionNoise;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;95;1145.931,-2175.688;Inherit;False;Property;_DistortionIntensity;Distortion Intensity;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;50;-2911.51,-2551.852;Inherit;False;1894.068;530.1917;Alpha Override;12;49;165;48;44;45;177;193;51;188;187;43;47;;0,0.5461459,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;174;1411.421,-2066.139;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;1371.933,-2262.688;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;47;-2880.768,-2493.259;Inherit;True;Property;_AlphaOverride;Alpha Override;6;0;Create;True;0;0;0;False;0;False;None;06a7f39644cb3f1409288cd16fdb9720;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleAddOpNode;96;1548.933,-2129.688;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;187;-2592.767,-2413.259;Inherit;False;184;LocalUVOffset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;43;-2619.169,-2337.565;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;34;-2548.24,-1516.688;Inherit;False;1576.333;998.0396;Main Texture Set Vars;16;195;25;163;10;6;22;23;30;164;28;12;33;27;32;73;78;;0,0.5461459,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;194;1715.339,-2074.068;Inherit;False;UVFlipbookInput;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector2Node;74;-2538.623,-1820.153;Inherit;False;Property;_FlipbooksColumsRows;Flipbooks Colums & Rows;9;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;195;-2524.121,-1448.583;Inherit;False;194;UVFlipbookInput;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;75;-2512.07,-1982.654;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;188;-2400.768,-2413.259;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.IntNode;193;-2276.262,-2251.418;Inherit;False;Constant;_Int1;Int 1;9;0;Create;True;0;0;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.IntNode;78;-2286.642,-1281.145;Inherit;False;Constant;_Int0;Int 0;9;0;Create;True;0;0;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.FunctionNode;177;-2274.88,-2413.233;Inherit;False;Flipbook;-1;;118;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;3;False;5;FLOAT;3;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
Node;AmplifyShaderEditor.Vector2Node;51;-2289.648,-2177.604;Inherit;False;Property;_AlphaOverridePanning;Alpha Override Panning;7;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;73;-2289.329,-1441.295;Inherit;False;Flipbook;-1;;119;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;3;False;5;FLOAT;3;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
Node;AmplifyShaderEditor.Vector2Node;32;-1965.995,-1302.504;Inherit;False;Property;_MainAlphaPanning;Main Alpha Panning;5;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;44;-2000.767,-2381.259;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;33;-1965.995,-1414.504;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;27;-1997.995,-1110.504;Inherit;True;Property;_MainTex;Main Texture;0;0;Create;False;0;0;0;False;0;False;None;bc91fcfe50eb54b4c82c0365082043cc;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.Vector4Node;48;-1750.768,-2290.259;Inherit;False;Property;_AlphaOverrideChannel;Alpha Override Channel;8;0;Create;True;0;0;0;False;0;False;1,0,0,0;0,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;28;-1741.995,-1430.504;Inherit;True;Property;_TextureSample1;Texture Sample 1;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;12;-1717.069,-1206.742;Inherit;False;Property;_MainAlphaChannel;Main Alpha Channel;3;0;Create;True;0;0;0;False;0;False;0,0,0,1;0,0,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;45;-1808.768,-2477.259;Inherit;True;Property;_TextureSample2;Texture Sample 2;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;87;151.4388,-2185.69;Inherit;False;Property;_DetailAdditiveChannel;Detail Additive Channel;15;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;23;-1981.995,-726.5037;Inherit;False;Property;_MainTexturePanning;Main Texture Panning;4;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;164;-1421.995,-1430.504;Inherit;False;Channel Picker Alpha;-1;;120;e49841402b321534583d1dc019041b68;0;2;5;FLOAT4;1,0,0,0;False;7;FLOAT4;0,0,0,1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;165;-1504.768,-2477.259;Inherit;False;Channel Picker Alpha;-1;;121;e49841402b321534583d1dc019041b68;0;2;5;FLOAT4;1,0,0,0;False;7;FLOAT4;0,0,0,1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;30;-1197.995,-1430.504;Inherit;True;MainAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;22;-1965.995,-838.5037;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;49;-1296.768,-2477.259;Inherit;True;AlphaOverride;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;157;426.8263,-2036.38;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;52;-208.8856,-525.6209;Inherit;False;49;AlphaOverride;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;53;-186.5113,-450.0185;Inherit;False;30;MainAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;86;-193.6613,-1574.091;Inherit;False;Property;_DetailMultiplyChannel;Detail Multiply Channel;14;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;-1757.995,-966.5038;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;10;-1749.062,-746.3322;Inherit;False;Property;_MainTextureChannel;Main Texture Channel;2;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;156;549.593,-2040.846;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;108;253.3054,-1362.1;Inherit;False;Constant;_Float0;Float 0;15;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;105;55.67661,-1545.463;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;163;-1405.995,-774.5037;Inherit;False;Channel Picker;-1;;143;dc5f4cb24a8bdf448b40a1ec5866280e;0;2;5;FLOAT4;1,0,0,0;False;7;FLOAT4;0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FunctionNode;162;183.4254,-1498.115;Inherit;False;Channel Picker;-1;;142;dc5f4cb24a8bdf448b40a1ec5866280e;0;2;5;FLOAT4;1,0,0,0;False;7;FLOAT4;0,0,0,1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;155;676.3929,-2043.947;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;5.488959,-546.0185;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;60;127.5556,-355.1935;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ConditionalIfNode;106;414.8053,-1547.1;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;64;188.3074,-430.0071;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode;191;804.6099,-2039.383;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;25;-1197.995,-774.5037;Inherit;True;MainTexInfo;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;167;14.5006,-924.7495;Inherit;False;Property;_Desaturate;Desaturate?;1;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;91;801.5255,-1951.116;Inherit;False;AdditiveNoise;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;36;68.63216,-1082.473;Inherit;False;25;MainTexInfo;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;202;153.5714,-84.502;Inherit;False;Property;_SoftFadeFactor;SoftFadeFactor;17;0;Create;True;0;0;0;False;0;False;0;0;0.1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode;189;572.5096,-1546.582;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;70;365.1349,-362.5852;Inherit;False;Step Antialiasing;-1;;122;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;199;436.5714,-155.502;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;61;708.7333,-566.5916;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;68;545.5906,-363.4737;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;92;729.9253,-1547.715;Inherit;False;MultiplyNoise;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;101;269.4243,-985.1658;Inherit;False;91;AdditiveNoise;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DesaturateOpNode;166;73.56007,-1012.246;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;71;521.1351,-885.5583;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;37;572.5109,-1181.602;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;98;768.8834,-899.3236;Inherit;False;92;MultiplyNoise;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;102;612.7458,-1010.09;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;136;565.3867,-724.3369;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;203;692.5714,-160.502;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;730.5135,-407.2835;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;794.9197,-1040.442;Inherit;False;4;4;0;COLOR;1,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;201;914.9233,-360.6032;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;206;1031.066,-948.66;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.StaticSwitch;198;978.9233,-552.6033;Inherit;False;Property;_UseSoftAlpha;UseSoftAlpha;16;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;207;1288.066,-767.66;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;205;1346.249,-1045.851;Float;False;True;-1;2;ASEMaterialInspector;0;7;Nokdef/Alpha Uber Shader;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;False;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;2;False;-1;True;3;False;-1;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;81;2;83;0
WireConnection;80;0;81;0
WireConnection;80;2;84;0
WireConnection;79;0;83;0
WireConnection;79;1;80;0
WireConnection;176;0;168;3
WireConnection;158;5;79;0
WireConnection;158;7;85;0
WireConnection;182;0;180;0
WireConnection;179;0;176;0
WireConnection;179;1;182;0
WireConnection;190;0;158;0
WireConnection;184;0;179;0
WireConnection;90;0;190;0
WireConnection;174;0;186;0
WireConnection;174;1;7;0
WireConnection;94;0;93;0
WireConnection;94;1;95;0
WireConnection;96;0;94;0
WireConnection;96;1;174;0
WireConnection;43;2;47;0
WireConnection;194;0;96;0
WireConnection;188;0;187;0
WireConnection;188;1;43;0
WireConnection;177;13;188;0
WireConnection;177;4;74;1
WireConnection;177;5;74;2
WireConnection;177;24;75;1
WireConnection;177;2;193;0
WireConnection;73;13;195;0
WireConnection;73;4;74;1
WireConnection;73;5;74;2
WireConnection;73;24;75;1
WireConnection;73;2;78;0
WireConnection;44;0;177;0
WireConnection;44;2;51;0
WireConnection;33;0;73;0
WireConnection;33;2;32;0
WireConnection;28;0;27;0
WireConnection;28;1;33;0
WireConnection;45;0;47;0
WireConnection;45;1;44;0
WireConnection;164;5;28;0
WireConnection;164;7;12;0
WireConnection;165;5;45;0
WireConnection;165;7;48;0
WireConnection;30;0;164;0
WireConnection;22;0;73;0
WireConnection;22;2;23;0
WireConnection;49;0;165;0
WireConnection;157;0;87;0
WireConnection;157;1;79;0
WireConnection;6;0;27;0
WireConnection;6;1;22;0
WireConnection;156;0;157;0
WireConnection;105;0;86;1
WireConnection;105;1;86;2
WireConnection;105;2;86;3
WireConnection;105;3;86;4
WireConnection;163;5;6;0
WireConnection;163;7;10;0
WireConnection;162;5;79;0
WireConnection;162;7;86;0
WireConnection;155;0;156;0
WireConnection;155;1;156;1
WireConnection;155;2;156;2
WireConnection;155;3;156;3
WireConnection;55;0;52;0
WireConnection;55;1;53;0
WireConnection;106;0;105;0
WireConnection;106;2;162;0
WireConnection;106;3;108;0
WireConnection;106;4;108;0
WireConnection;64;0;55;0
WireConnection;191;0;155;0
WireConnection;25;0;163;0
WireConnection;91;0;191;0
WireConnection;189;0;106;0
WireConnection;70;1;64;0
WireConnection;70;2;60;4
WireConnection;199;0;202;0
WireConnection;68;0;70;0
WireConnection;92;0;189;0
WireConnection;166;0;36;0
WireConnection;166;1;167;0
WireConnection;102;0;166;0
WireConnection;102;1;101;0
WireConnection;136;0;71;3
WireConnection;203;0;199;0
WireConnection;40;0;61;4
WireConnection;40;1;55;0
WireConnection;40;2;68;0
WireConnection;39;0;37;0
WireConnection;39;1;102;0
WireConnection;39;2;136;0
WireConnection;39;3;98;0
WireConnection;201;0;40;0
WireConnection;201;1;203;0
WireConnection;206;0;39;0
WireConnection;198;1;40;0
WireConnection;198;0;201;0
WireConnection;207;0;206;0
WireConnection;207;1;206;1
WireConnection;207;2;206;2
WireConnection;207;3;198;0
WireConnection;205;0;207;0
ASEEND*/
//CHKSM=E1BEBF4D9E46D8B2BFB1FB477D987F83D551EA5A