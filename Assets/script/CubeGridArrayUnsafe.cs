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
    public unsafe partial struct CubeGridArrayUnsafe
    {

        readonly public int3 GridLength;
        readonly int3 wholeGridLength;

        NativeList<CubeGrid32x32x32Unsafe> gridStock;           // 本体を格納（アドレスを変化させてはいけないので、拡張してはいけない）
        public NativeArray<CubeGrid32x32x32UnsafePtr> grids;    // 本体へのポインタを格納

        CubeGrid32x32x32UnsafePtr defaultBlankCubePtr;
        CubeGrid32x32x32UnsafePtr defaultFilledCubePtr;




        public CubeGridArrayUnsafe( int x, int y, int z ) : this()
        {
            this.GridLength = new int3( x, y, z );
            this.wholeGridLength = new int3( x, y, z ) + 2;

            this.gridStock = allocGridStock_( this.GridLength );
            this.grids = allocGrids_( this.wholeGridLength );

            makeDefaultGrids_( ref this );

            var startGrid = new int3( -1, -1, -1 );
            var endGrid = wholeGridLength;
            this.FillCubes( startGrid, endGrid, isFillAll: false );

            return;

            
            NativeArray<CubeGrid32x32x32UnsafePtr> allocGrids_( int3 wholeGridLength )
            {
                var totalLength = wholeGridLength.x * wholeGridLength.y * wholeGridLength.z;

                return new NativeArray<CubeGrid32x32x32UnsafePtr>( totalLength, Allocator.Persistent );
            }

            NativeList<CubeGrid32x32x32Unsafe> allocGridStock_( int3 gridLength )
            {
                var capacity = gridLength.x * gridLength.y * gridLength.z + 2;// +2 はデフォルト分

                return new NativeList<CubeGrid32x32x32Unsafe>( capacity, Allocator.Persistent );
            }

            void makeDefaultGrids_( ref CubeGridArrayUnsafe ga )
            {
                ga.gridStock.AddNoResize( new CubeGrid32x32x32Unsafe().ForDefault( isFillAll: false ) );
                ga.defaultBlankCubePtr = new CubeGrid32x32x32UnsafePtr
                {
                    p = (CubeGrid32x32x32Unsafe*)ga.gridStock.GetUnsafePtr() + 0
                };

                ga.gridStock.AddNoResize( new CubeGrid32x32x32Unsafe().ForDefault( isFillAll: true ) );
                ga.defaultFilledCubePtr = new CubeGrid32x32x32UnsafePtr
                {
                    p = (CubeGrid32x32x32Unsafe*)ga.gridStock.GetUnsafePtr() + 1
                };
            }
        }




        public unsafe void Dispose()
        {
            foreach( var g in this.gridStock )
            {
                g.Dispose();
            }

            this.gridStock.Dispose();
            this.grids.Dispose();
        }



        public unsafe CubeGrid32x32x32UnsafePtr this[ int x, int y, int z ]
        {
            get
            {
                var i3 = new int3( x, y, z ) + 1;
                var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
                var zspan = this.wholeGridLength.x;
                var i = i3.y * yspan + i3.z * zspan + i3.x;

                var grid = this.grids[ i ];

                if( !grid.p->IsFullOrEmpty ) return grid;

                if( grid.p == this.defaultFilledCubePtr.p || grid.p == this.defaultBlankCubePtr.p )
                {
                    var newGrid = new CubeGrid32x32x32Unsafe( isFillAll: grid.p->IsFull );
                    this.gridStock.AddNoResize( newGrid );// アドレスを変化させてはいけないので、拡張してはいけない。

                    return this.grids[ i ] = new CubeGrid32x32x32UnsafePtr
                    {
                        p = (CubeGrid32x32x32Unsafe*)this.gridStock.GetUnsafePtr() + ( this.gridStock.Length - 1 )
                    };
                }

                return grid;
            }
        }



        public void FillCubes( int3 topLeft, int3 length3, bool isFillAll = false )
        {
            var st = math.max( topLeft + 1, int3.zero );
            var ed = math.min( st + length3 - 1, this.wholeGridLength - 1 );

            var pGridTemplate = isFillAll ? this.defaultFilledCubePtr : this.defaultBlankCubePtr;

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = st.y; iy <= ed.y; iy++ )
                for( var iz = st.z; iz <= ed.z; iz++ )
                    for( var ix = st.x; ix <= ed.x; ix++ )
                    {
                        this.grids[ iy * yspan + iz * zspan + ix ] = pGridTemplate;
                    }
        }









        static NearCubeGrids getGridSet_
            ( ref CubeGridArrayUnsafe gridArray, int ix, int iy, int iz, int yspan_, int zspan_ )
        {
            var i = iy * yspan_ + iz * zspan_ + ix;

            return new NearCubeGrids
            {
                L =
                {
                    x = gridArray.grids[ i + 0 ],
                    y = gridArray.grids[ i + yspan_ + 0 ],
                    z = gridArray.grids[ i + zspan_ + 0 ],
                    w = gridArray.grids[ i + yspan_ + zspan_ + 0 ],
                },
                R =
                {
                    x = gridArray.grids[ i + 1 ],
                    y = gridArray.grids[ i + yspan_ + 1 ],
                    z = gridArray.grids[ i + zspan_ + 1 ],
                    w = gridArray.grids[ i + yspan_ + zspan_ + 1 ],
                },
            };
        }

        struct GridCounts
        {
            public int4 L, R;
        }
        static GridCounts countEach( ref NearCubeGrids g )
        {
            var gridCount = new int4
            (
                g.L.x.p->CubeCount,
                g.L.y.p->CubeCount,
                g.L.z.p->CubeCount,
                g.L.w.p->CubeCount
            );
            var gridCount_right = new int4
            (
                g.R.x.p->CubeCount,
                g.R.y.p->CubeCount,
                g.R.z.p->CubeCount,
                g.R.w.p->CubeCount
            );
            return new GridCounts { L = gridCount, R = gridCount_right };
        }

        static bool isNeedDraw_( int4 gridCount, int4 gridCount_right )
        {
            var addvalue = gridCount + gridCount_right;
            var isZero = !math.any( addvalue );
            var isFull = math.all( addvalue == 0x8000 << 1);
            return !(isZero | isFull);
        }


        static bool isNeedDraw_( ref NearCubeGrids g )
        {
            if( g.L.x.p->IsEmpty )
            {
                var isNoDraw =
                    g.R.x.p->IsEmpty &
                    g.L.y.p->IsEmpty &
                    g.R.y.p->IsEmpty &
                    g.L.z.p->IsEmpty &
                    g.R.z.p->IsEmpty &
                    g.L.w.p->IsEmpty &
                    g.R.w.p->IsEmpty
                    ;
                if( isNoDraw ) return false;

                // ブランク・フィル用のビルド関数も作るべき

                return true;
            }

            if( g.L.x.p->IsFull )
            {
                var isNoDraw =
                    g.R.x.p->IsFull &
                    g.L.y.p->IsFull &
                    g.R.y.p->IsFull &
                    g.L.z.p->IsFull &
                    g.R.z.p->IsFull &
                    g.L.w.p->IsFull &
                    g.R.w.p->IsFull
                    ;
                if( isNoDraw ) return false;

                // ブランク・フィル用のビルド関数も作るべき

                return true;
            }

            return true;
        }




        /// <summary>
        /// 
        /// </summary>
        public JobHandle BuildCubeInstanceData
            ( NativeList<float4> gridPositions, NativeList<CubeInstance> cubeInstances )
        {

            var job = new GridJob
            {
                gridArray = this,
                dstCubeInstances = cubeInstances,
                dstGridPositions = gridPositions,
            }
            .Schedule();

            return job;
        }
        

        /// <summary>
        /// 
        /// </summary>
        public JobHandle BuildCubeInstanceDataParaQ
            ( NativeList<float4> gridPositions, NativeList<CubeInstance> cubeInstances )
        {

            var gridsets = new NativeList<NearCubeGrids>( 100, Allocator.TempJob );


            var dispJob = new GridDispatchJob
            {
                gridArray = this,
                dstGridPositions = gridPositions,
                dstNearGrids = gridsets,
            }
            .Schedule();


            //var dstCubeInstances = new InstanceCubeByParaList { list = cubeInstances.AsParallelWriter() };
            var cubeQueue = new NativeQueue<CubeInstance>( Allocator.TempJob );
            var dstCubeInstances = new InstanceCubeByParaQueue { queue = cubeQueue.AsParallelWriter() };
            var instJob = new CubeInstanceJob<InstanceCubeByParaQueue>
            {
                nearGrids = gridsets.AsDeferredJobArray(),
                dstCubeInstances = dstCubeInstances,
            }
            .Schedule( gridsets, -1, dispJob );

            var copyJob = new QueueToListJob<CubeInstance>
            {
                queue = cubeQueue,
                list = cubeInstances,
            }
            .Schedule( instJob );

            gridsets.Dispose( instJob );
            cubeQueue.Dispose( copyJob );

            //return instJob;
            return copyJob;
        }


        /// <summary>
        /// 
        /// </summary>
        public JobHandle BuildCubeInstanceDataPara
            ( NativeList<float4> gridPositions, NativeList<CubeInstance> cubeInstances )
        {

            var gridsets = new NativeList<NearCubeGrids>( 100, Allocator.TempJob );


            var dispJob = new GridDispatchJob
            {
                gridArray = this,
                dstGridPositions = gridPositions,
                dstNearGrids = gridsets,
            }
            .Schedule();


            var dstCubeInstances = new InstanceCubeByParaList { list = cubeInstances.AsParallelWriter() };
            var instJob = new CubeInstanceJob<InstanceCubeByParaList>
            {
                nearGrids = gridsets.AsDeferredJobArray(),
                dstCubeInstances = dstCubeInstances,
            }
            .Schedule( gridsets, -1, dispJob );


            gridsets.Dispose( instJob );

            return instJob;
        }




    }
}

