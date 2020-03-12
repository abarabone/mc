using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using MarchingCubes;

public class put : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
    }


    public MarchingCubes.mc mc;


    // Update is called once per frame
    void Update()
    {
        //if( Input.GetKeyDown( KeyCode.Escape ) ) Cursor.lockState ^= CursorLockMode.Locked;

        if( Input.GetMouseButton(0) )
        {
            var cam = this.GetComponentInChildren<Camera>();
            var res = Physics.Raycast( cam.transform.position, cam.transform.forward, out var hit );
            if( res )
            {
                var targetpos = (float3)( hit.point + hit.normal * 0.1f );

                var cubepos = (int3)targetpos * new int3(1,-1,-1);
                Debug.Log( cubepos );

                var igrid = cubepos >> 5;
                var gridpos = igrid * new float3( 32, -32, -32 );


                // グリッド書き換え
                var cube = this.mc.cubeGrids[ igrid.x, igrid.y, igrid.z ];
                var innerpos = cubepos & 0x1f;
                cube[ innerpos.x, innerpos.y, innerpos.z ] = 1;


                // コライダ
                var instances = new NativeList<CubeInstance>( 32 * 32 * 32, Allocator.TempJob );

                this.mc.cubeGrids.BuildCubeInstanceDataDirect( igrid, instances );

                var collider = this.mc.cubeGridColliders[ igrid.x, igrid.y, igrid.z ];
                this.mc.cubeGridColliders[ igrid.x, igrid.y, igrid.z ] =
                    this.mc.BuildMeshCollider( gridpos, collider, instances.AsArray() );

                instances.Dispose();
            }
        }
    }
}
