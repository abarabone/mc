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
using Unity.Collections.Experimental;

namespace mc
{

    public struct CubeInstance
    {
        public uint instance;
        static public implicit operator CubeInstance( uint cubeInstance ) => new CubeInstance { instance = cubeInstance };
    }
    public unsafe struct CubeGrid32x32x32UnsafePtr
    {
        [NativeDisableUnsafePtrRestriction]
        public CubeGrid32x32x32Unsafe* p;
    }

    public unsafe struct CubeGridArrayUnsafe
    {

        public int3 GridLength { get; private set; }
        readonly int3 wholeGridLength;

        NativeList<CubeGrid32x32x32Unsafe> gridStock;
        public NativeArray<CubeGrid32x32x32UnsafePtr> grids;

        CubeGrid32x32x32UnsafePtr pDefaultBlankCube;
        CubeGrid32x32x32UnsafePtr pDefaultFilledCube;
        

        public CubeGridArrayUnsafe( int x, int y, int z ) : this()
        {
            this.GridLength = new int3( x, y, z );
            this.wholeGridLength = new int3( x, y, z ) + 2;

            this.gridStock = allocGridStock_( this.GridLength );
            this.grids = allocGrids_( this.wholeGridLength );

            makeDefaultGrids_( ref this );

            this.FillCubes( new int3( -1, -1, -1 ), wholeGridLength, isFillAll: false );

            return;

            
            NativeArray<CubeGrid32x32x32UnsafePtr> allocGrids_( int3 wholeGridLength )
            {
                var totalLength = wholeGridLength.x * wholeGridLength.y * wholeGridLength.z;

                return new NativeArray<CubeGrid32x32x32UnsafePtr>( totalLength, Allocator.Persistent );
            }

            NativeList<CubeGrid32x32x32Unsafe> allocGridStock_( int3 gridLength )
            {
                var capacity = gridLength.x * gridLength.y * gridLength.z;

                return new NativeList<CubeGrid32x32x32Unsafe>( capacity, Allocator.Persistent );
            }

            void makeDefaultGrids_( ref CubeGridArrayUnsafe ga )
            {
                ga.gridStock.AddNoResize( new CubeGrid32x32x32Unsafe( isFillAll: false ) );
                ga.pDefaultBlankCube = new CubeGrid32x32x32UnsafePtr
                {
                    p = (CubeGrid32x32x32Unsafe*)ga.gridStock.GetUnsafePtr() + 0
                };

                ga.gridStock.AddNoResize( new CubeGrid32x32x32Unsafe( isFillAll: true ) );
                ga.pDefaultFilledCube = new CubeGrid32x32x32UnsafePtr
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


                var newGrid = new CubeGrid32x32x32Unsafe( isFillAll: grid.p->IsFull );
                this.gridStock.AddNoResize( newGrid );// アドレスを変化させてはいけないので、拡張してはいけない。
                
                return this.grids[ i ] = new CubeGrid32x32x32UnsafePtr
                {
                    p = (CubeGrid32x32x32Unsafe*)this.gridStock.GetUnsafePtr() + ( this.gridStock.Length - 1 )
                };
            }
        }



        public void FillCubes( int3 topLeft, int3 length3, bool isFillAll = false )
        {
            var st = math.max( topLeft + 1, int3.zero );
            var ed = math.min( st + length3 - 1, this.wholeGridLength - 1 );

            var pGridTemplate = isFillAll ? this.pDefaultFilledCube : this.pDefaultBlankCube;

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = st.y; iy <= ed.y; iy++ )
                for( var iz = st.z; iz <= ed.z; iz++ )
                    for( var ix = st.x; ix <= ed.x; ix++ )
                    {
                        this.grids[ iy * yspan + iz * zspan + ix ] = pGridTemplate;
                    }
        }









