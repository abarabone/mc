using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movech : MonoBehaviour
{

    Camera cam;
    Rigidbody rb;

    Vector3 prepos;
    
    void Start()
    {
        this.cam = this.GetComponentInChildren<Camera>();
        this.rb = this.GetComponentInChildren<Rigidbody>();
        
    }
    
    void Update()
    {
        var nowpos = Input.mousePosition;

        var d = ( nowpos - prepos ) * Time.deltaTime * 30.0f;

        var e = this.rb.rotation.eulerAngles;
        e.y += d.x;
        this.rb.rotation = Quaternion.Euler( e );

        var tf = this.transform;
        cam.transform.RotateAround( tf.position, tf.right, -d.y );

        if( Input.GetKey( KeyCode.Space ) )
            rb.AddForce( Vector3.up * 100.0f );

        this.prepos = nowpos;
    }
}
