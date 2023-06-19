using SuperNet.Unity.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SuperNet.Examples.BPP {

	[RequireComponent(typeof(NetworkSpawner))]
	public class BPP_PlayerSpawner : MonoBehaviour {

		// The spawner to spawn on
		public NetworkSpawner Spawner;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			Spawner = GetComponent<NetworkSpawner>();
		}

		private void Start() {

			// Spawn a local player
			NetworkPrefab prefab = Spawner.Spawn();

			// Get player components
			NetworkTransform prefabTransform = prefab.GetComponent<NetworkTransform>();
			BPP_Player prefabPlayer = prefab.GetComponent<BPP_Player>();

			// Manually set authority on the newly spawned player
			// This does not send any network messages, it only sets authority to true on the local player
			// All players spawned on remote peers will still have authority set to false
			// Which will result in only us having authority true and everybody else having authority false
			prefabTransform.SetAuthority(true);
			prefabPlayer.SetAuthority(true);

		}

		public IReadOnlyList<NetworkPrefab> GetSpawnedPrefabs() {
			// Get a list of all spawned prefabs
			return Spawner.GetSpawnedPrefabs();
		}

	}

}
