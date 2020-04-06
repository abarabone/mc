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

            StructuredBuffer<uint> Instances;
            StructuredBuffer<int> IdxList;
            StructuredBuffer<float4> BaseVtxList;
            StructuredBuffer<float4> GridPositions;
			StructuredBuffer<float3> Normals;

			StructuredBuffer<int4> near_gridids_prev_and_next;
			// +0 -> prev gridid { x:left,  y:up,   z:front, w:current }
			// +1 -> next gridid { x:right, y:down, z:back,  w:current }

			StructuredBuffer<uint> grid_cubeids;
			StructuredBuffer<float3> cube_normals;

			static const int _32e0 = 1;
			static const int _32e1 = 32;
			static const int _32e2 = 32 * 32;
			static const int _32e3 = 32 * 32 * 32;
			static const int xspan = _32e0;
			static const int yspan = _32e2;
			static const int zspan = _32e1;
			static const int grid_span = _32e3;
			static const int3 inner_span = int3(xspan, yspan, zspan);

			static int3 near_cube_offsets[] =
			{
				{0, 0, -1}, {0, -1, 0},
				{-1, 0, 0}, {0, -1, 0},
				{+1, 0, 0}, {0, -1, 0},
				{0, 0, +1}, {0, -1, 0},

				{-1, 0, 0}, {0, 0, -1},
				{+1, 0, 0}, {0, 0, -1},
				{-1, 0, 0}, {0, 0, +1},
				{+1, 0, 0}, {0, 0, +1},

				{0, 0, -1}, {0, +1, 0},
				{-1, 0, 0}, {0, +1, 0},
				{+1, 0, 0}, {0, +1, 0},
				{0, 0, +1}, {0, +1, 0},
			};
			static int3 near_cube_ivtxs[] =
			{
				{3,8,11},
				{2,9,10},
				{1,10,9},
				{0,11,8},

				{5,6,7},
				{4,7,6},
				{7,4,5},
				{6,5,4},

				{11,0,3},
				{10,1,2},
				{9,2,1},
				{8,3,0},
			};

			struct OrthoTempData
			{
				int4 grid_mask;
				int3 offset;
				int gridid;
				int pvev_next_selector;
			};

			int get_cubeid(int gridid, int3 cubepos)
			{
				int igrid = gridid * grid_span;

				int3 innerpos = cubepos & 0x1f;
				int icube = dot(innerpos, inner_span);

				return grid_cubeids[igrid + icube] & 0xff;
			}
			float3 get_vtx_normal_current(int cubeid_current, int ivtx_current)
			{
				return cube_normals[cubeid_current * 12 + ivtx_current];
			}

			int get_gridid_ortho(int gridid_current, int3 cubepos, out int pvev_next_selector, out int4 grid_mask)
			{
				int3 outerpos = cubepos >> 5;

				pvev_next_selector = (outerpos.x + outerpos.y + outerpos.z) + 1 >> 1;//
				int4 near_grid = near_gridids_prev_and_next[gridid_current * 2 + pvev_next_selector];

				grid_mask = int4(abs(outerpos), 1 - any(outerpos));

				return dot(near_grid, grid_mask);
			}
			float3 get_vtx_normal_ortho
				(int index, int gridid_current, int3 cubepos, int ivtx_current, int ivtx, out OrthoTempData o)
			{
				o.offset = near_cube_offsets[ivtx_current * 2 + index];
				int3 pos = cubepos + o.offset;
				o.gridid = get_gridid_ortho(gridid_current, pos, o.pvev_next_selector, o.grid_mask);
				return cube_normals[get_cubeid(o.gridid, pos) * 12 + ivtx];
			}

			int get_gridid_slant(int gridid_current, int gridid0, int pvev_next_selector1, int4 grid_mask1)
			{
				int4 near_grid = near_gridids_prev_and_next[gridid0 * 2 + pvev_next_selector1];
				int4 near_grid01 = int4(near_grid.xyz, gridid_current);
				return dot(near_grid01, grid_mask1);
			}
			float3 get_vtx_normal_slant(int gridid_current, int3 cubepos, int ivtx, int3 offset0, int3 offset1, int gridid0, int pvev_next_selector1, int4 grid_mask1)
			{
				int3 offset = offset0 + offset1;
				int3 pos = cubepos + offset;
				int gridid = get_gridid_slant(gridid_current, gridid0, pvev_next_selector1, grid_mask1);
				return cube_normals[get_cubeid(gridid, pos) * 12 + ivtx];
			}

			float3 get_and_caluclate_triangle_to_vertex_normal(int gridid_current, int cubeid_current, int ivtx_current, int3 cubepos)
			{
				int3 ivtx = near_cube_ivtxs[ivtx_current];

				OrthoTempData o0, o1;
				float3 nm = get_vtx_normal_current(cubeid_current, ivtx_current);
				//nm += get_vtx_normal_ortho(0, gridid_current, cubepos, ivtx_current, ivtx.x, o0);
				//nm += get_vtx_normal_ortho(1, gridid_current, cubepos, ivtx_current, ivtx.y, o1);
				nm += get_vtx_normal_slant(gridid_current, cubepos, ivtx.z, o0.offset, o1.offset, o0.gridid, o1.pvev_next_selector, o1.grid_mask);

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
				half3 normal = get_and_caluclate_triangle_to_vertex_normal(gridId, cubeId, vtxIdx, cubepos.xyz);
				half3 worldNormal = normal;
				fixed nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.color = _LightColor0 * nl;
				// この処理をしないと陰影が強くつきすぎる
				// https://docs.unity3d.com/ja/current/Manual/SL-VertexFragmentShaderExamples.html
				// の「アンビエントを使った拡散ライティング」を参考
				o.color.rgb += ShadeSH9(half4(worldNormal, 1));

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
