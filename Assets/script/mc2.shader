Shader "Custom/mc2"
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
			#pragma geometry geom
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


			StructuredBuffer<int3> src_tri_indices;
			StructuredBuffer<float4> src_base_vertices;
			StructuredBuffer<float4> src_grid_positions;
			StructuredBuffer<float3> src_tri_normals;
			StructuredBuffer<int> src_next_gridids;

			Texture1D<uint> grid_cubeids;

			static const int _32e0 = 1;
			static const int _32e1 = 32;
			static const int _32e2 = 32 * 32;
			static const int _32e3 = 32 * 32 * 32;


			appdata vert(appdata v, uint i : SV_InstanceID)
			{
				v.z = i;
				return v;
			}

			[maxvertexcount(12)]
			void geom(point appdata input[1], inout TriangleStream<v2f> outStream)
			{
				static const int3 cube_span = int3(_32e0, _32e2, _32e1);
				static const int3 cubeid_span_slant = cube_span.xyz - cube_span.yzx;

				int icube = dot(cubepos, cube_span);
				int3 inear = icube.xxx - cubeid_span;	// left, up, forward
				int3 inear_slant = icube.xxx - cubeid_span_slant;	// leftup, upforward, forwardleft
				int3 inext = icube.xxx + cubeid_span;
				int3 inext_slant = icube.xxx + cubeid_span_slant;


				int3 outer = inear >> 15;
				int3 outer_slant = inear_slant >> 15;
				int3 inner = inear & 0x7fff;
				int3 inner_slant = inear_slant & 0x7fff;

				int igrid_current = gridid;
				int3 igrid_near = int3(0,0,0,0);//lerp(src_prenear_gridids[gridid * 2 + 0], igrid_current, -outer) * _32e3;
				int3 igrid_near_slant = int3(0, 0, 0, 0);// lerp(src_prenear_gridids[gridid * 2 + 1], igrid_current, -outer) * _32e3;

				uint cubeid_current		= cubeid * 12;
				uint cubeid_left		= grid_cubeids[igrid_naar.x + inear.x] * 12;
				uint cubeid_up			= grid_cubeids[igrid_naar.y + inear.y] * 12;
				uint cubeid_forward		= grid_cubeids[igrid_naar.z + inear.z] * 12;
				uint cubeid_leftup		= grid_cubeids[igrid_naar_slant.x + inear_slant.x] * 12;
				uint cubeid_upforward	= grid_cubeids[igrid_naar_slant.y + inear_slant.y] * 12;
				uint cubeid_forwardleft	= grid_cubeids[igrid_naar_slant.z + inear_slant.z] * 12;
				uint cubeid_right		= grid_cubeids[igrid_naar.x + inext.x] * 12;
				uint cubeid_down		= grid_cubeids[igrid_naar.y + inext.y] * 12;
				uint cubeid_back		= grid_cubeids[igrid_naar.z + inext.z] * 12;
				uint cubeid_rightdown	= grid_cubeids[igrid_naar_slant.x + inext_slant.x] * 12;
				uint cubeid_downback	= grid_cubeids[igrid_naar_slant.y + inext_slant.y] * 12;
				uint cubeid_backright	= grid_cubeids[igrid_naar_slant.z + inext_slant.z] * 12;

				//float3 nm0 = cube_normals[cubeid_current + 0];
				//nm0 += cube_normals[cubeid_forward + 3];
				//nm0 += cube_normals[cubeid_up + 8];
				//nm0 += cube_normals[cubeid_upforward + 11];

				//float3 nm1 = cube_normals[cubeid_current + 1];
				//nm1 += cube_normals[cubeid_left + 2];
				//nm1 += cube_normals[cubeid_up + 9];
				//nm1 += cube_normals[cubeid_leftup + 10];

				//float3 nm4 = cube_normals[cubeid_current + 4];
				//nm4 += cube_normals[cubeid_left + 5];
				//nm4 += cube_normals[cubeid_forward + 6];
				//nm4 += cube_normals[cubeid_forwardleft + 7];


				float3 nm0 = cube_normals[cubeid_current + 0];
				nm0 += cube_normals[cubeid_forward + 3];
				nm0 += cube_normals[cubeid_up + 8];
				nm0 += cube_normals[cubeid_upforward + 11];

				float3 nm1 = cube_normals[cubeid_current + 1];
				nm1 += cube_normals[cubeid_left + 2];
				nm1 += cube_normals[cubeid_up + 9];
				nm1 += cube_normals[cubeid_leftup + 10];

				float3 nm2 = cube_normals[cubeid_current + 2];
				nm2 += cube_normals[cubeid_right + 1];
				nm2 += cube_normals[cubeid_down + 10];
				nm2 += cube_normals[cubeid_rightdown + 9];


				float3 nm3 = cube_normals[cubeid_current + 3];
				nm3 += cube_normals[cubeid_back + 0];
				nm3 += cube_normals[cubeid_up + 11];
				nm3 += cube_normals[cubeid_upback + 8];//

				float3 nm4 = cube_normals[cubeid_current + 4];
				nm4 += cube_normals[cubeid_left + 5];
				nm4 += cube_normals[cubeid_forward + 6];
				nm4 += cube_normals[cubeid_forwardleft + 7];

				float3 nm5 = cube_normals[cubeid_current + 5];
				nm5 += cube_normals[cubeid_right + 4];
				nm5 += cube_normals[cubeid_back + 7];
				nm5 += cube_normals[cubeid_backright + 6];


				float3 nm6 = cube_normals[cubeid_current + 6];
				nm6 += cube_normals[cubeid_ + ];
				nm6 += cube_normals[cubeid_ + ];
				nm6 += cube_normals[cubeid_ + ];

				float3 nm7 = cube_normals[cubeid_current + 7];
				nm7 += cube_normals[cubeid_ + ];
				nm7 += cube_normals[cubeid_ + ];
				nm7 += cube_normals[cubeid_ + ];

				float3 nm8 = cube_normals[cubeid_current + 8];
				nm8 += cube_normals[cubeid_ + ];
				nm8 += cube_normals[cubeid_ + ];
				nm8 += cube_normals[cubeid_ + ];


				float3 nm9 = cube_normals[cubeid_current + 9];
				nm9 += cube_normals[cubeid_ + ];
				nm9 += cube_normals[cubeid_ + ];
				nm9 += cube_normals[cubeid_ + ];

				float3 nm10 = cube_normals[cubeid_current + 10];
				nm10 += cube_normals[cubeid_ + ];
				nm10 += cube_normals[cubeid_ + ];
				nm10 += cube_normals[cubeid_ + ];

				float3 nm11 = cube_normals[cubeid_current + 11];
				nm11 += cube_normals[cubeid_ + ];
				nm11 += cube_normals[cubeid_ + ];
				nm11 += cube_normals[cubeid_ + ];



			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = i.color;// tex2D(_MainTex, i.uv) * i.color;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

		ENDCG
	}
	}
}
