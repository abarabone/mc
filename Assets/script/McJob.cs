using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

namespace mc
{ 

    using CubeGrid32x32x32Native = CubeGrid32x32x32;
    

    public struct McJob : IJob
    {
        public void Execute()
        {

        }
    }


    public struct CubeLine
    {
        public uint4 cube;
    }



    static public class Mc
    {
        
        /// <summary>
        /// native contener 化必要、とりあえずは配列で動作チェック
        /// あとでＹＺカリングもしたい
        /// </summary>
        // xyz各32個目のキューブは1bitのために隣のグリッドを見なくてはならず、効率悪いしコードも汚くなる、なんとかならんか？
        static public bool SampleAllCubes(
            ref this (
                CubeGrid32x32x32Native current,
                CubeGrid32x32x32Native current_right,
                CubeGrid32x32x32Native back,
                CubeGrid32x32x32Native back_right,
                CubeGrid32x32x32Native under,
                CubeGrid32x32x32Native under_right,
                CubeGrid32x32x32Native backUnder,
                CubeGrid32x32x32Native backUnder_right
            ) g,
            int gridId,
            NativeList<uint> outputCubes
        )
        {
            var preCubeCount = outputCubes.Length;

            for( var iy = 0; iy < 31; iy++ )
            {
                for( var iz = 0; iz < 31; iz+=4 )
                {
                    var (y0z0, y0z1, y1z0, y1z1) =
                        getXLine_( iy, iz, ref g.current, ref g.current, ref g.current, ref g.current );
                    var cubes = bitwiseCubesXLine_( y0z0, y0z1, y1z0, y1z1 );

                    var (y0z0r, y0z1r, y1z0r, y1z1r) =
                        getXLine_( iy, iz, ref g.current_right, ref g.current_right, ref g.current_right, ref g.current_right );
                    cubes.__f870f87 |= bitwiseHalfCubeXLine_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromXLine_( cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = 31;

                    var (y0z0, y0z1, y1z0, y1z1) =
                        getXLine_( iy, iz, ref g.current, ref g.back, ref g.current, ref g.back );
                    var cubes = bitwiseCubesXLine_( y0z0, y0z1, y1z0, y1z1 );
                    
                    var (y0z0r, y0z1r, y1z0r, y1z1r) =
                        getXLine_( iy, iz, ref g.current_right, ref g.back_right, ref g.current_right, ref g.back_right );
                    cubes.__f870f87 |= bitwiseHalfCubeXLine_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromXLine_( cubes, gridId, iy, iz, outputCubes );
                }
            }
            {
                const int iy = 31;
                for( var iz = 0; iz < 31; iz+=4 )
                {
                    var (y0z0, y0z1, y1z0, y1z1) =
                        getXLine_( iy, iz, ref g.current, ref g.current, ref g.under, ref g.under );
                    var cubes = bitwiseCubesXLine_( y0z0, y0z1, y1z0, y1z1 );
                    
                    var (y0z0r, y0z1r, y1z0r, y1z1r) =
                        getXLine_( iy, iz, ref g.current_right, ref g.current_right, ref g.under_right, ref g.under_right );
                    cubes.__f870f87 |= bitwiseHalfCubeXLine_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromXLine_( cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = 31;

                    var (y0z0, y0z1, y1z0, y1z1) =
                        getXLine_( iy, iz, ref g.current, ref g.back, ref g.under, ref g.backUnder );
                    var cubes = bitwiseCubesXLine_( y0z0, y0z1, y1z0, y1z1 );

                    var (y0z0r, y0z1r, y1z0r, y1z1r) =
                        getXLine_( iy, iz, ref g.current_right, ref g.back_right, ref g.under_right, ref g.backUnder_right );
                    cubes.__f870f87 |= bitwiseHalfCubeXLine_( y0z0r, y0z1r, y1z0r, y1z1r );

                    addCubeFromXLine_( cubes, gridId, iy, iz, outputCubes );
                }
            }

            return preCubeCount != outputCubes.Length;
        }



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static (uint4, uint4, uint4, uint4) getXLine_(
            int iy, int iz,
            ref CubeGrid32x32x32Native current, ref CubeGrid32x32x32Native back,
            ref CubeGrid32x32x32Native under, ref CubeGrid32x32x32Native backUnder
        )
        {
            var iy_ = iy;
            var iz_ = new int4( iz >> 2, iz >> 2, iz, iz );
            var yofs = new int4( 0, 1, 0, 1 );
            var zofs = new int4( 0, 0, 1, 1 );
            var ymask = 0x1f;
            var zmask = new int4( 0x7, 0x7, 0xff, 0xff );

            var i = (iy_ + yofs & ymask ) * 32 + (iz_ + zofs & zmask);
            var y0 = current.units.Reinterpret<uint, uint4>()[i.x];//[ ( iy + 0 & 0x1f ) * 32 + ( iz>>2 + 0 & 0x7 ) ];
            var y1 = under  .units.Reinterpret<uint, uint4>()[i.y];//[ ( iy + 1 & 0x1f ) * 32 + ( iz>>2 + 0 & 0x7 ) ];
            var y0z0 = y0;
            var y1z0 = y1;

            y0.x = back     .units[i.z];//[ ( iy + 0 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];
            y1.x = backUnder.units[i.w];//[ ( iy + 1 & 0x1f ) * 32 + ( iz + 1 & 0x1f ) ];
            var y0z1 = y0.yzwx;
            var y1z1 = y1.yzwx;

            return (y0z0, y0z1, y1z0, y1z1);
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static void addCubeFromXLine_(
            (uint4 _98109810, uint4 _a921a921, uint4 _ba32ba32, uint4 _cb43cb43,
             uint4 _dc54dc54, uint4 _ed65ed65, uint4 _fe76fe76, uint4 __f870f87) cubes,
            int gridId_, int iy, int iz, NativeList<uint> outputCubes_
        )
        {
            var i = 0;
            var ix = 0;
            var iz_ = new int4( iz + 0, iz + 1, iz + 2, iz + 3 );
            for( var ipack = 0; ipack < 32 >> 3; ipack++ )// 8 は 1cube の 8bit
            {
                addCubeIfVisible_( cubes._98109810 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._a921a921 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._ba32ba32 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._cb43cb43 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._dc54dc54 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._ed65ed65 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes._fe76fe76 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                addCubeIfVisible_( cubes.__f870f87 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                i += 8;
            }
            return;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static void addCubeIfVisible_
            ( uint4 cube, int gridId__, int4 ix_, int4 iy_, int4 iz_, NativeList<uint> cubeInstances )
        {
            if( cube.x != 0 || cube.x != 255 )
            {
                var cubeInstance = CubeUtiilty.ToCubeInstance( ix_.x, iy_.x, iz_.x, gridId__, cube.x );
                cubeInstances.Add( cubeInstance );
            }

            if( cube.y != 0 || cube.y != 255 )
            {
                var cubeInstance = CubeUtiilty.ToCubeInstance( ix_.y, iy_.y, iz_.y, gridId__, cube.y );
                cubeInstances.Add( cubeInstance );
            }

            if( cube.z != 0 || cube.z != 255 )
            {
                var cubeInstance = CubeUtiilty.ToCubeInstance( ix_.z, iy_.z, iz_.z, gridId__, cube.z );
                cubeInstances.Add( cubeInstance );
            }

            if( cube.w != 0 || cube.w != 255 )
            {
                var cubeInstance = CubeUtiilty.ToCubeInstance( ix_.w, iy_.w, iz_.w, gridId__, cube.w );
                cubeInstances.Add( cubeInstance );
            }
        }


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        
        // あらかじめ共通段階までビット操作しておいたほうが速くなるかも、でも余計なエリアにストアするから、逆効果の可能性もある
        static
        ( uint4 _98109810, uint4 _a921a921, uint4 _ba32ba32, uint4 _cb43cb43,
          uint4 _dc54dc54, uint4 _ed65ed65, uint4 _fe76fe76, uint4 __f870f87 )
        bitwiseCubesXLine_( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1 )
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

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static uint4 bitwiseHalfCubeXLine_( uint4 y0z0r, uint4 y0z1r, uint4 y1z0r, uint4 y1z1r )
        {
            return ( y0z0r & 1 ) << 25 | ( y0z1r & 1 ) << 27 | ( y1z0r & 1 ) << 29 | ( y1z1r & 1 ) << 31;
        }


    }

}