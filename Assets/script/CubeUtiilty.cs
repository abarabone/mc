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




        static public uint[] SampleAllCubes( this GridArray ga, int igrid )
        {
            var outputCubes = new List<uint>( 32 * 32 );

            var current         = ga.grids[ igrid + 0 ];
            var current_right   = ga.grids[ igrid + 1 ];
            var back            = ga.grids[ igrid + ga.GridLength.x + 0 ];
            var back_right      = ga.grids[ igrid + ga.GridLength.x + 1 ];
            var under           = ga.grids[ igrid + ga.GridLength.x * ga.GridLength.y + 0 ];
            var under_right     = ga.grids[ igrid + ga.GridLength.x * ga.GridLength.y + 1 ];

            (uint y0z0, uint y0z1, uint y1z0, uint y1z1)
            loadLineX_( CubeGrid32x32x32 current, CubeGrid32x32x32 , int iy, int iz )
            {
                var y0z0 = ga_y0z0.units[ ( iy + 0 ) * 32 + iz + 0 ];
                var y0z1 = ga_y0z1.units[ ( iy + 0 ) * 32 + iz + 1 ];
                var y1z0 = ga_y1z0.units[ ( iy + 1 ) * 32 + iz + 0 ];
                var y1z1 = ga_y1z1.units[ ( iy + 1 ) * 32 + iz + 1 ];

                var y0z0r = ga_y0z0r.units[ ( iy + 0 ) * 32 + iz + 0 ];
                var y0z1r = ga_y0z1r.units[ ( iy + 0 ) * 32 + iz + 1 ];
                var y1z0r = ga_y1z0r.units[ ( iy + 1 ) * 32 + iz + 0 ];
                var y1z1r = ga_y1z1r.units[ ( iy + 1 ) * 32 + iz + 1 ];

                return (y0z0 | y0z0r & 1, y0z1 | y0z0r & 1, y1z0 | y0z0r & 1, y1z1 | y0z0r & 1);
            }

            void addCubeX_(
                (uint _98109810, uint _a921a921, uint _ba32ba32, uint _cb43cb43,
                 uint _dc54dc54, uint _ed65ed65, uint _fe76fe76, uint ___870f87) cubes,
                int iy, int iz
            )
            {
                var i = 0;
                var ix = 0;
                for( var ipack = 0; ipack < 32 >> 3; ipack++ )// 8 は 1cube の 8bit
                {
                    addCubeIfVisible_( cubes._98109810 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._a921a921 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._ba32ba32 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._cb43cb43 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._dc54dc54 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._ed65ed65 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes._fe76fe76 >> i, outputCubes, ix++, iy, iz );
                    addCubeIfVisible_( cubes.___870f87 >> i, outputCubes, ix++, iy, iz );
                    i += 8;
                }
                return;

                void addCubeIfVisible_( uint cube8bit, List<uint> output, int ix, int iy, int iz )
                {
                    var cube = cube8bit & 0xff;
                    if( cube == 0 ) return;

                    var posAndCube = (uint)iz << 24 | (uint)iy << 16 | (uint)ix << 8 | cube;
                    output.Add( posAndCube );
                }
            }


            for( var iy = 0; iy < 31; iy++ )
            {
                for( var iz = 0; iz < 31; iz++ )
                {
                    var y0z0 = this.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = this.units[ ( iy + 0 ) * 32 + iz + 1 ];
                    var y1z0 = this.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1 = this.units[ ( iy + 1 ) * 32 + iz + 1 ];

                    var cubes = makeCubesLineX_( y0z0, y0z1, y1z0, y1z1 );
                    addCubeX_( cubes, iy, iz );
                }
                {
                    var iz = 31;
                    var y0z0 = this.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = back.units[ ( iy + 0 ) * 32 + 0 + 1 ];
                    var y1z0 = this.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1 = back.units[ ( iy + 1 ) * 32 + 0 + 1 ];

                    var cubes = makeCubesLineX_( y0z0, y0z1, y1z0, y1z1 );
                    addCubeX_( cubes, iy, iz );
                }
            }
            {
                var iy = 31;
                for( var iz = 0; iz < 31; iz++ )
                {
                    var y0z0 = this.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = this.units[ ( iy + 0 ) * 32 + iz + 1 ];
                    var y1z0 = under.units[ ( 0 + 1 ) * 32 + iz + 0 ];
                    var y1z1 = under.units[ ( 0 + 1 ) * 32 + iz + 1 ];

                    var cubes = makeCubesLineX_( y0z0, y0z1, y1z0, y1z1 );
                    addCubeX_( cubes, iy, iz );
                }
            }

            return outputCubes.ToArray();



            // あらかじめ共通段階までビット操作しておいたほうが速くなるかも、でも余計なエリアにストアするから、逆効果の可能性もある
            (uint _98109810, uint _a921a921, uint _ba32ba32, uint _cb43cb43,
             uint _dc54dc54, uint _ed65ed65, uint _fe76fe76, uint ___870f87)
            makeCubesLineX_( uint y0z0, uint y0z1, uint y1z0, uint y1z1 )
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
                var ___870f87 = ( _98109810 >> 8 & 0x_55_5555u ) << 1 | ( _fe76fe76 & 0x_aa_aaaau ) >> 1;
                // a9a9a9a921212121a9a9a9a921212121
                // cbcbcbcb43434343cbcbcbcb43434343
                // edededed65656565edededed65656565
                // --------878787870f0f0f0f87878787

                return (_98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, ___870f87);
            }
        }


    }

}

