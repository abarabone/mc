using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace mc
{

    public struct CubeGridArrayUnsafe
    {

        public int3 GridLength { get; private set; }
        readonly int3 wholeGridLength;

        public NativeArray<CubeGrid32x32x32Unsafe> grids;



        public CubeGridArrayUnsafe( int x, int y, int z )
        {
            this.GridLength = new int3( x, y, z );
            this.wholeGridLength = new int3( x, y, z ) + 2;

            this.grids = allocGrids_(ref this.wholeGridLength);

            return;


            NativeArray<CubeGrid32x32x32Unsafe> allocGrids_( ref int3 wholeGridLength )
            {
                var totalLength = wholeGridLength.x * wholeGridLength.y * wholeGridLength.z;

                return new NativeArray<CubeGrid32x32x32Unsafe>
                    ( totalLength, Allocator.Persistent, NativeArrayOptions.ClearMemory );
            }
        }

        public void Dispose()
        {
            foreach( var g in this.grids )
                g.Dispose();

            DefaultBlankCube.Dispose();
            DefaultFilledCube.Dispose();
        }


        public CubeGrid32x32x32 this[ int x, int y, int z ]
        {
            get
            {
                var i3 = new int3( x, y, z ) + 1;
                var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
                var zspan = this.wholeGridLength.x;
                var i = i3.y * yspan + i3.z * zspan + i3.x;

                var grid = this.grids[ i ];

                if( grid.IsFullOrEmpty )
                {
                    return this.grids[ i ] = new CubeGrid32x32x32( isFillAll: grid.IsFull );
                }

                return grid;
            }
        }



        public void FillCubes( CubeGrid32x32x32 gridUnit, int3 topLeft, int3 length3 )
        {
            var st = math.max( topLeft + 1, int3.zero );
            var ed = math.min( st + length3 - 1, this.wholeGridLength - 1 );

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = st.y; iy <= ed.y; iy++ )
                for( var iz = st.z; iz <= ed.z; iz++ )
                    for( var ix = st.x; ix <= ed.x; ix++ )
                    {
                        this.grids[ iy * yspan + iz * zspan + ix ] = gridUnit;
                    }
        }


        /// <summary>
        /// とりあえずは全描画、カリングは後で
        /// フィル／ブランクは描画不要、ただし右下後にフィルのくるブランクは、描画必要
        /// </summary>
        //public (float4[] gridPositions, uint[] cubeIds) BuildCubeInstanceData()
        public unsafe void BuildCubeInstanceData( NativeList<float4> gridPositions, NativeList<uint> instanceCubes )
        {

            //var gridPositions = new List<float4>();
            //var instanceCubes = new List<uint>();

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            //var gs = new NativeQueue<CubeNearGrids>( Allocator.TempJob );
            var gs = new NativeList<CubeNearGrids>( Allocator.TempJob );

            for( var iy = 1; iy < this.wholeGridLength.y - 1; iy++ )
                for( var iz = 1; iz < this.wholeGridLength.z - 1; iz++ )
                    for( var ix = 1; ix < this.wholeGridLength.x - 1; ix++ )
                    {

                        var gridset = getGridSet_( ix, iy, iz, yspan, zspan );

                        if( !isNeedDraw_( ref gridset ) ) continue;


                        var ggg = new CubeNearGrids
                        {
                            current = (uint*)gridset.current.units.GetUnsafeReadOnlyPtr(),
                            current_right = (uint*)gridset.current_right.units.GetUnsafeReadOnlyPtr(),
                            back = (uint*)gridset.back.units.GetUnsafeReadOnlyPtr(),
                            back_right = (uint*)gridset.back_right.units.GetUnsafeReadOnlyPtr(),
                            under = (uint*)gridset.under.units.GetUnsafeReadOnlyPtr(),
                            under_right = (uint*)gridset.under_right.units.GetUnsafeReadOnlyPtr(),
                            backUnder = (uint*)gridset.backUnder.units.GetUnsafeReadOnlyPtr(),
                            backUnder_right = (uint*)gridset.backUnder_right.units.GetUnsafeReadOnlyPtr(),
                        };
                        gs.Add( ggg );
                        //var gridId = gridPositions.Length;
                        //var isCubeAdded = gridset.SampleAllCubes( gridId, instanceCubes );
                        //if( isCubeAdded )
                        //{
                        //    gridPositions.Add( new float4( ix * 32, -iy * 32, -iz * 32, 0 ) );
                        //}
                    }

            var job = new McJob
            {
                srcCubeGrids = gs,
                dstCubes = instanceCubes,
                dstGridPositions = gridPositions,
            };
            var j = job.Schedule();
            j.Complete();
            gs.Dispose();

            return;
        }


    }
}
