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



    public struct CubeUnit
    {
        public byte unit;

        public uint this[ int x, int y, int z ]
        {
            get => (byte)( ( this.unit >> ( x + y << 2 + z << 1 ) ) & 1 );
            set 
            { 
                var shift = ( x + ( y << 2 ) + ( z << 1 ) );
                this.unit ^= (byte)( ( value << shift ) ^ ( this.unit & ( 1 << shift ) ) );
            }
        }
    }
    [StructLayout( LayoutKind.Explicit )]
    public struct Cube2x2
    {
        [FieldOffset( 0 )]
        public uint unit2x2;

        [FieldOffset( 0 )] CubeUnit unit0;
        [FieldOffset( 1 )] CubeUnit unit1;
        [FieldOffset( 2 )] CubeUnit unit2;
        [FieldOffset( 3 )] CubeUnit unit3;
        [FieldOffset( 4 )] CubeUnit unit4;
        [FieldOffset( 5 )] CubeUnit unit5;
        [FieldOffset( 6 )] CubeUnit unit6;
        [FieldOffset( 7 )] CubeUnit unit7;

        //public uint this[ int x, int y, int z ]
        //{
        //    //get => (byte)( 
        //    //set
        //    //{
        //    //    var shift = ( x + ( y << 2 ) + ( z << 1 ) );
        //    //    this.unit ^= (byte)( ( value << shift ) ^ ( this.unit & ( 1 << shift ) ) );
        //    //}
        //}
    }

    // x 0101 0101
    // y 1111 0000
    // z 1100 1100
    public struct Cubes
    {
        uint[] units;

        //public this[int x, int y, int z]
        //{

        //}
    }


    public class CubeGrid
    {
        public uint xLength, zLength, yLength;

        uint[] units;

        public CubeGrid( int x, int y, int z )
        {
            this.xLength = (uint)x;
            this.yLength = (uint)y;
            this.zLength = (uint)z;
            this.units = //new uint[ (x>>5) * z * y ];
            Enumerable.Empty<uint>()
                .Concat( Enumerable.Repeat( (uint)0x_ffff_ffff, 32 ) )
                .Concat( Enumerable.Repeat( (uint)0x_0000_0001, 32 ) )
                .Repeat( 16 )
                .ToArray();
        }

        public uint GetCube( int x, int y, int z )
        {
            var zofs = this.xLength;
            var yofs = this.xLength * this.zLength;

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

            return (unit67 << 6) | (unit45 << 4) | (unit23 << 2) | (unit01 << 0);
        }
        public uint[] GetCubesRect()// Rect rc )
        {

            var iy_ = new int4( 0, 0, 1, 1 );
            var iz_ = new int4( 0, 1, 0, 1 );
            var l = loadLine_( iy_, iz_ );
            var iy2_ = new int4( 2, 2, 3, 3 );
            var iz2_ = new int4( 0, 1, 0, 1 );
            var l2 = loadLine_( iy2_, iz2_ );
            return makeCubesLineX_(l.c0, l.c1, l.c2, l.c3, iy_, iz_)
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
                var m1100 = 0b_11001100__11001100_11001100_11001100u;
                var m0011 = m1100 >> 2;
                // --dc--98--54--10--dc--98--54--10
                // fe--ba--76--32--fe--ba--76--32--
                var y0_eca86420 = y0z0 & m0011 | (y0z1 & m0011) << 2;
                var y0_fdb97531 = (y0z0 & m1100) >> 2 | y0z1 & m1100;
                var y1_eca86420 = y1z0 & m0011 | (y1z1 & m0011) << 2;
                var y1_fdb97531 = (y1z0 & m1100) >> 2 | y1z1 & m1100;
                // dcdc989854541010dcdc989854541010
                // fefebaba76763232fefebaba76763232

                var mf0 = 0x_f0f0_f0f0u;
                var m0f = 0x_0f0f_0f0fu;
                // dcdc----5454----dcdc----5454----
                // ----9898----1010----9898----1010
                // fefe----7676----fefe----7676----
                // ----baba----3232----baba----3232
                var i_c840 = y0_eca86420 & m0f | (y1_eca86420 & m0f) << 4;
                var i_ea62 = (y0_eca86420 & mf0) >> 4 | y1_eca86420 & mf0;
                var i_d951 = y0_fdb97531 & m0f | (y1_fdb97531 & m0f) << 4;
                var i_fb73 = (y0_fdb97531 & mf0) >> 4 | y1_fdb97531 & mf0;
                // dcdcdcdc54545454dcdcdcdc54545454
                // 98989898101010109898989810101010
                // fefefefe76767676fefefefe76767676
                // babababa32323232babababa32323232

                var m55 = 0x_5555_5555u;
                var maa = 0x_aaaa_aaaau;
                // 
                var j_dc985410 = ( i_d951 & m55 ) << 1 | ( i_c840 & maa ) >> 1;
                var j_ed9a6521 = ( i_ea62 & m55 ) << 1 | ( i_d951 & maa ) >> 1;
                var j_feba7632 = ( i_fb73 & m55 ) << 1 | ( i_ea62 & maa ) >> 1;
                var j_cb8743 = ( i_c840>>8 & 0x_55_5555u ) << 1 | ( i_fb73 & 0x_aa_aaaau ) >> 1;


                var res = new uint4[]
                {
                    0 << 8 | i_c840 & 0xff,
                    1 << 8 | j_dc985410 & 0xff,
                    2 << 8 | i_d951 & 0xff,
                    3 << 8 | j_ed9a6521 & 0xff,
                    4 << 8 | i_ea62 & 0xff,
                    5 << 8 | j_feba7632 & 0xff,
                    6 << 8 | i_fb73 & 0xff,
                    7 << 8 | j_cb8743 & 0xff,
                    
                    8 << 8 | (i_c840 & 0xff00) >> 8,
                    9 << 8 | (j_dc985410 & 0xff00) >> 8,
                    10 << 8 | (i_d951 & 0xff00) >> 8,
                    11 << 8 | (j_ed9a6521 & 0xff00) >> 8,
                    12 << 8 | (i_ea62 & 0xff00) >> 8,
                    13 << 8 | (j_feba7632 & 0xff00) >> 8,
                    14 << 8 | (i_fb73 & 0xff00) >> 8,
                    15 << 8 | (j_cb8743 & 0xff00) >> 8,

                    16 << 8 | (i_c840 & 0xff0000) >> 16,
                    17 << 8 | (j_dc985410 & 0xff0000) >> 16,
                    18 << 8 | (i_d951 & 0xff0000) >> 16,
                    19 << 8 | (j_ed9a6521 & 0xff0000) >> 16,
                    20 << 8 | (i_ea62 & 0xff0000) >> 16,
                    21 << 8 | (j_feba7632 & 0xff0000) >> 16,
                    22 << 8 | (i_fb73 & 0xff0000) >> 16,
                    23 << 8 | (j_cb8743 & 0xff0000) >> 16,

                    24 << 8 | (i_c840 & 0xff0000) >> 24,
                    25 << 8 | (j_dc985410 & 0xff000000) >> 24,
                    26 << 8 | (i_d951 & 0xff000000) >> 24,
                    27 << 8 | (j_ed9a6521 & 0xff000000) >> 24,
                    28 << 8 | (i_ea62 & 0xff000000) >> 24,
                    29 << 8 | (j_feba7632 & 0xff000000) >> 24,
                    30 << 8 | (i_fb73 & 0xff000000) >> 24,
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
        public void SetCube( int x, int y, int z, uint cube )
        {

        }
    }


    public class CubeGrid32x32x32
    {
        public const int unitLength = 32;

        uint[] units;

        public CubeGrid32x32x32()
        {
            this.units = //new uint[ (x>>5) * z * y ];
            Enumerable.Repeat( (uint)0, 32 ).Concat( Enumerable.Repeat( (uint)0xffffffef, 32 ) ).ToArray();
        }

        public uint GetCube( int x, int y, int z )
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
    }
}