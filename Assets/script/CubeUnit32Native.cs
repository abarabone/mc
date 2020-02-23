using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace mc
{

    //public struct CubeGrid32x32x32
    public class CubeGrid32x32x32
    {
        public const int unitLength = 32;

        public NativeArray<uint> units;
        public int cubeCount;


        public CubeGrid32x32x32()
        {
            this.units = new NativeArray<uint>( 1 * 32 * 32, Allocator.Persistent, NativeArrayOptions.ClearMemory );
            this.cubeCount = 0;
        }

        public unsafe CubeGrid32x32x32( bool isFillAll )
        {
            if( isFillAll )
            {
                this.units = new NativeArray<uint>
                    ( 1 * 32 * 32, Allocator.Persistent, NativeArrayOptions.UninitializedMemory );
                UnsafeUtility.MemSet( this.units.GetUnsafePtr(), 0xff, sizeof(uint) * this.units.Length );

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
            if(this.units.IsCreated) this.units.Dispose();
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