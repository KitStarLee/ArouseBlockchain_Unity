using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	public partial class NetworkManager : MonoBehaviour {

		/// <summary>
		/// Receive and process a component message.
		/// </summary>
		/// <param name="peer">Peer message was received from.</param>
		/// <param name="reader">Message itself.</param>
		/// <param name="info">Message information.</param>
		public static void Receive(Peer peer, Reader reader, MessageReceived info) {

			// Validate
			if (peer == null) {
				throw new ArgumentNullException(nameof(peer), "No peer provided");
			} else if (reader == null) {
				throw new ArgumentNullException(nameof(reader), "No reader provided");
			} else if (info == null) {
				throw new ArgumentNullException(nameof(info), "No info provided");
			}

			// Check if peer is registered
			if (!IsRegistered(peer)) {
				return;
			}

			try {

				// Get all registered peers
				IEnumerable<Peer> peers = GetPeers();

				// Found component
				NetworkComponent component = null;

				// Read message header
				NetworkMessageComponent wrapper = new NetworkMessageComponent();
				wrapper.ReadHeader(reader);

				// Check Network ID
				if (wrapper.NetworkID.IsInvalid) {
					if (LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [Manager] Received invalid network ID from {0}.",
							peer.Remote
						));
					}
					return;
				}

				// Calculate hash
				uint hash = CRC32.Compute(reader.Buffer, reader.First + 2, reader.Last - reader.First - 2);

				// Find component
				try {
					ComponentLock.EnterReadLock();
					ComponentIdentities.TryGetValue(wrapper.NetworkID, out component);
				} finally {
					ComponentLock.ExitReadLock();
				}

				lock (ReceiveLock) {

					// Check if duplicated
					bool received = ReceiveHash.TryGetValue(hash, out HostTimestamp timestamp);
					if (received && timestamp.Elapsed.GetTicks(Host.Frequency) < DuplicateTimeout) {
						return;
					}

					// Save timestamp to check for future duplicates
					ReceiveHash[hash] = info.Timestamp;

				}

				// Process messsage via component
				int position = reader.Position;
				if (component != null) {
					component.InvokeNetworkMessage(peer, reader, info);
				}

				// Check TTL
				if (wrapper.Hops >= wrapper.TTL) {
					if (LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [Manager] Received message from {0} exceeded TTL {1} with {2} hops. Ignoring.",
							peer.Remote, wrapper.TTL, wrapper.Hops
						));
					}
					return;
				}

				// Resend message
				NetworkMessageCopy copy = null;
				foreach (Peer other in peers) {

					// Dont resend to the sender or if peer isn't connected
					if (other == peer) continue;
					if (!other.Connected) continue;

					// Notify component before resending
					if (component != null) {
						reader.Reset(position);
						bool allowed = component.InvokeNetworkResend(peer, other, reader, info);
						if (!allowed) {
							continue;
						}
					}

					// Create copy
					if (copy == null) {
						copy = new NetworkMessageCopy(peer.Host.Allocator, reader, info);
						copy.Buffer[0]++; // Increment hops
					}

					// Send
					try {
						copy.Send(other);
					} catch (Exception e) {
						if (LogExceptions) {
							Debug.LogException(e);
						}
					}

				}

			} catch (Exception exception) {

				if (LogExceptions) {
					Debug.LogException(exception);
				}

			}

		}

		/// <summary>
		/// Send a component message to all registered peers.
		/// </summary>
		/// <param name="component">Registered component to send through.</param>
		/// <param name="message">Message to send.</param>
		public static void Send(NetworkComponent component, IMessage message) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			} else if (message == null) {
				throw new ArgumentNullException(nameof(message), "No message provided");
			}

			// Send
			IEnumerable<Peer> peers = GetPeers();
			NetworkIdentity networkID = component.NetworkID;
			Send(networkID, message, peers, p => true);

		}

		/// <summary>
		/// Send a component message to all registered peers.
		/// </summary>
		/// <param name="component">Registered component to send through.</param>
		/// <param name="message">Message to send.</param>
		/// <param name="filter">Predicate to filter peers.</param>
		public static void Send(NetworkComponent component, IMessage message, Predicate<Peer> filter) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			} else if (message == null) {
				throw new ArgumentNullException(nameof(message), "No message provided");
			}

			// Send
			IEnumerable<Peer> peers = GetPeers();
			NetworkIdentity networkID = component.NetworkID;
			Send(networkID, message, peers, filter);

		}

		/// <summary>
		/// Send a component message to specific peers.
		/// </summary>
		/// <param name="component">Registered component to send through.</param>
		/// <param name="message">Message to send.</param>
		/// <param name="peers">Peers to send to.</param>
		public static void Send(NetworkComponent component, IMessage message, IEnumerable<Peer> peers) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			} else if (message == null) {
				throw new ArgumentNullException(nameof(message), "No message provided");
			}

			// Send
			NetworkIdentity networkID = component.NetworkID;
			Send(networkID, message, peers, p => true);

		}

		/// <summary>
		/// Send a component message to specific peers.
		/// </summary>
		/// <param name="component">Registered component to send through.</param>
		/// <param name="message">Message to send.</param>
		/// <param name="peers">Peers to send to.</param>
		public static void Send(NetworkComponent component, IMessage message, params Peer[] peers) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			} else if (message == null) {
				throw new ArgumentNullException(nameof(message), "No message provided");
			}

			// Send
			NetworkIdentity networkID = component.NetworkID;
			Send(networkID, message, peers, p => true);

		}

		private static void Send(NetworkIdentity networkID, IMessage message, IEnumerable<Peer> peers, Predicate<Peer> filter) {

			try {

				// Check Network ID
				if (networkID.IsInvalid) {
					if (LogExceptions) {
						Debug.LogWarning("[SuperNet] [Manager] Sending message with invalid network ID.");
					}
					return;
				}

				// Create sequence
				ushort sequence = (ushort)Interlocked.Increment(ref SendSequence);
				bool hashed = false;

				// Create message
				NetworkMessageComponent wrapper = new NetworkMessageComponent() {
					Hops = 0,
					TTL = TTL,
					Sequence = sequence,
					NetworkID = networkID,
					Message = message,
				};

				// Send message to all peers
				foreach (Peer peer in peers) {
					if (peer.Connected && filter.Invoke(peer)) {
						
						// Save message hash to check for future duplicates
						try {
							if (!hashed) {
								hashed = true;
								Writer writer = new Writer(peer.Host.Allocator, 0);
								wrapper.Write(writer);
								uint hash = CRC32.Compute(writer.Buffer, 2, writer.Position - 2);
								writer.Dispose();
								lock (ReceiveLock) {
									ReceiveHash[hash] = Host.Now;
								}
							}
						} catch (Exception exception) {
							if (LogExceptions) {
								Debug.LogException(exception);
							}
						}
						
						// Send message
						try {
							peer.Send(wrapper);
						} catch (Exception exception) {
							if (LogExceptions) {
								Debug.LogException(exception);
							}
						}

					}
				}
				
			} catch (Exception exception) {

				if (LogExceptions) {
					Debug.LogException(exception);
				}

			}

		}

	}

}
