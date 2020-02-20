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

    public struct CubeGrid32x32x32Native
    {
        public const int unitLength = 32;

        public NativeArray<uint> units;
        public int cubeCount;


        //public CubeGrid32x32x32Native()
        //{
        //    this.units = new NativeArray<uint>( 1 * 32 * 32, Allocator.Persistent );
        //    this.cubeCount = 0;
        //}

        public CubeGrid32x32x32Native( bool isFillAll )
        {
            if( isFillAll )
            {
                this.units = new NativeArray<uint>
                    ( 1 * 32 * 32, Allocator.Persistent, NativeArrayOptions.UninitializedMemory );
                for( var i = 0; i < this.units.Length; i++ ) this.units[ i ] = 0xffffffff;

                this.cubeCount = 32 * 32 * 32;
            }
            else
            {
                this.units = new NativeArray<uint>
                    ( 1 * 32 * 32, Allocator.Persistent, NativeArrayOptions.ClearMemory );
                
                this.cubeCount = 0;
            }
        }

        public void Dispose()
        {
            this.units.Dispose();
        }


        public uint this[ int ix, int iy, int iz ]
        {
            get
            {
                return (uint)( this.units[ ( iy << 5 ) + iz ] & 1 << ix );
            }
            set
            {
                var maskedValue = value & 1;

                var i = ( iy << 5 ) + iz;
                var b = this.units[ i ];
                this.units[ i ] |= b ^ b ^ maskedValue << ix;

                this.cubeCount += (int)( maskedValue << 1 - 1 );
            }
        }
    }
}