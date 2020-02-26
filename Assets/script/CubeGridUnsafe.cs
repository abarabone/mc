﻿using System.Collections;
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

    public unsafe struct CubeGrid32x32x32Unsafe
    {
        public uint* pUnits;

        public int cubeCount;
        public bool IsFullOrEmpty => ( this.cubeCount & 0x7fff ) == 0;
        public bool IsFull => this.cubeCount == 0x8000;
        public bool IsEmpty => this.cubeCount == 0;

        
        public unsafe CubeGrid32x32x32Unsafe( bool isFillAll )
        {
            //var align = UnsafeUtility.AlignOf<uint4>();
            const int align = 16;
            const int size = sizeof( uint ) * 1 * 32 * 32;
            
            this.pUnits = (uint*)UnsafeUtility.Malloc( size, align, Allocator.Persistent );

            if( isFillAll )
            {
                UnsafeUtility.MemSet( this.pUnits, 0xff, size );
                this.cubeCount = 32 * 32 * 32;
            }
            else
            {
                UnsafeUtility.MemClear( this.pUnits, size );
                this.cubeCount = 0;
            }
        }

        public void Dispose()
        {
            if( this.pUnits != null )// struct なので、複製された場合意味がない
            {
                UnsafeUtility.Free( this.pUnits, Allocator.Persistent );
                this.pUnits = null;
            }
        }


        public uint this[ int ix, int iy, int iz ]
        {
            get
            {
                return (uint)( this.pUnits[ ( iy << 5 ) + iz ] & 1 << ix );
            }
            set
            {
                var maskedValue = value & 1;

                var i = ( iy << 5 ) + iz;
                var b = this.pUnits[ i ];
                this.pUnits[ i ] |= b ^ b ^ maskedValue << ix;

                this.cubeCount += (int)( maskedValue << 1 - 1 );
            }
        }
    }
}