using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace mc
{

    public class GridArray : MonoBehaviour
    {

        [SerializeField]
        public float3 GridLength;

        CubeGrid[] grids;

        
        void Awake()
        {
            var range = this.GridLength + 2;
            var totalLength = (int)math.dot( this.GridLength, Vector3.one );
            this.grids = new CubeGrid[totalLength];
        }
        
        void Update()
        {

        }
    }

}

