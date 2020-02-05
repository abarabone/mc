using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace mc
{
    static public class MarchingCubesDataBuilder
    {

        static public ((byte cubeId, int[] indices)[] cubesAndIndexLists, Vector3[] baseVtxList)
        ConvertObjectDataToMachingCubesData( (string name, Vector3[] vtxs, int[][] tris)[] objectsData )
        {

            var baseVtxList = makeBaseVtxList_();
            var baseVtxIndexBySbvtxDict = makeBaseVtxIndexBySbvtxDict_( baseVtxList );

            var prototypeCubes = makePrototypeCubes_( objectsData );
            var cube254Pattarns = makeCube254Pattarns_( prototypeCubes );
            var triVtxLists = transformSbvtxs_( cube254Pattarns, prototypeCubes );

            var triIdxLists = makeVtxIndexListsPerCube_( triVtxLists, baseVtxIndexBySbvtxDict );

            return (triIdxLists, baseVtxList);


            (byte id, (Vector3 v0, Vector3 v1, Vector3 v2)[] trivtxs)[] makePrototypeCubes_
                ( (string name, Vector3[] vtxs, int[][] tris)[] objectsData_ )
            {
                var qExtractedData =
                    from obj in objectsData_
                    where obj.name.Length == 8 + 1
                    where obj.name[ 4 ] == '_'
                    let n = obj.name.Replace( "_", "" )
                    select (cubeId: Convert.ToByte( n, 2 ), obj.vtxs, obj.tris)
                    ;
                var extractedData = qExtractedData.ToArray();


                var qTriVtx =
                    from cube in extractedData
                    select
                        from tri in cube.tris
                        select (v0: cube.vtxs[ tri[ 0 ] ], v1: cube.vtxs[ tri[ 1 ] ], v2: cube.vtxs[ tri[ 2 ] ])
                    ;
                var qId =
                    from cube in extractedData
                    select cube.cubeId
                    ;

                var qVtxAndId =
                    from x in Enumerable.Zip( qId, qTriVtx, ( l, r ) => (id: l, trivtx: r) )
                    select (x.id, trivtxs: x.trivtx.ToArray())
                    ;
                var vtxsAndIds = qVtxAndId.ToArray();


                // 確認
                foreach( var x in qExtractedData )
                {
                    Debug.Log( $"{Convert.ToString( x.cubeId, 2 ).PadLeft( 8, '0' )}" );
                }

                return vtxsAndIds;
            }

            CubePattarn[] makeCube254Pattarns_
                ( IEnumerable<(byte id, (Vector3 v0, Vector3 v1, Vector3 v2)[])> prototypeCubes_ )
            {
                var qPrototypeId =
                    from cube in prototypeCubes_
                    select new CubePattarn( cube.id )
                    ;
                var prototypeId = qPrototypeId.ToArray();

                var stds = prototypeId;
                var revs = qReverseId_( prototypeId );//.ToArray();
                //var flips = qFlipId_X_( prototypeId );//.ToArray();//
                var rotstds = rot_( stds );
                var rotrevs = rot_( revs );
                //var rotFlips = rot_( flips );//
                var qId =
                    from x in rotstds.Concat( rotrevs )//.Concat( rotFlips )
                group x by (x.id, x.primaryId) into g
                    orderby g.Key
                    select g
                    ;
                var idsAndPattarns = qId.Select( x => x.First() ).ToArray();

                // 確認
                using( var f = new StreamWriter( @"C:\Users\abarabone\Desktop\mydata\mc.txt" ) )
                {
                    var idGroups = qId.ToArray();

                    f.WriteLine( idGroups.Length );
                    var ss =
                        from g in idGroups
                        where g.Key.primaryId == 240//
                        //from p in g
                        let p = g.First()
                        let id = Convert.ToString( p.id, 2 ).PadLeft( 8, '0' )
                        let primaryId = Convert.ToString( p.primaryId, 2 ).PadLeft( 8, '0' )
                        select (id, primaryId, p.dir, p.up)
                        ;
                    var s = string.Join( "\r\n", ss );
                    f.WriteLine( s );
                }

                return idsAndPattarns;


                // 左ねじの回転方向とする
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

                IEnumerable<CubePattarn> qFlipId_X_( IEnumerable<CubePattarn> src ) =>
                    from x in src
                    let l = x.id & 0b_0101_0101
                    let r = x.id & 0b_1010_1010
                    select new CubePattarn( x, (byte)( ( l << 1 ) | ( r >> 1 ) ) ).FlipX()
                    ;
                IEnumerable<CubePattarn> qFlipId_Y_( IEnumerable<CubePattarn> src ) =>
                    from x in src
                    let u = x.id & 0b_0000_1111
                    let d = x.id & 0b_1111_0000
                    select new CubePattarn( x, (byte)( ( u << 4 ) | ( d >> 4 ) ) ).FlipY()
                    ;
                IEnumerable<CubePattarn> qFlipId_Z_( IEnumerable<CubePattarn> src ) =>
                    from x in src
                    let f = x.id & 0b_0011_0011
                    let b = x.id & 0b_1100_1100
                    select new CubePattarn( x, (byte)( ( f << 2 ) | ( b >> 2 ) ) ).FlipZ()
                    ;

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
            }


            IEnumerable<(byte cubeId, IEnumerable<(sbyte x, sbyte y, sbyte z)[]> triVtxs)> transformSbvtxs_(
                IEnumerable<CubePattarn> cubePattarns,
                IEnumerable<(byte id, (Vector3 v0, Vector3 v1, Vector3 v2)[] trivtxs)> prototypeCubes_
            )
            {

                var q =
                    from pat in cubePattarns
                    join proto in prototypeCubes_ on pat.primaryId equals proto.id
                    select
                        from trivtx in proto.trivtxs
                        select new[]
                        {
                            transform_( pat, trivtx.v0 ),
                            transform_( pat, trivtx.v1 ),
                            transform_( pat, trivtx.v2 ),
                        }
                    ;
                return Enumerable.Zip( cubePattarns, q, ( l, r ) => (l.id, l.isReverseTriangle ? r.Reverse() : r) );

                (sbyte x, sbyte y, sbyte z) transform_( CubePattarn cube, Vector3 protoVtx )
                {
                    var vtx = (x: math.sign( protoVtx.x ), y: math.sign( protoVtx.y ), z: math.sign( protoVtx.z ));
                    var fwd = cube.dir;
                    var up = cube.up;
                    var side = (
                        x: - fwd.y * up.z + fwd.z * up.y, 
                        y: - fwd.z * up.x + fwd.x * up.z, 
                        z: - fwd.x * up.y + fwd.y * up.x
                    );
                    var x = vtx.x * side.x + vtx.y * side.y + vtx.z * side.z;
                    var y = vtx.x * up.x + vtx.y * up.y + vtx.z * up.z;
                    var z = vtx.x * fwd.x + vtx.y * fwd.y + vtx.z * fwd.z;
                    return ((sbyte)x, (sbyte)y, (sbyte)z);
                }
            }



            Vector3[] makeBaseVtxList_()
            {
                return new Vector3[]
                {
                new Vector3(0, 1, 1) * 0.5f,
                new Vector3(-1, 1, 0) * 0.5f,
                new Vector3(1, 1, 0) * 0.5f,
                new Vector3(0, 1, -1) * 0.5f,

                new Vector3(-1, 0, 1) * 0.5f,
                new Vector3(1, 0, 1) * 0.5f,
                new Vector3(-1, 0, -1) * 0.5f,
                new Vector3(1, 0, -1) * 0.5f,

                new Vector3(0, -1, 1) * 0.5f,
                new Vector3(-1, -1, 0) * 0.5f,
                new Vector3(1, -1, 0) * 0.5f,
                new Vector3(0, -1, -1) * 0.5f,
                };
            }

            Dictionary<(sbyte x, sbyte y, sbyte z), int>
                makeBaseVtxIndexBySbvtxDict_( IEnumerable<Vector3> baseVtxList_ )
            {
                var dict = baseVtxList_
                    .Select( ( x, i ) => (sbvtx: ((sbyte)math.sign( x.x ), (sbyte)math.sign( x.y ), (sbyte)math.sign( x.z )), i) )
                    .ToDictionary( x => x.sbvtx, x => x.i )
                    ;
                return dict;
            }


            (byte cubeId, int[] indices)[] makeVtxIndexListsPerCube_(
                IEnumerable<(byte cubeId, IEnumerable<(sbyte x, sbyte y, sbyte z)[]> triVtxs)> cubeIdsAndVtxLists_,
                Dictionary<(sbyte x, sbyte y, sbyte z), int> baseVtxIndexBySbvtxDict_
            )
            {
                var q =
                    from cube in cubeIdsAndVtxLists_
                    select
                        from triVtx in cube.triVtxs
                        from vtx in triVtx
                        select baseVtxIndexBySbvtxDict_[ vtx ]
                    ;
                return Enumerable.Zip( cubeIdsAndVtxLists_, q, ( l, r ) => (l.cubeId, indices: r.ToArray()) )
                    .ToArray()
                    ;
            }
        }




        struct CubePattarn
        {
            public byte primaryId;
            public byte id;
            public bool isReverseTriangle;
            public (sbyte x, sbyte y, sbyte z) dir;
            public (sbyte x, sbyte y, sbyte z) up;
            public CubePattarn( byte id )
            {
                this.primaryId = id;
                this.id = id;
                this.isReverseTriangle = false;
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
                this.up = (this.up.x, this.up.z, (sbyte)-this.up.y);
                return this;
            }
            public CubePattarn RotY()
            {
                this.dir = ((sbyte)-this.dir.z, this.dir.y, this.dir.x);
                this.up = ((sbyte)-this.up.z, this.up.y, this.up.x);
                return this;
            }
            public CubePattarn RotZ()
            {
                this.dir = (this.dir.y, (sbyte)-this.dir.x, this.dir.z);
                this.up = (this.up.y, (sbyte)-this.up.x, this.up.z);
                return this;
            }
            public CubePattarn FlipX()
            {
                this.dir.x = (sbyte)-this.dir.x;
                this.up.x = (sbyte)-this.up.x;
                this.isReverseTriangle ^= true;
                return this;
            }
            public CubePattarn FlipY()
            {
                this.dir.y = (sbyte)-this.dir.y;
                this.up.y = (sbyte)-this.up.y;
                this.isReverseTriangle ^= true;
                return this;
            }
            public CubePattarn FlipZ()
            {
                this.dir.z = (sbyte)-this.dir.z;
                this.up.z = (sbyte)-this.up.z;
                this.isReverseTriangle ^= true;
                return this;
            }
            public CubePattarn Reverse()
            {
                this.dir = ((sbyte)-this.dir.x, (sbyte)-this.dir.y, (sbyte)-this.dir.z);
                this.up = ((sbyte)-this.up.x, (sbyte)-this.up.y, (sbyte)-this.up.z);
                this.isReverseTriangle ^= true;
                return this;
            }
        }

    }
}
