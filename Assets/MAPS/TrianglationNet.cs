using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

namespace MAPS {
	public class TrianglationNet {
		public static bool triangulateFromRing(List<int> ring, Dictionary<int,Vector2> mapped_ring, out List<Triangle> added_triangle){
			added_triangle = new List<Triangle>();
			Polygon poly = new Polygon();
			List<int> outIndices = new List<int>();
			List<Vector2> outVertices = new List<Vector2>();
			
			for(int i = 0; i < ring.Count; i++){
				poly.Add(new Vertex(mapped_ring[ring[i]].x, mapped_ring[ring[i]].y));

				if(i == ring.Count - 1){
					poly.Add(new Segment(new Vertex(mapped_ring[ring[i]].x, mapped_ring[ring[i]].y), new Vertex(mapped_ring[ring[0]].x, mapped_ring[ring[0]].y)));
				}else{
					poly.Add(new Segment(new Vertex(mapped_ring[ring[i]].x, mapped_ring[ring[i]].y), new Vertex(mapped_ring[ring[i + 1]].x, mapped_ring[ring[i + 1]].y)));
				}
			}

			var mesh = poly.Triangulate();
			foreach(ITriangle t in mesh.Triangles){
				for(int j = 2; j >= 0; j--){
					bool found = false;
					foreach(KeyValuePair<int, Vector2> kv in mapped_ring){
						if(kv.Value.x == t.GetVertex(j).X && kv.Value.y == t.GetVertex(j).Y){
							outIndices.Add(kv.Key);
							found = true;
							break;
						}
					}
					if(!found){ return false; }
				}
			}
			
			for(int i = 0; i < outIndices.Count / 3; i++){
				int i1 = outIndices[i * 3];
				int i2 = outIndices[i * 3 + 1];
				int i3 = outIndices[i * 3 + 2];
				added_triangle.Add(new Triangle(i1, i2, i3));
			}
			return true;
		}
	}
}