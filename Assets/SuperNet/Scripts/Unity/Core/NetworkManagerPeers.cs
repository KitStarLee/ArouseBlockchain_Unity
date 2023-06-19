using SuperNet.Netcode.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	public partial class NetworkManager : MonoBehaviour {

		// Resources
		private static object PeersLock;
		private static HashSet<Peer> PeersSet;
		private static Peer[] PeersArray;

		private static void InitializePeers() {
			PeersLock = new object();
			PeersSet = new HashSet<Peer>();
			PeersArray = new Peer[0];
		}

		/// <summary>
		/// Return all registered peers.
		/// </summary>
		/// <returns>All registered peers.</returns>
		public static IReadOnlyList<Peer> GetPeers() {
			return PeersArray;
		}

		/// <summary>
		/// Check if peer is registered.
		/// </summary>
		/// <param name="peer">Peer to check.</param>
		/// <returns>True if registered, false if not.</returns>
		public static bool IsRegistered(Peer peer) {
			lock (PeersLock) {
				return PeersSet.Contains(peer);
			}
		}

		/// <summary>
		/// Register a peer to be used for sending and receiving component messages.
		/// </summary>
		/// <param name="peer">Peer to register.</param>
		public static void Register(Peer peer) {

			// Validate
			if (peer == null) {
				throw new ArgumentNullException(nameof(peer), "No peer provided");
			}

			// Add peer and update array if needed
			try {
				lock (PeersLock) {
					bool added = PeersSet.Add(peer);
					if (added) {
						Peer[] array = new Peer[PeersSet.Count];
						PeersSet.CopyTo(array, 0);
						Interlocked.Exchange(ref PeersArray, array);
					} else {
						return;
					}
				}
			} catch (Exception exception) {
				if (LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Log
			if (LogEvents) {
				Debug.Log(string.Format("[SuperNet] [Manager] Peer {0} registered.", peer.Remote));
			}

			// Notify components
			IReadOnlyList<NetworkComponent> components = GetComponents();
			foreach (NetworkComponent component in components) {
				if (component != null) {
					component.InvokeNetworkConnect(peer);
				}
			}

		}

		/// <summary>
		/// Unregister a peer to stop being used for sending component messages.
		/// </summary>
		/// <param name="peer">Peer to unregister.</param>
		public static void Unregister(Peer peer) {

			// Validate
			if (peer == null) {
				throw new ArgumentNullException(nameof(peer), "No peer provided");
			}

			// Remove peer and update array if needed
			try {
				lock (PeersLock) {
					bool removed = PeersSet.Remove(peer);
					if (removed) {
						Peer[] array = new Peer[PeersSet.Count];
						PeersSet.CopyTo(array, 0);
						Interlocked.Exchange(ref PeersArray, array);
					} else {
						return;
					}
				}
			} catch (Exception exception) {
				if (LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Log
			if (LogEvents) {
				Debug.Log(string.Format("[SuperNet] [Manager] Peer {0} unregistered.", peer.Remote));
			}

			// Notify components
			IReadOnlyList<NetworkComponent> components = GetComponents();
			foreach (NetworkComponent component in components) {
				if (component != null) {
					component.InvokeNetworkDisconnect(peer);
				}
			}

		}

	}

}
