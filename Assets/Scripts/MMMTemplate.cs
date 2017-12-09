using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MAPS;
using UnityEditor;

namespace MMM {
	public class MMMTemplate : ScriptableObject {

		#region Public properties

		#endregion

		#region Private members
		[SerializeField] Mesh _sourceMesh;
		[SerializeField] Mesh _targetMesh;
		[SerializeField] MapsMesh _sourceBaseMesh;
		[SerializeField] MapsMesh _targetBaseMesh;
		[SerializeField] Mesh _laycastMesh;

		//Compose M0
		[SerializeField] Mesh _transformedSourceMesh;
		[SerializeField] List<int> fps;
		[SerializeField] List<int> fpt;
		#endregion

		#region Public methods
		public void debugLog(){
			/* 
			Debug.LogFormat("fps:{0}", fps.Count);
			Debug.LogFormat("valid:{0}", fps.Where(p => p != -1).ToArray().Count());

			Debug.LogFormat("fpt:{0}", fpt.Count);
			Debug.LogFormat("valid:{0}", fpt.Where(p => p != -1).ToArray().Count());

			GameObject go = new GameObject();
			go.name = "test";
		 	MeshFilter mf = go.AddComponent<MeshFilter>();
			Debug.Log(_targetBaseMesh);
			mf.sharedMesh = RemeshUtility.rebuiltMesh(ref _targetBaseMesh);
			MeshRenderer mr = go.AddComponent<MeshRenderer>();
			*/
			Debug.LogFormat("Remain:{0}", _sourceBaseMesh.K.vertices.Count);
		}

		public void build(){
			
			if(_sourceMesh == null || _targetMesh == null || _laycastMesh == null){
				Debug.LogError("Please set meshes before building");
				return;
			}

			Maps sourceMaps = new Maps(); 
			Maps targetMaps = new Maps();

			fps = setFeaturePoints(_sourceMesh);
			Debug.Log("Source fp done");
			fpt = setFeaturePoints(_targetMesh);
			Debug.Log("Target fp done");
			removeDeplication(ref fps, ref fpt);

			_sourceBaseMesh = sourceMaps.generateBaseDomain(_sourceMesh, fps);
			Debug.Log("Source maps done");
			_targetBaseMesh = targetMaps.generateBaseDomain(_targetMesh, fpt);
			Debug.Log("Target maps done");
			generateTransformedSourceMesh();
		}

		public Texture getPreviewAsset(){
			var texture = AssetPreview.GetAssetPreview(_sourceMesh);
			return texture;
		}

		public void getInterpolatedMesh(float p){

		}

		#endregion

		#region Private methods
		List<int> setFeaturePoints(Mesh mesh){
			List<int> fp = new List<int>();
			List<Vector3> hitPoints = new List<Vector3> (0);
			Vector3[] castTargetPoints = _laycastMesh.vertices; 
			GameObject meshObject = new GameObject();
			MeshCollider mc = meshObject.AddComponent<MeshCollider>();
			meshObject.name = "meshObj";
			mc.sharedMesh = mesh;
			
			for (int i = 0; i < castTargetPoints.GetLength (0); i++) {

				Ray ray = new Ray (castTargetPoints [i] * 1000.0f, castTargetPoints [i] * -1f);
				RaycastHit[] hits = Physics.RaycastAll (ray, Mathf.Infinity);
				bool added = false;
				foreach (var obj in hits) {
					if (obj.collider.gameObject.name == meshObject.name) {
						hitPoints.Add (obj.point);
						added = true;
						break; 
					} 
				}
				if(!added){
					hitPoints.Add(Vector3.zero);
				}
			}

			for(int i = 0; i < hitPoints.Count; i++){

				if(hitPoints[i] == Vector3.zero){
					fp.Add(-1);
					continue;
				}
				float min = float.MaxValue;
				int minInd = -1;
				for(int j = 0; j < mesh.vertices.Length; j++){
					float dist = (hitPoints[i] - mesh.vertices[j]).magnitude;
					if(dist < min){
						min = dist;
						minInd = j;
					}
				}
				fp.Add(minInd);
			}

			GameObject.DestroyImmediate(meshObject);
			return fp;
		}

