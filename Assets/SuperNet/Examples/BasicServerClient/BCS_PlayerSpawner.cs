using SuperNet.Netcode.Transport;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SuperNet.Examples.BCS {

	[RequireComponent(typeof(NetworkSpawner))]
	public class BCS_PlayerSpawner : NetworkComponent {

		// The spawner to spawn on
		private NetworkSpawner Spawner;

		// Server only list of spawned prefabs
		private Dictionary<Peer, NetworkPrefab> Prefabs;

		private void Awake() {
			// Get the spawner we're managing and initialize prefab dictionary
			Spawner = GetComponent<NetworkSpawner>();
			Prefabs = new Dictionary<Peer, NetworkPrefab>();
		}

		protected override void Start() {

			// Register this component with the network so it starts receiving events 
			base.Start();

			// The spawner starts with Locked set to true and Authority set to false
			// Because the spawner is locked, only peers with Authority set to true can spawn on it
			// If we are the server we need to manually give it authority so we can spawn on it
			Spawner.SetAuthority(BCS_Menu.IsServer);

			// If we are a non-dedicated server and we launched the server with the menu
			// Spawn a server player too, claim ownership over it but don't add it to the prefabs dictionary
			// This player will never get despawned
			if (BCS_Menu.IsServer && !BCS_Menu.Config.ServerDedicated) {
				NetworkPrefab prefab = Spawner.Spawn();
				NetworkAuthority authority = prefab.GetComponent<NetworkAuthority>();
				authority.SetAuthority(true);
				authority.Claim();
			}

		}

		public IReadOnlyList<NetworkPrefab> GetSpawnedPrefabs() {
			// Get a list of all spawned prefabs
			return Spawner.GetSpawnedPrefabs();
		}

		public override void OnNetworkConnect(Peer peer) {

			// A peer connected, if we are a server we need to spawn a player for it

			if (BCS_Menu.IsServer) {

				// OnNetworkConnect is called in a separate thread
				// We use Run() to move it to the main unity thread
				// Spawner.Spawn() only works in the main unity thread
				Run(() => {

					// Spawn the prefab
					NetworkPrefab prefab = Spawner.Spawn();
					Prefabs.Add(peer, prefab);

					// Find authority on the prefab
					NetworkAuthority authority = prefab.GetComponent<NetworkAuthority>();

					// The NetworkAuthority starts with Locked set to true and Authority set to false
					// Because the NetworkAuthority is locked, only peers with Authority set to true can change ownership
					// We are the server so we need to manually give it authority so we can then grant ownership to the newly connected peer
					authority.SetAuthority(true);

					// Grant ownership to the peer
					authority.Grant(peer);

				});

			}

		}

		public override void OnNetworkDisconnect(Peer peer) {

			// A peer disconnected, if we are a server we need to despawn the associated player

			if (BCS_Menu.IsServer) {

				// OnNetworkDisconnect is called in a separate thread
				// We use Run() to move it to the main unity thread
				Run(() => {

					// Check if disconnected peer has a prefab spawned
					Prefabs.TryGetValue(peer, out NetworkPrefab prefab);

					// Despawn the prefab
					if (prefab != null) {
						Spawner.Despawn(prefab);
					}

					// Remove the prefab from dictionary
					Prefabs.Remove(peer);

				});

			}

		}

	}

}
