Shader "VFog/VerticalFog"
{
    Properties
    {
       [HDR]
       _Color1("Main Color1", Color) = (1, 1, 1, 1)
       [HDR]
       _Color2("Main Color2", Color) = (1, 1, 1, 1)
       _Bias("Bias", float) = 0.5
       _Intensity("Intensity", float) = 1
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  }

        Pass
        {
           Blend SrcAlpha OneMinusSrcAlpha
           ZWrite Off
           CGPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           #pragma multi_compile_fog
           #include "UnityCG.cginc"

           struct appdata
           {
               float4 vertex : POSITION;
               UNITY_VERTEX_INPUT_INSTANCE_ID
           };

           struct v2f
           {
               float4 scrPos : TEXCOORD0;
               UNITY_FOG_COORDS(1)
               float4 vertex : SV_POSITION;
               UNITY_VERTEX_OUTPUT_STEREO
           };



           UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
           float4 _Color1;
           float4 _Color2;
           float _Bias;
           float _Intensity;

           v2f vert(appdata v)
           {
               v2f o;

               UNITY_SETUP_INSTANCE_ID(v);
               UNITY_INITIALIZE_OUTPUT(v2f, o);
               UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

               
               o.vertex = UnityObjectToClipPos(v.vertex);
               o.scrPos = ComputeScreenPos(o.vertex);
               UNITY_TRANSFER_FOG(o,o.vertex);
               return o;
           }


            half4 frag(v2f i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

               float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)));
               float diff = saturate(_Intensity * (depth - i.scrPos.w));


               float4 collerp = _Color1;
               if(i.vertex.y > diff){
                   collerp = lerp(_Color1,_Color2,_Bias);
               }

              // float4 collerp = lerp(_Color1,_Color2,_Bias);
               fixed4 col = lerp(fixed4(collerp.rgb, 0.0), collerp, diff * diff * diff * (diff * (6 * diff - 15) + 10));

               UNITY_APPLY_FOG(i.fogCoord, col);
               return col;
            }

            ENDCG
        }
    }
}