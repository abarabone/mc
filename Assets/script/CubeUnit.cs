using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace mc
{
    
    public class CubeGrid
    {
        public int3 Length;

        uint[] units;

        public CubeGrid( int x, int y, int z )
        {
            this.Length = new int3(x, y, z);
            this.units = new uint[ (x>>5) * z * y ];
        }

        public uint GetCube( int x, int y, int z )
        {
            var zofs = (uint)this.Length.x;
            var yofs = (uint)( this.Length.x * this.Length.z );

            var _x = (uint)( x + 0 );
            var _zzyy = new uint4( (uint)z, (uint)z, (uint)y, (uint)y );
            _zzyy += new uint4( 0, 1, 0, 1 );
            _zzyy *= new uint4( zofs, zofs, yofs, yofs );

            var _ix = new uint4( _x, _x, _x, _x );
            var _iz = new uint4( _zzyy.x, _zzyy.y, _zzyy.x, _zzyy.y );
            var _iy = new uint4( _zzyy.z, _zzyy.z, _zzyy.w, _zzyy.w );

            var i = _ix + _iz + _iy;
            var igrid = i >> 5;
            var ibit = i & 0x1f;

            var unit01 = ( this.units[ igrid.x ] >> (int)ibit.x ) & 0b11;
            var unit23 = ( this.units[ igrid.y ] >> (int)ibit.y ) & 0b11;
            var unit45 = ( this.units[ igrid.z ] >> (int)ibit.z ) & 0b11;
            var unit67 = ( this.units[ igrid.w ] >> (int)ibit.w ) & 0b11;

            return ( unit67 << 6 ) | ( unit45 << 4 ) | ( unit23 << 2 ) | ( unit01 << 0 );
        }
        public uint[] GetCubesRect()// Rect rc )
        {
            // ちゅうとはんぱ
            var iy_ = new int4( 0, 0, 1, 1 );
            var iz_ = new int4( 0, 1, 0, 1 );
            var l = loadLine_( iy_, iz_ );
            var iy2_ = new int4( 2, 2, 3, 3 );
            var iz2_ = new int4( 0, 1, 0, 1 );
            var l2 = loadLine_( iy2_, iz2_ );
            return makeCubesLineX_( l.c0, l.c1, l.c2, l.c3, iy_, iz_ )
                .Concat( makeCubesLineX_( l2.c0, l2.c1, l2.c2, l2.c3, iy2_, iz2_ ) )
                .Select( x => new[] { x.x, x.y, x.z, x.w } )
                .SelectMany( x => x )
                .ToArray();


            uint4x4 loadLine_( int4 iy, int4 iz )
            {
                uint4 y0z0, y0z1, y1z0, y1z1;

                y0z0.x = this.units[ ( iy.x + 0 ) * 32 + iz.x + 0 ];
                y0z1.x = this.units[ ( iy.x + 0 ) * 32 + iz.x + 1 ];
                y1z0.x = this.units[ ( iy.x + 1 ) * 32 + iz.x + 0 ];
                y1z1.x = this.units[ ( iy.x + 1 ) * 32 + iz.x + 1 ];

                y0z0.y = this.units[ ( iy.y + 0 ) * 32 + iz.y + 0 ];
                y0z1.y = this.units[ ( iy.y + 0 ) * 32 + iz.y + 1 ];
                y1z0.y = this.units[ ( iy.y + 1 ) * 32 + iz.y + 0 ];
                y1z1.y = this.units[ ( iy.y + 1 ) * 32 + iz.y + 1 ];

                y0z0.z = this.units[ ( iy.z + 0 ) * 32 + iz.z + 0 ];
                y0z1.z = this.units[ ( iy.z + 0 ) * 32 + iz.z + 1 ];
                y1z0.z = this.units[ ( iy.z + 1 ) * 32 + iz.z + 0 ];
                y1z1.z = this.units[ ( iy.z + 1 ) * 32 + iz.z + 1 ];

                y0z0.w = this.units[ ( iy.w + 0 ) * 32 + iz.w + 0 ];
                y0z1.w = this.units[ ( iy.w + 0 ) * 32 + iz.w + 1 ];
                y1z0.w = this.units[ ( iy.w + 1 ) * 32 + iz.w + 0 ];
                y1z1.w = this.units[ ( iy.w + 1 ) * 32 + iz.w + 1 ];

                return new uint4x4( y0z0, y0z1, y1z0, y1z1 );
            }

            uint4[] makeCubesLineX_( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1, int4 iy, int4 iz )
            {
                // fedcba9876543210fedcba9876543210

                var m1100 = 0b_11001100__11001100_11001100_11001100u;
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

                var res = new uint4[]
                {
                    0 << 8 | _98109810 & 0xff,
                    1 << 8 | _a921a921 & 0xff,
                    2 << 8 | _ba32ba32 & 0xff,
                    3 << 8 | _cb43cb43 & 0xff,
                    4 << 8 | _dc54dc54 & 0xff,
                    5 << 8 | _ed65ed65 & 0xff,
                    6 << 8 | _fe76fe76 & 0xff,
                    7 << 8 | ___870f87 & 0xff,

                    8 << 8 | (_98109810 & 0xff00) >> 8,
                    9 << 8 | (_a921a921 & 0xff00) >> 8,
                    10 << 8 | (_ba32ba32 & 0xff00) >> 8,
                    11 << 8 | (_cb43cb43 & 0xff00) >> 8,
                    12 << 8 | (_dc54dc54 & 0xff00) >> 8,
                    13 << 8 | (_ed65ed65 & 0xff00) >> 8,
                    14 << 8 | (_fe76fe76 & 0xff00) >> 8,
                    15 << 8 | (___870f87 & 0xff00) >> 8,

                    16 << 8 | (_98109810 & 0xff0000) >> 16,
                    17 << 8 | (_a921a921 & 0xff0000) >> 16,
                    18 << 8 | (_ba32ba32 & 0xff0000) >> 16,
                    19 << 8 | (_cb43cb43 & 0xff0000) >> 16,
                    20 << 8 | (_dc54dc54 & 0xff0000) >> 16,
                    21 << 8 | (_ed65ed65 & 0xff0000) >> 16,
                    22 << 8 | (_fe76fe76 & 0xff0000) >> 16,
                    23 << 8 | (___870f87 & 0xff0000) >> 16,

                    24 << 8 | (_98109810 & 0xff000000) >> 24,
                    25 << 8 | (_a921a921 & 0xff000000) >> 24,
                    26 << 8 | (_ba32ba32 & 0xff000000) >> 24,
                    27 << 8 | (_cb43cb43 & 0xff000000) >> 24,
                    28 << 8 | (_dc54dc54 & 0xff000000) >> 24,
                    29 << 8 | (_ed65ed65 & 0xff000000) >> 24,
                    30 << 8 | (_fe76fe76 & 0xff000000) >> 24,
                };
                var q =
                    from i in res
                    let x = (uint)iz.x << 24 | (uint)iy.x << 16 | i.x
                    let y = (uint)iz.y << 24 | (uint)iy.y << 16 | i.y
                    let z = (uint)iz.z << 24 | (uint)iy.z << 16 | i.z
                    let w = (uint)iz.w << 24 | (uint)iy.w << 16 | i.w
                    select new uint4( x, y, z, w )
                    ;
                return q.ToArray();
            }
        }
    }


    public class CubeGrid32x32x32
    {
        public const int unitLength = 32;

        uint[] units;


        public CubeGrid32x32x32()
        {
            this.units = Enumerable.Empty<uint>()
                .Concat( Enumerable.Repeat( (uint)0x_ffff_f5ff, 32 ) )
                .Concat( Enumerable.Repeat( (uint)0x_0070_6001, 32 ) )
                .Repeat( 16 )
                .ToArray();
        }

        public CubeGrid32x32x32( bool isFillAll )
        {
            this.units = new uint[ 1 * 32 * 32 ];
            if( isFillAll )
                System.Buffer.SetByte( this.units, 0, 0xff );
            else
                System.Array.Clear( this.units, 0, this.units.Length );
        }


        public uint this[ int ix, int iy, int iz ]
        {
            get
            {
                return (uint)( this.units[(iy<<5) + iz] & 1<<ix );
            }
            set
            {
                var ( iy << 5 ) + iz;
                var b = this.units[ ( iy << 5 ) + iz ] & 1 << ix;
                value;
            }
        }
        public uint[] this[ int3 leftTop, int3 length3 ]
        {
            get
            {

            }
            set
            {

            }
        }


        public uint SampleCube( int x, int y, int z )
        {
            var _x0 = (uint)( x + 0 );
            var _z0 = (uint)( ( z + 0 ) << 5 );
            var _z1 = (uint)( ( z + 1 ) << 5 );
            var _y0 = (uint)( ( y + 0 ) << 10 );
            var _y1 = (uint)( ( y + 1 ) << 10 );

            var _ix = new uint4( _x0, _x0, _x0, _x0 );
            var _iz = new uint4( _z0, _z1, _z0, _z1 );
            var _iy = new uint4( _y0, _y0, _y1, _y1 );

            var i = _ix + _iz + _iy;
            var igrid = i >> 5;
            var ibit = i & 0x1f;

            var unit01 = ( this.units[ igrid.x ] >> (int)ibit.x ) & 0b11;
            var unit23 = ( this.units[ igrid.y ] >> (int)ibit.y ) & 0b11;
            var unit45 = ( this.units[ igrid.z ] >> (int)ibit.z ) & 0b11;
            var unit67 = ( this.units[ igrid.w ] >> (int)ibit.w ) & 0b11;

            return ( unit67 << 6 ) | ( unit45 << 4 ) | ( unit23 << 2 ) | ( unit01 << 0 );
        }


        public uint[] SampleAllCubes()
        {
            var outputCubes = new List<uint>( 32 * 32 );

            for( var iy = 0; iy < 31; iy++ )
                for( var iz = 0; iz < 31; iz++ )
                {
                    var y0z0 = this.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = this.units[ ( iy + 0 ) * 32 + iz + 1 ];
                    var y1z0 = this.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1 = this.units[ ( iy + 1 ) * 32 + iz + 1 ];

                    var cubes = makeCubesLineX_( y0z0, y0z1, y1z0, y1z1 );

                    var i = 0;
                    var ix = 0;
                    for( var ipack = 0; ipack < 32 / 8; ipack++ )
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
                }

            return outputCubes.ToArray();


            void addCubeIfVisible_( uint cube8bit, List<uint> output, int ix, int iy, int iz )
            {
                var cube = cube8bit & 0xff;
                if( cube == 0 ) return;

                var posAndCube = (uint)iz << 24 | (uint)iy << 16 | (uint)ix << 8 | cube;
                output.Add( posAndCube );
            }

            (uint _98109810, uint _a921a921, uint _ba32ba32, uint _cb43cb43,
            uint _dc54dc54, uint _ed65ed65, uint _fe76fe76, uint ___870f87)
            makeCubesLineX_( uint y0z0, uint y0z1, uint y1z0, uint y1z1 )
            {
                // fedcba9876543210fedcba9876543210

                var m1100 = 0b_11001100__11001100_11001100_11001100u;
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