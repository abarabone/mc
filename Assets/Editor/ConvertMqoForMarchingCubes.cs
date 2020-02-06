using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

using mc;

static public class ConvertMqoForMarchingCubes
{

    //[MenuItem( "Assets/Convert Mqo For Marching Cubes" )]
    static public void CreateMesh()
    {
        if( Selection.objects == null ) return;

        var qMqoPath = Selection.objects
            .Select( o => AssetDatabase.GetAssetPath( o ) )
            .Where( path => Path.GetExtension( path ) == ".mqo" )
            ;
        foreach( var path in qMqoPath )
        {
            using( var f = new StreamReader(path) )
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

            save( Selection.objects, cubesAndIndexLists, baseVtxList );
        }
    }

    static void save(
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
        var qCubeIndexLists = from x in cubeIdsAndIndexLists
                select new MarchingCubeAsset.CubeWrapper { cubeId = x.cubeId, indices = x.indices }
                ;
        asset.CubeIndexLists = qCubeIndexLists.ToArray();
        asset.BaseVertexList = baseVertexList;
        AssetDatabase.CreateAsset( asset, dstFilePath );
        AssetDatabase.Refresh();
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

}
