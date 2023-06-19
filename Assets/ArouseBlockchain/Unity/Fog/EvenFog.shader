Shader "VFog/EvenFog"
{
    Properties
    {
       // [HDR]
       _Color1("Main Color1", Color) = (1, 1, 1, 1)
      // [HDR]
       _Color2("Main Color2", Color) = (1, 1, 1, 1)
       _Bias("Bias", float) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent"  "IgnoreProjector" = "True" "RenderType" = "Transparent"  }
        LOD 100
        ZWrite Off
         Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 viewDir: TEXCOORD2;
                float4 scrPos:TEXCOORD3;
                float4 scrPosRaw:TEXCOORD4;
                float3 worldNormal : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _Color1;
            float4 _Color2;
            float _Bias;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.scrPos = ComputeScreenPos(o.vertex);
                o.scrPosRaw = o.vertex;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


                float3 view = i.viewDir / i.scrPosRaw.w;
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)));

                float3 viewDepth = _WorldSpaceCameraPos.xyz - (view* depth) ;

                float3 worldToObjPos = mul(unity_WorldToObject, float4(viewDepth, 1.0)).xyz;
                float3 worldNormals = normalize(i.worldNormal);

                float dotProduct = dot(worldToObjPos, worldNormals);
                float negateDotProduct = saturate( - 1 * dotProduct);

                float4 collerp = _Color1;
                //_Bias = i.worldNormal.y;
               // if(i.scrPos.y > worldToObjPos.y){
                 //  collerp = lerp(_Color1,_Color2,  i.viewDir.y * _Bias);
               // }

                float3 color = float3(collerp.r, collerp.g, collerp.b);
                float4 result = float4(color, negateDotProduct* _Color1.a);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }


}
