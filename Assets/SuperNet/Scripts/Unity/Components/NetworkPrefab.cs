using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using UnityEngine;
using UnityEngine.Serialization;
using System.Threading;
using System.Collections.Generic;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Spawnable prefab with network components.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("SuperNet/NetworkPrefab")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkPrefab.html")]
	public sealed class NetworkPrefab : NetworkComponent {

		/// <summary>
		/// Network channel to use.
		/// </summary>
		[FormerlySerializedAs("SyncChannel")]
		[Tooltip("Network channel to use.")]
		public byte SyncChannel;

		/// <summary>
		/// Network spawner responsible for this prefab or null if not spawned.
		/// </summary>
		public NetworkSpawner Spawner => PrefabSpawner;

		/// <summary>
		/// The peer that spawned this prefab or null if none.
		/// </summary>
		public Peer Spawnee => PrefabSpawnee;

		// Resources
		private ReaderWriterLockSlim Lock;
		private NetworkComponent[] Components;
		private NetworkSpawner PrefabSpawner;
		private Peer PrefabSpawnee;
		private bool Destroyed;

		private void Reset() {
			ResetNetworkID();
			SyncChannel = 0;
		}

		private void Awake() {

			// Initialize
			Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			Components = null;
			PrefabSpawner = null;
			PrefabSpawnee = null;
			Destroyed = false;

			// Get all child network components in the hierarchy
			List<NetworkComponent> result = new List<NetworkComponent>();
			GetComponentsInChildren(true, result);

			// Remove all prefabs and their children
			NetworkPrefab[] prefabs = GetComponentsInChildren<NetworkPrefab>(true);
			foreach (NetworkPrefab prefab in prefabs) {
				result.Remove(prefab);
				if (prefab != this) {
					NetworkComponent[] components = prefab.GetComponentsInChildren<NetworkComponent>(true);
					foreach (NetworkComponent component in components) {
						result.Remove(component);
					}
				}
			}

			// Save the remaining network components to be managed by this prefab
			Components = result.ToArray();

		}

		protected override void Start() {
			try {
				Lock.EnterWriteLock();

				// Check if already spawned
				NetworkSpawner spawner = PrefabSpawner;
				if (spawner != null) {
					return;
				}

				// Get full path to the prefab
				string path = "";
				Transform parent = transform;
				while (parent != null) {
					if (parent.parent == null) {
						path = "/" + parent.name + path;
					} else {
						int index = parent.GetSiblingIndex();
						path = "/" + parent.name + ":" + index + path;
					}
					parent = parent.parent;
				}

				// Register prefab
				byte[] bytes = System.Text.Encoding.Unicode.GetBytes(path);
				uint hash = CRC32.Compute(bytes, 0, bytes.Length);
				NetworkManager.Register(this, hash);

				// Register components
				for (int index = 0; index < Components.Length; index++) {
					string subpath = path + ":" + index;
					bytes = System.Text.Encoding.Unicode.GetBytes(subpath);
					hash = CRC32.Compute(bytes, 0, bytes.Length);
					NetworkManager.Register(Components[index], hash);
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkPrefab - {0}] Prefab {1} spawned without a spawner.",
						NetworkID, path
					), this);
				}

			} finally {
				Lock.ExitWriteLock();
			}
		}

		protected override void OnDestroy() {

			// Despawn prefab from the registered spawner
			try {
				Lock.EnterReadLock();
				NetworkSpawner spawner = PrefabSpawner;
				if (spawner != null) spawner.OnPrefabDestroy(this);
			} finally {
				Lock.ExitReadLock();
			}

			// Unregister prefab
			base.OnDestroy();
			Destroyed = true;

		}

		/// <summary>
		/// Return all components managed by this prefab.
		/// </summary>
		/// <returns>All prefab components.</returns>
		public IReadOnlyList<NetworkComponent> GetNetworkComponents() {
			return Components;
		}

		internal NetworkIdentity OnSpawnLocal(NetworkSpawner spawner) {
			try {
				Lock.EnterWriteLock();

				// Save spawner and spawnee
				PrefabSpawner = spawner;
				PrefabSpawnee = null;

				// Register prefab and components
				NetworkIdentity registerID = NetworkManager.Register(this);
				foreach (NetworkComponent component in Components) {
					NetworkManager.Register(component);
				}

				// Return register ID
				return registerID;

			} finally {
				Lock.ExitWriteLock();
			}
		}

		internal void OnSpawnRemote(NetworkSpawner spawner, Peer peer, NetworkIdentity prefabID, NetworkIdentity[] componentID, int length) {
			try {
				Lock.EnterWriteLock();

				// Save spawner and spawnee
				PrefabSpawner = spawner;
				PrefabSpawnee = peer;

				// Warn if identites don't match components
				if (length != Components.Length && NetworkManager.LogExceptions) {
					Debug.LogError(string.Format(
						"[SuperNet] [NetworkPrefab - {0}] Spawned with {1} components but {2} expected.",
						prefabID, length, Components.Length
					), this);
				}

				// Register prefab and components
				NetworkManager.Register(this, prefabID);
				int count = Mathf.Min(Components.Length, length);
				for (int i = 0; i < count; i++) {
					NetworkManager.Register(Components[i], componentID[i]);
				}

			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override void OnNetworkDisconnect(Peer peer) {
			try {
				Lock.EnterReadLock();

				// Check if spawned
				NetworkSpawner spawner = PrefabSpawner;
				if (spawner == null) {
					return;
				}

				// Remove spawnee peer if it disconnected
				Peer previous = Interlocked.CompareExchange(ref PrefabSpawnee, null, peer);

				// Despawn if spawnee disconnected
				if (peer == previous && !spawner.Locked && spawner.IsAuthority) {
					Run(() => {
						if (!Destroyed) Destroy(gameObject);
					});
				}

			} finally {
				Lock.ExitReadLock();
			}
		}

	}

}
