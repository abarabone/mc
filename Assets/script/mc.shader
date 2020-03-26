﻿Shader "Custom/mc"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
		Tags {
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
		}
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
            #include "UnityLightingCommon.cginc" // _LightColor0 に対し


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
				half3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            StructuredBuffer<uint> Instances;
            StructuredBuffer<int> IdxList;
            StructuredBuffer<float4> BaseVtxList;
            StructuredBuffer<float4> GridPositions;
			StructuredBuffer<float3> Normals;

			StructuredBuffer<int> src_next_gridids;

			static const int _32e0 = 1;
			static const int _32e1 = 32;
			static const int _32e2 = 32 * 32;
			static const int _32e3 = 32 * 32 * 32;
			static const int xspan = _32e0;
			static const int yspan = _32e2;
			static const int zspan = _32e1;
			static const int gridspan = _32e3;

			int3 vtx_offsets[] =
			{
					{0,0,0},
				{0,0,0}, {1,0,0},
					{0,0,1},
				{0,0,0}, {1,0,0},
				{0,0,1}, {1,0,1},
					{0,1,0},
				{0,1,0}, {1,1,0},
					{0,1,1},
			};
			half3 cube_normals[] =
			{
				{1,1,1}
			};

			int3 near_cube_spans[] =
			{
				{-zspan, -yspan, -zspan-yspan},
				{-xspan, -yspan, -xspan-yspan},
				{+xspan, -yspan, +xspan-yspan},
				{+zspan, -yspan, +zspan-yspan},

				{-xspan, -zspan, -xspan-zspan},
				{+xspan, -zspan, +xspan-zspan},
				{-xspan, +zspan, -xspan+zspan},
				{+xspan, +zspan, +xspan+zspan},

				{-zspan, +yspan, -zspan+yspan},
				{-xspan, +yspan, -xspan+yspan},
				{+xspan, +yspan, +xspan+yspan},
				{+zspan, +yspan, +xspan+yspan},
			};
			int3 near_cube_inms[] =
			{
				3,8,11,
				2,9,10,
				1,10,9,
				0,11,8,

				5,6,7,
				4,7,6,
				7,4,5,
				6,5,4,

				11,0,3,
				10,1,2,
				9,2,1,
				8,3,0,
			};
			float3 get_and_caluclate_triangle_to_vertex_normal(int cubeid, int inm_current)
			{
				int3 span = near_cube_spans[inm_current];
				int3 inm = near_cube_inms[inm_current];

				float3 nm = cube_normals[cubeid * 12 + inm_current];
				nm += cube_normals[grid_cubeids[span.x] * 12 + inm.x];
				nm += cube_normals[grid_cubeids[span.y] * 12 + inm.y];
				nm += cube_normals[grid_cubeids[span.z] * 12 + inm.z];

				return normalize(nm);
			}

            v2f vert(appdata v, uint i : SV_InstanceID)
            {
                v2f o;

                uint data = Instances[i];
                uint cubeId = (data & 0xff) - 1;
				uint2 idxofs = cubeId * uint2(12,4) + v.vertex.xy;

				uint vtxIdx = IdxList[idxofs.x];

                uint gridId = data >> 8 & 0xff;
                float4 gridpos = GridPositions[gridId];

                int4 cubepos = int4(data >> 16 & 0x1f, data >> 21 & 0x1f, data >> 26 & 0x1f, 0);
                int4 center = cubepos * int4(1, -1, -1, 1);
                float4 lvtx = gridpos + center + BaseVtxList[vtxIdx];

                o.vertex = mul(UNITY_MATRIX_VP, lvtx);//UnityObjectToClipPos(lvtx);

				
				//half3 normal = Normals[idxofs.y].xyz;
				half3 normal = get_and_caluclate_triangle_to_vertex_normal(cubeId, idxofs.x);
				fixed nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.color = _LightColor0 * nl;
				// この処理をしないと陰影が強くつきすぎる
				// https://docs.unity3d.com/ja/current/Manual/SL-VertexFragmentShaderExamples.html
				// の「アンビエントを使った拡散ライティング」を参考
				o.color.rgb += ShadeSH9(half4(worldNormal, 1));


				o.uv = half2(0,0);//lvtx.xy; TRANSFORM_TEX(lvtx.xyspan, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
				fixed4 col = i.color;// tex2D(_MainTexspan, i.uv) * i.color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

        ENDCG
    }
    }
}
