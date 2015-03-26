using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;
using ClipperLib;
using PolygonizerLib;

namespace Demo {
	using Path = List<IntPoint>;
	using Paths = List<List<IntPoint>>;

	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(PolygonCollider2D))]
	public class TerrainController : MonoBehaviour {

		const int MAP_SCALE_INV = 10;
		const float MAP_SCALE = 0.1f;

		public Texture2D texture;
		private Paths polygons;
		private bool dirty = false;
		private PolygonCollider2D polygonCollider;

		public void Awake() {
			this.polygons = Polygonizer.FromTexture(texture);
			this.polygonCollider = GetComponent<PolygonCollider2D>();

			foreach(List<IntPoint> li in this.polygons)
				for(int i=0; i< li.Count; i++)
					li[i] = new IntPoint(li[i].X * MAP_SCALE_INV, li[i].Y * MAP_SCALE_INV);

			dirty = true;
		}

		void Start(){

		}

		void Update(){
			if(dirty){
				dirty = false;
				UpdateMap();
			}
		}

		public void Clip(Vector2 center, float radius){
			center.x *= MAP_SCALE_INV;
			center.y *= MAP_SCALE_INV;
			radius *= MAP_SCALE_INV;
			Path clip = new Path();
			for (int th=0; th < 360; th+=20){
				clip.Add(new IntPoint((int)(center.x + radius*Mathf.Cos(th*Mathf.Deg2Rad)),
				                      (int)(center.y + radius*Mathf.Sin(th*Mathf.Deg2Rad))));
			}

			Clipper c = new Clipper();
			c.AddPaths(this.polygons, PolyType.ptSubject, true);
			c.AddPath(clip, PolyType.ptClip, true);
			c.Execute(ClipType.ctDifference, polygons);
			dirty = true;
		}

		/// <summary>
		/// Update mesh according to internally stored polygons
		/// </summary>
		void UpdateMap(){
			polygonCollider.pathCount = this.polygons.Count;

			Mesh mesh = GetComponent<MeshFilter>().mesh;
			mesh.Clear();
			#region Triangulation
			List<IntPolygon> polyList = new List<IntPolygon>();
			int indexCount=0;
			foreach(Path p in this.polygons){
				indexCount += p.Count;
				polyList.Add(new IntPolygon(IntPolygon.toPolygonPoints(p)));
				P2T.Triangulate(polyList[polyList.Count-1]);
			}
			#endregion

			#region vertices and triangles
			Vector3[] vertices = new Vector3[indexCount];
			int[][] triangless = new int[polyList.Count][];
			int indexOffset=0;
			//UnityEngine.Debug.Log("polygon count = " + polyList.Count);

			// join vertices to form an array of vertices
			for(int i=0; i<polyList.Count; i++){
				polyList[i].getVerticesVector3(MAP_SCALE).CopyTo(vertices, indexOffset);
				// also set collider paths (Vector3 implicitly converts to Vector2)
				polygonCollider.SetPath(i, polyList[i].getVerticesVector2(MAP_SCALE));
				// retrieve triangles for each polygons
				triangless[i] = polyList[i].getTriangles(indexOffset);
				indexOffset += this.polygons[i].Count;
			}
			mesh.vertices = vertices;

			// count the total number of triangles
			int triangleCount = 0;
			foreach(int[] tri in triangless){
				triangleCount += tri.Length;
			}
			// join triangles to form an array of triangles
			int triangleOffset = 0;
			int[] triangles = new int[triangleCount];
			for(int i=0; i<polyList.Count; i++){
				triangless[i].CopyTo(triangles, triangleOffset);
				triangleOffset += triangless[i].Length;
			}

			mesh.triangles = triangles;
			#endregion

			#region UVs
			Vector2[] uv = new Vector2[mesh.vertices.Length];
			for(int i=0; i< mesh.vertices.Length; i++){
				uv[i].Set(mesh.vertices[i].x, mesh.vertices[i].y);
			}
			mesh.uv = uv;
			#endregion

			Vector3[] normals = new Vector3[indexCount];
			for(int i=0; i<normals.Length; i++){
				normals[i] = Vector3.back;
			}
			mesh.normals = normals;

			mesh.Optimize();
		}
	}
}