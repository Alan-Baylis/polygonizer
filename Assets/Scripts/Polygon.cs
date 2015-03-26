using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Poly2Tri;
using ClipperLib;

namespace Demo
{
	/// <summary>
	/// A Polygon class compatible with Poly2Tri
	/// </summary>
	public class IntPolygon: Polygon
	{
		public IntPolygon(IList<PolygonPoint> points): base(points){
		}

		public IntPolygon(IEnumerable<PolygonPoint> points):base(points){
		}

		public void AddPoint(IntPoint p){
			PolygonPoint ppt = new PolygonPoint(p.X, p.Y);
			base.AddPoint(ppt);
		}

		/// <summary>
		/// [Unity] Gets the indices in the form of Vector3[].
		/// </summary>
		/// <returns>The vector3s.</returns>
		/// <param name="Zf">Zf.</param>
		public Vector3[] getVerticesVector3(float scale = 1f){
			Vector3[] points = new Vector3[_points.Count];

			if(scale == 1f)
				for(int i=0; i<_points.Count; i++)
					points[i] = new Vector3(_points[i].Xf, _points[i].Yf, 0f);
			else 
				for(int i=0; i<_points.Count; i++)
					points[i] = new Vector3(_points[i].Xf * scale, _points[i].Yf * scale, 0f);
			return points;
		}

		/// <summary>
		/// [Unity] Gets the indices in the form of Vector3[].
		/// </summary>
		/// <returns>The vector3s.</returns>
		/// <param name="Zf">Zf.</param>
		public Vector2[] getVerticesVector2(float scale = 1f){
			Vector2[] points = new Vector2[_points.Count];
			if(scale == 1f)
				for(int i=0; i<_points.Count; i++)
					points[i] = new Vector2(_points[i].Xf, _points[i].Yf);
			else 
				for(int i=0; i<_points.Count; i++)
					points[i] = new Vector2(_points[i].Xf * scale, _points[i].Yf * scale);
			return points;
		}

		/// <summary>
		/// [Unity] Gets the triangles.
		/// </summary>
		/// <returns>The triangles.</returns>
		/// <param name="cw">If set to <c>true</c> return triangles in clockwise order.</param>
		public int[] getTriangles(int offset, bool cw = true){
			IDictionary<TriangulationPoint, int> indices = getIndices();
			int[] triangles = new int[_triangles.Count*3];
			DelaunayTriangle t;
			TriangulationPoint p1, p2, p3;
			for(int i=0; i<_triangles.Count; i++){
				t = _triangles[i];
				p1 = t.Points._0;
				p2 = cw ? t.PointCWFrom(p1) : t.PointCCWFrom(p1);
				p3 = cw ? t.PointCWFrom(p2) : t.PointCCWFrom(p2);
				triangles[3*i] = indices[p1] + offset;
				triangles[3*i+1] = indices[p2] + offset;
				triangles[3*i+2] = indices[p3] + offset;
			}
			return triangles;
		}

		/// <summary>
		/// [Clipper] Gets the vertices in the form of IntPoint, imp
		/// </summary>
		/// <returns>The vertices.</returns>
		public IList<IntPoint> getVerticesIntPoint(){
			IList<IntPoint> points = new List<IntPoint>(_points.Count);
			for(int i=0; i<_points.Count; i++){
				points.Add(new IntPoint(_points[i].X, _points[i].Y));
			}
			return points;
		}

		/// <summary>
		/// [Clipper] Converts IntPoints to PolygonPoints to feed the constructor for this class.
		/// </summary>
		/// <returns>The polygon points.</returns>
		/// <param name="p">Point.</param>
		public static IList<PolygonPoint> toPolygonPoints(IList<IntPoint> p){
			IList<PolygonPoint> pts = new List<PolygonPoint>(p.Count);
			PolygonPoint curr;
			for(int i=0; i<p.Count; i++){
				curr = new PolygonPoint(p[i].X, p[i].Y);
				if(pts.Count != 0) {
					pts[pts.Count-1].Next = curr;
					curr.Previous = pts[pts.Count-1];
				}
				pts.Add(curr);
			}
			return pts;
		}

		/// <summary>
		/// [Unity] Gets a dictionary of point-index pair, 
		/// </summary>
		/// <returns>The dictionary</returns>
		/// <param name="p">Point</param>
		private IDictionary<TriangulationPoint, int> getIndices(){
			//Add an extra Dictionary for index lookup
			Dictionary<TriangulationPoint, int> d = new Dictionary<TriangulationPoint, int>(_points.Count);
			for(int i=0; i<_points.Count; i++){
				d.Add(_points[i], i);
			}
			return d;
		}
	}
}

