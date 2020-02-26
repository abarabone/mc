using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace mc
{
    static public class CubeUtiilty
    {

        static public CubeGrid32x32x32Native DefaultBlankCube { get; } = new CubeGrid32x32x32Native( isFillAll: false );
        static public CubeGrid32x32x32Native DefaultFilledCube { get; } = new CubeGrid32x32x32Native( isFillAll: true );


        //static public uint GetCube(  )
        //{
        //    var i = new int3( 1, 1, 1 );

        //    var ii = ( i.x & 0b_100 ) << 2 | ( i.x & 0b_10) << 1;
        //}


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public uint ToCubeInstance(  int ix, int iy, int iz, int gridId, uint cubeId ) =>
            //(uint)iz << 24 | (uint)iy << 16 | (uint)ix << 8 | cubeId;
            //(uint)iz << 26 & 0x1fu << 26 | (uint)iy << 21 & 0x1fu << 21 | (uint)ix << 16 & 0x1fu << 16 | (uint)gridId << 8 & 0xffu << 8 | cubeId & 0xff;
            (uint)iz << 26 | (uint)iy << 21 | (uint)ix << 16 | (uint)gridId << 8 | cubeId;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public (float3 center, uint gridId, uint cubeId) FromCubeInstance( uint cubeInstance ) =>
            //(new float3( cubeInstance >> 8 & 0xff, -( cubeInstance >> 16 & 0xff ), -( cubeInstance >> 24 & 0xff ) ), cubeInstance & 0xff);
            (new float3( cubeInstance >> 16 & 0x1f, -(cubeInstance >> 21 & 0x1f ), -(cubeInstance >> 26 & 0x1f ) ), cubeInstance >> 8 & 0xff, cubeInstance & 0xff);


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


        /// <summary>
        /// native contener 化必要、とりあえずは配列で動作チェック
        /// あとでＹＺカリングもしたい
        /// </summary>
        // xyz各32個目のキューブは1bitのために隣のグリッドを見なくてはならず、効率悪いしコードも汚くなる、なんとかならんか？
        static public bool SampleAllCubes_(
            ref this (
                CubeGrid32x32x32 current___,
                CubeGrid32x32x32 current___right,
                CubeGrid32x32x32 back______,
                CubeGrid32x32x32 back______right,
                CubeGrid32x32x32 under_____,
                CubeGrid32x32x32 under_____right,
                CubeGrid32x32x32 backUnder_,
                CubeGrid32x32x32 backUnder_right
            ) g,
            int gridId,
            NativeList<uint> outputCubes
        )
        {
            var preCubeCount = outputCubes.Length;
            
            for( var iy = 0; iy < 31; iy++ )
            {
                for( var iz = 0; iz < 31; iz++ )
                {
                    var y0z0 = g.current___.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = g.current___.units[ ( iy + 0 ) * 32 + iz + 1 ];
                    var y1z0 = g.current___.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1 = g.current___.units[ ( iy + 1 ) * 32 + iz + 1 ];

                    var cubes = bitwiseCubesLineX_( y0z0, y0z1, y1z0, y1z1 );

                    var y0z0r = g.current___right.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1r = g.current___right.units[ ( iy + 0 ) * 32 + iz + 1 ];
                    var y1z0r = g.current___right.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1r = g.current___right.units[ ( iy + 1 ) * 32 + iz + 1 ];

                    cubes.__f870f87 |= bitwiseHalfCubeLineX_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromLineX_( cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = 31;
                    var y0z0 = g.current___.units[ ( iy + 0 ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y0z1 = g.back______.units[ ( iy + 0 ) * 32 + ( iz + 1 & 0x1f ) ];
                    var y1z0 = g.current___.units[ ( iy + 1 ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y1z1 = g.back______.units[ ( iy + 1 ) * 32 + ( iz + 1 & 0x1f ) ];

                    var cubes = bitwiseCubesLineX_( y0z0, y0z1, y1z0, y1z1 );

                    var y0z0r = g.current___right.units[ ( iy + 0 ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y0z1r = g.back______right.units[ ( iy + 0 ) * 32 + ( iz + 1 & 0x1f ) ];
                    var y1z0r = g.current___right.units[ ( iy + 1 ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y1z1r = g.back______right.units[ ( iy + 1 ) * 32 + ( iz + 1 & 0x1f ) ];

                    cubes.__f870f87 |= bitwiseHalfCubeLineX_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromLineX_( cubes, gridId, iy, iz, outputCubes );
                }
            }
            {
                const int iy = 31;
                for( var iz = 0; iz < 31; iz++ )
                {
                    var y0z0 = g.current___.units[ ( iy + 0 & 0x1f ) * 32 + iz + 0 ];
                    var y0z1 = g.current___.units[ ( iy + 0 & 0x1f ) * 32 + iz + 1 ];
                    var y1z0 = g.under_____.units[ ( iy + 1 & 0x1f ) * 32 + iz + 0 ];
                    var y1z1 = g.under_____.units[ ( iy + 1 & 0x1f ) * 32 + iz + 1 ];

                    var cubes = bitwiseCubesLineX_( y0z0, y0z1, y1z0, y1z1 );

                    var y0z0r = g.current___right.units[ ( iy + 0 & 0x1f ) * 32 + iz + 0 ];
                    var y0z1r = g.current___right.units[ ( iy + 0 & 0x1f ) * 32 + iz + 1 ];
                    var y1z0r = g.under_____right.units[ ( iy + 1 & 0x1f ) * 32 + iz + 0 ];
                    var y1z1r = g.under_____right.units[ ( iy + 1 & 0x1f ) * 32 + iz + 1 ];

                    cubes.__f870f87 |= bitwiseHalfCubeLineX_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromLineX_( cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = 31;
                    var y0z0 = g.current___.units[ ( iy + 0 & 0x1f ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y0z1 = g.back______.units[ ( iy + 0 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];
                    var y1z0 = g.under_____.units[ ( iy + 1 & 0x1f ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y1z1 = g.backUnder_.units[ ( iy + 1 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];

                    var cubes = bitwiseCubesLineX_( y0z0, y0z1, y1z0, y1z1 );

                    var y0z0r = g.current___right.units[ ( iy + 0 & 0x1f ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y0z1r = g.back______right.units[ ( iy + 0 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];
                    var y1z0r = g.under_____right.units[ ( iy + 1 & 0x1f ) * 32 + ( iz + 0 & 0x1f ) ];
                    var y1z1r = g.backUnder_right.units[ ( iy + 1 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];

                    cubes.__f870f87 |= bitwiseHalfCubeLineX_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromLineX_( cubes, gridId, iy, iz, outputCubes );
                }
            }

            return preCubeCount != outputCubes.Length;


            void addCubeFromLineX_(
                (uint _98109810, uint _a921a921, uint _ba32ba32, uint _cb43cb43,
                 uint _dc54dc54, uint _ed65ed65, uint _fe76fe76, uint __f870f87) cubes,
                int gridId_, int iy, int iz, NativeList<uint> outputCubes_
            )
            {
                var i = 0;
                var ix = 0;
                for( var ipack = 0; ipack < 32 >> 3; ipack++ )// 8 は 1cube の 8bit
                {
                    addCubeIfVisible_( cubes._98109810 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._a921a921 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._ba32ba32 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._cb43cb43 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._dc54dc54 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._ed65ed65 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes._fe76fe76 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    addCubeIfVisible_( cubes.__f870f87 >> i & 0xff, gridId_, ix++, iy, iz, outputCubes_ );
                    i += 8;
                }
                return;

                void addCubeIfVisible_
                    ( uint cube, int gridId__, int ix_, int iy_, int iz_, NativeList<uint> cubeInstances )
                {
                    if( cube == 0 || cube == 255 ) return;

                    //var cubeInstance = (uint)iz_ << 24 | (uint)iy_ << 16 | (uint)ix_ << 8 | cube;
                    var cubeInstance = CubeUtiilty.ToCubeInstance(ix_, iy_, iz_, gridId__, cube);
                    cubeInstances.Add( cubeInstance );
                }
            }


            // あらかじめ共通段階までビット操作しておいたほうが速くなるかも、でも余計なエリアにストアするから、逆効果の可能性もある
            (uint _98109810, uint _a921a921, uint _ba32ba32, uint _cb43cb43,
             uint _dc54dc54, uint _ed65ed65, uint _fe76fe76, uint __f870f87)
            bitwiseCubesLineX_( uint y0z0, uint y0z1, uint y1z0, uint y1z1 )
            {
                // fedcba9876543210fedcba9876543210

                var m1100 = 0b_11001100_11001100_11001100_11001100u;
                var m0011 = m1100 >> 2;
                // --dc--98--54--10--dc--98--54--10
                // dc--98--54--10--dc--98--54--10--
                // fe--ba--76--32--fe--ba--76--32--
                // --fe--ba--76--32--fe--ba--76--32
                var y0_dc985410 = y0z0 & m0011 | ( y0z1 & m0011 ) << 2;
                var y0_feba7632 = ( y0z0 & m1100 ) >> 2 | y0z1 & m1100;
                var y1_dc985410 = y1z0 & m0011 | ( y1z1 & m0011 ) << 2;
                var y1_feba7632 = ( y1z0 & m1100 ) >> 2 | y1z1 & m1100;
                // dcdc989854541010dcdc989854541010
                // fefebaba76763232fefebaba76763232
                // dcdc989854541010dcdc989854541010
                // fefebaba76763232fefebaba76763232

                var mf0 = 0x_f0f0_f0f0u;
                var m0f = 0x_0f0f_0f0fu;
                // ----9898----1010----9898----1010
                // dcdc----5454----dcdc----5454----
                // ----baba----3232----baba----3232
                // fefe----7676----fefe----7676----
                var _98109810 = y0_dc985410 & m0f | ( y1_dc985410 & m0f ) << 4;
                var _dc54dc54 = ( y0_dc985410 & mf0 ) >> 4 | y1_dc985410 & mf0;
                var _ba32ba32 = y0_feba7632 & m0f | ( y1_feba7632 & m0f ) << 4;
                var _fe76fe76 = ( y0_feba7632 & mf0 ) >> 4 | y1_feba7632 & mf0;
                // 98989898101010109898989810101010
                // dcdcdcdc54545454dcdcdcdc54545454
                // babababa32323232babababa32323232
                // fefefefe76767676fefefefe76767676

                var m55 = 0x_5555_5555u;
                var maa = 0x_aaaa_aaaau;
                var _a921a921 = ( _ba32ba32 & m55 ) << 1 | ( _98109810 & maa ) >> 1;
                var _cb43cb43 = ( _dc54dc54 & m55 ) << 1 | ( _ba32ba32 & maa ) >> 1;
                var _ed65ed65 = ( _fe76fe76 & m55 ) << 1 | ( _dc54dc54 & maa ) >> 1;
                var __f870f87 = ( _98109810 >> 8 & 0x_55_5555u ) << 1 | ( _fe76fe76 & maa ) >> 1;
                // a9a9a9a921212121a9a9a9a921212121
                // cbcbcbcb43434343cbcbcbcb43434343
                // edededed65656565edededed65656565
                // -f-f-f-f878787870f0f0f0f87878787

                return (_98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, __f870f87);
            }

            //[MethodImpl( MethodImplOptions.AggressiveInlining )]
            uint bitwiseHalfCubeLineX_( uint y0z0r, uint y0z1r, uint y1z0r, uint y1z1r )
            {
                return ( y0z0r & 1 ) << 25 | ( y0z1r & 1 ) << 27 | ( y1z0r & 1 ) << 29 | ( y1z1r & 1 ) << 31;
            }
        }
        

    }

}