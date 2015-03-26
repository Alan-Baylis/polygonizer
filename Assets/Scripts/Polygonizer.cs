using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ClipperLib;

namespace PolygonizerLib
{
	using Path = List<IntPoint>;
	using Paths = List<List<IntPoint>>;

	/// <summary>
	/// A Polygonizer that produces polygons compatible with ClipperLib.
	/// </summary>
	public class Polygonizer
	{

		/// <summary>
		/// Returns a list of polygons parsed from a bitmap.
		/// </summary>
		/// <returns>The texture.</returns>
		/// <param name="t">T.</param>
		public static Paths FromTexture(Texture2D t){
			//UnityEngine.Debug.Log("Texture spec: "+t.width+", "+t.height);
			// texture -> bitmatrix -> a list of polygons
			BitMatrix bm = BitMatrix.fromTexture(t);
			return FromBitMatrix(bm);
		}

		private static BitMatrix DrawPoints(Path p, Rect r){
			// Current polygon must have at least one point
			if (p.Count == 0)
				return new BitMatrix(1, 1);
			BitMatrix bm = new BitMatrix((int)r.width, (int)r.height);

			for(int i=0; i<p.Count; i++){
				bm.Set(p[i].X - (int)r.xMin, p[i].Y - (int)r.yMin, true);
			}
			return bm;
		}

		private static Rect BoundBox(Path p){
			// margins
			int x1 = p[0].X, y1 = p[0].Y, x2 = p[0].X, y2 = p[0].Y;

			for(int i=0; i<p.Count; i++){
				if (p[i].X < x1) x1 = p[i].X;
				if (p[i].X > x2) x2 = p[i].X;
				if (p[i].Y < y1) y1 = p[i].Y;
				if (p[i].Y > y2) y2 = p[i].Y;
			}

			return new Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
		}
		
		/// <summary>
		/// Converts the specified BitMatrix into a list of Polygons.
		/// </summary>
		/// <param name='bm'>
		/// The BitMatrix to convert from.
		/// </param>
		private static Paths FromBitMatrix(BitMatrix bm){
			#region pixels grouping
			// group pixels into separate areas, keep in mind that a polygon only stores points here
			Paths areas = new Paths();
			
			// current scanline
			BitArray currLineBuf = new BitArray(bm.width);

			Path[] prevLine = new Path[bm.width];

			for (int i=0; i< bm.width; i++) {
				prevLine[i] = null;
			}

			// the area that current pixel belongs to
			Path currArea, prevArea;

			int left;			// counter for checking pixels on the left side 

			for(int y=0; y<bm.height; y++){
				// scan current line
				for(int x=0; x<bm.width; x++){
					// store current pixel in the buffer
					currLineBuf[x] = bm.Get(x, y);
					// index of the left pixel
					left = x - 1;

					// pixel connects with the pixel above it
					if (currLineBuf[x]) {
						//Debug.WriteLine("{0}, {1}: active", y, x);
						// check pixels on top of it
						currArea = prevLine[x];
						if (currArea != null) {
							//Debug.WriteLine("Join top");
							// join current pixel into top area
							currArea.Add(new IntPoint(x, y));
							// Checks the pixels on the left side
							// if they are connected to current pixel,
							// 	 if they belong to different area
							//	   change all of them to current area, and merge that area into current area
							//	 else do nothing
							// else do nothing

							// merge left area into top one
							if (left >= 0) {
								prevArea = prevLine[left];
								if (prevArea != null && prevArea != currArea) {
									//Debug.WriteLine("Merging left to top");
									prevArea = prevLine[left];
									while (left >= 0) {
										// update references of pixels on the left side
										if (prevLine[left] == prevArea) {
											prevLine[left] = currArea;
										}
										left--;
									}
									// merge area into current one
									areas.Remove(prevArea);
									currArea.AddRange(prevArea);
								}
							}

						} else {
							// Since no active pixel on top, check left side
							// if left side is active
							//	 simply join to the left area
							// else 
							//	 create a new area
							if (left >= 0 && prevLine[left] != null) {
								//Debug.WriteLine("Join left");
								prevLine[left].Add(new IntPoint(x, y));
								prevLine[x] = prevLine[left];
							} else {
								//Debug.WriteLine("Create new");
								Path newArea = new Path();
								newArea.Add(new IntPoint(x, y));
								areas.Add(newArea);
								prevLine[x] = newArea;
							}
						}
					// pixel not activated
					} else {
						//Debug.WriteLine("{0},{1}: not active", x, y);
						prevLine[x] = null;
					}
				}
			}
			#endregion

			#region polygonize each group of pixels into contour pixels
			Path p;
			//Create Polygons from given areas
			for(int i=0; i<areas.Count; i++){
				// dequeue a group of points 
				p = areas[0];
				areas.RemoveAt(0);
				// convert to contour
				p = Polygonizer.FromGroup(p);
				// enqueue back
				areas.Add(p);
			}
			#endregion

			return areas;
		}
	
