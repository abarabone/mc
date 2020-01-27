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
            from x in qFloatEx.Token()
            from y in qFloatEx.Token()
            from z in qFloatEx.Token()
            select new Vector3( -x/x, y/y, z/z )
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


    struct CubePattarn
    {
        public byte primaryId;
        public byte id;
        public (sbyte x, sbyte y, sbyte z) dir;
        public (sbyte x, sbyte y, sbyte z) up;
        public CubePattarn( byte id )
        {
            this.primaryId = id;
            this.id = id;
            this.dir = (0, 0, 1);
            this.up = (0, 1, 0);
        }
        public CubePattarn( CubePattarn src, byte id )
        {
            this = src;
            this.id = id;
        }
        public CubePattarn RotX()
        {
            this.dir = (this.dir.x, this.dir.z, (sbyte)-this.dir.y);
            this.up  = (this.up.x,  this.up.z,  (sbyte)-this.up.y );
            return this;
        }
        public CubePattarn RotY()
        {
            this.dir = ((sbyte)-this.dir.z, this.dir.y, this.dir.x);
            this.up  = ((sbyte)-this.up.z,  this.up.y,  this.up.x );
            return this;
        }
        public CubePattarn RotZ()
        {
            this.dir = (this.dir.y, (sbyte)-this.dir.x, this.dir.z);
            this.up  = (this.up.y,  (sbyte)-this.up.x,  this.up.z );
            return this;
        }
        public CubePattarn Reverse()
        {
            this.dir = ((sbyte)-this.dir.x, (sbyte)-this.dir.y, (sbyte)-this.dir.z);
            this.up  = ((sbyte)-this.up.x,  (sbyte)-this.up.y,  (sbyte)-this.up.z );
            return this;
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

        (sbyte p0, sbyte p1, sbyte p2) toSbyte_( Vector3 vec ) =>
            ((sbyte)vec.x, (sbyte)vec.y, (sbyte)vec.z);

        var vtxsDistinct = qData.SelectMany( x => x.vtxs ).Distinct().ToArray();
        var vtxIdDictByVector3 = vtxsDistinct.Select( ( v, i ) => (v, i) ).ToDictionary( x => x.i, x => x.v );
        Debug.Log( vtxsDistinct.Length );

        var qStandard =
            from cube in qData
            select
                from tri in cube.tris
                select new[]
                {
                    toSbyte_(cube.vtxs[tri[0]]),
                    toSbyte_(cube.vtxs[tri[1]]),
                    toSbyte_(cube.vtxs[tri[2]]),
                };
        var qStandardId =
            from cube in qData
            select new CubePattarn( cube.cubeId )
            ;

        // 右ねじの回転方向とする
        IEnumerable<CubePattarn> qRotId_AxisZ_( IEnumerable<CubePattarn> src ) =>
            from x in src
            let z0 = x.id & 0b_0000_0101
            let z1 = x.id & 0b_0101_0000
            let z2 = x.id & 0b_1010_0000
            let z3 = x.id & 0b_0000_1010
            select new CubePattarn( x, (byte)( z0 << 4 | z1 << 1 | z2 >> 4 | z3 >> 1 ) ).RotZ()
            ;
        IEnumerable<CubePattarn> qRotId_AxisX_( IEnumerable<CubePattarn> src ) =>
            from x in src
            let x0 = x.id & 0b_0000_0011
            let x1 = x.id & 0b_0011_0000
            let x2 = x.id & 0b_1100_0000
            let x3 = x.id & 0b_0000_1100
            select new CubePattarn( x, (byte)( x0 << 4 | x1 << 2 | x2 >> 4 | x3 >> 2 ) ).RotX()
            ;
        IEnumerable<CubePattarn> qRotId_AxisY_( IEnumerable<CubePattarn> src ) =>
            from x in src
            let y0 = x.id & 0b_0001_0001
            let y1 = x.id & 0b_0010_0010
            let y2 = x.id & 0b_1000_1000
            let y3 = x.id & 0b_0100_0100
            select new CubePattarn( x, (byte)( y0 << 1 | y1 << 2 | y2 >> 1 | y3 >> 2 ) ).RotY()
            ;

        //IEnumerable<CubePattarn> qFlipId_X_( IEnumerable<CubePattarn> src ) =>
        //    from x in src
        //    let l = x.id & 0b_0101_0101
        //    let r = x.id & 0b_1010_1010
        //    select new CubePattarn( x, (byte)( ( l << 1 ) | ( r >> 1 ) ), false, true )
        //    ;
        //IEnumerable<CubePattarn> qFlipId_Y_( IEnumerable<CubePattarn> src ) =>
        //    from x in src
        //    let u = x.id & 0b_0000_1111
        //    let d = x.id & 0b_1111_0000
        //    select new CubePattarn( x, (byte)( ( u << 4 ) | ( d >> 4 ) ), false, true )
        //    ;
        //IEnumerable<CubePattarn> qFlipId_Z_( IEnumerable<CubePattarn> src ) =>
        //    from x in src
        //    let f = x.id & 0b_0011_0011
        //    let b = x.id & 0b_1100_1100
        //    select new CubePattarn( x, (byte)( ( f << 2 ) | ( b >> 2 ) ), false, true )
        //    ;

        IEnumerable<CubePattarn> qReverseId_( IEnumerable<CubePattarn> src ) =>
            from x in src
            select new CubePattarn( x, (byte)( x.id ^ 0b_1111_1111 ) ).Reverse()
            ;

        CubePattarn[] rot_( IEnumerable<CubePattarn> src )
        {
            var rotx0 = qRotId_AxisX_( src ).ToArray();
            var rotx1 = qRotId_AxisX_( rotx0 ).ToArray();
            var rotx2 = qRotId_AxisX_( rotx1 ).ToArray();
            var rotx = src.Concat( rotx0 ).Concat( rotx1 ).Concat( rotx2 );//.ToArray();

            var roty0 = qRotId_AxisY_( rotx ).ToArray();
            var roty1 = qRotId_AxisY_( roty0 ).ToArray();
            var roty2 = qRotId_AxisY_( roty1 ).ToArray();
            var rotxy = rotx.Concat( roty0 ).Concat( roty1 ).Concat( roty2 );//.ToArray();

            var rotz0 = qRotId_AxisZ_( rotxy ).ToArray();
            var rotz1 = qRotId_AxisZ_( rotz0 ).ToArray();
            var rotz2 = qRotId_AxisZ_( rotz1 ).ToArray();
            var rotxyz = rotxy.Concat( rotz0 ).Concat( rotz1 ).Concat( rotz2 );//.ToArray();

            return rotxyz.ToArray();
        }

        var standardId = qStandardId.ToArray();//.Concat( Enumerable.Repeat((byte)0, 1) ).ToArray();

        var stds = standardId;
        //var flips = qFlipId_X_( standardId );//.ToArray();
        var revs = qReverseId_( standardId );//.ToArray();
        var rotstds = rot_( stds );
        //var rotFlips = rot_( flips );
        var rotrevs = rot_( revs );
        var qId =
            from x in rotstds.Concat( rotrevs )//.Concat( rotFlips )
            group x by (x.id, x.primaryId) into g
            orderby g.Key
            select g
            ;
        var idGroups = qId.ToArray();

        using( var f = new StreamWriter( @"C:\Users\abarabone\Desktop\mydata\mc.txt" ) )
        {
            f.WriteLine( idGroups.Length );
            var ss =
                from g in idGroups
                from p in g
                let id = Convert.ToString( p.id, 2 ).PadLeft( 8, '0' )
                let primaryId = Convert.ToString( p.primaryId, 2 ).PadLeft( 8, '0' )
                select (id, primaryId, p.dir, p.up)
                ;
            var s = string.Join( "\r\n", ss );
            f.WriteLine( s );
        }

        var expandPattern256 = qData
            ;

    }

}