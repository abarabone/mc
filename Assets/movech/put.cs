﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class put : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


    public MarchingCubes.mc mc;


    // Update is called once per frame
    void Update()
    {
        if( Input.GetKeyDown( KeyCode.Escape ) ) Cursor.lockState = CursorLockMode.None;

        if( Input.GetMouseButton(0) )
        {
            var cam = this.GetComponentInChildren<Camera>();
            var res = Physics.Raycast( cam.transform.position, cam.transform.forward, out var hit );
            if( res )
            {  
                var p = Vector3.Scale( hit.point + hit.normal, new Vector3(1,-1,-1) );
                Debug.Log( p );
                var c = this.mc.cubeGrids[ (int)p.x >> 5, (int)p.y >> 5, (int)p.z >> 5 ];
                c[ (int)p.x & 0x1f, (int)p.y & 0x1f, (int)p.z & 0x1f ] = 1;
            }
        }
    }
}
