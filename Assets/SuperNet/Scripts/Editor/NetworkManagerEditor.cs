using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperNet.Unity.Editor {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(NetworkManager))]
	public sealed class NetworkManagerEditor : UnityEditor.Editor {

		private static float RollbackSlider = 0f;
		private static bool RollbackEnabled = false;
		private static HostTimestamp RollbackTime = HostTimestamp.MinValue;

		public override void OnInspectorGUI() {

			base.OnInspectorGUI();

			if (serializedObject.isEditingMultipleObjects) {
				EditorGUILayout.HelpBox("Multiple managers selected.", MessageType.Warning);
				return;
			}

			if (!Application.IsPlaying(target)) {
				return;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
			if (RollbackEnabled) {
				if (GUILayout.Button("Disable Rollback")) {
					RollbackSlider = 0f;
					RollbackEnabled = false;
					NetworkManager.RollbackEnd();
				}
			} else {
				if (GUILayout.Button("Enable Rollback")) {
					RollbackEnabled = true;
					RollbackTime = Host.Now;
				}
			}
			EditorGUI.BeginDisabledGroup(!RollbackEnabled);
			RollbackSlider = EditorGUILayout.Slider(RollbackSlider, 2f, 0f);
			EditorGUI.EndDisabledGroup();
			if (RollbackEnabled) {
				HostTimestamp timestamp = RollbackTime - HostTimespan.FromSeconds(RollbackSlider);
				NetworkManager.RollbackBegin(timestamp, null, true, false);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Peers", EditorStyles.boldLabel);
			IReadOnlyList<Peer> peers = NetworkManager.GetPeers();
			if (peers.Count <= 0) {
				EditorGUILayout.HelpBox("No registered peers.", MessageType.Info);
			} else {
				EditorGUI.BeginDisabledGroup(true);
				for (int i = 0; i < peers.Count; i++) {
					Peer peer = peers[i];
					EditorGUILayout.TextField(peer.Remote.ToString());
				}
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Registered Components", EditorStyles.boldLabel);
			IReadOnlyList<NetworkComponent> components = NetworkManager.GetComponents();
			if (components == null || components.Count <= 0) {
				EditorGUILayout.HelpBox("No components.", MessageType.Info);
			} else {
				EditorGUI.BeginDisabledGroup(true);
				for (int i = 0; i < components.Count; i++) {
					NetworkComponent component = components[i];
					EditorGUILayout.ObjectField("NetworkID: " + component.NetworkID, component, typeof(NetworkComponent), true);
				}
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.Space();
			EditorUtility.SetDirty(target);

		}

	}

}
