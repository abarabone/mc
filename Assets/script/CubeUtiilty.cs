using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;

namespace mc
{
    static public class CubeUtiilty
    {


        //static public uint GetCube(  )
        //{
        //    var i = new int3( 1, 1, 1 );

        //    var ii = ( i.x & 0b_100 ) << 2 | ( i.x & 0b_10) << 1;
        //}




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
                var cubeId = cubeInstance & 0xff;
                if( cubeId == 0 || cubeId == 255 ) return vtxOffset_;

                var center = new float3( cubeInstance >> 8 & 0xff, -( cubeInstance >> 16 & 0xff ), -( cubeInstance >> 24 & 0xff ) );

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

