using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Base class for synchronized network components.
	/// </summary>
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Core.NetworkComponent.html")]
	public abstract class NetworkComponent : MonoBehaviour {

		/// <summary>
		/// Registered network ID or serialized ID if not registered.
		/// </summary>
		public NetworkIdentity NetworkID {
			get {
				NetworkIdentity registered = NetworkIDRegistered;
				if (registered.IsInvalid) {
					return NetworkIDSerialized;
				} else {
					return registered;
				}
			}
		}

		/// <summary>
		/// True if this component is registered on the network.
		/// </summary>
		public bool IsRegisteredOnNetwork => !NetworkIDRegistered.IsInvalid;

		/// <summary>
		/// Network ID used to identify same components across the network.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("NetworkIdentity")]
		[Tooltip("Network ID used to identify same components across the network.")]
		private NetworkIdentity NetworkIDSerialized = NetworkIdentity.VALUE_INVALID;

		/// <summary>
		/// Registered Network ID or invalid if not registered.
		/// </summary>
		[NonSerialized]
		private NetworkIdentity NetworkIDRegistered = NetworkIdentity.VALUE_INVALID;

		/// <summary>
		/// Automatically registers the component. Do not override.
		/// </summary>
		protected virtual void Start() {
			NetworkManager.Register(this);
		}

		/// <summary>
		/// Automatically unregisters the component. Do not override.
		/// </summary>
		protected virtual void OnDestroy() {
			NetworkManager.Unregister(this);
		}

		/// <summary>
		/// Reset serialized network ID to default. Editor only.
		/// </summary>
		protected void ResetNetworkID() {
		#if UNITY_EDITOR

			// Get prefab information
			var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			bool isPrefabPart = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
			bool isPrefabAsset = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this);
			bool isPrefabInstance = UnityEditor.PrefabUtility.IsPartOfNonAssetPrefabInstance(this);

			// Don't change anything on prefab instances
			if (isPrefabPart && !isPrefabAsset && isPrefabInstance) {
				return;
			}

			// Set to invalid value on prefab assets
			if ((stage != null && stage.scene == gameObject.scene) || (isPrefabPart && isPrefabAsset && !isPrefabInstance)) {
				NetworkIDSerialized = NetworkIdentity.VALUE_INVALID;
				return;
			}

			// All others, set to instance ID
			NetworkIDSerialized = (uint)GetInstanceID();

		#endif
		}

		internal void SetNetworkID(NetworkIdentity id) {
			NetworkIDRegistered = id;
		}

		internal void InvokeNetworkRegister() {
			try {
				OnNetworkRegister();
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal void InvokeNetworkUnregister() {
			try {
				OnNetworkUnregister();
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal bool InvokeNetworkResend(Peer origin, Peer peer, Reader reader, MessageReceived info) {
			try {
				return OnNetworkResend(origin, peer, reader, info);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
				return true;
			}
		}

		internal void InvokeNetworkMessage(Peer peer, Reader reader, MessageReceived info) {
			try {
				OnNetworkMessage(peer, reader, info);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal void InvokeNetworkConnect(Peer peer) {
			try {
				OnNetworkConnect(peer);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal void InvokeNetworkDisconnect(Peer peer) {
			try {
				OnNetworkDisconnect(peer);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal void InvokeNetworkRegister(NetworkComponent component) {
			try {
				OnNetworkRegister(component);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		internal void InvokeNetworkUnregister(NetworkComponent component) {
			try {
				OnNetworkUnregister(component);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception, this);
				}
			}
		}

		/// <summary>
		/// Return all registered peers.
		/// </summary>
		/// <returns>All registered peers.</returns>
		public IReadOnlyList<Peer> GetNetworkPeers() {
			return NetworkManager.GetPeers();
		}

		/// <summary>
		/// Queue action to be ran on the main unity thread.
		/// </summary>
		/// <param name="action">Action to run.</param>
		public void Run(Action action) {
			NetworkManager.Run(action);
		}

		/// <summary>
		/// Queue action to be ran on the main unity thread after a delay.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="seconds">Delay in seconds.</param>
		public void Run(Action action, float seconds) {
			NetworkManager.Run(action, seconds);
		}

		/// <summary>
		/// Queue action to be ran on a separate thread.
		/// </summary>
		/// <param name="action">Action to run.</param>
		public void RunAsync(Action action) {
			NetworkManager.RunAsync(action);
		}

		/// <summary>
		/// Send a component message to all registered peers.
		/// </summary>
		/// <param name="message">Message to send.</param>
		public void SendNetworkMessage(IMessage message) {
			NetworkManager.Send(this, message);
		}

		/// <summary>
		/// Send a component message to all registered peers.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="filter">Predicate to filter peers.</param>
		public void SendNetworkMessage(IMessage message, Predicate<Peer> filter) {
			NetworkManager.Send(this, message, filter);
		}

		/// <summary>
		/// Send a component message to specific peers.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="peers">Peers to send to.</param>
		public void SendNetworkMessage(IMessage message, IEnumerable<Peer> peers) {
			NetworkManager.Send(this, message, peers);
		}

		/// <summary>
		/// Send a component message to specific peers.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="peers">Peers to send to.</param>
		public void SendNetworkMessage(IMessage message, params Peer[] peers) {
			NetworkManager.Send(this, message, peers);
		}

		/// <summary>
		/// Called when a message is received from a peer.
		/// </summary>
		/// <param name="peer">Peer that received the message.</param>
		/// <param name="reader">Message received.</param>
		/// <param name="info">Message information.</param>
		public virtual void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) { }

		/// <summary>
		/// Called when a received message is about to be resent to a peer.
		/// </summary>
		/// <param name="origin">Original sender of the message.</param>
		/// <param name="peer">Peer message is being resent to.</param>
		/// <param name="reader">Message being resent.</param>
		/// <param name="info">Message information.</param>
		/// <returns>True to resend, false to block resending.</returns>
		public virtual bool OnNetworkResend(Peer origin, Peer peer, Reader reader, MessageReceived info) {
			return true;
		}

		/// <summary>
		/// Called when this component is registered locally.
		/// </summary>
		public virtual void OnNetworkRegister() { }

		/// <summary>
		/// Called when this component is unregistered locally.
		/// </summary>
		public virtual void OnNetworkUnregister() { }

		/// <summary>
		/// Called when any component is registered locally.
		/// </summary>
		/// <param name="component">Registered component.</param>
		public virtual void OnNetworkRegister(NetworkComponent component) { }

		/// <summary>
		/// Called when any component is unregistered locally.
		/// </summary>
		/// <param name="component">Unregistered component.</param>
		public virtual void OnNetworkUnregister(NetworkComponent component) { }

		/// <summary>
		/// Called when any peer connects to the network.
		/// </summary>
		/// <param name="peer">Peer that connected.</param>
		public virtual void OnNetworkConnect(Peer peer) { }

		/// <summary>
		/// Called when any peer disconnects from the network.
		/// </summary>
		/// <param name="peer">Peer that disconnected.</param>
		public virtual void OnNetworkDisconnect(Peer peer) { }

	}

}
