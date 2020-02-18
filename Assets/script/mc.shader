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
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
//#pragma exclude_renderers d3d11 gles

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
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
				fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

			StructuredBuffer<uint> Instances;
			StructuredBuffer<int> IdxList;
			StructuredBuffer<float4> BaseVtxList;
			StructuredBuffer<float4> GridPositions;


            v2f vert (appdata v, uint i : SV_InstanceID)
            {
                v2f o;

				uint data = Instances[i];

				int cubeId = (data & 0xff) - 1;
				int vtxIndex = IdxList[cubeId * 12 + v.vertex.x];

				int gridId = data >> 8 & 0xff;
				float4 gridpos = GridPositions[gridId];

				int4 unitpos = int4(data >> 16 & 0x1f, data >> 21 & 0x1f, data >> 26 & 0x1f, 0 );
				int4 center = unitpos * int4(1, -1, -1, 1);
				float4 lvtx = gridpos + center + BaseVtxList[vtxIndex];

				o.vertex = mul(UNITY_MATRIX_VP, lvtx);//UnityObjectToClipPos(lvtx);
				o.uv = lvtx.xy; TRANSFORM_TEX(lvtx.xy, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
				
				o.color = fixed4(unitpos.y,1,1,1);
				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);// *i.color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
