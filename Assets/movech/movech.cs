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

        this.prepos = Input.mousePosition;
    }

    void FixedUpdate()
    {
        var nowpos = Input.mousePosition;

        var d = ( nowpos - prepos ) * Time.fixedDeltaTime * 50.0f;

        var e = this.rb.rotation.eulerAngles;
        e.y += d.x;
        this.rb.MoveRotation( Quaternion.Euler( e ) );

        var tf = this.transform;
        var precex = cam.transform.localEulerAngles.x;
        if( precex > 180.0f ) precex -= 360.0f;
        var newcex = precex;
        newcex -= d.y;
        newcex = Mathf.Clamp( newcex, -90.0f, 90.0f );
        cam.transform.RotateAround( tf.position, tf.right, newcex - precex );


        if( Input.GetKeyDown( KeyCode.Space ) )
            rb.AddForce( Vector3.up * 500.0f );

        this.prepos = nowpos;


        var move = Vector3.zero;
        if( Input.GetKey( KeyCode.E ) ) move += tf.forward;
        if( Input.GetKey( KeyCode.D ) ) move -= tf.forward;
        if( Input.GetKey( KeyCode.F ) ) move += tf.right;
        if( Input.GetKey( KeyCode.S ) ) move -= tf.right;

        rb.AddForce( move*0.01f / Time.fixedDeltaTime, ForceMode.VelocityChange );

    }
}
