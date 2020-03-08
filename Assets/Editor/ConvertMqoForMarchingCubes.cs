using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

using MqoUtility;

namespace MarchingCubes
{
    static public class ConvertMqoForMarchingCubes
    {

        [MenuItem( "Assets/Convert Mqo To Meshes" )]
        static public void CreateMesh()
        {
            if( Selection.objects == null ) return;

            var qMqoPath = Selection.objects
                .Select( o => AssetDatabase.GetAssetPath( o ) )
                .Where( path => Path.GetExtension( path ) == ".mqo" )
                ;
            foreach( var path in qMqoPath )
            {
                using( var f = new StreamReader( path ) )
                {
                    var s = f.ReadToEnd();
                    var data = MqoParser.ConvertToObjectsData( s );
                    var meshes = createMesh( data );

                    foreach( var m in meshes )
                    {
                        var folderPath = Path.GetDirectoryName( path );
                        AssetDatabase.CreateAsset( m, $"{folderPath}/{m.name}.asset" );
                    }

                    AssetDatabase.Refresh();
                }
            }
        }

        [MenuItem( "Assets/Convert Mqo For Marching Cubes" )]
        static public void CreateMachingCubesAsset()
        {
            if( Selection.objects == null ) return;

            var qMqoPath = Selection.objects
                .Select( o => AssetDatabase.GetAssetPath( o ) )
                .Where( path => Path.GetExtension( path ) == ".mqo" )
                ;

            var mqopath = qMqoPath.First();

            using( var f = new StreamReader( mqopath ) )
            {
                var s = f.ReadToEnd();
                var objdata = MqoParser.ConvertToObjectsData( s );
                var baseVtxList = MarchingCubesDataBuilder.MakeBaseVtxList();
                var cubesAndIndexLists =
                    MarchingCubesDataBuilder.ConvertObjectDataToMachingCubesData( objdata, baseVtxList );

                save_( Selection.objects, cubesAndIndexLists, baseVtxList );
            }

            return;


            void save_(
                UnityEngine.Object[] selectedObjects,
                (byte cubeId, int[] indices)[] cubeIdsAndIndexLists,
                Vector3[] baseVertexList
            )
            {

                // 渡されたアセットと同じ場所のパス生成

                var srcFilePath = AssetDatabase.GetAssetPath( selectedObjects.First() );

                var folderPath = Path.GetDirectoryName( srcFilePath );

                var fileName = Path.GetFileNameWithoutExtension( srcFilePath );

                var dstFilePath = folderPath + $"/Marching Cubes Resource.asset";


                // アセットとして生成
                var asset = ScriptableObject.CreateInstance<MarchingCubeAsset>();
                var qCubeIndexLists =
                    from x in cubeIdsAndIndexLists
                    select new MarchingCubeAsset.CubeWrapper { cubeId = x.cubeId, vertexIndices = x.indices }
                    ;
                asset.CubeIdAndVertexIndicesList = qCubeIndexLists.ToArray();
                asset.BaseVertexList = baseVertexList;
                AssetDatabase.CreateAsset( asset, dstFilePath );
                AssetDatabase.Refresh();
            }
        }





        static Mesh[] createMesh( (string name, Vector3[] vtxs, int[][] tris)[] objectsData )
        {

            return objectsData
                .Select( x => createMesh_( x.name, x.vtxs, x.tris ) )
                .ToArray();


            Mesh createMesh_( string name, Vector3[] vtxs, int[][] tris )
            {
                var mesh = new Mesh();
                mesh.name = name;
                mesh.vertices = vtxs;
                mesh.triangles = tris.SelectMany( x => x ).ToArray();

                return mesh;
            }
        }


        static (byte cubeId, Vector3[] normals)[] calculateNormals
            ( (byte cubeId, int[] indices)[] cubeIdsAndIndexLists, Vector3[] baseVertexList )
        {

            var qNormalPerTriangleInCube =
                from x in cubeIdsAndIndexLists
                select
                    from tri in x.indices.Buffer( 3 )
                    let v0 = baseVertexList[ tri[ 0 ] ]
                    let v1 = baseVertexList[ tri[ 1 ] ]
                    let v2 = baseVertexList[ tri[ 2 ] ]
                    select Vector3.Cross( ( v1 - v0 ), ( v2 - v0 ) )
                ;

            return Enumerable.Zip( cubeIdsAndIndexLists, qNormalPerTriangleInCube, ( x, y ) => (x.cubeId, normals: y) )
                .Select( x => (x.cubeId, x.normals.ToArray()) )
                .ToArray();
        }


    }

}
