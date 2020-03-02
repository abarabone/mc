using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Experimental;

namespace MarchingCubes
{
    public partial struct CubeGridArrayUnsafe
    {

        public unsafe struct CubeGrid32x32x32UnsafePtr
        {
            [NativeDisableUnsafePtrRestriction]
            public CubeGrid32x32x32Unsafe* p;

            public uint this[ int ix, int iy, int iz ]
            {
                get => ( *this.p )[ ix, iy, iz ];
                set => ( *this.p )[ ix, iy, iz ] = value;
            }
        }

        public struct NearCubeGrids
        {
            public int gridId;
            public CubeGrid32x32x32UnsafePtr current;
            public CubeGrid32x32x32UnsafePtr current_right;
            public CubeGrid32x32x32UnsafePtr under;
            public CubeGrid32x32x32UnsafePtr under_right;
            public CubeGrid32x32x32UnsafePtr back;
            public CubeGrid32x32x32UnsafePtr back_right;
            public CubeGrid32x32x32UnsafePtr backUnder;
            public CubeGrid32x32x32UnsafePtr backUnder_right;
        }


        public interface ICubeInstanceWriter
        {
            void Add( CubeInstance ci );
        }
        public struct InstanceCubeByList : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeList<CubeInstance> list;
            public void Add( CubeInstance ci ) => list.AddNoResize( ci );
        }
        public struct InstanceCubeByParaList : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeList<CubeInstance>.ParallelWriter list;
            public void Add( CubeInstance ci ) => list.AddNoResize( ci );
        }
        public struct InstanceCubeByParaQueue : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeQueue<CubeInstance>.ParallelWriter queue;
            public void Add( CubeInstance ci ) => queue.Enqueue( ci );
        }



        [BurstCompile]
        struct GridJob : IJob
        {

            [ReadOnly]
            public CubeGridArrayUnsafe gridArray;

            [WriteOnly]
            public NativeList<CubeInstance> dstCubeInstances;
            [WriteOnly]
            public NativeList<float4> dstGridPositions;


            public void Execute()
            {
                var yspan = this.gridArray.wholeGridLength.x * this.gridArray.wholeGridLength.z;
                var zspan = this.gridArray.wholeGridLength.x;

                var gridId = 0;

                for( var iy = 0; iy < this.gridArray.wholeGridLength.y - 1; iy++ )
                    for( var iz = 0; iz < this.gridArray.wholeGridLength.z - 1; iz++ )
                        for( var ix = 0; ix < this.gridArray.wholeGridLength.x - 1; ix++ )
                        {
                            
                            var gridset = getGridSet_( ref this.gridArray, ix, iy, iz, yspan, zspan );
                            var gridcount = countEach( ref gridset );

                            if( !isNeedDraw_( gridcount.left, gridcount.right ) ) continue;
                            //if( !isNeedDraw_( ref gridset ) ) continue;

                            var grid0or1 = math.min( gridcount.left & 0x7fff, new int4(1,1,1,1) );
                            var grid0or1_right = math.min( gridcount.right & 0x7fff, new int4(1,1,1,1) );

                            var dstCubeInstances = new InstanceCubeByList { list = this.dstCubeInstances };
                            SampleAllCubes( ref gridset, grid0or1, grid0or1_right, gridId, dstCubeInstances );
                            //SampleAllCubes( ref gridset, gridId, dstCubeInstances );

                            this.dstGridPositions.Add( new float4( ix * 32, -iy * 32, -iz * 32, 0 ) );

                            gridId++;

                        }
            }
        }



        [BurstCompile]
        struct GridDispatchJob : IJob
        {

            [ReadOnly]
            public CubeGridArrayUnsafe gridArray;

            [WriteOnly]
            public NativeList<NearCubeGrids> dstNearGrids;
            [WriteOnly]
            public NativeList<float4> dstGridPositions;


            public void Execute()
            {
                var yspan = this.gridArray.wholeGridLength.x * this.gridArray.wholeGridLength.z;
                var zspan = this.gridArray.wholeGridLength.x;

                var gridId = 0;

                for( var iy = 0; iy < this.gridArray.wholeGridLength.y - 1; iy++ )
                    for( var iz = 0; iz < this.gridArray.wholeGridLength.z - 1; iz++ )
                        for( var ix = 0; ix < this.gridArray.wholeGridLength.x - 1; ix++ )
                        {

                            var gridset = getGridSet_( ref this.gridArray, ix, iy, iz, yspan, zspan );

                            if( !isNeedDraw_( ref gridset ) ) continue;


                            gridset.gridId = gridId++;

                            this.dstNearGrids.Add( gridset );
                            this.dstGridPositions.Add( new float4( ix * 32, -iy * 32, -iz * 32, 0 ) );

                        }
            }
        }

        [BurstCompile]
        struct CubeInstanceJob<TCubeInstanceWriter> : IJobParallelForDefer where TCubeInstanceWriter : ICubeInstanceWriter
        {

            [ReadOnly]
            public NativeArray<NearCubeGrids> nearGrids;

            [WriteOnly]
            //public NativeList<CubeInstance>.ParallelWriter dstCubeInstances;
            public TCubeInstanceWriter dstCubeInstances;


            public void Execute( int index )
            {

                var gridset = this.nearGrids[ index ];


                SampleAllCubes( ref gridset, gridset.gridId, dstCubeInstances );

            }
        }

        [BurstCompile]
        struct QueueToListJob<T> : IJob where T : struct
        {
            [ReadOnly]
            public NativeQueue<T> queue;
            [WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
                while( queue.TryDequeue( out T item ) )
                {
                    list.AddNoResize( item );
                }
            }
        }

    }
}
