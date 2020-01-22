//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEngine;
//using UnityEditor;
//using Sprache;

//public class ConvertMqoForMarchingCubes : MonoBehaviour
//{
//    [MenuItem( "Assets/Convert Mqo For Marching Cubes" )]
//    static public void Create()
//    {
//        if( Selection.objects == null ) return;

//        var qMqo =
//        Selection.objects
//            .Select( o => AssetDatabase.GetAssetPath( o ) )
//            .Where( path => Path.GetExtension( path ) == ".mqo" )
//            .Select( path => new StreamReader(path) )
//            ;
//        foreach( var f in qMqo )
//        {
//            using( f )
//            {
//                var s = f.ReadToEnd();
//                convertToMesh_( s );
//            }
//        }

//        return;


//        void convertToMesh_( string s )
//        {
//            var qObjectName =
//                from obj in Parse.String( "Object" )
//                from ws0 in Parse.WhiteSpace.Many()
//                from d0 in Parse.Char( '"' )
//                from obj_name in Parse.AnyChar.Many().Text()
//                from d1 in Parse.Char( '"' )
//                from ws1 in Parse.WhiteSpace.Many()
//                from blacket in Parse.Char( '{' )
//                select obj_name
//                ;
//            var qObject =
//                from obj_name in qObjectName
//                from content in Parse.AnyChar.Many().Text()
//                select (obj_name, content)
//                ;
//            var q =
//                from content in qObject.DelimitedBy( qObjectName )
//                select content
//                ;
//            var txt = q.Parse( s );
//            Debug.Log( txt.First().obj_name + " " + txt.First().content );
//        }
//    }
//}