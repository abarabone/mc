using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace mc
{

    using vc = Vector3;


    public class mc : MonoBehaviour
    {

        public Material mat;

        ComputeBuffer mcb;

        void Start()
        {
            this.mcb = new ComputeBuffer( 100, Marshal.SizeOf( typeof( Vector3 ) ) );

            this.mat.SetBuffer( "mcb", this.mcb );


            var idxs = new int[ 3 * 4 ];// 3 * 5 の可能性もある

            var vtxs = new Vector3[]
            {
                new vc( )
            };

            var mesh = new Mesh();
        }

        private void OnDestroy()
        {
            this.mcb.Dispose();
        }
    }

}

