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
	/// Manages authority over child authoritative components.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("SuperNet/NetworkAuthority")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkAuthority.html")]
	public sealed class NetworkAuthority : NetworkComponent, INetworkAuthoritative {

		/// <summary>
		/// Network channel to use.
		/// </summary>
		[FormerlySerializedAs("SyncChannel")]
		[Tooltip("Network channel to use.")]
		public byte SyncChannel;

		/// <summary>
		/// True if only we can grant and revoke ownership to peers. Ignored if unlocked.
		/// <para>If this is true at start, ownership will automatically be claimed.</para>
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("Authority")]
		[Tooltip("True if we can grant and revoke ownership to peers. Ignored if unlocked.")]
		private bool Authority;

		/// <summary>
		/// True if only authority can claim access. Set to false to let anybody claim.
		/// </summary>
		[FormerlySerializedAs("Locked")]
		[Tooltip("True if only authority can claim access. Set to false to let anybody claim.")]
		public bool Locked;

		/// <summary>
		/// True if we can grant and revoke ownership to peers.
		/// </summary>
		public bool IsAuthority => Authority;

		/// <summary>
		/// True if we have local ownership over child authoritative components.
		/// </summary>
		public bool IsOwner => OwnershipClaimed;

		/// <summary>
		/// Current owner of the components or null if unknown.
		/// </summary>
		public Peer Owner => OwnershipPeer;

		/// <summary>
		/// Timestamp when the ownership was last changed.
		/// </summary>
		public HostTimestamp Timestamp => GetOwnershipTimestamp();

		// Resources
		private object Lock;
		private INetworkAuthoritative[] Components;
		private HostTimestamp OwnershipTimestamp;
		private uint OwnershipPriority;
		private bool OwnershipClaimed;
		private Peer OwnershipPeer;
		private Peer AuthorityPeer;

		private void Awake() {
			Lock = new object();
			Components = new INetworkAuthoritative[0];
			OwnershipTimestamp = Host.Now;
			OwnershipPriority = 0;
			OwnershipClaimed = false;
			OwnershipPeer = null;
			AuthorityPeer = null;
		}

		protected override void Start() {

			// Claim ownership if we have authority
			lock (Lock) {
				if (Authority && OwnershipPeer == null) {
					OwnershipTimestamp = Host.Now;
					OwnershipPriority = 0;
					OwnershipClaimed = true;
					OwnershipPeer = null;
				}
			}

			// Register authority
			base.Start();
			Rebind();

		}

		private void Reset() {
			ResetNetworkID();
			SyncChannel = 0;
			Authority = true;
			Locked = false;
		}

		private HostTimestamp GetOwnershipTimestamp() {
			lock (Lock) {
				return OwnershipTimestamp;
			}
		}

		/// <summary>
		/// Return all authoritative components managed by this authority.
		/// </summary>
		/// <returns>All authoritative components.</returns>
		public IReadOnlyList<INetworkAuthoritative> GetAuthoritativeComponents() {
			return Components;
		}

		/// <summary>
		/// Force an update to the list of child components this authority has control over.
		/// </summary>
		public void Rebind() {
			Run(() => {

				// Temporary list
				List<INetworkAuthoritative> result = new List<INetworkAuthoritative>();

				// Add all children of this component
				INetworkAuthoritative[] components = GetComponentsInChildren<INetworkAuthoritative>(true);
				foreach (INetworkAuthoritative child in components) {
					if (child is NetworkComponent component) {
						if (component == this) continue;
						result.Add(child);
					}
				}

				// Remove all children of any other child authorities
				foreach (INetworkAuthoritative child in components) {
					if (child is NetworkAuthority auth && auth.transform.parent != transform.parent) {
						INetworkAuthoritative[] remove = auth.GetComponentsInChildren<INetworkAuthoritative>();
						foreach (INetworkAuthoritative obj in remove) {
							result.Remove(obj);
						}
					}
				}

				// Update array
				INetworkAuthoritative[] arrayNew = new INetworkAuthoritative[result.Count];
				result.CopyTo(arrayNew);
				INetworkAuthoritative[] arrayOld = Interlocked.Exchange(ref Components, arrayNew);

				// Check if any components changed
				bool changed = false;
				if (arrayNew.Length == arrayOld.Length) {
					for (int i = 0; i < arrayOld.Length; i++) {
						if (arrayOld[i] != arrayNew[i]) {
							changed = true;
							break;
						}
					}
				} else {
					changed = true;
				}

				// Notify components if changed
				if (changed) {
					if (arrayOld.Length > 0 && NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Components changed.",
							NetworkID, OwnershipPeer.Remote
						), this);
					}
					UpdateComponentOwnership(OwnershipClaimed, GetOwnershipTimestamp());
				}

			});
		}

		/// <summary>
		/// Claim local ownership access.
		/// </summary>
		/// <param name="priority">Claim priority. Higher values have more claim.</param>
		public void Claim(uint priority = 1) {

			// Get current time
			HostTimestamp now = Host.Now;

			lock (Lock) {

				if (Locked) {

					// If we already own access locally, do nothing
					if (OwnershipClaimed) {
						return;
					}

					if (Authority) {

						// Check if access needs to be revoked first
						if (OwnershipPeer != null && OwnershipPeer.Connected) {

							// Log
							if (NetworkManager.LogEvents) {
								Debug.Log(string.Format(
									"[SuperNet] [NetworkAuthority - {0}] Ownership revoked from {0}.",
									NetworkID, OwnershipPeer.Remote
								), this);
							}

							// Revoke access
							SendNetworkMessage(new NetworkMessageRevoke(SyncChannel), OwnershipPeer);

						}

						// Update ownership
						OwnershipTimestamp = now;
						OwnershipPriority = priority;
						OwnershipClaimed = true;
						OwnershipPeer = null;

					} else {

						// Log
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Attempted to claim ownership on a locked authority.",
								NetworkID
							), this);
						}

						// Do nothing else
						return;

					}

				} else {

					// Log
					if (NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Claiming ownership.",
							NetworkID
						), this);
					}

					// Send claim to everybody
					SendNetworkMessage(new NetworkMessageClaim(priority, 0f, SyncChannel));

					// Update ownership
					OwnershipTimestamp = now;
					OwnershipPriority = priority;
					OwnershipClaimed = true;
					OwnershipPeer = null;

				}

			}

			// Update ownership
			UpdateComponentOwnership(true, now);

		}

		/// <summary>
		/// Grant ownership access to a specific peer.
		/// </summary>
		/// <param name="peer">New owner.</param>
		public void Grant(Peer peer) {

			// Validate
			if (peer == null) {
				throw new ArgumentNullException(nameof(peer), "Peer is null");
			}

			// Get current time
			HostTimestamp now = Host.Now;

			lock (Lock) {

				// If already granted, do nothing
				if (OwnershipPeer == peer) {
					return;
				}

				if (Locked) {

					if (Authority) {

						if (OwnershipPeer != peer && OwnershipPeer != null && OwnershipPeer.Connected) {

							// Access needs to be revoked first

							// Log
							if (NetworkManager.LogEvents) {
								Debug.Log(string.Format(
									"[SuperNet] [NetworkAuthority - {0}] Ownership revoked from {1}.",
									NetworkID, OwnershipPeer.Remote
								), this);
							}

							// Revoke access
							SendNetworkMessage(new NetworkMessageRevoke(SyncChannel), OwnershipPeer);

						}

					} else {

						// We don't have authority so we can't grant access

						// Log
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Attempted to grant ownership to {1} without authority.",
								NetworkID, peer.Remote
							), this);
						}

						// Do nothing else
						return;

					}

				}

				if (Host.IsLocal(peer.Remote)) {

					// Log
					if (NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Ownership granted locally to {1}.",
							NetworkID, peer.Remote
						), this);
					}

					// Update ownership
					OwnershipTimestamp = now;
					OwnershipPriority = 0;
					OwnershipClaimed = true;
					OwnershipPeer = null;

				} else {

					if (OwnershipPeer != peer) {

						// Log
						if (NetworkManager.LogEvents) {
							Debug.Log(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Ownership granted to {1}.",
								NetworkID, peer.Remote
							), this);
						}

						// Grant access
						SendNetworkMessage(new NetworkMessageGrant(SyncChannel), peer);

					}

					// Update ownership
					OwnershipTimestamp = now;
					OwnershipPriority = 0;
					OwnershipClaimed = false;
					OwnershipPeer = peer;

				}

			}

			// Update ownership
			if (Host.IsLocal(peer.Remote)) {
				UpdateComponentOwnership(true, now);
			} else {
				UpdateComponentOwnership(false, now);
			}

		}

		void INetworkAuthoritative.OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp) {

			// Update authority and resync state
			SetAuthority(authority);

		}

		/// <summary>
		/// Update authority on this component.
		/// </summary>
		/// <param name="authority">Authority to set.</param>
		public void SetAuthority(bool authority) {

			lock (Lock) {

				// Check if authority changed
				if (Authority == authority) {
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkAuthority - {0}] Authority {1}.",
						NetworkID, authority ? "gained" : "lost"
					), this);
				}

				// Update authority
				Authority = authority;

				// Remove authority peer if we are now authority
				if (Authority) {
					Interlocked.Exchange(ref AuthorityPeer, null);
				}

				// Send or request state
				if (IsRegisteredOnNetwork) {
					foreach (Peer peer in GetNetworkPeers()) {
						SyncNetworkState(peer, false);
					}
				}

			}

		}

		public override void OnNetworkRegister() {

			lock (Lock) {

				// Send or request state
				foreach (Peer peer in GetNetworkPeers()) {
					SyncNetworkState(peer, false);
				}

			}

		}

		public override void OnNetworkConnect(Peer peer) {

			// Ignore if local peer
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			// Send or request state
			lock (Lock) {
				SyncNetworkState(peer, true);
			}

		}

		public override void OnNetworkDisconnect(Peer peer) {

			// Ignore if local peer
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			bool reclaim = false;
			HostTimestamp now = Host.Now;

			lock (Lock) {

				// Remove authority peer or component owner if they disconnected
				Peer previousAuthority = Interlocked.CompareExchange(ref AuthorityPeer, null, peer);
				Peer previousOwner = Interlocked.CompareExchange(ref OwnershipPeer, null, peer);

				if (Locked) {

					// Request state from a new authority
					if (!Authority && previousAuthority == peer) {
						SendNetworkMessage(new NetworkMessageRequest(SyncChannel));
					}

					// Reclaim if component owner disconnected and we have authority
					if (Authority && previousOwner == peer) {

						// Log
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Owner {0} disconnected. Ownership revoked.",
								NetworkID, peer.Remote
							), this);
						}

						// Update ownership
						OwnershipTimestamp = now;
						OwnershipPriority = 0;
						OwnershipClaimed = true;
						OwnershipPeer = null;
						reclaim = true;

					}

				} else {

					// Reclaim if component owner disconnected
					if (previousOwner == peer) {

						// Log
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Owner {0} disconnected. Reclaiming ownership.",
								NetworkID, peer.Remote
							), this);
						}

						// Send claim to everybody
						SendNetworkMessage(new NetworkMessageClaim(0, 0f, SyncChannel), peer);

						// Update ownership
						OwnershipTimestamp = now;
						OwnershipPriority = 0;
						OwnershipClaimed = true;
						OwnershipPeer = null;
						reclaim = true;

					}

				}

			}

			// Update ownerhip
			if (reclaim) UpdateComponentOwnership(true, now);

		}

		private void SyncNetworkState(Peer peer, bool requestAuthority) {
			if (Host.IsLocal(peer.Remote)) {
				return;
			} else if (Locked) {
				if (Authority) {
					if (peer == OwnershipPeer) {
						SendNetworkMessage(new NetworkMessageGrant(SyncChannel), peer);
					} else {
						SendNetworkMessage(new NetworkMessageRevoke(SyncChannel), peer);
					}
				} else if (requestAuthority || AuthorityPeer == null) {
					SendNetworkMessage(new NetworkMessageRequest(SyncChannel), peer);
				}
			} else {
				if (OwnershipClaimed) {
					float seconds = (float)(Host.Now - OwnershipTimestamp).Seconds;
					SendNetworkMessage(new NetworkMessageClaim(OwnershipPriority, seconds, SyncChannel), peer);
				} else {
					SendNetworkMessage(new NetworkMessageRequest(SyncChannel), peer);
				}
			}
		}

		public override bool OnNetworkResend(Peer origin, Peer peer, Reader reader, MessageReceived info) {

			// Dont resend to local connections
			if (Host.IsLocal(peer.Remote)) {
				return false;
			}

			// Read header and decide if it needs to be resent
			Header header = reader.ReadEnum<Header>();
			switch (header) {
				case Header.Request:
					if (Locked) {
						// Only resend if we dont have authority
						return !Authority;
					} else {
						// Unlocked, anybody can respond
						return true;
					}
				case Header.Claim:
					// Only resend if unlocked
					return !Locked;
				case Header.Grant:
				case Header.Revoke:
					// Only sent by authority to a single peer, don't resend
					return false;
				default:
					// Invalid header, don't resend
					return false;
			}

		}

		public override void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) {

			// Ignore messages from local connections
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			// Read header and process message
			Header header = reader.ReadEnum<Header>();
			switch (header) {
				case Header.Request:
					OnNetworkMessageRequest(peer);
					break;
				case Header.Claim:
					OnNetworkMessageClaim(peer, reader, info);
					break;
				case Header.Grant:
					OnNetworkMessageGrant(peer, info.Timestamp);
					break;
				case Header.Revoke:
					OnNetworkMessageRevoke(peer, info.Timestamp);
					break;
				default:
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAuthority - {0}] Received invalid header {1} from {2}. Ignoring.",
						NetworkID, header, peer.Remote
					), this);
					break;
			}

		}

		private void OnNetworkMessageRequest(Peer peer) {
			lock (Lock) {

				if (Locked) {

					// Respond to request if we have authority
					if (Authority) {
						if (peer == OwnershipPeer) {
							SendNetworkMessage(new NetworkMessageGrant(SyncChannel), peer);
						} else {
							SendNetworkMessage(new NetworkMessageRevoke(SyncChannel), peer);
						}
					}

				} else {

					// If we own components locally, send claim
					if (OwnershipClaimed) {
						float seconds = (float)(Host.Now - OwnershipTimestamp).Seconds;
						SendNetworkMessage(new NetworkMessageClaim(OwnershipPriority, seconds, SyncChannel), peer);
					}

				}

			}
		}

		private void OnNetworkMessageClaim(Peer peer, Reader reader, MessageReceived info) {

			lock (Lock) {

				// Ignore if locked
				if (Locked) {

					// Log
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Ownership claimed by {1} but authority is locked. Ignoring.",
							NetworkID, peer.Remote
						), this);
					}

					// Do nothing else
					return;

				}

				// If we already dont have local ownership, do nothing
				if (!OwnershipClaimed) {
					return;
				}

				// Read message
				uint priority = reader.ReadUInt32();
				HostTimespan age = HostTimespan.FromSeconds(reader.ReadSingle());
				HostTimestamp time = info.Timestamp - age;

				// If we have bigger priority, send reclaim back
				if (OwnershipPriority > priority) {
					if (NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Ownership claimed by {1} with priority {2} but we have priority {3}. Reclaiming back.",
							NetworkID, peer.Remote, priority, OwnershipPriority
						), this);
					}
					float seconds = (float)(Host.Now - OwnershipTimestamp).Seconds;
					SendNetworkMessage(new NetworkMessageClaim(OwnershipPriority, seconds, SyncChannel), peer);
					return;
				}

				// If priority is 0, the oldest claim wins
				if (OwnershipPriority == priority && priority == 0 && OwnershipTimestamp < time) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Ownership claimed by {1}, but we claimed {2} seconds earlier. Reclaiming back.",
							NetworkID, peer.Remote, (time - OwnershipTimestamp).Seconds
						), this);
					}
					float seconds = (float)(Host.Now - OwnershipTimestamp).Seconds;
					SendNetworkMessage(new NetworkMessageClaim(OwnershipPriority, seconds, SyncChannel), peer);
					return;
				}

				// If priority is non-zero, the newest claim wins
				if (OwnershipPriority == priority && priority != 0 && OwnershipTimestamp > time) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [NetworkAuthority - {0}] Ownership claimed by {1}, but we claimed {2} seconds after. Reclaiming back.",
							NetworkID, peer.Remote, (OwnershipTimestamp - time).Seconds
						), this);
					}
					float seconds = (float)(Host.Now - OwnershipTimestamp).Seconds;
					SendNetworkMessage(new NetworkMessageClaim(OwnershipPriority, seconds, SyncChannel), peer);
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkAuthority - {0}] Ownership claimed by {1} with priority {2}.",
						NetworkID, peer.Remote, priority
					), this);
				}

				// Update ownerhip
				OwnershipTimestamp = info.Timestamp;
				OwnershipPriority = 0;
				OwnershipClaimed = false;
				OwnershipPeer = peer;

			}

			// Update ownerhip
			UpdateComponentOwnership(false, info.Timestamp);

		}

		private void OnNetworkMessageGrant(Peer peer, HostTimestamp timestamp) {

			lock (Lock) {

				if (Locked) {

					// Ignore if we have authority
					if (Authority) {
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Granted ownership by {1} but we have authority. Ignoring.",
								NetworkID, peer.Remote
							), this);
						}
						return;
					}

					// Save authority peer
					Peer previous = Interlocked.Exchange(ref AuthorityPeer, peer);

					// If authority peer changed, request state from the old authority peer
					// This prevents unauthorized peers to change ownership permanently
					if (previous != null && previous != peer && previous.Connected) {
						SendNetworkMessage(new NetworkMessageRequest(SyncChannel), previous);
					}

				}

				// If we already have local ownership, do nothing
				if (OwnershipClaimed) {
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkAuthority - {0}] Granted ownership by {1}.",
						NetworkID, peer.Remote
					), this);
				}

				// If unlocked, send claim to everybody
				if (!Locked) {
					SendNetworkMessage(new NetworkMessageClaim(0, 0f, SyncChannel));
				}

				// Update ownerhip
				OwnershipTimestamp = timestamp;
				OwnershipPriority = 0;
				OwnershipClaimed = true;
				OwnershipPeer = null;

			}

			// Update ownerhip
			UpdateComponentOwnership(true, timestamp);

		}

		private void OnNetworkMessageRevoke(Peer peer, HostTimestamp timestamp) {

			lock (Lock) {

				if (Locked) {

					// Ignore if we have authority
					if (Authority) {
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [NetworkAuthority - {0}] Revoked ownership by {1} but we have authority. Ignoring.",
								NetworkID, peer.Remote
							), this);
						}
						return;
					}

					// Save authority peer
					Peer previous = Interlocked.Exchange(ref AuthorityPeer, peer);

					// If authority peer changed, request state from the old authority peer
					// This prevents unauthorized peers to change ownership permanently
					if (previous != null && previous != peer && previous.Connected) {
						SendNetworkMessage(new NetworkMessageRequest(SyncChannel), previous);
					}

				}

				// If we already don't have local ownership, do nothing
				if (!OwnershipClaimed) {
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkAuthority - {0}] Revoked ownership by {1}.",
						NetworkID, peer.Remote
					), this);
				}

				// Update ownerhip
				OwnershipTimestamp = timestamp;
				OwnershipPriority = 0;
				OwnershipClaimed = false;
				OwnershipPeer = peer;

			}

			// Update ownerhip
			UpdateComponentOwnership(false, timestamp);

		}

		private void UpdateComponentOwnership(bool authority, HostTimestamp timestamp) {

			// Go through all bound components and update their authority
			INetworkAuthoritative[] components = Components;
			foreach (INetworkAuthoritative component in components) {
				try {
					component.OnNetworkAuthorityUpdate(authority, timestamp);
				} catch (Exception exception) {
					if (NetworkManager.LogExceptions) {
						Debug.LogException(exception, this);
					}
				}
			}

		}

		private enum Header : byte {

			/// <summary>Sent when requesting state from authority.</summary>
			Request = 1,

			/// <summary>Sent by authority to grant component access to a single peer.</summary>
			Grant = 2,

			/// <summary>Sent by authority to revoke component access from a single peer.</summary>
			Revoke = 3,

			/// <summary>Sent when claiming access if unlocked.</summary>
			Claim = 4,

		}

		private struct NetworkMessageRequest : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageRequest(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Request);
			}

		}

		private struct NetworkMessageGrant : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageGrant(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Grant);
			}

		}

		private struct NetworkMessageRevoke : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageRevoke(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Revoke);
			}

		}

		private struct NetworkMessageClaim : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public readonly uint Priority;
			public readonly float Seconds;
			public readonly byte Channel;
			
			public NetworkMessageClaim(uint priority, float seconds, byte channel) {
				Priority = priority;
				Seconds = seconds;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.WriteEnum(Header.Claim);
				writer.Write(Priority);
				writer.Write(Seconds);
			}

		}

	}

}
