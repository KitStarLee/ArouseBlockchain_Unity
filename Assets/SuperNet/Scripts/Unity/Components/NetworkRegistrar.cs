using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Singleton used to notify network components about register and unregister events.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("SuperNet/NetworkRegistrar")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkRegistrar.html")]
	public sealed class NetworkRegistrar : NetworkComponent {

		private static bool Initialized = false;
		private static NetworkRegistrar Instance = null;
		private static readonly NetworkIdentity DefaultNetworkID = 191898903;
		
		private static NetworkRegistrar GetInstance() {
			if (Initialized) return Instance;
			NetworkManager manager = FindObjectOfType<NetworkManager>();
			NetworkRegistrar instance = FindObjectOfType<NetworkRegistrar>();
			if (instance != null) {
				Instance = instance;
			} else if (manager != null) {
				Instance = manager.gameObject.AddComponent<NetworkRegistrar>();
				NetworkManager.Register(Instance, DefaultNetworkID);
			} else {
				GameObject obj = new GameObject(nameof(NetworkRegistrar));
				Instance = obj.AddComponent<NetworkRegistrar>();
				NetworkManager.Register(Instance, DefaultNetworkID);
			}
			Initialized = true;
			return Instance;
		}

		/// <summary>
		/// Network channel to use.
		/// </summary>
		[FormerlySerializedAs("SyncChannel")]
		[Tooltip("Network channel to use.")]
		public byte SyncChannel = 0;

		// Resources
		private ReaderWriterLockSlim Lock;
		private Dictionary<NetworkIdentity, Tracker> Trackers;

		private class Tracker {
			public NetworkIdentity NetworkID;
			public INetworkRegisterable Component;
			public HashSet<Peer> Peers;
		}

		private void Awake() {
			if (Initialized && Instance != this) {
				Destroy(this);
			} else {
				transform.parent = null;
				DontDestroyOnLoad(this);
				Instance = this;
				Initialized = true;
				Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
				Trackers = new Dictionary<NetworkIdentity, Tracker>();
			}
		}

		protected override void OnDestroy() {
			base.OnDestroy();
		}

		public override void OnNetworkConnect(Peer peer) {

			// Notify peer so it sends us register list
			SendNetworkMessage(new NetworkMessageConnect(SyncChannel), peer);

			// Send register list to the peer
			SendRegisterList(peer);

		}

		public override void OnNetworkDisconnect(Peer peer) {

			// Notify peer so it removes us from its register list
			SendNetworkMessage(new NetworkMessageDisconnect(SyncChannel), peer);

			// Remove peer from our register list
			RemovePeerFromTrackers(peer);

		}

		public override void OnNetworkRegister(NetworkComponent component) {
			if (component is INetworkRegisterable registerable) {

				// Get registered ID
				NetworkIdentity networkID = component.NetworkID;
				bool added = false;

				try {
					Lock.EnterWriteLock();

					// Find or create a tracker
					Trackers.TryGetValue(networkID, out Tracker tracker);
					if (tracker == null) {
						tracker = new Tracker() {
							NetworkID = networkID,
							Component = registerable,
							Peers = new HashSet<Peer>(),
						};
						Trackers[networkID] = tracker;
						added = true;
					}

					// Save tracker component
					tracker.Component = registerable;

				} finally {
					Lock.ExitWriteLock();
				}

				// If newly added ID, send register message to all peers
				if (added) {
					SendNetworkMessage(new NetworkMessageRegister(networkID, SyncChannel));
				}

			}
		}


		public override void OnNetworkUnregister(NetworkComponent component) {

			if (component is INetworkRegisterable) {

				// Get registered ID
				NetworkIdentity networkID = component.NetworkID;
				bool removed = false;

				// Remove component from tracker and remove tracker if empty
				try {
					Lock.EnterWriteLock();
					Trackers.TryGetValue(networkID, out Tracker tracker);
					if (tracker != null) {
						tracker.Component = null;
						if (tracker.Peers.Count <= 0) {
							Trackers.Remove(networkID);
							removed = true;
						}
					}
				} finally {
					Lock.ExitWriteLock();
				}

				// If tracker removed, send unregister message
				if (removed) {
					SendNetworkMessage(new NetworkMessageUnregister(networkID, SyncChannel));
				}

			}

		}

		public override void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) {

			// Read message
			Header header = reader.ReadEnum<Header>();
			
			// Process message
			switch (header) {
				case Header.Register:
					OnNetworkMessageRegister(peer, reader);
					break;
				case Header.Unregister:
					OnNetworkMessageUnregister(peer, reader);
					break;
				case Header.Connect:
					OnNetworkMessageConnect(peer);
					break;
				case Header.Disconnect:
					OnNetworkMessageDisconnect(peer);
					break;
				default:
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [NetworkRegistrar - {0}] Received invalid header {1} from {2}. Ignoring.",
							NetworkID, header, peer.Remote
						), this);
					}
					break;
			}

		}

		public override bool OnNetworkResend(Peer origin, Peer peer, Reader reader, MessageReceived info) {

			// Don't automatically resend any messages, we do that manually
			return false;

		}

		private void OnNetworkMessageRegister(Peer peer, Reader reader) {

			// Read network ID
			NetworkIdentity networkID = reader.ReadNetworkID();

			// Temporary variables
			INetworkRegisterable component = null;
			bool resend = false;

			try {
				Lock.EnterWriteLock();

				// Find tracker
				Trackers.TryGetValue(networkID, out Tracker tracker);

				// If tracker doesn't exist, create one
				if (tracker == null) {
					tracker = new Tracker() {
						NetworkID = networkID,
						Component = null,
						Peers = new HashSet<Peer>(),
					};
					Trackers[networkID] = tracker;
					resend = true;
				}

				// Add peer to tracker
				bool added = tracker.Peers.Add(peer);

				// Save component if peer was newly added
				if (added) {
					component = tracker.Component;
				}

			} finally {
				Lock.ExitWriteLock();
			}

			// Log
			if (NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [NetworkRegistrar - {0}] Component {1} registered on {2}.",
					NetworkID, networkID, peer.Remote
				), this);
			}

			// If tracker was newly created, resend message to all other peers
			if (resend) {
				SendNetworkMessage(new NetworkMessageRegister(networkID, SyncChannel), p => p != peer);
			}

			// Notify component
			if (component != null) {
				try {
					component.OnNetworkRegister(peer);
				} catch (Exception exception) {
					if (NetworkManager.LogExceptions) {
						Debug.LogException(exception, this);
					}
				}
			}

		}

		private void OnNetworkMessageUnregister(Peer peer, Reader reader) {

			// Read network ID
			NetworkIdentity networkID = reader.ReadNetworkID();

			// Temporary variables
			INetworkRegisterable component = null;
			bool resend = false;

			try {
				Lock.EnterWriteLock();

				// Find tracker
				Trackers.TryGetValue(networkID, out Tracker tracker);
				if (tracker == null) {
					return;
				}

				// Remove peer from tracker
				bool removed = tracker.Peers.Remove(peer);

				// Remove tracker if empty
				if (tracker.Peers.Count <= 0 && tracker.Component == null) {
					Trackers.Remove(networkID);
					resend = true;
				}

				// Save component if peer was removed
				if (removed) {
					component = tracker.Component;
				}

			} finally {
				Lock.ExitWriteLock();
			}

			// Log
			if (NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [NetworkRegistrar - {0}] Component {1} unregistered from {2}.",
					NetworkID, networkID, peer.Remote
				), this);
			}

			// Resend message if tracker was removed
			if (resend) {
				SendNetworkMessage(new NetworkMessageUnregister(networkID, SyncChannel), p => p != peer);
			}

			// Notify component
			if (component != null) {
				try {
					component.OnNetworkUnregister(peer);
				} catch (Exception exception) {
					if (NetworkManager.LogExceptions) {
						Debug.LogException(exception, this);
					}
				}
			}

		}

		private void OnNetworkMessageConnect(Peer peer) {

			// Send register list to the peer
			SendRegisterList(peer);

		}

		private void OnNetworkMessageDisconnect(Peer peer) {

			// Remove peer from our register list
			RemovePeerFromTrackers(peer);

		}

		private void RemovePeerFromTrackers(Peer peer) {
			try {
				Lock.EnterWriteLock();

				// Remove peer from all trackers
				foreach (Tracker tracker in Trackers.Values) {
					tracker.Peers.Remove(peer);
				}

				// Save all empty trackers
				List<NetworkIdentity> empty = new List<NetworkIdentity>();
				foreach (Tracker tracker in Trackers.Values) {
					if (tracker.Peers.Count <= 0 && tracker.Component == null) {
						empty.Add(tracker.NetworkID);
					}
				}

				// Remove network ID and notify all peers for every empty tracker
				foreach (NetworkIdentity networkID in empty) {
					SendNetworkMessage(new NetworkMessageUnregister(networkID, SyncChannel));
					Trackers.Remove(networkID);
				}

			} finally {
				Lock.ExitWriteLock();
			}
		}

		private void SendRegisterList(Peer peer) {
			try {
				Lock.EnterReadLock();
				foreach (Tracker tracker in Trackers.Values) {
					if (tracker.Peers.Count > 0 || tracker.Component != null) {
						SendNetworkMessage(new NetworkMessageRegister(tracker.NetworkID, SyncChannel), peer);
					}
				}
			} finally {
				Lock.ExitReadLock();
			}
		}

		private enum Header : byte {

			/// <summary>Sent whenever a component is registered on a peer.</summary>
			Register = 1,

			/// <summary>Sent whenever a component is unregistered from a peer.</summary>
			Unregister = 2,

			/// <summary>Sent whenever a peer connects to request list of registered components.</summary>
			Connect = 3,

			/// <summary>Sent whenever a peer disconnects to remove itself from list of registered components.</summary>
			Disconnect = 4,

		}

		private struct NetworkMessageRegister : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly NetworkIdentity NetworkID;
			public readonly byte Channel;

			public NetworkMessageRegister(NetworkIdentity networkID, byte channel) {
				NetworkID = networkID;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Register);
				writer.Write(NetworkID);
			}

		}

		private struct NetworkMessageUnregister : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly NetworkIdentity NetworkID;
			public readonly byte Channel;

			public NetworkMessageUnregister(NetworkIdentity networkID, byte channel) {
				NetworkID = networkID;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Unregister);
				writer.Write(NetworkID);
			}

		}

		private struct NetworkMessageConnect : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageConnect(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Connect);
			}

		}

		private struct NetworkMessageDisconnect : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageDisconnect(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Disconnect);
			}

		}

	}

}
