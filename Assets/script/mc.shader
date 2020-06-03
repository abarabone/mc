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
			
			
		#pragma enable_d3d11_debug_symbols


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
			//Texture2DArray<uint> grid_cubeids;
			StructuredBuffer<uint> grid_cubeids;


			float4 normals[155];

			//StructuredBuffer<uint4> cube_patterns;
			float4 cube_patterns[254][2];
			// [0] : vertex posision index for tringle { x: tri0(i0>>0 | i1>>8 | i2>>16)  y: tri1  z: tri2  w: tri3 }
			// [1] : vertex normal index for vertex { x: (i0>>0 | i1>>8 | i2>>16 | i3>>24)  y: i4|5|6|7  z:i8|9|10|11 }

			static const uint itri_to_ivtx = 0;
			static const uint ivtx_to_inml = 1;


			float4 cube_vtxs[12];
			// x: near vertex index (ortho1>>0 | ortho2>>8 | slant>>16)
			// y: near vertex offset ortho1 (x>>0 | y>>8 | z>>16)
			// z: near vertex offset ortho2 (x>>0 | y>>8 | z>>16)
			// w: pos(x>>0 | y>>8 | z>>16)

			//static const uint near_ivtx = 0;
			//static const uint near_iofs = 1;
			//static const uint vtx_pos = 3;


			float4 grids[512][2];
			// [0] : position as float3
			// [1] : near grid id
			// { x : back>>0 | up>>16  y : left>>0 | current>>16  z : right>>0 | down>>16  w : forward>>0 }

			static const uint grid_pos = 0;
			static const uint grid_near_id = 1;


			static const uint4 element_mask_table[] =
			{
				{1,0,0,0}, {0,1,0,0}, {0,0,1,0}, {0,0,0,1}
			};
			
			uint unpack8bit_uint4_to_uint(uint4 packed_uint4, uint element_index, uint packed_index)
			{
				const uint iouter = element_index;
				const uint iinner = packed_index << 3;// * 8
				const uint element = dot(packed_uint4, element_mask_table[iouter]);
				return element >> iinner & 0xff;
			}
			uint unpack8bit_uint4_to_uint(uint4 packed_uint4, uint index)
			{
				const uint element_index = index >> 2;// / 4
				const uint packed_index = index & 0x3;
				return unpack8bit_uint4_to_uint(packed_uint4, element_index, packed_index);
			}

			uint unpack16bit_uint4_to_uint(uint4 packed_uint4, uint index)
			{
				const uint iouter = index >> 1;
				const uint iinner = (index & 1) << 4;
				const uint element = dot(packed_uint4, element_mask_table[iouter]);
				return element >> iinner & 0xffff;
			}

			uint3 unpack8bits_uint_to_uint3(uint packed3_uint)
			{
				//return packed3_uint.xxx >> uint3(0, 8, 16) & 0xff;
				return uint3(packed3_uint, packed3_uint, packed3_uint) >> uint3(0, 8, 16) & uint3(0xff,0xff,0xff);
			}
			uint3 unpack8bits_uint3_to_uint3(uint3 packed3_uint3, uint element_index)
			{
				const uint element = dot(packed3_uint3, element_mask_table[element_index].xyz);
				return unpack8bits_uint_to_uint3(element);
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



			int3 calc_outerpos(uint3 cubepos, uint ivtx_in_cube, uint ortho_selector)
			{
				const uint3 offset_packed = asuint(cube_vtxs[ivtx_in_cube].xyz);
				//const int3 offset = (int3)unpack8bits_uint3_to_uint3(offset_packed, ortho_selector) - 1;
				const int3 offset = (int3)unpack8bits_uint3_to_uint3(offset_packed, ortho_selector) - int3(1,1,1);
				const int3 outerpos = cubepos + offset;
				return outerpos;
			}
			uint3 calc_innerpos(int3 outerpos)
			{
				//return outerpos & 0x1f;
				return outerpos & int3(0x1f, 0x1f, 0x1f);
			}

			uint get_gridid_near(uint gridid_current, int3 outerpos)
			{
				//const int3 outer_offset = outerpos >> 5;
				const int3 outer_offset = outerpos >> int3(5,5,5);
				const uint grid_near_selector = dot(outer_offset, int3(1, 2, 3)) + 3;

				const uint4 near_gridid_packed = asuint(grids[gridid_current][grid_near_id]);
				const uint near_gridid = unpack16bit_uint4_to_uint(near_gridid_packed, grid_near_selector);

				return near_gridid;
			}
			uint get_cubeid_near(uint gridid, int3 outerpos)
			{
				const uint3 innerpos = calc_innerpos(outerpos);
				//const uint3 index = uint3(innerpos.z * 32 + innerpos.x, innerpos.y, gridid);
				//return grid_cubeids[index];
				
				const int igrid = gridid * grid_span;
				const int icube = dot(innerpos, inner_span);
				return grid_cubeids[igrid + icube];
			}

			float3 get_vtx_normal(uint cubeid, uint ivtx_in_cube)
			{
				const uint4 inml_packed = asuint(cube_patterns[cubeid][ivtx_to_inml]);
				const uint inml = unpack8bit_uint4_to_uint(inml_packed, ivtx_in_cube);
				return normals[inml];
			}
			float3 get_vtx_normal_ortho
				(uint gridid_current, uint3 cubepos_current, uint ivtx_ortho, uint ortho_selector, out uint gridid_ortho, out int3 outerpos_ortho)
			{
				const int3 outerpos = calc_outerpos(cubepos_current, ivtx_ortho, ortho_selector);
				const uint gridid = get_gridid_near(gridid_current, outerpos);
				//const uint cubeid = get_cubeid_near(gridid, outerpos);
				const uint cubeid = get_cubeid_near(gridid_current, outerpos);
				const float3 normal = get_vtx_normal(cubeid, ivtx_ortho);

				gridid_ortho = gridid;
				outerpos_ortho = outerpos;
				return normal;
			}
			float3 get_vtx_normal_slant(uint gridid, int3 outerpos, uint ivtx_in_cube)
			{

			}

			float3 get_and_caluclate_triangle_to_vertex_normal
				(uint gridid_current, uint cubeid_current, uint ivtx_in_cube, uint3 cubepos_current)
			{
				const uint ivtx_near_packed = asuint(cube_vtxs[ivtx_in_cube].x);
				const uint3 ivtx_near = unpack8bits_uint_to_uint3(ivtx_near_packed);

				uint gridid_ortho1, gridid_ortho2;
				int3 outerpos_ortho1, outerpos_ortho2;
				const float3 nm0 = get_vtx_normal(cubeid_current, ivtx_in_cube);
				const float3 nm1 = get_vtx_normal_ortho(gridid_current, cubepos_current, ivtx_near.x, 1, gridid_ortho1, outerpos_ortho1);
				const float3 nm2 = get_vtx_normal_ortho(gridid_current, cubepos_current, ivtx_near.y, 2, gridid_ortho2, outerpos_ortho2);
				//const float3 nm3 = get_vtx_normal_slant(gridid_ortho1, outerpos_ortho2, ivtx_near.z, 2);
				//const float3 nm3 = get_vtx_normal_near(gridid_ortho, cubepos_current, ivtx_near.z, 2, gridid_ortho);

				return normalize(nm1);// nm0 + nm1 + nm2 + nm3);
			}

			static const float3 vvvv[] = { {0,0,0}, {1,0,0}, {0,1,0} };
			v2f vert(appdata v, uint i : SV_InstanceID)
			{
				v2f o;

				const uint data = cube_instances[i];
				const uint cubeid = (data & 0xff) - 1;
				//const uint2 idxofs = cubeId * uint2(12,4) + v.vertex.xy;

				const uint gridid = data >> 8 & 0xff;
				const float3 gridpos = grids[gridid][grid_pos];

				const uint4 ivtx_packed = asuint(cube_patterns[cubeid][itri_to_ivtx]);
				const uint ivtx_in_cube = unpack8bit_uint4_to_uint(ivtx_packed, v.vertex.y, v.vertex.x);

				const int3 cube_location = int3(data >> 16 & 0x1f, data >> 21 & 0x1f, data >> 26 & 0x1f);
				const int3 cube_location_ltb = cube_location * int3(1, -1, -1);
				const uint cube_vtx_lpos_packed = asuint(cube_vtxs[ivtx_in_cube].w);
				const float3 cube_vtx_lpos = ((int3)unpack8bits_uint_to_uint3(cube_vtx_lpos_packed) - 1) * 0.5f;

				const float4 lvtx = float4(gridpos + cube_location_ltb + cube_vtx_lpos, 1.0f);
				//const float4 lvtx = float4(gridpos + cube_location_ltb + vvvv[v.vertex.x], 1.0f);

				o.vertex = mul(UNITY_MATRIX_VP, lvtx);//UnityObjectToClipPos(lvtx);


				//const half3 normal = Normals[idxofs.y].xyz;
				//const half3 normal = get_vtx_normal_current(cubeId, vtxIdx);
				const half3 normal = get_and_caluclate_triangle_to_vertex_normal(gridid, cubeid, ivtx_in_cube, cube_location.xyz);
				const half3 worldNormal = normal;
				const fixed nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.color = _LightColor0 * nl;
				//// この処理をしないと陰影が強くつきすぎる
				//// https://docs.unity3d.com/ja/current/Manual/SL-VertexFragmentShaderExamples.html
				//// の「アンビエントを使った拡散ライティング」を参考
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
