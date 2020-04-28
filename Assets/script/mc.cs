using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace MarchingCubes
{

    using vc = Vector3;
    //using MarchingCubeAsset = MarchingCubes.MarchingCubeAsset;
    //using CubeGridArrayUnsafe = MarchingCubes.CubeGridArrayUnsafe;
    //using CubeInstance = MarchingCubes.CubeInstance;
    //using CubeUtility = MarchingCubes.CubeUtility;

    public class mc : MonoBehaviour
    {
        public MarchingCubeAsset MarchingCubeAsset;
        public Material Material;

        public ComputeShader setGridCubeIdShader;

        public CubeGridArrayUnsafe cubeGrids { get; private set; }
        public MeshCollider[,,] cubeGridColliders { get; private set; }

        //uint[] cubeInstances;
        NativeList<float4> gridPositions;
        NativeList<int4> nearGrids;
        NativeList<CubeInstance> cubeInstances;
        //NativeQueue<CubeInstance> cubeInstances;

        MeshResources meshResources;

        public int maxDrawGridLength;

        
        void setResources()
        {
            var res = this.meshResources;
            
            //float3 normals[ 155 ];

            //uint4 cube_patterns[ 254 ][2];
			// [0] : vertex posision index { x: tri0(i0>>0 | i1>>8 | i2>>16)  y: tri1  z: tri2  w: tri3 }
			// [1] : vertex normal index { x: (i0>>0 | i1>>8 | i2>>16 | i3>>24)  y: i4|5|6|7  z:i8|9|10|11 }
            
            //uint4 cube_vtxs[ 12 ];
            // x: near vertex index (x>>0 | y>>8 | z>>16)
            // y: near vertex index offset prev (left >>0 | up  >>8 | front>>16)
            // z: near vertex index offset next (right>>0 | down>>8 | back >>16)
            // w: pos(x>>0 | y>>8 | z>>16)
            
            //uint3 grids[ 512 ][2];
			// [0] : position as float3
			// [1] : near grid id
			// { x: prev(left>>0 | up>>9 | front>>18)  y: next(right>>0 | down>>9 | back>>18)  z: current }
            
            this.Material.SetConstantBuffer( "normals", res.CubeIndexPatternBuffer );
            this.Material.SetConstantBuffer( "cube_patterns", res.CubeVertexPatternBuffer );
            this.Material.SetConstantBuffer( "cube_vtxs", res.CubeVertexBuffer );
            this.Material.SetConstantBuffer( "grids", res.GridBuffer );

        }



        unsafe void Awake()
        {
            this.gridPositions = new NativeList<float4>( this.maxDrawGridLength, Allocator.Persistent );
            this.nearGrids = new NativeList<int4>( this.maxDrawGridLength * 2, Allocator.Persistent );
            this.cubeInstances = new NativeList<CubeInstance>( 1000000, Allocator.Persistent );
            //this.cubeInstances = new NativeQueue<CubeInstance>( Allocator.Persistent );

            this.meshResources = new MeshResources( this.MarchingCubeAsset, this.maxDrawGridLength );
            var res = this.meshResources;

            this.setGridCubeIdShader.SetBuffer( 0, "src_instances", res.instancesBuffer );
            this.setGridCubeIdShader.SetTexture( 0, "dst_grid_cubeids", res.gridCubeIdBuffer );

            setResources2();


            var cb = createCommandBuffer( res, this.Material );
            Camera.main.AddCommandBuffer( CameraEvent.BeforeSkybox, cb );


            this.cubeGrids = new CubeGridArrayUnsafe(8, 3, 8 );
            this.cubeGrids.FillCubes( new int3( -1, 2, -1 ), new int3( 11, 11, 11 ), isFillAll: true );
            this.cubeGrids.FillCubes( new int3( 2, 1, 3 ), new int3( 1, 2, 1 ), isFillAll: true );
            
            var c = this.cubeGrids[ 0, 0, 0 ];           
            (*c.p)[ 1, 1, 1 ] = 1;
            c[ 31, 1, 1 ] = 1;
            c[ 31, 31, 31 ] = 1;
            c[ 1, 31, 1 ] = 1;
            for( var iy = 0; iy < 15; iy++ )
                for( var iz = 0; iz < 15; iz++ )
                    for( var ix = 0; ix < 13; ix++ )
                        c[ 5 + ix, 5 + iy, 5 + iz ] = 1;
            this.job = this.cubeGrids.BuildCubeInstanceData( this.gridPositions, this.nearGrids, this.cubeInstances );

            this.job.Complete();
            nearGrids.AsArray().ForEach( x => Debug.Log( x ) );

            res.instancesBuffer.SetData( this.cubeInstances.AsArray() );
            res.gridPositionBuffer.SetData( this.gridPositions.AsArray() );
            res.nearGridIdBuffer.SetData( this.nearGrids.AsArray() );
            var remain = ( 64 - ( this.cubeInstances.Length & 0x3f ) ) & 0x3f;
            for( var i = 0; i < remain; i++ ) this.cubeInstances.AddNoResize( new CubeInstance { instance = 1 } );
            this.setGridCubeIdShader.Dispatch( 0, this.cubeInstances.Length >> 6, 1, 1 );
            Debug.Log($"{cubeInstances.Length} / {res.instancesBuffer.count}");

            var glen = this.cubeGrids.GridLength;
            this.cubeGridColliders = new MeshCollider[ glen.x, glen.y, glen.z ];

            this.idxLists = this.MarchingCubeAsset.CubeIdAndVertexIndicesList.Select( x => x.vertexIndices ).ToArray();
            this.vtxList = this.MarchingCubeAsset.BaseVertexList.Select( x => new float3( x.x, x.y, x.z ) ).ToArray();
            //var idxLists = this.MarchingCubeAsset.CubeIdAndVertexIndicesList.Select( x => x.vertexIndices ).ToArray();
            //var vtxList = this.MarchingCubeAsset.BaseVertexList.Select( x => new float3( x.x, x.y, x.z ) ).ToArray();
            var q =
                from x in this.cubeInstances.ToArray()
                let gridId = CubeUtility.FromCubeInstance( x.instance ).gridId
                group x by gridId
                ;
            foreach( var cubeId in q )
            {
                var gridid = (int)cubeId.Key;
                var gridpos = this.gridPositions[ gridid ];
                var igrid = ((int4)gridpos >> 5) * new int4( 1, -1, -1, 0 );

                if( igrid.x < 0 || igrid.y < 0 || igrid.z < 0 ) continue;

                var collider = this.cubeGridColliders[ igrid.x, igrid.y, igrid.z ];
                this.cubeGridColliders[ igrid.x, igrid.y, igrid.z ] = this.BuildMeshCollider( gridpos.xyz, collider, cubeId );
            }
        }


        CommandBuffer createCommandBuffer( MeshResources res, Material mat )
        {
            var cb = new CommandBuffer();
            cb.name = "marching cubes drawer";

            cb.DispatchCompute( this.setGridCubeIdShader, 0, res.argsBufferForDispatch, 0 );

            cb.DrawMeshInstancedIndirect( res.mesh, 0, mat, 0, res.argsBufferForInstancing );

            return cb;
        }


        int[][] idxLists;
        float3[] vtxList;
        public MeshCollider BuildMeshCollider( float3 gridpos, MeshCollider mc, IEnumerable<CubeInstance> cubeInstances )
        {
            if( !cubeInstances.Any() ) return mc;

            var gridid = CubeUtility.FromCubeInstance( cubeInstances.First().instance ).gridId;

            if( mc == null )
            {
                var igrid = ( (int3)gridpos >> 5 ) * new int3( 1, -1, -1 );
                var go = new GameObject( $"grid {igrid.x} {igrid.y} {igrid.z}" );
                go.transform.position = gridpos;

                mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = new Mesh();
            }
            mc.enabled = false;

            var (i, v) = CubeUtility.MakeCollisionMeshData( cubeInstances, this.idxLists, this.vtxList );
            using( i )
            using( v )
            {
                var mesh = mc.sharedMesh;
                mesh.Clear();
                mesh.MarkDynamic();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices( v.AsArray() );
                mesh.SetIndices( i.AsArray(), MeshTopology.Triangles, 0 );

                mc.sharedMesh = mesh;
            }
            mc.enabled = true;
            return mc;
        }



        private void OnDestroy()
        {
            this.job.Complete();

            this.meshResources.Dispose();
            this.cubeGrids.Dispose();
            this.gridPositions.Dispose();
            this.cubeInstances.Dispose();
        }


        JobHandle job;

        private unsafe void Update()
        {
            var c = this.cubeGrids[ 5, 1, 3 ];
            c[ i, 0, 0 ] ^= 1;
            i = i + 1 & 31;
            this.gridPositions.Clear();
            this.nearGrids.Clear();
            this.cubeInstances.Clear();
            this.job = this.cubeGrids.BuildCubeInstanceData( this.gridPositions, this.nearGrids, this.cubeInstances );

        //}
        //private void LateUpdate()
        //{
            this.job.Complete();

            var res = this.meshResources;
            res.instancesBuffer.SetData( this.cubeInstances.AsArray() );
            res.gridPositionBuffer.SetData( this.gridPositions.AsArray() );
            res.nearGridIdBuffer.SetData( this.nearGrids.AsArray() );

            var remain = (64 - (this.cubeInstances.Length & 0x3f) ) & 0x3f;
            for(var i=0; i<remain; i++) this.cubeInstances.AddNoResize( new CubeInstance { instance = 1 } );
            var dargparams = new IndirectArgumentsForDispatch( this.cubeInstances.Length >> 6, 1, 1 );
            var dargs = res.argsBufferForDispatch;
            dargs.SetData( ref dargparams );
            //this.setGridCubeIdShader.Dispatch( 0, this.cubeInstances.Length >> 6, 1, 1 );

            var mesh = res.mesh;
            var mat = this.Material;
            var iargs = res.argsBufferForInstancing;

            var instanceCount = this.cubeInstances.Length;
            var iargparams = new IndirectArgumentsForInstancing( mesh, instanceCount );
            iargs.SetData( ref iargparams );

            //var bounds = new Bounds() { center = Vector3.zero, size = Vector3.one * 1000.0f };
            //Graphics.DrawMeshInstancedIndirect( mesh, 0, mat, bounds, iargs );

        }
        int i;


        
        ((int x, int y, int z) prev, (int x, int y, int z) next)[] near_cube_offsets =
        {
            (( 0, 0, -1), ( 0, -1, 0)),
            (( -1, 0, 0), ( 0, -1, 0)),
            (( +1, 0, 0), ( 0, -1, 0)),
            (( 0, 0, +1), ( 0, -1, 0)),

            (( -1, 0, 0), ( 0, 0, -1)),
            (( +1, 0, 0), ( 0, 0, -1)),
            (( -1, 0, 0), ( 0, 0, +1)),
            (( +1, 0, 0), ( 0, 0, +1)),

            (( 0, 0, -1), ( 0, +1, 0)),
            (( -1, 0, 0), ( 0, +1, 0)),
            (( +1, 0, 0), ( 0, +1, 0)),
            (( 0, 0, +1), ( 0, +1, 0)),
        };
        (int x, int y, int z)[] near_cube_ivtxs =
        {
            (3,8,11),
            (2,9,10),
            (1,10,9),
            (0,11,8),

            (5,6,7),
            (4,7,6),
            (7,4,5),
            (6,5,4),

            (11,0,3),
            (10,1,2),
            (9,2,1),
            (8,3,0),
        };




        struct MeshResources : System.IDisposable
        {
            public ComputeBuffer ArgsBufferForInstancing;
            public ComputeBuffer ArgsBufferForDispatch;

            public ComputeBuffer NormalBuffer;
            public ComputeBuffer CubePatternBuffer;
            public ComputeBuffer CubeVertexBuffer;
            public ComputeBuffer GridBuffer;
        
            public ComputeBuffer CubeInstancesBuffer;
            public RenderTexture GridCubeIdBuffer;
        
            public Mesh mesh;

            public MeshResources( MarchingCubeAsset asset, int maxGridLength ) : this()
            {
                this.ArgsBufferForInstancing = ComputeShaderUtility.CreateIndirectArgumentsBufferForInstancing();
                this.ArgsBufferForDispatch = ComputeShaderUtility.CreateIndirectArgumentsBufferForDispatch();

                this.CubeInstancesBuffer = createCubeIdInstancingShaderBuffer_( 32 * 32 * 32 * maxGridLength );
                this.GridCubeIdBuffer = createGridCubeIdShaderBuffer_( maxGridLength );

                this.NormalBuffer = createCubeIndexPatternShaderBuffer( 254 );
                this.CubePatternBuffer = createCubeVertexPatternShaderBuffer( 254 );
                this.CubeVertexBuffer = createVetexShaderBuffer();
                this.GridBuffer = createGridShaderBuffer_( 512 );

                this.mesh = createMesh_();
            }

            public void Dispose()
            {
                if( this.ArgsBufferForInstancing != null ) this.ArgsBufferForInstancing.Dispose();
                if( this.ArgsBufferForDispatch != null ) this.ArgsBufferForDispatch.Dispose();

                if( this.CubeInstancesBuffer != null ) this.CubeInstancesBuffer.Dispose();
                if( this.GridCubeIdBuffer != null ) this.GridCubeIdBuffer.Release();

                if( this.NormalBuffer != null ) this.NormalBuffer.Dispose();
                if( this.CubePatternBuffer != null ) this.CubePatternBuffer.Dispose();
                if( this.CubeVertexBuffer != null ) this.CubeVertexBuffer.Dispose();
                if( this.GridBuffer != null ) this.GridBuffer.Dispose();
            }

            ComputeBuffer createCubeIdInstancingShaderBuffer_( int maxUnitLength )
            {
                var buffer = new ComputeBuffer( maxUnitLength, Marshal.SizeOf<uint>() );

                return buffer;
            }

            RenderTexture createGridCubeIdShaderBuffer_( int maxGridLength )
            {
                var buffer = new RenderTexture( 32 * 32, 32, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt, 0 );
                buffer.enableRandomWrite = true;
                buffer.dimension = TextureDimension.Tex2DArray;
                buffer.volumeDepth = maxGridLength;
                buffer.Create();

                return buffer;
            }
            

            ComputeBuffer createNormalList_( MarchingCubeAsset.CubeWrapper[] cubeIdsAndVtxIndexLists_ )
            {
                var normalToIdDict = cubeIdsAndVtxIndexLists_
                    .SelectMany( x => x.normalsForVertex )
                    .Distinct( x => x )
                    .Select( ( x, i ) => (x, i) )
                    .ToDictionary( x => x.x, x => x.i );


                var buffer = new ComputeBuffer( normalToIdDict.Count, Marshal.SizeOf<Vector4>(), ComputeBufferType.Constant );

                buffer.SetData( normalToIdDict.Select( x => x.Key new Vector4(x.Key, ).ToArray() );

                return buffer;
            }

            ComputeBuffer createCubePatternBuffer_( MarchingCubeAsset.CubeWrapper[] cubeIdsAndVtxIndexLists_ )
            {
                var normalToIdDict = cubeIdsAndVtxIndexLists_
                    .SelectMany( x => x.normalsForVertex )
                    .Distinct( x => x )
                    .Select( ( x, i ) => (x, i) )
                    .ToDictionary( x => x.x, x => x.i );


                var buffer = new ComputeBuffer( 254, Marshal.SizeOf<uint4>() * 2, ComputeBufferType.Constant );

                buffer.SetData( normalToIdDict.Select( x => x.Key ).ToArray() );

                return buffer;
            }




            ComputeBuffer createCubeIndexPatternShaderBuffer( int cubePatternLength )
            {
                var buffer = new ComputeBuffer( cubePatternLength, Marshal.SizeOf<int>() * 3 * 4, ComputeBufferType.Constant );
            
                buffer.SetData( baseVtxList_.Select( v => new Vector4( v.x, v.y, v.z, 1.0f ) ).ToArray() );
                //buffer.SetData( baseVtxList_ );

                return buffer;
            }
            ComputeBuffer createCubeVertexPatternShaderBuffer( int cubePatternLength )
            {
                var buffer = new ComputeBuffer( cubePatternLength, Marshal.SizeOf<float4>() * 12, ComputeBufferType.Constant );

                buffer.SetData( baseVtxList_.Select( v => new Vector4( v.x, v.y, v.z, 1.0f ) ).ToArray() );
                //buffer.SetData( baseVtxList_ );

                return buffer;
            }
            ComputeBuffer createVetexShaderBuffer()
            {
                var buffer = new ComputeBuffer( 12, Marshal.SizeOf<float4>() + Marshal.SizeOf<int4>() * 3, ComputeBufferType.Constant );

                    var q =
                        from x in cubeIdsAndVtxIndexLists_
                            //.Prepend( (cubeId: (byte)0, vtxIdxs: new int[ 0 ]) )
                            //.Append( (cubeId: (byte)255, vtxIdxs: new int[ 0 ]) )
                        orderby x.cubeId
                        select x.vertexIndices.Concat( Enumerable.Repeat( 0, 12 - x.vertexIndices.Length ) )
                        ;
                    buffer.SetData( q.SelectMany( x => x ).Cast<int>().ToArray() );

                    return buffer;
            }
            ComputeBuffer createGridShaderBuffer_( int maxGridLength )
            {
                var buffer = new ComputeBuffer( maxGridLength, Marshal.SizeOf<float4>() + Marshal.SizeOf<int4>() * 2, ComputeBufferType.Constant );

                return buffer;
            }



            Mesh createMesh_()
            {
                var mesh_ = new Mesh();
                mesh_.name = "marching cube unit";

                var qVtx =
                    from i in Enumerable.Range( 0, 12 )
                    select new Vector3( i, i / 3, 0 )
                    ;
                var qIdx =
                    from i in Enumerable.Range( 0, 3 * 4 )
                    select i
                    ;
                mesh_.vertices = qVtx.ToArray();
                mesh_.triangles = qIdx.ToArray();

                return mesh_;
            }


            ComputeBuffer createGridPositionShaderBuffer_( int maxBufferLength )
            {
                var buffer = new ComputeBuffer( maxBufferLength, Marshal.SizeOf<float4>() );

                return buffer;
            }
            ComputeBuffer createNearGridShaderBuffer_( int maxBufferLength )
            {
                var buffer = new ComputeBuffer( maxBufferLength * 2, Marshal.SizeOf<int4>() );

                return buffer;
            }

            ComputeBuffer createIdxListsShaderBuffer_( MarchingCubeAsset.CubeWrapper[] cubeIdsAndVtxIndexLists_ )
            //ComputeBuffer createIdxListsShaderBuffer_( (byte cubeId, int[] vtxIdxs)[] cubeIdsAndVtxIndexLists_ )
            {
                var buffer = new ComputeBuffer( 254 * 12, Marshal.SizeOf<int>() );//, ComputeBufferType.Constant );

                var q =
                    from x in cubeIdsAndVtxIndexLists_
                        //.Prepend( (cubeId: (byte)0, vtxIdxs: new int[ 0 ]) )
                        //.Append( (cubeId: (byte)255, vtxIdxs: new int[ 0 ]) )
                    orderby x.cubeId
                    select x.vertexIndices.Concat( Enumerable.Repeat( 0, 12 - x.vertexIndices.Length ) )
                    ;
                buffer.SetData( q.SelectMany( x => x ).Cast<int>().ToArray() );

                return buffer;
            }

            ComputeBuffer createBaseVtxShaderBuffer_( Vector3[] baseVtxList_ )
            {
                var buffer = new ComputeBuffer( 12, Marshal.SizeOf<float4>(), ComputeBufferType.Constant );

                buffer.SetData( baseVtxList_.Select( v => new Vector4( v.x, v.y, v.z, 1.0f ) ).ToArray() );
                //buffer.SetData( baseVtxList_ );

                return buffer;
            }


            ComputeBuffer createCubeNormalShaderBuffer_( MarchingCubeAsset.CubeWrapper[] cubeIdAndVertexIndicesList )
            {
                var buffer = new ComputeBuffer( 256 * 12, Marshal.SizeOf<float3>() );

                buffer.SetData( cubeIdAndVertexIndicesList.SelectMany( x => x.normalsForVertex ).ToArray() );

                return buffer;
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

        static public ComputeBuffer SetData( this ComputeBuffer cbuf, ref IndirectArgumentsForDispatch args )
        {
            using( var nativebuf = args.ToNativeArray( Allocator.Temp ) )
                cbuf.SetData( nativebuf );

            return cbuf;
        }
    }

    static public class ComputeShaderUtility
    {
        static public ComputeBuffer CreateIndirectArgumentsBufferForInstancing() =>
            new ComputeBuffer( 1, sizeof( uint ) * 5, ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable );

        static public ComputeBuffer CreateIndirectArgumentsBufferForDispatch() =>
            new ComputeBuffer( 1, sizeof( int ) * 3, ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable );
        
        static public void SetConstantBuffer( this Material mat, string name, ComputeBuffer buffer ) =>
            mat.SetConstantBuffer( name, buffer, 0, buffer.stride * buffer.count );
    }



    public struct IndirectArgumentsForDispatch
    {
        public int x, y, z;

        public IndirectArgumentsForDispatch( int numx, int numy, int numz )
        {
            this.x = numx;
            this.y = numy;
            this.z = numz;
        }

        public NativeArray<int> ToNativeArray( Allocator allocator )
        {
            var arr = new NativeArray<int>( 3, allocator );
            arr[ 0 ] = this.x;
            arr[ 1 ] = this.y;
            arr[ 2 ] = this.z;
            return arr;
        }
    }


}

