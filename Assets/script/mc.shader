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


            v2f vert(appdata v, uint i : SV_InstanceID)
            {
                v2f o;

                uint data = Instances[i];
                uint cubeId = (data & 0xff) - 1;
				uint2 idxofs = cubeId * uint2(12,4) + v.vertex.xy;
                

				uint vtxIdx = IdxList[idxofs.x];

                uint gridId = data >> 8 & 0xff;
                float4 gridpos = GridPositions[gridId];

                int4 unitpos = int4(data >> 16 & 0x1f, data >> 21 & 0x1f, data >> 26 & 0x1f, 0);
                int4 center = unitpos * int4(1, -1, -1, 1);
                float4 lvtx = gridpos + center + BaseVtxList[vtxIdx];

                o.vertex = mul(UNITY_MATRIX_VP, lvtx);//UnityObjectToClipPos(lvtx);

				
				half3 normal = Normals[idxofs.y].xyz;
				half3 worldNormal = normal;// mul(UNITY_MATRIX_VP, normal);//UnityObjectToWorldNormal(normal);//
				fixed nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.color = _LightColor0 * nl;
				// この処理をしないと陰影が強くつきすぎる
				// https://docs.unity3d.com/ja/current/Manual/SL-VertexFragmentShaderExamples.html
				// の「アンビエントを使った拡散ライティング」を参考
				o.color.rgb += ShadeSH9(half4(worldNormal, 1));


				o.uv = half2(0,0);//lvtx.xy; TRANSFORM_TEX(lvtx.xy, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
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
