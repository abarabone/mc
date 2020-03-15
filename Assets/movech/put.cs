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
        this.tfCam = this.GetComponentInChildren<Camera>().transform;
    }


    public MarchingCubes.mc mc;

    public Mesh hitmark;
    public Material mat;

    Transform tfCam;



    // Update is called once per frame
    void Update()
    {
        //if( Input.GetKeyDown( KeyCode.Escape ) ) Cursor.lockState ^= CursorLockMode.Locked;

        if( Input.GetMouseButton(0) || Input.GetMouseButton(1) )
        {
            var pos = tfCam.transform.position;
            var dir = tfCam.transform.forward;
            var res = Physics.Raycast( pos + dir, dir, out var hit );
            if( res )
            {
                var targetpos = (float3)( hit.point );// + hit.normal * 0.5f * (Input.GetMouseButton( 0 ) ? 1f : -1f) );
                Graphics.DrawMesh( this.hitmark, targetpos, quaternion.identity, this.mat, 0 );

                var cubepos_ = (int3)targetpos * new int3(1,-1,-1);
                //Debug.Log( cubepos_ );

                for(var ix=0; ix<3; ix++ )
                    for(var iy=0; iy<3; iy++ )
                        for(var iz=0; iz<3; iz++ )
                            setVisible_( cubepos_ + new int3( ix-1, iy-1, iz-1 ) );


                void setVisible_( int3 cubepos )
                {
                    var igrid = cubepos >> 5;
                    var gridpos = igrid * new float3( 32, -32, -32 );


                    // グリッド書き換え
                    var cube = this.mc.cubeGrids[ igrid.x, igrid.y, igrid.z ];
                    var innerpos = cubepos & 0x1f;
                    var v = Input.GetMouseButton( 0 ) ? 1u : 0u;
                    cube[ innerpos.x, innerpos.y, innerpos.z ] = v;


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
}