        static CubeNearGrids getGridSet_
            ( ref CubeGridArrayUnsafe gridArray, int ix, int iy, int iz, int yspan_, int zspan_ )
        {
            var i = iy * yspan_ + iz * zspan_ + ix;

            return new CubeNearGrids
            {
                current = gridArray.grids[ i + 0 ],
                current_right = gridArray.grids[ i + 1 ],
                back = gridArray.grids[ i + zspan_ + 0 ],
                back_right = gridArray.grids[ i + zspan_ + 1 ],
                under = gridArray.grids[ i + yspan_ + 0 ],
                under_right = gridArray.grids[ i + yspan_ + 1 ],
                backUnder_right = gridArray.grids[ i + yspan_ + zspan_ + 1 ],
                backUnder = gridArray.grids[ i + yspan_ + zspan_ + 0 ],
            };
        }

        static bool isNeedDraw_( ref CubeNearGrids g )
        {
            if( g.current.p->IsEmpty )
            {
                var isNoDraw =
                    g.current_right.p->IsEmpty &
                    g.back.p->IsEmpty &
                    g.back_right.p->IsEmpty &
                    g.under.p->IsEmpty &
                    g.under_right.p->IsEmpty &
                    g.backUnder.p->IsEmpty &
                    g.backUnder_right.p->IsEmpty
                    ;
                if( isNoDraw ) return false;

                // ブランク・フィル用のビルド関数も作るべき

                return true;
            }

            if( g.current.p->IsFull )
            {
                var isNoDraw =
                    g.current_right.p->IsFull &
                    g.back.p->IsFull &
                    g.back_right.p->IsFull &
                    g.under.p->IsFull &
                    g.under_right.p->IsFull &
                    g.backUnder.p->IsFull &
                    g.backUnder_right.p->IsFull
                    ;
                if( isNoDraw ) return false;

                // ブランク・フィル用のビルド関数も作るべき

                return true;
            }

            return true;
        }





        public JobHandle BuildCubeInstanceData_
            ( NativeList<float4> gridPositions, NativeList<CubeInstance> cubeInstances )
        {

            var job = new GridJob
            {
                gridArray = this,
                dstCubeInstances = cubeInstances.AsParallelWriter(),
                dstGridPositions = gridPositions,
            }
            .Schedule();

            return job;
        }
        public JobHandle BuildCubeInstanceData
            ( NativeList<float4> gridPositions, NativeList<CubeInstance> cubeInstances )
        {

            var gridsets = new NativeList<CubeNearGrids>( 100, Allocator.TempJob );


            var dispJob = new GridDispatchJob
            {
                gridArray = this,
                dstGridPositions = gridPositions,
                dstNearGrids = gridsets,
            }
            .Schedule();

            var instJob = new CubeInstanceJob
            {
                nearGrids = gridsets.AsDeferredJobArray(),
                dstCubeInstances = cubeInstances.AsParallelWriter(),
            }
            .Schedule( gridsets, -1, dispJob );

            gridsets.Dispose( instJob );

            return instJob;
        }




        public struct CubeNearGrids
        {
            public int gridId;
            public CubeGrid32x32x32UnsafePtr current;
            public CubeGrid32x32x32UnsafePtr current_right;
            public CubeGrid32x32x32UnsafePtr back;
            public CubeGrid32x32x32UnsafePtr back_right;
            public CubeGrid32x32x32UnsafePtr under;
            public CubeGrid32x32x32UnsafePtr under_right;
            public CubeGrid32x32x32UnsafePtr backUnder;
            public CubeGrid32x32x32UnsafePtr backUnder_right;
        }

        [BurstCompile]
        struct GridJob : IJob
        {

            [ReadOnly]
            public CubeGridArrayUnsafe gridArray;

            [WriteOnly]
            public NativeList<CubeInstance>.ParallelWriter dstCubeInstances;
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


                            SampleAllCubes( ref gridset, gridId, this.dstCubeInstances );
                            
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
            public NativeList<CubeNearGrids> dstNearGrids;
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
        struct CubeInstanceJob : IJobParallelForDefer
        {

            [ReadOnly]
            public NativeArray<CubeNearGrids> nearGrids;

            [WriteOnly]
            public NativeList<CubeInstance>.ParallelWriter dstCubeInstances;


