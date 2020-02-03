﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace mc
{

    using vc = Vector3;

    public class mc : MonoBehaviour
    {
        public MarchingCubeAsset MarchingCubeAsset;
        public Material Material;

        
        Mesh mesh;
        ComputeBuffer baseVtxsBuffer;
        ComputeBuffer idxListsBuffer;
        ComputeBuffer instancesBuffer;
        ComputeBuffer argsBuffer;

        int[] cubeInstances;

        void Awake()
        {
            var res = convertMqoToMarchingCubesData();

            this.baseVtxsBuffer = res.basevtxs;
            this.idxListsBuffer = res.idxLists;
            this.instancesBuffer = res.instance;
            this.argsBuffer = ComputeShaderUtility.CreateIndirectArgumentsBuffer();
            this.mesh = res.mesh;

            this.Material.SetBuffer( "BaseVtxList", this.baseVtxsBuffer );
            this.Material.SetBuffer( "IdxList", this.idxListsBuffer );
            this.Material.SetBuffer( "Instances", this.instancesBuffer );

            this.cubeInstances = Enumerable.Range( 0, 300 ).ToArray();
            this.instancesBuffer.SetData( this.cubeInstances );
        }

        private void OnDestroy()
        {
            if( this.baseVtxsBuffer != null ) this.baseVtxsBuffer.Dispose();
            if( this.idxListsBuffer != null ) this.idxListsBuffer.Dispose();
            if( this.instancesBuffer != null ) this.instancesBuffer.Dispose();
            if( this.argsBuffer != null ) this.argsBuffer.Dispose();
        }

        private void Update()
        {
            var mesh = this.mesh;
            var mat = this.Material;
            var args = this.argsBuffer;

            //var vectorOffset = offset.pVectorOffsetInBuffer - nativeBuffer.pBuffer;
            //mat.SetInt( "BoneVectorOffset", (int)vectorOffset );
            ////mat.SetInt( "BoneLengthEveryInstance", mesh.bindposes.Length );
            ////mat.SetBuffer( "BoneVectorBuffer", computeBuffer );

            var instanceCount = 12;
            var argparams = new IndirectArgumentsForInstancing( mesh, instanceCount );
            args.SetData( ref argparams );

            var bounds = new Bounds() { center = Vector3.zero, size = Vector3.one * 1000.0f };
            Graphics.DrawMeshInstancedIndirect( mesh, 0, mat, bounds, args );
        }


        (ComputeBuffer basevtxs, ComputeBuffer idxLists, ComputeBuffer instance, Mesh mesh)
        convertMqoToMarchingCubesData()
        {
            var asset = this.MarchingCubeAsset;

            var res = createMarchingCubesResources(asset.CubeIdsAndIndexLists, asset.BaseVertexList);

            return res;


            (ComputeBuffer basevtxs, ComputeBuffer idxLists, ComputeBuffer instance, Mesh mesh)
            createMarchingCubesResources( (byte cubeId, int[] vtxIdxs)[] cubeIdsAndIndexLists, Vector3[] baseVtxList )
            {

                var instance = createCubeIdInstancingShaderBuffer_( 10000 );
                var basevtxs = createBaseVtxShaderBuffer_( baseVtxList );
                var cubeid = createIdxListsShaderBuffer_( cubeIdsAndIndexLists );
                var mesh = createMesh_();

                return (basevtxs, cubeid, instance, mesh);



                ComputeBuffer createCubeIdInstancingShaderBuffer_( int maxUnitLength )
                {
                    var buffer = new ComputeBuffer( maxUnitLength, Marshal.SizeOf<int>() );

                    return buffer;
                }


                ComputeBuffer createIdxListsShaderBuffer_( (byte cubeId, int[] vtxIdxs)[] cubeIdsAndVtxIndexLists_ )
                {
                    var buffer = new ComputeBuffer( 254 * 12, Marshal.SizeOf<int>() );

                    var q =
                        from x in cubeIdsAndVtxIndexLists_
                            //.Prepend( (cubeId: (byte)0, vtxIdxs: new int[ 0 ]) )
                            //.Append( (cubeId: (byte)255, vtxIdxs: new int[ 0 ]) )
                        orderby x.cubeId
                        select x.vtxIdxs.Concat( Enumerable.Repeat( 0, 12 - x.vtxIdxs.Length ) )
                        ;
                    buffer.SetData( q.SelectMany(x=>x).Cast<int>().ToArray() );

                    return buffer;
                }

                ComputeBuffer createBaseVtxShaderBuffer_( Vector3[] baseVtxList_ )
                {
                    var buffer = new ComputeBuffer( 12, Marshal.SizeOf<Vector4>() );

                    buffer.SetData( baseVtxList_.Select( v => new Vector4( v.x, v.y, v.z, 1.0f ) ).ToArray() );

                    return buffer;
                }

                Mesh createMesh_()
                {
                    var mesh_ = new Mesh();
                    mesh_.name = "marching cube unit";

                    var qVtx =
                        from i in Enumerable.Range( 0, 12 )
                        select new Vector3( i, 0, 0 )
                        ;
                    var qIdx =
                        from i in Enumerable.Range( 0, 3 * 4 )
                        select i
                        ;
                    mesh_.vertices = qVtx.ToArray();
                    mesh_.triangles = qIdx.ToArray();

                    return mesh_;
                }
            }
        }


    }





    public struct IndirectArgumentsForInstancing
    {
        public uint MeshIndexCount;
        public uint InstanceCount;
        public uint MeshBaseIndex;
        public uint MeshBaseVertex;
        public uint BaseInstance;

        public IndirectArgumentsForInstancing
            ( Mesh mesh, int instanceCount = 0, int submeshId = 0, int baseInstance = 0 )
        {
            //if( mesh == null ) return;

            this.MeshIndexCount = mesh.GetIndexCount( submeshId );
            this.InstanceCount = (uint)instanceCount;
            this.MeshBaseIndex = mesh.GetIndexStart( submeshId );
            this.MeshBaseVertex = mesh.GetBaseVertex( submeshId );
            this.BaseInstance = (uint)baseInstance;
        }

        public NativeArray<uint> ToNativeArray( Allocator allocator )
        {
            var arr = new NativeArray<uint>( 5, allocator );
            arr[ 0 ] = this.MeshIndexCount;
            arr[ 1 ] = this.InstanceCount;
            arr[ 2 ] = this.MeshBaseIndex;
            arr[ 3 ] = this.MeshBaseVertex;
            arr[ 4 ] = this.BaseInstance;
            return arr;
        }
    }

    static public class IndirectArgumentsExtensions
    {
        static public ComputeBuffer SetData( this ComputeBuffer cbuf, ref IndirectArgumentsForInstancing args )
        {
            using( var nativebuf = args.ToNativeArray( Allocator.Temp ) )
                cbuf.SetData( nativebuf );

            return cbuf;
        }
    }
    static public class ComputeShaderUtility
    {
        static public ComputeBuffer CreateIndirectArgumentsBuffer() =>
            new ComputeBuffer( 1, sizeof( uint ) * 5, ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable );
    }



}