		void removeDeplication(ref List<int> fp1, ref List<int> fp2){
			if(fp1.Count != fp2.Count){
				Debug.LogError("They are needed to have same count feature points. Please rebuild");
			}
			//Remove not found
			for(int i = 0; i < fp1.Count; i++){
				if(fp1[i] == -1 || fp2[i] == -1){
					fp1.RemoveAt(i);
					fp2.RemoveAt(i);
					i--;
					continue;
				}
			}
			//Remove Deplication
			HashSet<int> setTablefp1 = new HashSet<int>();
			HashSet<int> setTablefp2 = new HashSet<int>();

			for(int i = 0; i < fp1.Count; i++){
				if(setTablefp1.Contains(fp1[i]) || setTablefp2.Contains(fp2[i])){
					fp1.RemoveAt(i);
					fp2.RemoveAt(i);
					i--;
					continue;
				}
				setTablefp1.Add(fp1[i]);
				setTablefp2.Add(fp2[i]);
			}
		}

		void generateTransformedSourceMesh(){
			//------------------------------------------------------
			//Pi(s) 
			//------------------------------------------------------
			//This is a bijection of sourceBaseMesh.


			//------------------------------------------------------
			//Calculate M0
			//------------------------------------------------------
			Dictionary<int, Dictionary<int, float>> M0 = new Dictionary<int, Dictionary<int, float>>();
			foreach(Vert vert in _sourceBaseMesh.K.vertices){
				M0.Add(vert.ind, new Dictionary<int, float>());
			}

			//About feature points, M0(s_i) = t_i.
			for(int i = 0; i < fps.Count; i++){
				M0[fps[i]].Add(fpt[i], 1.0f);
			}
			
			//Search where other points located on the Target base domain
			for(int i = 0; i < _sourceBaseMesh.K.vertices.Count; i++){
				if(fps.Contains(_sourceBaseMesh.K.vertices[i].ind)) continue;

				//Find Closest triangle
				int closestTriangleIndex = -1;
				Vector3 minimumDistP0_p = Vector3.zero;
				float minimumDist = float.MaxValue;


				//Projected point treated as a p0_p
				//Initialy, check the triangle contains the p0_p. If not contained, continue.
				//At the same, Np, and distance are calculated
				//Second, if calculated distance is minimun, update contained triangle.

				for(int j = 0; j < _targetBaseMesh.K.triangles.Count; j++){
					Triangle triangle = _targetBaseMesh.K.triangles[j];
					Vector3 p0 = _sourceBaseMesh.P[_sourceBaseMesh.K.vertices[i].ind];
					Vector3 p1 = _targetBaseMesh.P[triangle.ind1];
					Vector3 p2 = _targetBaseMesh.P[triangle.ind2];
					Vector3 p3 = _targetBaseMesh.P[triangle.ind3];
					Vector3 np = Vector3.Cross(p2 - p1, p3 - p1);
					float cosAlpha = Vector3.Dot(p0 - p1, np) / ((p0 - p1).magnitude * np.magnitude);
					float dist =  (p1 - p0).magnitude * cosAlpha;
					Vector3 p0p0_p = - dist * cosAlpha / np.magnitude * np;
					Vector3 p0_p = p0 + p0p0_p;

					if(!MathUtility.checkContain3D(new Vector3[3]{p1, p2, p3}, p0_p, np)) continue;
					if(dist < minimumDist){
						minimumDist = dist;
						minimumDistP0_p = p0_p;
						closestTriangleIndex = j;
					}
				}

				//Finally, update dict by minimum distance triangle and its barycentric coordinate of projected point.
				Triangle closestTriangle = _targetBaseMesh.K.triangles[closestTriangleIndex];
				Vector3 _p1 = _targetBaseMesh.P[closestTriangle.ind1];
				Vector3 _p2 = _targetBaseMesh.P[closestTriangle.ind2];
				Vector3 _p3 = _targetBaseMesh.P[closestTriangle.ind3];	
				Vector3[] points = new Vector3[3]{_p1, _p2, _p3};			
				Vector3 _p0_p = minimumDistP0_p;
				float[] param = new float[3]{MathUtility.calcArea(points[1], points[2]), MathUtility.calcArea(points[2], points[0]), MathUtility.calcArea(points[0], points[1])};
				for(int x = 0; x < 3; x++) param[x] = double.IsNaN(param[x]) ? 0 : param[x];
				float params_sum = param.Sum();  
				param = param.Select(p => p / params_sum).ToArray();
				if(param[0] != 0) M0[_sourceBaseMesh.K.vertices[i].ind].Add(closestTriangle.ind1, param[0]);
				if(param[1] != 0) M0[_sourceBaseMesh.K.vertices[i].ind].Add(closestTriangle.ind2, param[1]);
				if(param[2] != 0) M0[_sourceBaseMesh.K.vertices[i].ind].Add(closestTriangle.ind3, param[2]);
			}

			//------------------------------------------------------
			//Pi(T)^(-1)はpoint location algorithmで求める
			//------------------------------------------------------
			//Where does projected points on.
			//In other words, calculate where projected points on the original target mesh.
			



		}
		#endregion
	}
}
