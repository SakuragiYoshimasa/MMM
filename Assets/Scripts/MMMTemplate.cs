using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MAPS;

namespace MMM {
	public class MMMTemplate : ScriptableObject {

		#region Public properties

		#endregion

		#region Private members
		[SerializeField] Mesh _sourceMesh;
		[SerializeField] Mesh _targetMesh;
		[SerializeField] public MapsMesh _sourceBaseMesh;
		[SerializeField] MapsMesh _targetBaseMesh;
		[SerializeField] Mesh _laycastMesh;

		//Compose M0
		[SerializeField] List<Dictionary<int, float>> M0;
		[SerializeField] List<int> fps;
		[SerializeField] List<int> fpt;
		#endregion

		#region Public methods
		public void debugLog(){
			Debug.LogFormat("fps:{0}", fps.Count);
			Debug.LogFormat("valid:{0}", fps.Where(p => p != -1).ToArray().Count());

			Debug.LogFormat("fpt:{0}", fpt.Count);
			Debug.LogFormat("valid:{0}", fpt.Where(p => p != -1).ToArray().Count());
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
			calclateCompose();
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

		void calclateCompose(){

		}

		#endregion
	}
}
