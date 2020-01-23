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
    [MenuItem( "Assets/Convert Mqo For Marching Cubes" )]
    static public void Create()
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
                var meshes = convertToMeshes_( s );

                foreach( var m in meshes )
                {
                    var folderPath = Path.GetDirectoryName( path );
                    AssetDatabase.CreateAsset( m, $"{folderPath}/{m.name}.asset" );
                }

                AssetDatabase.Refresh();
            }
        }

        return;


        Mesh[] convertToMeshes_( string s )
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
                select new []{v0, v1, v2}
                ;
            var qIdx =
                from corner_length in Parse.Decimal.Token().Select( x => int.Parse( x ) )
                from index_body in Parse.Numeric.Or( Parse.WhiteSpace ).Many().Contained( Parse.String( "V(" ), Parse.Char( ')' ) ).Token().Text()
                from _ in Parse.AnyChar.Until( Parse.LineEnd )
                select (corner_length, indices:index_body.Split(' ').Select(x => int.Parse(x)).ToArray() )
                ;

            return objectTexts
                .Select( x => createMesh_( x.obj_name, x.vtx, x.face ) )
                .ToArray();

            
            Mesh createMesh_( string name, string vtx, string face )
            {
                //Debug.Log( $"{txt.obj_name} {txt.vtx} {txt.face}" );
                var vtxs = qVtx.Many().Parse( vtx );
                var tris = qIdx.Where( x => x.corner_length == 3 ).Select( x => x.indices ).Many().Parse( face );
                //foreach( var v in vtxs ) Debug.Log( $"{v[ 0 ]} {v[ 1 ]} {v[ 2 ]}" );
                //foreach( var t in tris ) Debug.Log( $"{t[ 0 ]} {t[ 1 ]} {t[ 2 ]}" );

                var mesh = new Mesh();
                mesh.name = name;
                mesh.vertices = vtxs.Select( vcs => new Vector3( vcs[ 0 ], vcs[ 1 ], vcs[ 2 ] ) ).ToArray();
                mesh.triangles = tris.SelectMany( x => x ).ToArray();

                return mesh;
            }
        }
    }
}