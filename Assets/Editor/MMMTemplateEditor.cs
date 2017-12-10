using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using MAPS;
using System.Reflection;

namespace MMM{
	[CustomEditor(typeof(MMMTemplate))]
	public class MMMTemplateEditor : Editor {

		SerializedProperty _targetMesh;
		SerializedProperty _sourceMesh;
		SerializedProperty _targetBaseMesh;
		SerializedProperty _sourceBaseMesh;
		SerializedProperty _laycastMesh;
		SerializedProperty _debugMat;

		PreviewRenderUtility previewRenderUtility;
		GameObject previewObject;
		Vector3 centerPosition;

		const string _helpText = 
            "MMM is a Multiresolutional Mesh Morphing";

		void OnEnable()
        {
            _targetMesh = serializedObject.FindProperty("_targetMesh");
            _targetBaseMesh = serializedObject.FindProperty("_targetBaseMesh");
			_laycastMesh = serializedObject.FindProperty("_laycastMesh");
			_sourceMesh = serializedObject.FindProperty("_sourceMesh");
            _sourceBaseMesh = serializedObject.FindProperty("_sourceBaseMesh");
			_debugMat = serializedObject.FindProperty("debugMat");
	
       		previewRenderUtility = new PreviewRenderUtility (true);
        	previewRenderUtility.cameraFieldOfView = 30f;
        	previewRenderUtility.camera.nearClipPlane = 0.3f;
        	previewRenderUtility.camera.farClipPlane = 1000;
        	previewObject = ((MMMTemplate)target).getPreviewObject();
			previewObject.hideFlags = HideFlags.HideAndDontSave;
			previewObject.SetActive(false);

			var flags = BindingFlags.Static | BindingFlags.NonPublic;
			var propInfo = typeof(Camera).GetProperty ("PreviewCullingLayer", flags);
			int previewLayer = (int)propInfo.GetValue (null, new object[0]);
			previewRenderUtility.camera.cullingMask = 1 << previewLayer;
			previewObject.layer = previewLayer;
			foreach (Transform transform in previewObject.transform) {
    			transform.gameObject.layer = previewLayer;
			}

			Bounds bounds = new Bounds (previewObject.transform.position, Vector3.zero);
			foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>()) { bounds.Encapsulate (renderer.bounds); }
			centerPosition = bounds.center;
			previewObject.SetActive (false);
			RotatePreviewObject (new Vector2 (-120, 20));
        }

		void OnDisable (){
	        DestroyImmediate (previewObject);
    	    previewRenderUtility.Cleanup ();
        	previewRenderUtility = null;
	    }

		public override void OnInspectorGUI(){
			
			var template = (MMMTemplate)target;

            // Editable properties
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(_laycastMesh);
            EditorGUILayout.PropertyField(_targetMesh);
            EditorGUILayout.PropertyField(_sourceMesh);
			EditorGUILayout.PropertyField(_sourceBaseMesh);
			EditorGUILayout.PropertyField(_targetBaseMesh);
			EditorGUILayout.PropertyField(_debugMat);
            EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

			if(GUILayout.Button("Build MMM template")){
				((MMMTemplate)target).build();
			}

			if(GUILayout.Button("MMM template debugging")){
				((MMMTemplate)target).debugLog();
			}
			EditorGUILayout.HelpBox(_helpText, MessageType.None);
    	}

		public override bool HasPreviewGUI(){
			return true;
		}
		
		public override void OnInteractivePreviewGUI (Rect r, GUIStyle background){
			if(previewObject == null) return;
			previewRenderUtility.BeginPreview (r, background);
			var drag = Vector2.zero;
			if (Event.current.type == EventType.MouseDrag) { drag = Event.current.delta; }
			
			previewRenderUtility.camera.transform.position = centerPosition + Vector3.forward * -5;
			RotatePreviewObject (drag);
			previewObject.SetActive (true);
			previewRenderUtility.camera.Render ();
			previewObject.SetActive (false);
			previewRenderUtility.EndAndDrawPreview (r);
			if (drag != Vector2.zero) Repaint ();
		}

		private void RotatePreviewObject (Vector2 drag){
        	previewObject.transform.RotateAround (centerPosition, Vector3.up, -drag.x);
        	previewObject.transform.RotateAround (centerPosition, Vector3.right, -drag.y);
    	}

		[MenuItem("Assets/Create/MMM/MMM Template")]
        public static void CreateTemplateAsset()
        {
            // Make a proper path from the current selection.
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                path = "Assets";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(path), "");
            var assetPathName = AssetDatabase.GenerateUniqueAssetPath(path + "/New MMM Template.asset");

            // Create a template asset.
            var asset = ScriptableObject.CreateInstance<MMMTemplate>();
            AssetDatabase.CreateAsset(asset, assetPathName);
           // AssetDatabase.AddObjectToAsset(asset.mesh, asset);

            // Build an initial mesh for the asset.
            //asset.RebuildMesh();

            // Save the generated mesh asset.
            AssetDatabase.SaveAssets();

            // Tweak the selection.
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
	}
}
