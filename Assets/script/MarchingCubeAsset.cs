using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace mc
{

    public class MarchingCubeAsset : ScriptableObject
    {
        public Vector3[] BaseVertexList;
        public CubeWrapper[] CubeIndexLists;

        [System.Serializable]
        public struct CubeWrapper// タプルがシリアライズできないので
        {
            public byte cubeId;
            public int[] indices;
        }
        public (byte cubeId, int[] vtxIdxs)[] CubeIdsAndIndexLists =>
            this.CubeIndexLists.Select( x => (x.cubeId, x.indices) ).ToArray();
    }

}
