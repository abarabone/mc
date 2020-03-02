using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Experimental;

namespace MarchingCubes
{
    public unsafe partial struct CubeGridArrayUnsafe
    {


        static public void SampleAllCubes<TCubeInstanceWriter>
            ( ref NearCubeGrids g, int gridId, TCubeInstanceWriter outputCubes )
            where TCubeInstanceWriter : ICubeInstanceWriter
        {
            SampleAllCubes( ref g, new int4( 1, 1, 1, 1 ), new int4( 1, 1, 1, 1 ), gridId, outputCubes );
        }



        // 標準 -------------------------------------------------------------

        /// <summary>
        /// native contener 化必要、とりあえずは配列で動作チェック
        /// あとでＹＺカリングもしたい
        /// </summary>
        // xyz各32個目のキューブは1bitのために隣のグリッドを見なくてはならず、効率悪いしコードも汚くなる、なんとかならんか？
        static public void SampleAllCubes<TCubeInstanceWriter>
            ( ref NearCubeGrids g, int4 grid0or1, int4 grid0or1_right, int gridId, TCubeInstanceWriter outputCubes )
            where TCubeInstanceWriter : ICubeInstanceWriter
        {
            var grid0ro1xz = math.any( new int4(grid0or1.xz, grid0or1_right.xz) ).AsByte();
            var grid0or1z = math.any( new int2( grid0or1.z, grid0or1_right.z ) ).AsByte();

            var g0or1x      = grid0or1.xxxx;
            var g0or1x_r    = grid0or1_right.xxxx;
            var g0or1xz     = grid0or1.xxzz;
            var g0or1xz_r   = grid0or1_right.xxzz;
            var g0or1xy     = grid0or1.xyxy;
            var g0or1xy_r   = grid0or1_right.xyxy;
            var g0or1       = grid0or1;
            var g0or1_r     = grid0or1_right;

            for( var iy = 0; iy < 31 * grid0ro1xz; iy++ )
            {
                for( var iz = 0; iz < ( 31 * grid0or1z & ~( 0x3 ) ); iz += 4 )
                {
                    var c = getXLine_( iy, iz, g0or1x, g.current, g.current, g.current, g.current );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g0or1x_r, g.current_right, g.current_right, g.current_right, g.current_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = ( 31 & ~( 0x3 ) );

                    var c = getXLine_( iy, iz, g0or1xz, g.current, g.current, g.back, g.back );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g0or1xz_r, g.current_right, g.current_right, g.back_right, g.back_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
            }
            {
                const int iy = 31;
                for( var iz = 0; iz < ( 31 * grid0or1z & ~( 0x3 ) ); iz += 4 )
                {
                    var c = getXLine_( iy, iz, g0or1xy, g.current, g.under, g.current, g.under );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g0or1xy_r, g.current_right, g.under_right, g.current_right, g.under_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = ( 31 & ~( 0x3 ) );

                    var c = getXLine_( iy, iz, g0or1, g.current, g.under, g.back, g.backUnder );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g0or1_r, g.current_right, g.under_right, g.back_right, g.backUnder_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
            }

        }



        public struct CubeXLineBitwise// タプルだと burst 利かないので
        {
            public uint4 _98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, _0f870f87;

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public CubeXLineBitwise
            (
                uint4 _98109810, uint4 _a921a921, uint4 _ba32ba32, uint4 _cb43cb43,
                uint4 _dc54dc54, uint4 _ed65ed65, uint4 _fe76fe76, uint4 _0f870f87
            )
            {
                this._98109810 = _98109810;
                this._a921a921 = _a921a921;
                this._ba32ba32 = _ba32ba32;
                this._cb43cb43 = _cb43cb43;
                this._dc54dc54 = _dc54dc54;
                this._ed65ed65 = _ed65ed65;
                this._fe76fe76 = _fe76fe76;
                this._0f870f87 = _0f870f87;
            }
        }

        public struct CubeNearXLines// タプルだと burst 利かないので
        {
            public uint4 y0z0, y0z1, y1z0, y1z1;

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public CubeNearXLines( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1 )
            {
                this.y0z0 = y0z0;
                this.y0z1 = y0z1;
                this.y1z0 = y1z0;
                this.y1z1 = y1z1;
            }
        }



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static unsafe CubeNearXLines getXLine_(
            int iy, int iz, int4 index0or1,
            CubeGrid32x32x32UnsafePtr current, CubeGrid32x32x32UnsafePtr under,
            CubeGrid32x32x32UnsafePtr back, CubeGrid32x32x32UnsafePtr backUnder
        )
        {
            //y0  -> ( iy + 0 & 31 ) * 32/4 + ( iz>>2 + 0 & 31>>2 );
            //y1  -> ( iy + 1 & 31 ) * 32/4 + ( iz>>2 + 0 & 31>>2 );
            //y0r -> ( iy + 0 & 31 ) * 32 + ( iz + 1<<2 & 31 );
            //y1r -> ( iy + 1 & 31 ) * 32 + ( iz + 1<<2 & 31 );
            var iy_ = iy;
            var iz_ = new int4( iz >> 2, iz >> 2, iz, iz );
            var yofs = new int4( 0, 1, 0, 1 );
            var zofs = new int4( 0, 0, 1 << 2, 1 << 2 );
            var ymask = 31;
            var zmask = new int4( 31 >> 2, 31 >> 2, 31, 31 );
            var yspan = new int4( 32 / 4, 32 / 4, 32, 32 );

            var _i = ( iy_ + yofs & ymask ) * yspan + ( iz_ + zofs & zmask );
            var i = _i * index0or1;
            var y0 = ( (uint4*)current.p->pUnits )[ i.x ];
            var y1 = ( (uint4*)under.p->pUnits )[ i.y ];
            var y0z0 = y0;
            var y1z0 = y1;

            y0.x = back.p->pUnits[ i.z ];
            y1.x = backUnder.p->pUnits[ i.w ];
            var y0z1 = y0.yzwx;
            var y1z1 = y1.yzwx;

            return new CubeNearXLines( y0z0, y0z1, y1z0, y1z1 );
        }



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static bool addCubeFromXLine_<TCubeInstanceWriter>(
            ref CubeXLineBitwise cubes,
            int gridId_, int iy, int iz, TCubeInstanceWriter outputCubes_
        )
            where TCubeInstanceWriter : ICubeInstanceWriter
        {
            var isInstanceAppended = false;

            var i = 0;
            var ix = 0;
            var iz_ = new int4( iz + 0, iz + 1, iz + 2, iz + 3 );
            for( var ipack = 0; ipack < 32 / 8; ipack++ )// 8 は 1cube の 8bit
            {
                isInstanceAppended |= addCubeIfVisible_( cubes._98109810 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._a921a921 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._ba32ba32 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._cb43cb43 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._dc54dc54 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._ed65ed65 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._fe76fe76 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._0f870f87 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                i += 8;
            }

            return isInstanceAppended;
        }
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static bool addCubeIfVisible_<TCubeInstanceWriter>
            ( uint4 cubeId, int gridId__, int4 ix_, int4 iy_, int4 iz_, TCubeInstanceWriter cubeInstances )
            where TCubeInstanceWriter : ICubeInstanceWriter
        {
            var _0or255to0 = cubeId + 1 & 0xfe;
            if( !math.any( _0or255to0 ) ) return false;// すべての cubeId が 0 か 255 なら何もしない

            var cubeInstance = CubeUtiilty.ToCubeInstance( ix_, iy_, iz_, gridId__, cubeId );

            if( _0or255to0.x != 0 ) cubeInstances.Add( cubeInstance.x );
            if( _0or255to0.y != 0 ) cubeInstances.Add( cubeInstance.y );
            if( _0or255to0.z != 0 ) cubeInstances.Add( cubeInstance.z );
            if( _0or255to0.w != 0 ) cubeInstances.Add( cubeInstance.w );

            return true;
        }



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        // あらかじめ共通段階までビット操作しておいたほうが速くなるかも、でも余計なエリアにストアするから、逆効果の可能性もある
        static CubeXLineBitwise bitwiseCubesXLine_( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1 )
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

            return new CubeXLineBitwise
                ( _98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, __f870f87 );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static uint4 bitwiseLastHalfCubeXLine_( uint4 y0z0r, uint4 y0z1r, uint4 y1z0r, uint4 y1z1r )
        {
            return ( y0z0r & 1 ) << 25 | ( y0z1r & 1 ) << 27 | ( y1z0r & 1 ) << 29 | ( y1z1r & 1 ) << 31;
        }


        // ------------------------------------------------------------------


            

    }
}
