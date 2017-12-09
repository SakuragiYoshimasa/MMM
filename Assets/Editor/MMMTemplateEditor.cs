using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using MAPS;

namespace MMM{
	[CustomEditor(typeof(MMMTemplate))]
	public class MMMTemplateEditor : Editor {

		SerializedProperty _targetMesh;
		SerializedProperty _sourceMesh;
		SerializedProperty _targetBaseMesh;
		SerializedProperty _sourceBaseMesh;
		SerializedProperty _laycastMesh;

		const string _helpText = 
            "MMM is a Multiresolutional Mesh Morphing";

		void OnEnable()
        {
            _targetMesh = serializedObject.FindProperty("_targetMesh");
            _targetBaseMesh = serializedObject.FindProperty("_targetBaseMesh");
			_laycastMesh = serializedObject.FindProperty("_laycastMesh");
			_sourceMesh = serializedObject.FindProperty("_sourceMesh");
            _sourceBaseMesh = serializedObject.FindProperty("_sourceBaseMesh");
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
            EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

			if(GUILayout.Button("Build MMM template")){
				((MMMTemplate)target).build();
			}

			if(GUILayout.Button("MMM template debugging")){
				((MMMTemplate)target).debugLog();
			}

            // Readonly members
            EditorGUILayout.HelpBox(_helpText, MessageType.None);
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

		public override bool HasPreviewGUI(){
			return true;
		}

		public override void OnInteractivePreviewGUI(Rect r, GUIStyle background){
                
            if (_sourceMesh != null) {
                var texture = ((MMMTemplate)target).getPreviewAsset();
            	EditorGUI.DrawTextureTransparent(r, texture, ScaleMode.ScaleToFit);
            } else{                
				OnInteractivePreviewGUI(r, background);
			}
		}
	}
}