            public void Execute( int index )
            {

                var gridset = this.nearGrids[ index ];


                SampleAllCubes( ref gridset, gridset.gridId, this.dstCubeInstances );
                
            }
        }





        /// <summary>
        /// native contener 化必要、とりあえずは配列で動作チェック
        /// あとでＹＺカリングもしたい
        /// </summary>
        // xyz各32個目のキューブは1bitのために隣のグリッドを見なくてはならず、効率悪いしコードも汚くなる、なんとかならんか？
        static public void SampleAllCubes
            ( ref CubeNearGrids g, int gridId, NativeList<CubeInstance>.ParallelWriter outputCubes )
        {

            for( var iy = 0; iy < 31; iy++ )
            {
                for( var iz = 0; iz < ( 31 & ~( 0x3 ) ); iz += 4 )
                {
                    var c = getXLine_( iy, iz, g.current, g.current, g.current, g.current );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g.current_right, g.current_right, g.current_right, g.current_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = ( 31 & ~( 0x3 ) );

                    var c = getXLine_( iy, iz, g.current, g.back, g.current, g.back );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g.current_right, g.back_right, g.current_right, g.back_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
            }
            {
                const int iy = 31;
                for( var iz = 0; iz < ( 31 & ~( 0x3 ) ); iz += 4 )
                {
                    var c = getXLine_( iy, iz, g.current, g.current, g.under, g.under );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g.current_right, g.current_right, g.under_right, g.under_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
                {
                    const int iz = ( 31 & ~( 0x3 ) );

                    var c = getXLine_( iy, iz, g.current, g.back, g.under, g.backUnder );
                    var cubes = bitwiseCubesXLine_( c.y0z0, c.y0z1, c.y1z0, c.y1z1 );

                    var cr = getXLine_( iy, iz, g.current_right, g.back_right, g.under_right, g.backUnder_right );
                    cubes._0f870f87 |= bitwiseLastHalfCubeXLine_( cr.y0z0, cr.y0z1, cr.y1z0, cr.y1z1 );

                    addCubeFromXLine_( ref cubes, gridId, iy, iz, outputCubes );
                }
            }

        }



        public struct CubeXLineBitwise
        {
            public uint4 _98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, _0f870f87;
            public CubeXLineBitwise
            (
                uint4 _98109810, uint4 _a921a921, uint4 _ba32ba32, uint4 _cb43cb43,
                uint4 _dc54dc54, uint4 _ed65ed65, uint4 _fe76fe76, uint4 _0f870f87
            )
            {
                this._98109810 = _98109810;
                this._a921a921 = _a921a921;
                this._ba32ba32 = _ba32ba32;
                this._cb43cb43 = _cb43cb43;
                this._dc54dc54 = _dc54dc54;
                this._ed65ed65 = _ed65ed65;
                this._fe76fe76 = _fe76fe76;
                this._0f870f87 = _0f870f87;
            }
        }

        public struct CubeNearXLines
        {
            public uint4 y0z0, y0z1, y1z0, y1z1;
            public CubeNearXLines( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1 )
            {
                this.y0z0 = y0z0;
                this.y0z1 = y0z1;
                this.y1z0 = y1z0;
                this.y1z1 = y1z1;
            }
        }



        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static unsafe CubeNearXLines getXLine_(
            int iy, int iz,
            CubeGrid32x32x32UnsafePtr current, CubeGrid32x32x32UnsafePtr back,
            CubeGrid32x32x32UnsafePtr under, CubeGrid32x32x32UnsafePtr backUnder
        )
        {
            //y0  -> ( iy + 0 & 31 ) * 32/4 + ( iz>>2 + 0 & 31>>2 );
            //y1  -> ( iy + 1 & 31 ) * 32/4 + ( iz>>2 + 0 & 31>>2 );
            //y0r -> ( iy + 0 & 31 ) * 32 + ( iz + 1<<2 & 31 );
            //y1r -> ( iy + 1 & 31 ) * 32 + ( iz + 1<<2 & 31 );
            var iy_ = iy;
            var iz_ = new int4( iz >> 2, iz >> 2, iz, iz );
            var yofs = new int4( 0, 1, 0, 1 );
            var zofs = new int4( 0, 0, 1 << 2, 1 << 2 );
            var ymask = 31;
            var zmask = new int4( 31 >> 2, 31 >> 2, 31, 31 );
            var yspan = new int4( 32 / 4, 32 / 4, 32, 32 );
            
            var i = ( iy_ + yofs & ymask ) * yspan + ( iz_ + zofs & zmask );
            var y0 = ((uint4*)current.p->pUnits)[ i.x ];
            var y1 = ( (uint4*)under.p->pUnits )[ i.y ];
            var y0z0 = y0;
            var y1z0 = y1;

            y0.x = back.p->pUnits[ i.z ];
            y1.x = backUnder.p->pUnits[ i.w ];
            var y0z1 = y0.yzwx;
            var y1z1 = y1.yzwx;

            return new CubeNearXLines( y0z0, y0z1, y1z0, y1z1 );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static bool addCubeFromXLine_(
            ref CubeXLineBitwise cubes,
            int gridId_, int iy, int iz, NativeList<CubeInstance>.ParallelWriter outputCubes_
        )
        {
            var isInstanceAppended = false;

            var i = 0;
            var ix = 0;
            var iz_ = new int4( iz + 0, iz + 1, iz + 2, iz + 3 );
            for( var ipack = 0; ipack < 32 / 8; ipack++ )// 8 は 1cube の 8bit
            {
                isInstanceAppended |= addCubeIfVisible_( cubes._98109810 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._a921a921 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._ba32ba32 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._cb43cb43 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._dc54dc54 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._ed65ed65 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._fe76fe76 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                isInstanceAppended |= addCubeIfVisible_( cubes._0f870f87 >> i & 0xff, gridId_, ix++, iy, iz_, outputCubes_ );
                i += 8;
            }

            return isInstanceAppended;
        }
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static bool addCubeIfVisible_
            ( uint4 cubeId, int gridId__, int4 ix_, int4 iy_, int4 iz_, NativeList<CubeInstance>.ParallelWriter cubeInstances )
        {
            var _0or255to0 = cubeId + 1 & 0xfe;
            if( !math.any( _0or255to0 ) ) return false;// すべての cubeId が 0 か 255 なら何もしない

            var cubeInstance = CubeUtiilty.ToCubeInstance( ix_, iy_, iz_, gridId__, cubeId );

            if( _0or255to0.x != 0 ) cubeInstances.AddNoResize( cubeInstance.x );
            if( _0or255to0.y != 0 ) cubeInstances.AddNoResize( cubeInstance.y );
            if( _0or255to0.z != 0 ) cubeInstances.AddNoResize( cubeInstance.z );
            if( _0or255to0.w != 0 ) cubeInstances.AddNoResize( cubeInstance.w );

            return true;
        }
        


        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        // あらかじめ共通段階までビット操作しておいたほうが速くなるかも、でも余計なエリアにストアするから、逆効果の可能性もある
        static CubeXLineBitwise bitwiseCubesXLine_( uint4 y0z0, uint4 y0z1, uint4 y1z0, uint4 y1z1 )
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
            var __f870f87 = ( _98109810 >> 8 & 0x_55_5555u ) << 1 | ( _fe76fe76 & maa ) >> 1;
            // a9a9a9a921212121a9a9a9a921212121
            // cbcbcbcb43434343cbcbcbcb43434343
            // edededed65656565edededed65656565
            // -f-f-f-f878787870f0f0f0f87878787

            return new CubeXLineBitwise
                ( _98109810, _a921a921, _ba32ba32, _cb43cb43, _dc54dc54, _ed65ed65, _fe76fe76, __f870f87 );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static uint4 bitwiseLastHalfCubeXLine_( uint4 y0z0r, uint4 y0z1r, uint4 y1z0r, uint4 y1z1r )
        {
            return ( y0z0r & 1 ) << 25 | ( y0z1r & 1 ) << 27 | ( y1z0r & 1 ) << 29 | ( y1z1r & 1 ) << 31;
        }


    }


}

