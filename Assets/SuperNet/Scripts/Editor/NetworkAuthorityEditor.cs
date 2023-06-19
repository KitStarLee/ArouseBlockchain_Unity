using SuperNet.Netcode.Transport;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperNet.Unity.Editor {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(NetworkAuthority))]
	public sealed class NetworkAuthorityEditor : UnityEditor.Editor {

		public override void OnInspectorGUI() {

			base.OnInspectorGUI();

			if (serializedObject.isEditingMultipleObjects) {
				if (Application.isPlaying) {
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("Multiple authorities selected.", MessageType.Info);
					EditorGUILayout.Space();
				}
				return;
			}

			NetworkAuthority target = (NetworkAuthority)serializedObject.targetObject;
			Peer owner = target.Owner;
			
			if (!Application.IsPlaying(target)) {
				return;
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.TextField("Ownership", target.IsOwner ? "true" : "false");
			EditorGUILayout.TextField("Owner", owner == null ? "None" : owner.Remote.ToString());
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Managed Components", EditorStyles.boldLabel);
			IReadOnlyList<INetworkAuthoritative> components = target.GetAuthoritativeComponents();
			if (components == null || components.Count <= 0) {
				EditorGUILayout.HelpBox("No components.", MessageType.Info);
			} else {
				EditorGUI.BeginDisabledGroup(true);
				foreach (INetworkAuthoritative component in components) {
					if (component is NetworkComponent obj) {
						EditorGUILayout.ObjectField(obj, typeof(INetworkAuthoritative), true);
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
			if (GUILayout.Button("Rebind")) target.Rebind();
			EditorGUI.BeginDisabledGroup(!target.IsAuthority && target.Locked);
			if (GUILayout.Button("Claim")) target.Claim();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			if (target.Locked && target.IsAuthority) {
				EditorGUILayout.LabelField("Peers", EditorStyles.boldLabel);
				IReadOnlyList<Peer> peers = NetworkManager.GetPeers();
				if (peers.Count <= 0) {
					EditorGUILayout.HelpBox("No peers.", MessageType.Info);
				} else {
					foreach (Peer peer in NetworkManager.GetPeers()) {
						EditorGUILayout.BeginHorizontal();
						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.TextField(peer.Remote.ToString());
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(target.Owner == peer);
						if (GUILayout.Button("Grant")) target.Grant(peer);
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.Space();
			}
			
			EditorGUILayout.Space();
			EditorUtility.SetDirty(target);

		}

	}

}
