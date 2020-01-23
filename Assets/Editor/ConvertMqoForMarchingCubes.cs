using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sprache;

public class ConvertMqoForMarchingCubes : MonoBehaviour
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
                var data = convertToObjectsData( s );
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
    static public void ConvertMqoToMarchingCubesData()
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
                var data = convertToObjectsData( s );

                aaa( data );
            }
        }
    }



    static (string name, Vector3[] vtx, int[][] tris)[] convertToObjectsData( string s )
    {

        var qNotQuote =
            from text in Parse.AnyChar.Except( Parse.Char( '"' ) ).Many().Text()
            select text
            ;
        Func<string, Parser<string>> qSkipUntil = target =>
            from _ in Parse.AnyChar.Except( Parse.String( target ) ).Many().Text()
            select _
            ;

        Func<string, Parser<string>> qTagName = tag =>
            from _ in qSkipUntil( tag )
            from obj in Parse.String( tag )
            from tag_name in qNotQuote.Contained( Parse.Char( '"' ), Parse.Char( '"' ) ).Token()
            select tag_name
            ;
        Func<string, Parser<string>> qTagContent = tag =>
            from _ in qSkipUntil( tag )
            from name in Parse.String( tag )
            from __ in qSkipUntil( "{" )
            from content in Parse.CharExcept( '}' ).Many().Contained( Parse.Char( '{' ), Parse.Char( '}' ) ).Text()
            select content
            ;
        var qObjectContent =
            from _st in qSkipUntil( "{" )
            from vtx in qTagContent( "vertex" )
            from face in qTagContent( "face" )
            from _ed in qSkipUntil( "}" )
            select (vtx, face)
            ;
        var qObject =
            from _ in qSkipUntil( "Object" )
            from obj_name in qTagName( "Object" )
            from content in qObjectContent
            select (obj_name, content.vtx, content.face)
            ;
        var qAllObjects =
            from objects in qObject.Many()
            select objects
            ;
        var objectTexts = qAllObjects.Parse( s );


        var qExponent =
            from _ in Parse.Char( 'E' )
            from sign in Parse.Chars( "+-" )
            from num in Parse.Number
            select $"E{sign}{num}"//String.Format( "E{0}{1}", sign, num )
            ;
        var qFloatEx =
            from negative in Parse.Char( '-' ).Optional().Select( x => x.IsDefined ? x.Get().ToString() : "" )
            from num in Parse.Decimal
            from e in qExponent.Optional().Select( x => x.IsDefined ? x.Get() : "" )
            select Convert.ToSingle( negative + num + e )
            ;
        var qVtx =
            from v0 in qFloatEx.Token()
            from v1 in qFloatEx.Token()
            from v2 in qFloatEx.Token()
            select new Vector3( v0, v1, v2 )
            ;
        var qIdx =
            from corner_length in Parse.Decimal.Token().Select( x => int.Parse( x ) )
            from index_body in Parse.Numeric.Or( Parse.WhiteSpace ).Many().Contained( Parse.String( "V(" ), Parse.Char( ')' ) ).Token().Text()
            from _ in Parse.AnyChar.Until( Parse.LineEnd )
            select (corner_length, indices: index_body.Split( ' ' ).Select( x => int.Parse( x ) ).ToArray())
            ;

        var data = objectTexts
            .Select( x => toObjectsData_( x.obj_name, x.vtx, x.face ) )
            .ToArray();

        return data;


        (string name, Vector3[] vtxs, int[][] tris) toObjectsData_( string name, string vtx, string face )
        {
            //Debug.Log( $"{txt.obj_name} {txt.vtx} {txt.face}" );
            var vtxs = qVtx.Many().Parse( vtx );
            var tris = qIdx.Where( x => x.corner_length == 3 ).Select( x => x.indices ).Many().Parse( face );
            //foreach( var v in vtxs ) Debug.Log( $"{v[ 0 ]} {v[ 1 ]} {v[ 2 ]}" );
            //foreach( var t in tris ) Debug.Log( $"{t[ 0 ]} {t[ 1 ]} {t[ 2 ]}" );

            return (name, vtxs.ToArray(), tris.ToArray());
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

    static void aaa( (string name, Vector3[] vtxs, int[][] tris)[] objectsData )
    {

        var qData =
            from obj in objectsData
            where obj.name.Length == 8 + 1
            where obj.name[ 4 ] == '_'
            select (cubeId: makeCubeid_( obj.name ), obj.vtxs, obj.tris)
            ;
        byte makeCubeid_( string name )
        {
            var id =
                ( name[ 0 ] == '1' ? 1 : 0 ) << 7 |
                ( name[ 1 ] == '1' ? 1 : 0 ) << 6 |
                ( name[ 2 ] == '1' ? 1 : 0 ) << 5 |
                ( name[ 3 ] == '1' ? 1 : 0 ) << 4
                |
                ( name[ 5 ] == '1' ? 1 : 0 ) << 3 |
                ( name[ 6 ] == '1' ? 1 : 0 ) << 2 |
                ( name[ 7 ] == '1' ? 1 : 0 ) << 1 |
                ( name[ 8 ] == '1' ? 1 : 0 ) << 0
                ;
            return (byte)id;
        }
        foreach( var x in qData )
        {
            Debug.Log( $"{Convert.ToString(x.cubeId,2).PadLeft(8,'0')}" );
        }

        var vtxsDistinct = qData.SelectMany( x => x.vtxs ).Distinct().ToArray();
        var vtxIdDictByVector3 = vtxsDistinct.Select( ( v, i ) => (v, i) ).ToDictionary( x => x.i, x => x.v );
        Debug.Log( vtxsDistinct.Length );

        var qStandard =
            from cube in qData
            from tri in cube.tris
            select new[]
            {
                    cube.vtxs[tri[0]],
                    cube.vtxs[tri[1]],
                    cube.vtxs[tri[2]],
            };
        var standard = qStandard.ToArray();

        //var xflip =
        //    from cube in standard
        //    from vtri in cube
        //    select new[]
        //    {
        //        vtri * Vector3.right * -1
        //    };



        var expandPattern256 = qData
            ;

    }

}