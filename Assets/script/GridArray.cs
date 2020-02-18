using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace mc
{

    public class GridArray
    {

        public int3 GridLength => this.wholeGridLength - 2;
        public int3 wholeGridLength { get; private set; }

        public CubeGrid32x32x32[] grids { get; private set; }

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
            var ed = math.min( st + length3 + 1, this.wholeGridLength-1 );

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = st.y; iy <= ed.y; iy++ )
                for( var iz = st.z; iz <= ed.z; iz++ )
                    for( var ix = st.x; ix <= ed.y; ix++ )
                    {
                        this.grids[ iy * yspan + iz * zspan + ix ] = gridUnit;
                    }
        }


        /// <summary>
        /// とりあえずは全描画、カリングは後で
        /// フィル／ブランクは描画不要、ただし右下後にフィルのくるブランクは、描画必要
        /// </summary>
        public uint[] BuildCubeInstanceData()
        {
            var instanceCubes = new List<uint>();

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = 1; iy <= this.GridLength.y; iy++ )
                for( var iz = 1; iz <= this.GridLength.z; iz++ )
                    for( var ix = 1; ix <= this.GridLength.x; ix++ )
                    {

                        var i = iy * yspan + iz * zspan + ix;

                        var current         = this.grids[ i + 0 ];
                        var current_right   = this.grids[ i + 1 ];
                        var back            = this.grids[ i + zspan + 0 ];
                        var back_right      = this.grids[ i + zspan + 1 ];
                        var under           = this.grids[ i + yspan + 0 ];
                        var under_right     = this.grids[ i + yspan + 1 ];
                        var backUnder       = this.grids[ i + yspan + zspan + 0 ];
                        var backUnder_right = this.grids[ i + yspan + zspan + 1 ];

                        if( current == GridArray.DefaultBlankCube )
                        {
                            var isNoDraw =
                                current_right != GridArray.DefaultBlankCube ||
                                back != GridArray.DefaultBlankCube ||
                                back_right != GridArray.DefaultBlankCube ||
                                under != GridArray.DefaultBlankCube ||
                                under_right != GridArray.DefaultBlankCube ||
                                backUnder != GridArray.DefaultBlankCube ||
                                backUnder_right != GridArray.DefaultBlankCube
                                ;
                            //if( isNoDraw ) continue;
                            
                            // ブランク・フィル用のビルド関数も作るべき
                        }
                        if( current == GridArray.DefaultFilledCube )
                        {
                            var isNoDraw =
                                current_right != GridArray.DefaultFilledCube ||
                                back != GridArray.DefaultFilledCube ||
                                back_right != GridArray.DefaultFilledCube ||
                                under != GridArray.DefaultFilledCube ||
                                under_right != GridArray.DefaultFilledCube ||
                                backUnder != GridArray.DefaultFilledCube ||
                                backUnder_right != GridArray.DefaultFilledCube
                                ;
                            //if( isNoDraw ) continue;
                        }

                        this.SampleAllCubes( i, instanceCubes );
                        //instanceCubes.AddRange( this.grids[ i ].SampleAllCubes() );
                    }

            return instanceCubes.ToArray();
        }

    }

}

