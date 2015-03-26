using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClipperLib;

namespace PolygonizerLib
{
	using Path = List<IntPoint>;
	using Paths = List<List<IntPoint>>;

	public enum Direction { N, NE, E, SE, S, SW, W, NW };

	public class BitMatrix
	{

		public int width { get; private set; }

		public int height { get; private set; }

		public bool[] bits;

		public BitMatrix(int width, int height)
		{
			this.width = width;
			this.height = height;
			this.bits = new bool[width * height];
		}
		
		public BitMatrix(int width, int height, bool[] bits) : this(width, height)
		{
			if (bits.Length != width * height) {
				throw new ArgumentException("The length of array does not match given size", "bits");
			}
			Array.Copy(bits, this.bits, width * height);
		}

		public bool Equals(BitMatrix b)
		{
			int length = bits.Length;
			if (b.bits.Length != length)
				return false;
			for (int i = 0; i < length; i++) {
				if (bits[i] != b.bits[i])
					return false;
			}
			return true;
		}

		public void Set(int x, int y, bool v)
		{
			bits[y * width + x] = v;
		}

		public void Set(IntPoint pos, bool v)
		{
			bits[pos.Y * width + pos.X] = v;
		}

		public bool Get(int x, int y)
		{
			return bits[y * width + x];    
		}

		public bool Get(IntPoint pos)
		{
			return bits[pos.Y * width + pos.X];
		}

		/// <summary>
		/// Search clockwise the next Moore neighbour point that is assigned true, handles the boundary.
		/// </summary>
		/// <returns>The neighbour.</returns>
		/// <param name="center">Center point.</param>
		/// <param name="origin">Origin must be a moore neighbour of the center. </param> 
		public IntPoint nextNeighbour(IntPoint center, IntPoint current, bool value=true)
		{
			if (width + height < 2)
				throw new ArgumentException("Current BitMatrix is too small: " + width + "*" + height);

			// check boundaries 
			// stored by N, E, S, W clockwise
			bool[] bound = new bool[4];
			bound[0] = (center.Y == 0);
			bound[1] = (center.X == width - 1);
			bound[2] = (center.Y == height - 1);
			bound[3] = (center.X == 0);

			Direction dir = getDirection(center, current);
			int count = 0;
			while (count < 9) {
				switch (dir) {
					case Direction.N: 
						if (bound[0])
							goto case Direction.E;
						else 
							dir = Direction.N;
						break;
					case Direction.NE:
						if (bound[1])
							goto case Direction.S;
						if (bound[0])
							dir = Direction.E;
						else
							dir = Direction.NE;
						break;
					case Direction.E:
						if (bound[1])
							goto case Direction.S;
						else 
							dir = Direction.E;
						break;
					case Direction.SE:
						if (bound[2])
							goto case Direction.W;
						if (bound[1])
							dir = Direction.S;
						else
							dir = Direction.SE;
						break;
					case Direction.S:
						if (bound[2])
							goto case Direction.W;
						else
							dir = Direction.S;
						break;
					case Direction.SW:
						if (bound[3])
							goto case Direction.N;
						if (bound[2])
							dir = Direction.W;
						else
							dir = Direction.SW;
						break;
					case Direction.W:
						if (bound[3])
							goto case Direction.N;
						else
							dir = Direction.W;
						break;
					case Direction.NW:
						if (bound[0])
							goto case Direction.E;
						if (bound[3])
							dir = Direction.N;
						else
							dir = Direction.NW;
						break;
				}
				current = gotoDirection(center, dir);
				if (Get(current) == value) break;
				dir = (Direction) (((int) dir + 1) % 8);
				count++;
			}

			return count != 9 ? current : new IntPoint(-1, -1);
		}
//
//		public static Texture2D ToMonoBitmap(BitMatrix bMatrix)
//		{
//			int width = bMatrix.Width,
//			height = bMatrix.Height;
//			Texture2D bMap = new Texture2D(width, height);
//			Color c;
//			for (int y = 0; y < height; y++) {
//				// loop through each pixel on current scanline
//				for (int x = 0; x < width; x++) {
//					c = bMatrix.Get(x, y) ? new Color(0,0,0) : new Color(255, 255, 255);
//					// write bits into scanline buffer
//					bMap.SetPixel(x, y, c);
//				}
//			}
//                    
//			return bMap;
//		}

		/// <summary>
		/// Gets the direction from p1 to one of its moore neighbour
		/// </summary>
		/// <returns>The direction.</returns>
		/// <param name="p1">P1.</param>
		/// <param name="p2">P2. Must be one of the moore neighbours of p1.</param>
		public static Direction getDirection(IntPoint p1, IntPoint p2)
		{
			int offX = p2.X - p1.X, 
			offY = p2.Y - p1.Y;
			switch (offY * 3 + offX) {
				case -4:
					return Direction.NW;
				case -3:
					return Direction.N;
				case -2:
					return Direction.NE;
				case -1:
					return Direction.W;
				case 1:
					return Direction.E;
				case 2:
					return Direction.SW;
				case 3:
					return Direction.S;
				case 4:
					return Direction.SE;
				default:
				throw new ArgumentException(String.Format("[{0},{1}] is not a moore neighbour of [{2},{3}]: ",p2.X,p2.Y,p1.X,p1.Y));
			}
		}

		public static IntPoint gotoDirection(IntPoint p, Direction d)
		{
			switch (d) {
				case Direction.N:
					return new IntPoint(p.X, p.Y - 1);
				case Direction.NE:
					return new IntPoint(p.X + 1, p.Y - 1);
				case Direction.E:
					return new IntPoint(p.X + 1, p.Y);
				case Direction.SE:
					return new IntPoint(p.X + 1, p.Y + 1);
				case Direction.S:
					return new IntPoint(p.X, p.Y + 1);
				case Direction.SW:
					return new IntPoint(p.X - 1, p.Y + 1);
				case Direction.W:
					return new IntPoint(p.X - 1, p.Y);
				case Direction.NW:
					return new IntPoint(p.X - 1, p.Y - 1);
				default:
					throw new ArgumentException("Unknown Direction");
			}
		}
	}
}
