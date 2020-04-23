Shader "Custom/mc"
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

			StructuredBuffer<uint> cube_instances;
			Texture2DArray<uint> grid_cubeids;


			float3 normals[155];

			uint4 cube_patterns[254][2];
			// [0] : vertex posision index { x: tri0(i0>>0 | i1>>8 | i2>>16)  y: tri1  z: tri2  w: tri3 }
			// [1] : vertex normal index { x: (i0>>0 | i1>>8 | i2>>16 | i3>>24)  y: i4|5|6|7  z:i8|9|10|11 }


			uint4 cube_vtxs[12];
			// x: near vertex index (x>>0 | y>>8 | z>>16)
			// y: near vertex index offset prev (left >>0 | up  >>8 | front>>16)
			// z: near vertex index offset next (right>>0 | down>>8 | back >>16)
			// w: pos(x>>0 | y>>8 | z>>16)


			uint3 grids[512][2];
			// [0] : position as float3
			// [1] : near grid id
			// { x: prev(left>>0 | up>>9 | front>>18)  y: next(right>>0 | down>>9 | back>>18)  z: current }

			
			static const int4 element_mask_table[] =
			{
				{1,0,0,0}, {0,1,0,0}, {0,0,1,0}, {0,0,0,1}
			};
			
			uint get_packed8bits(uint4 packed_uint4, int element_index, int packed_index)
			{
				const int iouter = element_index;
				const int iinner = packed_index << 3;// * 8
				const uint element = dot(packed_uint4, element_mask_table[iouter]);
				return element >> iinner & 0xf;
			}
			uint get_packed8bits(uint4 packed_uint4, int index)
			{
				const int iouter = index >> 2;// / 4
				const int iinner = index & 0x3;
				return get_packed8bits(packed_uint4, iouter, iinner);
			}

			uint get_packed9bits(uint4 packed_uint4, int element_index, int packed_index)
			{
				const int iouter = element_index;
				const int iinner = packed_index * 9;
				const uint element = dot(packed_uint4, element_mask_table[iouter]);
				return element >> iinner & 0x1f;
			}


			static const int _32e0 = 1;
			static const int _32e1 = 32;
			static const int _32e2 = 32 * 32;
			static const int _32e3 = 32 * 32 * 32;
			static const int xspan = _32e0;
			static const int yspan = _32e2;
			static const int zspan = _32e1;
			static const int grid_span = _32e3;
			static const int3 inner_span = int3(xspan, yspan, zspan);


			struct OrthoTempData
			{
				int4 grid_mask;
				int3 offset;
				int gridid;
				int pvev_next_selector;
			};

			float3 get_vtx_normal_current(int cubeid_current, int ivtx_current)
			{
				return cube_vtx_patterns[cubeid_current].vtx_nmls[ivtx_current];
			}

			uint get_cubeid(int gridid, int3 cubepos)
			{
				const int igrid = gridid * grid_span;

				const int3 innerpos = cubepos & 0x1f;
				const int icube = dot(innerpos, inner_span);

				return grid_cubeids[int3(innerpos.z * 32 + innerpos.x, innerpos.y, gridid)];
			}
			int get_gridid_ortho(int gridid_current, int3 cubepos, out int pvev_next_selector, out int4 grid_mask)
			{
				const int3 outerpos = cubepos >> 5;

				pvev_next_selector = (outerpos.x + outerpos.y + outerpos.z) + 1 >> 1;//
				const int4 near_grid = grids[gridid_current].near_gridids_prev_and_next[pvev_next_selector];

				grid_mask = int4(abs(outerpos), 1 - any(outerpos));

				return dot(near_grid, grid_mask);
			}
			float3 get_vtx_normal_ortho
				(int iortho, int gridid_current, int3 cubepos, int ivtx_current, int ivtx, out OrthoTempData o)
			{
				o.offset = cube_vtxs[ivtx_current].near_cube_ivtx_offsets_prev_and_next[iortho];
				const int3 pos = cubepos + o.offset;
				o.gridid = get_gridid_ortho(gridid_current, pos, o.pvev_next_selector, o.grid_mask);
				return cube_vtx_patterns[get_cubeid(o.gridid, pos)].vtx_nmls[ivtx];
			}

			int get_gridid_slant(int gridid_current, int gridid0, int pvev_next_selector1, int4 grid_mask1)
			{
				const int4 near_grid = grids[gridid0].near_gridids_prev_and_next[pvev_next_selector1];
				const int4 near_grid01 = int4(near_grid.xyz, gridid_current);
				return dot(near_grid01, grid_mask1);
			}
			float3 get_vtx_normal_slant
				(int gridid_current, int3 cubepos, int ivtx, int3 offset0, int3 offset1, int gridid0, int pvev_next_selector1, int4 grid_mask1)
			{
				const int3 offset = offset0 + offset1;
				const int3 pos = cubepos + offset;
				const int gridid = get_gridid_slant(gridid_current, gridid0, pvev_next_selector1, grid_mask1);
				return cube_vtx_patterns[get_cubeid(gridid, pos)].vtx_nmls[ivtx];
			}

			float3 get_and_caluclate_triangle_to_vertex_normal(int gridid_current, int cubeid_current, int ivtx_current, int3 cubepos)
			{
				const int3 ivtx = cube_vtxs[ivtx_current].near_cube_ivtx;

				OrthoTempData o0, o1;
				float3 nm = get_vtx_normal_current(cubeid_current, ivtx_current);
				nm += get_vtx_normal_ortho(0, gridid_current, cubepos, ivtx_current, ivtx.x, o0);
				nm += get_vtx_normal_ortho(1, gridid_current, cubepos, ivtx_current, ivtx.y, o1);
				nm += get_vtx_normal_slant(gridid_current, cubepos, ivtx.z, o0.offset, o1.offset, o0.gridid, o1.pvev_next_selector, o1.grid_mask);

				return normalize(nm);
			}

			v2f vert(appdata v, uint i : SV_InstanceID)
			{
				v2f o;

				const uint data = cube_instances[i];
				const uint cubeId = (data & 0xff) - 1;
				//const uint2 idxofs = cubeId * uint2(12,4) + v.vertex.xy;

				const int vtxIdx = cube_idx_patterns[cubeId].tri_ivtxs[v.vertex.x];

				const uint gridId = data >> 8 & 0xff;
				const float3 gridpos = grids[gridId].pos;

				const int3 cubepos = int4(data >> 16 & 0x1f, data >> 21 & 0x1f, data >> 26 & 0x1f, 0);
				const int3 center = cubepos * int3(1, -1, -1);
				const float4 lvtx = float4(gridpos + center + cube_vtxs[vtxIdx].base_vtx, 1.0f);

				o.vertex = mul(UNITY_MATRIX_VP, lvtx);//UnityObjectToClipPos(lvtx);


				//const half3 normal = Normals[idxofs.y].xyz;
				//const half3 normal = get_vtx_normal_current(cubeId, vtxIdx);
				const half3 normal = get_and_caluclate_triangle_to_vertex_normal(gridId, cubeId, vtxIdx, cubepos.xyz);
				const half3 worldNormal = normal;
				const fixed nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.color = _LightColor0 * nl;
				//// この処理をしないと陰影が強くつきすぎる
				//// https://docs.unity3d.com/ja/current/Manual/SL-VertexFragmentShaderExamples.html
				//// の「アンビエントを使った拡散ライティング」を参考
				//o.color.rgb += ShadeSH9(half4(worldNormal, 1));

				o.normal = worldNormal;

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
