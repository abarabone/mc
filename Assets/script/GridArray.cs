using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace mc
{
    
    public class GridArray
    {

        public int3 GridLength => this.wholeGridLength - 2;
        public int3 wholeGridLength { get; private set; }

        CubeGrid32x32x32[] grids;

        static public CubeGrid32x32x32 DefaultBlankCube { get; } = new CubeGrid32x32x32( isFillAll: false );
        static public CubeGrid32x32x32 DefaultFilledCube { get; } = new CubeGrid32x32x32( isFillAll: true );


        public GridArray( int x, int y, int z )
        {
            this.wholeGridLength = new int3( x, y, z ) + 2;

            allocGrids_();

            return;


            void allocGrids_()
            {
                var totalLength = this.wholeGridLength.x * this.wholeGridLength.y * this.wholeGridLength.z;
                this.grids = new CubeGrid32x32x32[ totalLength ];
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

                if( grid == GridArray.DefaultBlankCube )
                {
                    return this.grids[ i ] = new CubeGrid32x32x32( isFillAll: false );
                }
                if( grid == GridArray.DefaultFilledCube )
                {
                    return this.grids[ i ] = new CubeGrid32x32x32( isFillAll: true );
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
        public void BuildCubeInstanceData( NativeList<float4> gridPositions, NativeList<uint> instanceCubes )
        {

            //var gridPositions = new List<float4>();
            //var instanceCubes = new List<uint>();

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = 0; iy < this.wholeGridLength.y - 1; iy++ )
                for( var iz = 0; iz < this.wholeGridLength.z - 1; iz++ )
                    for( var ix = 0; ix < this.wholeGridLength.x - 1; ix++ )
                    {

                        var gridset = getGridSet_( ix, iy, iz, yspan, zspan );

                        if( !isNeedDraw_( ref gridset ) ) continue;


                        var gridId = gridPositions.Length;
                        var isCubeAdded = gridset.SampleAllCubes( gridId, instanceCubes );
                        if( isCubeAdded )
                        {
                            gridPositions.Add( new float4( ix * 32, -iy * 32, -iz * 32, 0 ) );
                        }
                    }

            return;// (gridPositions.ToArray(), instanceCubes.ToArray());
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        (
                CubeGrid32x32x32 current,
                CubeGrid32x32x32 current_right,
                CubeGrid32x32x32 back,
                CubeGrid32x32x32 back_right,
                CubeGrid32x32x32 under,
                CubeGrid32x32x32 under_right,
                CubeGrid32x32x32 backUnder,
                CubeGrid32x32x32 backUnder_right
            )
                getGridSet_( int ix, int iy, int iz, int yspan_, int zspan_ )
            {

                var i = iy * yspan_ + iz * zspan_ + ix;

                var current         = this.grids[ i + 0 ];
                var current_right   = this.grids[ i + 1 ];
                var back            = this.grids[ i + zspan_ + 0 ];
                var back_right      = this.grids[ i + zspan_ + 1 ];
                var under           = this.grids[ i + yspan_ + 0 ];
                var under_right     = this.grids[ i + yspan_ + 1 ];
                var backUnder       = this.grids[ i + yspan_ + zspan_ + 0 ];
                var backUnder_right = this.grids[ i + yspan_ + zspan_ + 1 ];

                return ( current, current_right, back, back_right, under, under_right, backUnder, backUnder_right );
            }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        bool isNeedDraw_(
                ref (
                    CubeGrid32x32x32 current,
                    CubeGrid32x32x32 current_right,
                    CubeGrid32x32x32 back,
                    CubeGrid32x32x32 back_right,
                    CubeGrid32x32x32 under,
                    CubeGrid32x32x32 under_right,
                    CubeGrid32x32x32 backUnder,
                    CubeGrid32x32x32 backUnder_right
                ) g
            )
            {
                if( g.current == GridArray.DefaultBlankCube )
                {
                    var isNoDraw =
                        g.current_right == GridArray.DefaultBlankCube &&
                        g.back == GridArray.DefaultBlankCube &&
                        g.back_right == GridArray.DefaultBlankCube &&
                        g.under == GridArray.DefaultBlankCube &&
                        g.under_right == GridArray.DefaultBlankCube &&
                        g.backUnder == GridArray.DefaultBlankCube &&
                        g.backUnder_right == GridArray.DefaultBlankCube
                        ;
                    if( isNoDraw ) return false;

                    // ブランク・フィル用のビルド関数も作るべき

                    return true;
                }

                if( g.current == GridArray.DefaultFilledCube )
                {
                    var isNoDraw =
                        g.current_right == GridArray.DefaultFilledCube &&
                        g.back == GridArray.DefaultFilledCube &&
                        g.back_right == GridArray.DefaultFilledCube &&
                        g.under == GridArray.DefaultFilledCube &&
                        g.under_right == GridArray.DefaultFilledCube &&
                        g.backUnder == GridArray.DefaultFilledCube &&
                        g.backUnder_right == GridArray.DefaultFilledCube
                        ;
                    if( isNoDraw ) return false;

                    // ブランク・フィル用のビルド関数も作るべき

                    return true;
                }

                return true;
            }

    }
}
