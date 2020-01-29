using System.Collections;
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

    public class MarchingCubeAsset : ScriptableObject
    {
        public (byte cubeId, int[] vertexIndices)[] CubeIdsAndVtxIndexLists;
        public Dictionary<byte, int[]> vs;
        public int a;
        public Vector3[] BaseVertexList;
    }


    public class mc : MonoBehaviour
    {
        public MarchingCubeAsset MarchingCubeAsset;
        public Material mat;

        ComputeBuffer mcb;

        void Start()
        {
            convertMqoToMarchingCubesData();
        }

        private void OnDestroy()
        {

        }



        void convertMqoToMarchingCubesData()
        {
            var res = this.MarchingCubeAsset;
            Debug.Log( res.BaseVertexList.Length );//.CubeIdsAndVtxIndexLists.Length );
            //using( var f = new StreamReader( path ) )
            //{
            //    var s = f.ReadToEnd();
            //    var objdata = convertToObjectsData( s );

            //    var cubes = convertObjectDataToMachingCubesData( objdata );
            //    var resouce = createMarchingCubesResources( cubes.cubeIdsAndVtxIndexLists, cubes.baseVtxList );

            //}
        }

        static (ComputeBuffer basevtxs, ComputeBuffer cubeid, ComputeBuffer instance, Mesh mesh)
        createMarchingCubesResources( (byte cubeId, int[] vtxIdxs)[] cubeIdsAndVtxIndexLists, Vector3[] baseVtxList )
        {

            var instance = createCubeIdInstancingShaderBuffer_( 10000 );
            var basevtxs = createBaseVtxShaderBuffer_( baseVtxList );
            var cubeid = createIdxListsShaderBuffer_( cubeIdsAndVtxIndexLists );
            var mesh = createMesh_();

            return (basevtxs, cubeid, instance, mesh);



            ComputeBuffer createCubeIdInstancingShaderBuffer_( int maxUnitLength )
            {
                var buffer = new ComputeBuffer( maxUnitLength, Marshal.SizeOf<byte>() );

                return buffer;
            }


            ComputeBuffer createIdxListsShaderBuffer_( (byte cubeId, int[] vtxIdxs)[] cubeIdsAndVtxIndexLists_ )
            {
                var buffer = new ComputeBuffer( 254 * 12, Marshal.SizeOf<byte>() );

                var q =
                    from x in cubeIdsAndVtxIndexLists_
                    orderby x.cubeId
                    select x.vtxIdxs.Concat( Enumerable.Repeat( -1, 12 - x.vtxIdxs.Length ) )
                    ;
                buffer.SetData( q.Cast<byte>().ToArray() );

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
                    from i in Enumerable.Range( 0, 3 * 12 )
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

