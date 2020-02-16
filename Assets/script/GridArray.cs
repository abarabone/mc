using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace mc
{

    public class GridArray
    {

        public int3 GridLength => this.wholeGridLength - 2;
        int3 wholeGridLength;

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
                var i = i3.y* yspan +i3.z * zspan + i3.x;

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

    }

}

