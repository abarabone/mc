﻿using System;
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


        public unsafe interface ICubeInstanceWriter
        {
            void Add( CubeInstance ci );
            void AddRange( CubeInstance* pCi, int length );
        }
        public unsafe struct InstanceCubeByList : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeList<CubeInstance> list;
            public void Add( CubeInstance ci ) => list.AddNoResize( ci );
            public void AddRange( CubeInstance* pCi, int length ) => list.AddRangeNoResize( pCi, length );
        }
        public unsafe struct InstanceCubeByParaList : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeList<CubeInstance>.ParallelWriter list;
            public void Add( CubeInstance ci ) => list.AddNoResize( ci );
            public void AddRange( CubeInstance* pCi, int length ) => list.AddRangeNoResize( pCi, length );
        }
        public unsafe struct InstanceCubeByParaQueue : ICubeInstanceWriter
        {
            [WriteOnly]
            public NativeQueue<CubeInstance>.ParallelWriter queue;
            public void Add( CubeInstance ci ) => queue.Enqueue( ci );
            public void AddRange( CubeInstance* pCi, int length ) => queue.Enqueue( *pCi );// だめ
        }
        public unsafe struct InstanceCubeByTempMem : ICubeInstanceWriter
        {
            [WriteOnly]
            [NativeDisableUnsafePtrRestriction][NativeDisableParallelForRestriction]
            public CubeInstance* p;
            public int length;
            public void Add( CubeInstance ci ) => p[length++] = ci;
            public void AddRange( CubeInstance* pCi, int length ) =>
                UnsafeUtility.MemCpy( p, pCi, length * sizeof(CubeInstance) );
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

                            if( !isNeedDraw_( gridcount.L, gridcount.R ) ) continue;
                            //if( !isNeedDraw_( ref gridset ) ) continue;

                            var dstCubeInstances = new InstanceCubeByList { list = this.dstCubeInstances };
                            SampleAllCubes_( ref gridset, ref gridcount, gridId, ref dstCubeInstances );
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
        struct CubeInstanceJob<TCubeInstanceWriter> : IJobParallelForDefer
            where TCubeInstanceWriter : ICubeInstanceWriter
        {

            [ReadOnly]
            public NativeArray<NearCubeGrids> nearGrids;

            [WriteOnly]
            //public NativeList<CubeInstance>.ParallelWriter dstCubeInstances;
            public TCubeInstanceWriter dstCubeInstances;


            public unsafe void Execute_( int index )// 激しく遅い、リストに細かく並列書き込みするとだめっぽい
            {

                var gridset = this.nearGrids[ index ];

                SampleAllCubes( ref gridset, gridset.gridId, ref dstCubeInstances );

            }
            public unsafe void Execute( int index )// 結構速い、IJob より全体は 1.6 倍くらいかかるが完了はだいぶ速い
            {
                // 素の確保のほうがコンテナ類（初期化なし）よりだいぶ速い、また unsafe list より幾分か速い
                var p = (CubeInstance*)UnsafeUtility.Malloc( 32*32*32 * sizeof(CubeInstance), 16, Allocator.Temp );
                var instances = new InstanceCubeByTempMem { p = p };
                
                var gridset = this.nearGrids[ index ];

                SampleAllCubes( ref gridset, gridset.gridId, ref instances );

                dstCubeInstances.AddRange( instances.p, instances.length );
                UnsafeUtility.Free( p, Allocator.Temp );
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
