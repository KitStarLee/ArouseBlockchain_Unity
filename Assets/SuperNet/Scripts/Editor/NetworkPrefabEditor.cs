using SuperNet.Netcode.Transport;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperNet.Unity.Editor {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(NetworkPrefab))]
	public sealed class NetworkPrefabEditor : UnityEditor.Editor {

		public override void OnInspectorGUI() {

			base.OnInspectorGUI();

			if (serializedObject.isEditingMultipleObjects) {
				return;
			}

			NetworkPrefab target = (NetworkPrefab)serializedObject.targetObject;
			NetworkSpawner spawner = target.Spawner;
			Peer spawnee = target.Spawnee;

			if (!Application.IsPlaying(target)) {
				return;
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Spawner", spawner, typeof(NetworkSpawner), true);
			EditorGUILayout.TextField("Spawnee", spawnee == null ? "" : spawnee.Remote.ToString());
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Managed Components", EditorStyles.boldLabel);
			IReadOnlyList<NetworkComponent> components = target.GetNetworkComponents();
			if (components == null || components.Count <= 0) {
				EditorGUILayout.HelpBox("No components.", MessageType.Info);
			} else {
				EditorGUI.BeginDisabledGroup(true);
				foreach (NetworkComponent component in target.GetNetworkComponents()) {
					if (component is NetworkComponent obj) {
						EditorGUILayout.ObjectField(obj, typeof(INetworkAuthoritative), true);
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.Space();

			bool canDespawn = spawner != null && ((spawner.Locked && spawner.IsAuthority) || (!spawner.Locked && spawnee == null));
			EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(!canDespawn);
			if (GUILayout.Button("Despawn")) Destroy(target.gameObject);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

		}

	}

}
