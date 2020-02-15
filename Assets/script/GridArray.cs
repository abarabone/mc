using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace mc
{

    public class GridArray
    {

        int3 wholeGridLength;
        public int3 GridLength => this.wholeGridLength - 2;

        CubeGrid32x32x32[] grids;

        static public CubeGrid32x32x32 DefaultBlankCube { get; } = new CubeGrid32x32x32( isFillAll: false );
        static public CubeGrid32x32x32 DefaultFilledCube { get; } = new CubeGrid32x32x32( isFillAll: true );
        
        public CubeGrid32x32x32 this[ int x, int y, int z ]
        {
            get
            {
                var i = new int3( x, y, z ) + 1;
                var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
                var zspan = this.wholeGridLength.x;

                return this.grids[ i.y * yspan + i.z * zspan + i.x ];
            }
        }



        public GridArray( int x, int y, int z )
        {
            this.wholeGridLength = new int3(x, y, z) + 2;

            allocGrids_();

            return;


            void allocGrids_()
            {
                var totalLength = (int)math.dot( this.wholeGridLength, Vector3.one );
                this.grids = new CubeGrid32x32x32[ totalLength];
            }
        }

        public void FillCubes( CubeGrid32x32x32 gridUnit, int3 topLeft, int3 length3 )
        {
            var st = topLeft + 1;
            var ed = math.max( st + length3 + 1, this.wholeGridLength );

            var yspan = this.wholeGridLength.x * this.wholeGridLength.z;
            var zspan = this.wholeGridLength.x;

            for( var iy = st.y; iy < ed.y; iy++ )
                for( var iz = st.z; iz < ed.z; iz++ )
                    for( var ix = st.x; ix < ed.y; ix++ )
                    {
                        this.grids[ iy * yspan + iz * zspan + ix ] = gridUnit;
                    }
        }

    }

}

