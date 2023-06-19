using SuperNet.Unity.Components;
using UnityEditor;
using UnityEngine;

namespace SuperNet.Unity.Editor {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(NetworkSpawner))]
	public sealed class NetworkSpawnerEditor : UnityEditor.Editor {

		public override void OnInspectorGUI() {

			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			base.OnInspectorGUI();
			EditorGUI.EndDisabledGroup();

			bool warningNoPrefab = false;
			bool warningPrefabInstance = false;
			bool isPlaying = false;

			foreach (Object target in serializedObject.targetObjects) {

				NetworkSpawner spawner = (NetworkSpawner)target;
				if (spawner == null) {
					continue;
				}

				if (spawner.Prefab == null) {
					warningNoPrefab = true;
					continue;
				}

				bool isPrefabPart = PrefabUtility.IsPartOfAnyPrefab(spawner.Prefab);
				bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(spawner.Prefab);
				bool isPrefabInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(spawner.Prefab);
				if (isPrefabPart && !isPrefabAsset && isPrefabInstance) {
					warningPrefabInstance = true;
				} else if (Application.IsPlaying(target)) {
					isPlaying = true;
				}

			}

			if (warningNoPrefab) {
				EditorGUILayout.Space();
				if (serializedObject.isEditingMultipleObjects) {
					EditorGUILayout.HelpBox("Prefab not set on some of the spawners.", MessageType.Warning);
				} else {
					EditorGUILayout.HelpBox("Prefab not set.", MessageType.Warning);
				}
				EditorGUILayout.Space();
			}

			if (warningPrefabInstance) {
				EditorGUILayout.Space();
				if (serializedObject.isEditingMultipleObjects) {
					EditorGUILayout.HelpBox("Prefab is a scene object on some of the spawners.", MessageType.Warning);
				} else {
					EditorGUILayout.HelpBox("Prefab is a scene object.", MessageType.Warning);
				}
				EditorGUILayout.Space();
			}

			if (isPlaying && GUILayout.Button("Spawn")) {
				foreach (Object target in serializedObject.targetObjects) {
					NetworkSpawner spawner = (NetworkSpawner)target;
					if (spawner != null && Application.IsPlaying(target)) {
						spawner.Spawn();
					}
				}
			}
			
			foreach (Object target in serializedObject.targetObjects) {

				NetworkSpawner spawner = (NetworkSpawner)target;
				if (!Application.IsPlaying(target)) continue;
				if (spawner == null) continue;
				
				foreach (NetworkPrefab instance in spawner.GetSpawnedPrefabs()) {
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.ObjectField(instance, typeof(NetworkPrefab), true);
					if (GUILayout.Button("Despawn")) Destroy(instance.gameObject);
					EditorGUILayout.EndHorizontal();
				}

			}


		}

	}

}
