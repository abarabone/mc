using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace MarchingCubes
{
    static public class CubeUtiilty
    {


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public bool4 IsEmptyOrFull( this uint4 ui4 ) => math.any( ui4 + 1 & 0xfe );

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public uint4 _0or255to0( this uint4 ui4 ) => ui4 + 1 & 0xfe;



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public int AsByte( this bool b ) => new BoolAsByte() { bl = b }.by;
        [StructLayout(LayoutKind.Explicit)]
        public struct BoolAsByte
        {
            [FieldOffset( 0 )] public bool bl;
            [FieldOffset( 0 )] public byte by;
        }


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public uint ToCubeInstance( int ix, int iy, int iz, int gridId, uint cubeId ) =>
            //(uint)iz << 24 | (uint)iy << 16 | (uint)ix << 8 | cubeId;
            //(uint)iz << 26 & 0x1fu << 26 | (uint)iy << 21 & 0x1fu << 21 | (uint)ix << 16 & 0x1fu << 16 | (uint)gridId << 8 & 0xffu << 8 | cubeId & 0xff;
            (uint)iz << 26 | (uint)iy << 21 | (uint)ix << 16 | (uint)gridId << 8 | cubeId;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public (float3 center, uint gridId, uint cubeId) FromCubeInstance( uint cubeInstance ) =>
            //(new float3( cubeInstance >> 8 & 0xff, -( cubeInstance >> 16 & 0xff ), -( cubeInstance >> 24 & 0xff ) ), cubeInstance & 0xff);
            (new float3( cubeInstance >> 16 & 0x1f, -( cubeInstance >> 21 & 0x1f ), -( cubeInstance >> 26 & 0x1f ) ), cubeInstance >> 8 & 0xff, cubeInstance & 0xff);


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public uint4 ToCubeInstance( int4 ix, int4 iy, int4 iz, int gridId, uint4 cubeId ) =>
            (uint4)iz << 26 | (uint4)iy << 21 | (uint4)ix << 16 | (uint)gridId << 8 | cubeId;



        static public (int[] tris, float3[] vtxs) MakeCollisionMeshData
            ( uint[] cubeInstances, int[][] srcIdxLists, float3[] srcVtxList )
        {
            var dstIdxs = new List<int>();
            var dstVtxs = new List<float3>();

            var vtxOffset = 0;
            for( var i = 0; i < cubeInstances.Length; i++ )
            {
                vtxOffset = addCube_( cubeInstances[ i ], vtxOffset );
            }

            return (dstIdxs.ToArray(), dstVtxs.ToArray());


            int addCube_( uint cubeInstance, int vtxOffset_ )
            {
                //var cubeId = cubeInstance & 0xff;
                //if( cubeId == 0 || cubeId == 255 ) return vtxOffset_;

                //var center = new float3( cubeInstance >> 8 & 0xff, -( cubeInstance >> 16 & 0xff ), -( cubeInstance >> 24 & 0xff ) );

                var (center, gridId, cubeId) = CubeUtiilty.FromCubeInstance( cubeInstance );
                if( cubeId == 0 || cubeId == 255 ) return vtxOffset_;

                var srcIdxList = srcIdxLists[ cubeId - 1 ];

                for( var i = 0; i < srcIdxList.Length; i++ )
                {
                    var srcIdx = srcIdxList[ i ];
                    dstIdxs.Add( vtxOffset_ + srcIdx );
                }
                for( var i = 0; i < srcVtxList.Length; i++ )
                {
                    dstVtxs.Add( srcVtxList[ i ] + center );
                }

                return vtxOffset_ + srcVtxList.Length;
            }
        }


    }
}