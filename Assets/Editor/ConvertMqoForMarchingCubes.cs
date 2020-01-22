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

        var qMqo =
        Selection.objects
            .Select( o => AssetDatabase.GetAssetPath( o ) )
            .Where( path => Path.GetExtension( path ) == ".mqo" )
            .Select( path => new StreamReader( path ) )
            ;
        foreach( var f in qMqo )
        {
            using( f )
            {
                var s = f.ReadToEnd();
                convertToMesh_( s );
            }
        }

        return;


        void convertToMesh_( string s )
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
                from _st in qSkipUntil("{")
                from vtx in qTagContent("vertex" )
                from face in qTagContent( "face" )
                from _ed in qSkipUntil( "}" )
                select (vtx, face)
                ;
            var qObject =
                from _ in qSkipUntil("Object")
                from obj_name in qTagName( "Object" )
                from content in qObjectContent
                select (obj_name, content.vtx, content.face)
                ;
            var qAllObjects =
                from objects in qObject.Many()
                select objects
                ;
            var objectTexts = qAllObjects.Parse( s );

            var qVtx =
                from v0 in Parse.Number.Token()
                from v1 in Parse.Number.Token()
                from v2 in Parse.Number.Token()
                select (v0: int.Parse( v0 ), v1: int.Parse( v1 ), v2: int.Parse( v2 ))
                ;
            var qTri =
                from corner_count in Parse.Numeric.Token()

                let faceText = corner_count == 3
                    ? Parse.Numeric.Or( Parse.WhiteSpace ).Many().Contained( Parse.String( "V(" ), Parse.Char(')') ).Text()
                    : Parse.Return("")
                from f0 in 
                select faceText

            foreach( var txt in objectTexts )
            {
                Debug.Log( $"{txt.obj_name} {txt.vtx} {txt.face}" );
            }
        }
    }
}