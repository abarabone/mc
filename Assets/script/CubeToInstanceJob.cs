using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct CubeToInstanceJob : IJobParallelFor
{

    public NativeArray<uint> units;

    public void Execute( int index )
    {



    }
}
