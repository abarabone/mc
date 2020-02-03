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
        public int xLength, zLength, yLength;

        uint[] units;

        public CubeGrid( int x, int y, int z )
        {
            this.xLength = x;
            this.yLength = y;
            this.zLength = z;
            this.units = //new uint[ (x>>5) * z * y ];
            Enumerable.Repeat( (uint)0, 32 ).Concat( Enumerable.Repeat( (uint)0xffffffff, 32 ) ).ToArray();
        }

        public uint GetCube( int x, int y, int z )
        {
            var zofs = this.xLength;
            var yofs = this.xLength * this.zLength;

            var _x0 = (uint)( x + 0 );
            var _z0 = (uint)( ( z + 0 ) * zofs );
            var _z1 = (uint)( ( z + 1 ) * zofs );
            var _y0 = (uint)( ( y + 0 ) * yofs );
            var _y1 = (uint)( ( y + 1 ) * yofs );

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

            return (unit67 << 6) | (unit45 << 4) | (unit23 << 2) | (unit01 << 0);
        }
        //public uint GetCube_( int x, int y, int z )
        //{
        //    var zofs = this.xLength;
        //    var yofs = this.xLength * this.zLength;

        //    var _x0 = (uint)( x + 0 );
        //    var _x1 = (uint)( x + 1 );
        //    var _z0 = (uint)(( z + 0 ) * zofs);
        //    var _z1 = (uint)(( z + 1 ) * zofs);
        //    var _y0 = (uint)(( y + 0 ) * yofs);
        //    var _y1 = (uint)(( y + 1 ) * yofs);

        //    var _ix = new uint4( _x0, _x1, _x0, _x1 );
        //    var _iz = new uint4( _z0, _z0, _z1, _z1 );

        //    var i0123 = _ix + _iz + _y0;//new uint4( _y0, _y0, _y0, _y0 );
        //    var i4567 = _ix + _iz + _y1;// new uint4( _y1, _y1, _y1, _y1 );

        //    var g0123 = i0123 >> 5;
        //    var b0123 = i0123 & 0x1f;

        //    var unit0 = this.units[ g0123.x ];
        //    var res0 = (unit0 >> (int)b0123.x) & 1;

        //    return ();
        //}
    }
}