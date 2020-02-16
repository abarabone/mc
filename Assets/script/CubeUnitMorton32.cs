using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mc
{

    public class CubeUnitMorton32
    {

        uint[] units;


        uint SampleCube( int x, int y, int z )
        {

            var i = bitsp3_(y>>1)<<2 | bitsp3_(z>>1) <<1 | bitsp3_(x>>1);


            uint a(int n)
            {
                var s0 = (uint)n;
                var s1 = ( s0 | s0 << 8 ) & 0b_0000_0000_0000_1100_0011_0000_1100_0011u;
                return s1;
            }

            uint bitsp3_( int n )
            {
                var s0 = (uint)n;
                var s1 = ( s0 | s0 << 8 ) & 0b_0000_0000_0000_0000_1111_0000_0000_1111u;
                var s2 = ( s1 | s1 << 4 ) & 0b_0000_0000_0000_1100_0011_0000_1100_0011u;
                var s3 = ( s2 | s2 << 2 ) & 0b_0000_0000_0010_0100_1001_0010_0100_1001u;
                return s3;
            }

            uint bitsp3w_( int n )
            {
                var s0 = (uint)n;
                var s1 = ( s0 | s0 << 8 | s0 << 16 ) & 0b_0000_1111_0000_0000_1111_0000_0000_1111u;
                var s2 = ( s1 | s1 << 4 )            & 0b_1100_0011_0000_1100_0011_0000_1100_0011u;
                var s3 = ( s2 | s2 << 2 )            & 0b_0100_1001_0010_0100_1001_0010_0100_1001u;
                return s3;
            }

            return 0;
        }



    }
}
