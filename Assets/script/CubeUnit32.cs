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
                return (uint)( this.units[ ( iy << 5 ) + iz ] & 1 << ix );
            }
            set
            {
                var i = ( iy << 5 ) + iz;
                var b = this.units[ i ];
                this.units[ i ] = b ^ b ^ (value & 1) << ix;
            }
        }
        //public uint[] this[ int3 leftTop, int3 length3 ]
        //{
        //    get
        //    {

        //    }
        //    set
        //    {

        //    }
        //}


        //public uint SampleCube( int x, int y, int z )
        //{
        //    var _x0 = (uint)( x + 0 );
        //    var _z0 = (uint)( ( z + 0 ) << 5 );
        //    var _z1 = (uint)( ( z + 1 ) << 5 );
        //    var _y0 = (uint)( ( y + 0 ) << 10 );
        //    var _y1 = (uint)( ( y + 1 ) << 10 );

        //    var _ix = new uint4( _x0, _x0, _x0, _x0 );
        //    var _iz = new uint4( _z0, _z1, _z0, _z1 );
        //    var _iy = new uint4( _y0, _y0, _y1, _y1 );

        //    var i = _ix + _iz + _iy;
        //    var igrid = i >> 5;
        //    var ibit = i & 0x1f;

        //    var unit01 = ( this.units[ igrid.x ] >> (int)ibit.x ) & 0b11;
        //    var unit23 = ( this.units[ igrid.y ] >> (int)ibit.y ) & 0b11;
        //    var unit45 = ( this.units[ igrid.z ] >> (int)ibit.z ) & 0b11;
        //    var unit67 = ( this.units[ igrid.w ] >> (int)ibit.w ) & 0b11;

        //    return ( unit67 << 6 ) | ( unit45 << 4 ) | ( unit23 << 2 ) | ( unit01 << 0 );
        //}


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

        public uint[] SampleAllCubes( CubeGrid32x32x32 right, CubeGrid32x32x32 back, CubeGrid32x32x32 under )
        {
            var outputCubes = new List<uint>( 32 * 32 );

            for( var iy = 0; iy < 31; iy++ )
            {
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
                {
                    var iz = 31;
                    var y0z0 = this.units[ ( iy + 0 ) * 32 + iz + 0 ];
                    var y0z1 = back.units[ ( iy + 0 ) * 32 + 0 + 1 ];
                    var y1z0 = this.units[ ( iy + 1 ) * 32 + iz + 0 ];
                    var y1z1 = back.units[ ( iy + 1 ) * 32 + 0 + 1 ];

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