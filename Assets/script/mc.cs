using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using System;

namespace mc
{

    using vc = Vector3;

    public class mc : MonoBehaviour
    {
        public MarchingCubeAsset MarchingCubeAsset;
        public Material Material;

        GridArray cubeGrids;
        
        Mesh mesh;
        ComputeBuffer baseVtxsBuffer;
        ComputeBuffer idxListsBuffer;
        ComputeBuffer instancesBuffer;
        ComputeBuffer argsBuffer;
        ComputeBuffer gridPositionBuffer;

        //uint[] cubeInstances;
        NativeList<float4> gridPositions;// = new NativeList<float4>( 3000, Allocator.Persistent );
        NativeList<uint> cubeInstances;// = new NativeList<uint>( 80000, Allocator.Persistent );

        void Awake()
        {
            this.gridPositions = new NativeList<float4>( 3000, Allocator.Persistent );
            this.cubeInstances = new NativeList<uint>( 80000, Allocator.Persistent );

            var res = convertMqoToMarchingCubesData();

            this.baseVtxsBuffer = res.basevtxs;
            this.idxListsBuffer = res.idxLists;
            this.instancesBuffer = res.instance;
            this.gridPositionBuffer = res.gridposition;
            this.argsBuffer = ComputeShaderUtility.CreateIndirectArgumentsBuffer();
            this.mesh = res.mesh;

            this.Material.SetBuffer( "BaseVtxList", this.baseVtxsBuffer );
            this.Material.SetBuffer( "IdxList", this.idxListsBuffer );
            this.Material.SetBuffer( "Instances", this.instancesBuffer );
            this.Material.SetBuffer( "GridPositions", this.gridPositionBuffer );
            //this.Material.SetVector( "UnitLength", new Vector4(32,32,32,0) );

            this.cubeGrids = new GridArray( 1, 1, 1 );
            this.cubeGrids.FillCubes( GridArray.DefaultBlankCube, new int3( -1, -1, -1 ), new int3( 11, 11, 11 ) );
            this.cubeGrids.FillCubes( GridArray.DefaultFilledCube, new int3( -1, 2, -1 ), new int3( 11, 11, 11 ) );
            this.cubeGrids.FillCubes( GridArray.DefaultFilledCube, new int3( 2, 0, 3 ), new int3( 1, 2, 1 ) );
            
            var c = this.cubeGrids[ 0, 0, 0 ];
            c[ 1, 1, 1 ] = 1;
            c[ 31, 1, 1 ] = 1;
            c[ 31, 31, 31 ] = 1;
            c[ 1, 31, 1 ] = 1;
            for( var iy = 0; iy < 15; iy++ )
                for( var iz = 0; iz < 15; iz++ )
                    for( var ix = 0; ix < 13; ix++ )
                        c[ 5 + ix, 5 + iy, 5 + iz ] = 1;
            this.cubeGrids.BuildCubeInstanceData( this.gridPositions, this.cubeInstances );
            this.instancesBuffer.SetData( this.cubeInstances.AsArray() );
            this.gridPositionBuffer.SetData( this.gridPositions.AsArray() );
            Debug.Log($"{cubeInstances.Length} / {this.instancesBuffer.count}");

            //var idxLists = this.MarchingCubeAsset.CubeIdsAndIndexLists.Select( x => x.vtxIdxs ).ToArray();
            //var vtxList = this.MarchingCubeAsset.BaseVertexList.Select( x => new float3( x.x, x.y, x.z ) ).ToArray();
            //var q =
            //    from x in this.cubeInstances
            //    let gridId = CubeUtiilty.FromCubeInstance( x ).gridId
            //    group x by gridId
            //    ;
            //foreach( var cubeId in q )
            //{
            //    var (i, v) = CubeUtiilty.MakeCollisionMeshData( cubeId.ToArray(), idxLists, vtxList );
            //    var mesh = new Mesh();
            //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            //    mesh.vertices = v.Select( x => new Vector3( x.x, x.y, x.z ) ).ToArray();
            //    mesh.triangles = i;
            //    var go = new GameObject( $"new {cubeId.Key}" );
            //    var vc4 = gridPositions[ (int)cubeId.Key ];
            //    go.transform.position = new float3( vc4.x, vc4.y, vc4.z );
            //    go.AddComponent<MeshCollider>().sharedMesh = mesh;
            //}
        }

        private void OnDestroy()
        {
            if( this.baseVtxsBuffer != null ) this.baseVtxsBuffer.Dispose();
            if( this.idxListsBuffer != null ) this.idxListsBuffer.Dispose();
            if( this.instancesBuffer != null ) this.instancesBuffer.Dispose();
            if( this.gridPositionBuffer != null ) this.gridPositionBuffer.Dispose();
            if( this.argsBuffer != null ) this.argsBuffer.Dispose();

            this.cubeGrids.Dispose();
            this.gridPositions.Dispose();
            this.cubeInstances.Dispose();
        }


        private void Update()
        {
            this.gridPositions.Clear();
            this.cubeInstances.Clear();
            this.cubeGrids.BuildCubeInstanceData( this.gridPositions, this.cubeInstances );
            this.instancesBuffer.SetData( this.cubeInstances.AsArray() );
            this.gridPositionBuffer.SetData( this.gridPositions.AsArray() );


            var mesh = this.mesh;
            var mat = this.Material;
            var args = this.argsBuffer;

            //var vectorOffset = offset.pVectorOffsetInBuffer - nativeBuffer.pBuffer;
            //mat.SetInt( "BoneVectorOffset", (int)vectorOffset );
            ////mat.SetInt( "BoneLengthEveryInstance", mesh.bindposes.Length );
            ////mat.SetBuffer( "BoneVectorBuffer", computeBuffer );

            var instanceCount = this.cubeInstances.Length;
            var argparams = new IndirectArgumentsForInstancing( mesh, instanceCount );
            args.SetData( ref argparams );

            var bounds = new Bounds() { center = Vector3.zero, size = Vector3.one * 1000.0f };
            Graphics.DrawMeshInstancedIndirect( mesh, 0, mat, bounds, args );
        }


        (ComputeBuffer basevtxs, ComputeBuffer idxLists, ComputeBuffer instance, ComputeBuffer gridposition, Mesh mesh)
        convertMqoToMarchingCubesData()
        {
            var asset = this.MarchingCubeAsset;

            var instance = createCubeIdInstancingShaderBuffer_( 32*32*32*3000 );
            var basevtxs = createBaseVtxShaderBuffer_( asset.BaseVertexList );
            var cubeid = createIdxListsShaderBuffer_( asset.CubeIdsAndIndexLists );
            var gridposition = createGridPositionShaderBuffer_( 3000 );
            var mesh = createMesh_();

            return (basevtxs, cubeid, instance, gridposition, mesh);
            

            ComputeBuffer createCubeIdInstancingShaderBuffer_( int maxUnitLength )
            {
                var buffer = new ComputeBuffer( maxUnitLength, Marshal.SizeOf<uint>() );

                return buffer;
            }

            ComputeBuffer createGridPositionShaderBuffer_( int maxBufferLength )
            {
                var buffer = new ComputeBuffer( maxBufferLength, Marshal.SizeOf<float4>() );

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
                buffer.SetData( q.SelectMany( x => x ).Cast<int>().ToArray() );

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

