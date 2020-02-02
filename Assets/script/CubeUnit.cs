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

        public this[int x, int y, int z]
        {

        }
    }
}