		/// <summary>
		/// Create Polygon from a group of points
		private static Path FromGroup(Path area){

			Rect box = BoundBox(area);
			BitMatrix bm = Polygonizer.DrawPoints(area, box);	// The matrix holding the points
			Path polygon = new Path();						// the result

			// find highest & leftmost point
			int i = 0;
			for(i=0; i<bm.bits.Length; i++) {
				if (bm.bits[i]) break;
				i++;
			}

			IntPoint currP = new IntPoint();
			IntPoint nextP = new IntPoint();
			IntPoint guessP = new IntPoint();
			Direction currDirBack = Direction.NE;	// since the first point is uppermost & leftmost, prevDirBack can never be W
			Direction nextDirBack = Direction.E;	// a random direction different from prevDirBack 

			// extract the coordinate
			currP.X = i % bm.width;
			currP.Y = i / bm.width;
			IntPoint startP = new IntPoint(currP);

			// the position to look at based on the curr point 
			// NE SE SE
			// NE CT SW 
			// NW NW SW
			do {
				// make guessP the next moore neighbor
				switch(currDirBack){
					case Direction.N: goto case Direction.NE;
					case Direction.NE: 
						guessP.X = currP.X + 1;
						guessP.Y = currP.Y + 1;
						break;
					case Direction.E: goto case Direction.SE;
					case Direction.SE:
						guessP.X = currP.X - 1;
						guessP.Y = currP.Y + 1;
						break;
					case Direction.S: goto case Direction.SW;
					case Direction.SW:
						guessP.X = currP.X - 1;
						guessP.Y = currP.Y - 1;
						break;
					case Direction.W: goto case Direction.NW;
					case Direction.NW:
						guessP.X = currP.X + 1;
						guessP.Y = currP.Y - 1;
						break;
				}
				nextP = bm.nextNeighbour(currP, guessP);
				nextDirBack = BitMatrix.getDirection(nextP, currP);
				// only add the curr point if it is a true vertex
				if (nextDirBack != currDirBack){
					polygon.Add(new IntPoint(currP.X + box.xMin, currP.Y + box.yMin));
					currDirBack = nextDirBack;
				}
				currP = nextP;
			} while (nextP != startP);

			RDP(polygon);
			return polygon;
		}

		/// <summary>
		/// Optimize the specified polygon using Ramer–Douglas–Peucker algorithm.
		/// </summary>
		/// <param name="polygon">Polygon.</param>
		private static void RDP(Path polygon, float epsilon = 5f, int begin=0, int end=-1){
			if(end == -1) end = polygon.Count;
			if(end - begin <= 2) return;
			float d, dmax = 0; 
			int index = 0;
			for(int i=begin+1; i<end-1; i++){
				d = distToSegmentSqr(polygon[i], polygon[begin], polygon[end - 1]);
				if(d > dmax){
					dmax = d;
					index = i;
				}
			}
			if ( dmax > epsilon ) {
				RDP(polygon, epsilon, index, end);
				RDP(polygon, epsilon, begin, index);
			} else {
				// remove all points in between
				polygon.RemoveRange(begin + 1, end-begin-2);
			}
		}

		private static float distToSegmentSqr(IntPoint p, IntPoint v, IntPoint w) {
			float l2 = distSqr(v, w);
			if (l2 == 0) return distSqr(p, v);
			float t = ((p.X - v.X) * (w.X - v.X) + (p.Y - v.Y) * (w.Y - v.Y)) / l2;
			if (t < 0) return distSqr(p, v);
			if (t > 1) return distSqr(p, w);
			return distSqr(p, new IntPoint(v.X + t * (w.X - v.X), v.Y + t * (w.Y - v.Y)));
		}

		private static float distToSegment(IntPoint p, IntPoint v, IntPoint w) { 
			return Mathf.Sqrt(distToSegmentSqr(p, v, w));
		}

		private static float distSqr(IntPoint v, IntPoint w){
			return (float)(Math.Pow(v.X - w.X, 2) + Math.Pow(v.Y - w.Y, 2));
		}
	}
}

