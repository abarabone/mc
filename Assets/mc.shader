Shader "Custom/mc"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			//#pragma multi_compile_fog
			#include "UnityCG.cginc"
			//#include "AutoLight.cginc"
			

            struct appdata
            {
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			StructuredBuffer<int4> Instances;
			StructuredBuffer<int> IdxList;
			StructuredBuffer<float4> BaseVtxList;


			//int get_mcb(int instanceId, float3 index )
			//{
			//	const int4 mask[] =
			//	{
			//		int4(1,0,0,0),
			//		int4(0,1,0,0),
			//		int4(0,0,1,0),
			//		int4(0,0,0,1),
			//	};
			//	int4 a = mcb[instanceId] * mask[index.x];
			//	int b = a.x + a.y + a.z + a.w;
			//	int c = (b >> (int)index.y) & 0xf;
			//	return c;
			//}

            v2f vert (appdata v, uint i : SV_InstanceID)
            {
                v2f o;

				int cubeId = Instances[i];
				int vtxIndex = IdxList[cubeId * 12];

				float4 lvtx = BaseVtxList[vtxIndex];

				lvtx.x += i;

				//v.vertex.x = get_mcb(0, v.vertex.xyz);
                o.vertex = UnityObjectToClipPos(lv);
                o.uv = TRANSFORM_TEX(o.vertex.xy, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
