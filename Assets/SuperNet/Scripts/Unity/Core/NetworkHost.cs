using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Manages a network socket and all network communication between peers.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("SuperNet/NetworkHost")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Core.NetworkHost.html")]
	public sealed class NetworkHost : MonoBehaviour, IHostListener, IPeerListener {

		/// <summary>
		/// The maximum number of concurrent network connections to support.
		/// </summary>
		[FormerlySerializedAs("MaxConnections")]
		[Tooltip("Maximum number of concurrent connections.")]
		public int MaxConnections;

		/// <summary>
		/// Do not destroy the host when loading a new Scene.
		/// </summary>
		[FormerlySerializedAs("PersistAcrossScenes")]
		[Tooltip("Do not destroy the host when loading a new Scene.")]
		public bool PersistAcrossScenes;

		/// <summary>
		/// Should the host start listening on startup.
		/// </summary>
		[FormerlySerializedAs("AutoStartup")]
		[Tooltip("Should the host start listening on startup.")]
		public bool AutoStartup;

		/// <summary>
		/// Automatically register all peers to the network.
		/// </summary>
		[FormerlySerializedAs("AutoRegister")]
		[Tooltip("Automatically register all peers to the network.")]
		public bool AutoRegister;

		/// <summary>
		/// Remote address to connect to on startup or empty to disable.
		/// </summary>
		[FormerlySerializedAs("AutoConnect")]
		[Tooltip("Remote address to connect to on startup or empty to disable.")]
		public string AutoConnect;

		/// <summary>
		/// Host configuration values.
		/// </summary>
		[FormerlySerializedAs("HostConfiguration")]
		[Tooltip("Netcode configuration for the host.")]
		public HostConfig HostConfiguration;

		/// <summary>
		/// Peer configuration values.
		/// </summary>
		[FormerlySerializedAs("HostConfiguration")]
		[Tooltip("Netcode configuration for peers on this host.")]
		public PeerConfig PeerConfiguration;

		/// <summary>
		/// True if host is active and listening.
		/// </summary>
		public bool Listening => !(Host?.Disposed ?? true);

		/// <summary>
		/// Host allocator or an empty allocator if not listening.
		/// </summary>
		public Allocator Allocator => Host?.Allocator ?? EmptyAllocator;

		/// <summary>
		/// Peer events for all peers.
		/// </summary>
		public PeerEvents PeerEvents { get; private set; }

		/// <summary>
		/// Host events for this host.
		/// </summary>
		public HostEvents HostEvents { get; private set; }

		// Resources
		private readonly ReaderWriterLockSlim Lock;
		private readonly Dictionary<IPEndPoint, Peer> Peers;
		private readonly Dictionary<IPEndPoint, IPeerListener> Listeners;
		private readonly Allocator EmptyAllocator;
		private Peer[] PeersArray;
		private Host Host;

		public NetworkHost() {
			MaxConnections = 32;
			PersistAcrossScenes = false;
			AutoStartup = false;
			AutoConnect = "";
			AutoRegister = true;
			HostConfiguration = new HostConfig();
			PeerConfiguration = new PeerConfig();
			PeerEvents = new PeerEvents();
			HostEvents = new HostEvents();
			Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			Peers = new Dictionary<IPEndPoint, Peer>();
			Listeners = new Dictionary<IPEndPoint, IPeerListener>();
			EmptyAllocator = new Allocator();
			PeersArray = new Peer[0];
			Host = null;
		}

		private void Reset() {
			MaxConnections = 32;
			PersistAcrossScenes = true;
			AutoStartup = true;
			AutoConnect = "";
			AutoRegister = true;
			HostConfiguration = new HostConfig();
			PeerConfiguration = new PeerConfig();
		}

		private void Awake() {
			NetworkManager.Initialize();
		}

		private void Start() {

			// Persist across scenes if requested
			if (PersistAcrossScenes) {
				transform.parent = null;
				DontDestroyOnLoad(this);
			}

			// Automatically startup if requested
			if (AutoStartup) {
				bool success = Startup();
				bool connect = !string.IsNullOrWhiteSpace(AutoConnect);
				if (success && connect) IPResolver.Resolve(AutoConnect, OnConnectResolve);
			}

		}

		private void OnDestroy() {

			// Get host to dispose
			Host host = null;
			try {
				Lock.EnterWriteLock();
				host = Host;
				Host = null;
			} finally {
				Lock.ExitWriteLock();
			}

			// Log
			if (host != null && NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Destroying host listening on '{0}'.",
					host.BindAddress
				));
            }

			// Dispose host
			host?.Dispose();

		}

		/// <summary>
		/// Get all peers on this host.
		/// </summary>
		/// <returns>Array of all peers on this host.</returns>
		public IReadOnlyList<Peer> GetPeers() {
			return PeersArray;
		}

		/// <summary>
		/// Get netcode host or null if not listening.
		/// </summary>
		/// <returns>Netcode host.</returns>
		public Host GetHost() {
			try {
				Lock.EnterReadLock();
				return Host;
			} finally {
				Lock.ExitReadLock();
			}
		}

		/// <summary>
		/// Get address this host is listening on or loopback if none.
		/// This is usually <c>0.0.0.0</c> with the listen port.
		/// </summary>
		/// <returns>A valid address.</returns>
		public IPEndPoint GetBindAddress() {
			try {
				Lock.EnterReadLock();
				if (Host != null) {
					return Host.BindAddress;
				} else if (HostConfiguration.DualMode) {
					return new IPEndPoint(IPAddress.IPv6Loopback, 0);
				} else {
					return new IPEndPoint(IPAddress.Loopback, 0);
				}
			} finally {
				Lock.ExitReadLock();
			}
		}

		/// <summary>
		/// Get loopback address for this host.
		/// This is usually <c>127.0.0.1</c> with the listen port.
		/// </summary>
		/// <returns>Loopback address.</returns>
		public IPEndPoint GetLoopbackAddress() {
			IPEndPoint address = GetBindAddress();
			if (HostConfiguration.DualMode) {
				return new IPEndPoint(IPAddress.IPv6Loopback, address.Port);
			} else {
				return new IPEndPoint(IPAddress.Loopback, address.Port);
			}
		}

		/// <summary>
		/// Get LAN address for this host.
		/// An example is <c>192.168.1.10</c> with the listen port.
		/// </summary>
		/// <returns>Local address.</returns>
		public IPEndPoint GetLocalAddress() {
			IPEndPoint address = GetBindAddress();
			if (HostConfiguration.DualMode) {
				return new IPEndPoint(IPResolver.GetLocalAddressIPv6(), address.Port);
			} else {
				return new IPEndPoint(IPResolver.GetLocalAddress(), address.Port);
			}
		}

		/// <summary>
		/// Set listener for a specific peer.
		/// </summary>
		/// <param name="remote">Peer remote address.</param>
		/// <param name="listener">Listener to replace with.</param>
		/// <returns>True if replaced, false if peer doesn't exist.</returns>
		public bool SetListener(IPEndPoint remote, IPeerListener listener) {
			try {
				Lock.EnterWriteLock();
				if (Peers.ContainsKey(remote)) {
					Listeners[remote] = listener;
					return true;
				} else {
					return false;
				}
			} finally {
				Lock.ExitWriteLock();
			}
		}

		/// <summary>
		/// Start listening.
		/// </summary>
		/// <param name="rethrow">Rethrow any exceptions on failure.</param>
		/// <returns>True on success, false on failure.</returns>
		public bool Startup(bool rethrow = false) {
			try {
				Lock.EnterWriteLock();

				// Check if already listening
				if (Host != null && !Host.Disposed) {
					return true;
				}

				// Start listening
				try {
					Host = new Host(HostConfiguration, this);
				} catch (Exception exception) {
					if (NetworkManager.LogExceptions) {
						Debug.LogException(exception);
					}
					if (rethrow) throw;
					return false;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [Host] Host started listening on '{0}'",
						Host.BindAddress
					));
				}

				// Success
				return true;

			} finally {
				Lock.ExitWriteLock();
			}
		}

		/// <summary>
		/// Instantly dispose all resources held by this host and connected peers.
		/// </summary>
		public void Dispose() {

			// Get host to dispose
			Host host = null;
			try {
				Lock.EnterWriteLock();
				host = Host;
				Host = null;
			} finally {
				Lock.ExitWriteLock();
			}

			// Log
			if (host != null && NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Disposing host listening on '{0}' with {1} peers.",
					host.BindAddress, Peers.Count
				));
			}

			// Dispose
			host?.SetListener(null);
			host?.Dispose();

		}

		/// <summary>
		/// Gracefully disconnect all peers and perform a shutdown.
		/// </summary>
		public void Shutdown() {

			// Get host to shutdown
			Host host = null;
			try {
				Lock.EnterWriteLock();
				host = Host;
				Host = null;
			} finally {
				Lock.ExitWriteLock();
			}

			// Log
			if (host != null && NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Shutting down host listening on '{0}' with {1} peers.",
					host.BindAddress, Peers.Count
				));
			}

			// Shutdown
			host?.Shutdown();

		}

		/// <summary>
		/// Send a global message to all connected peers.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="exclude">Peers to exclude.</param>
		public void SendAll(IMessage message, params Peer[] exclude) {
			try {
				Lock.EnterReadLock();
				Host?.SendAll(message, exclude);
			} finally {
				Lock.ExitReadLock();
			}
		}

		/// <summary>Send an unconnected message to a remote host.</summary>
		/// <param name="remote">Remote address to send to.</param>
		/// <param name="message">Message to send.</param>
		public void SendUnconnected(IPEndPoint remote, IWritable message) {
			try {
				Lock.EnterReadLock();
				Host?.SendUnconnected(remote, message);
			} finally {
				Lock.ExitReadLock();
			}
		}

		/// <summary>Send an unconnected message to all machines on the local network.</summary>
		/// <param name="port">Network port to send to.</param>
		/// <param name="message">Message to send.</param>
		public void SendBroadcast(int port, IWritable message) {
			try {
				Lock.EnterReadLock();
				Host?.SendBroadcast(port, message);
			} finally {
				Lock.ExitReadLock();
			}
		}

		private void OnConnectResolve(IPEndPoint remote, Exception exception) {
			if (remote == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			} else {
				Connect(remote);
			}
		}

		/// <summary>
		/// Create a local peer and start connecting to an active remote host.
		/// </summary>
		/// <param name="address">Address to connect to.</param>
		public void Connect(string address) {
			IPResolver.Resolve(address, OnConnectResolve);
		}

		/// <summary>
		/// Create a local peer and start connecting to an active remote host.
		/// </summary>
		/// <param name="remote">Remote address to connect to.</param>
		/// <param name="listener">Peer listener to use or null for none.</param>
		/// <param name="message">Connect message to use.</param>
		/// <param name="rethrow">Rethrow any exceptions on failure.</param>
		/// <returns>Local peer that attempts to connect or null on failure.</returns>
		public Peer Connect(IPEndPoint remote, IPeerListener listener = null, IWritable message = null, bool rethrow = false) {

			// Validate
			if (remote == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning("[SuperNet] [Host] Remote connect address is null.");
				}
				if (rethrow) {
					throw new ArgumentNullException(nameof(remote), "Remote address is null");
				} else {
					return null;
				}
			}

			try {
				Lock.EnterWriteLock();

				// Check if listening
				if (Host == null || Host.Disposed) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [Host] Not listening, cannot connect to '{0}'.", remote
						));
					}
					if (rethrow) {
						throw new InvalidOperationException("Host is not listening");
					} else {
						return null;
					}
				}

				// Check if maximum connections was reached
				if (Peers.Count >= MaxConnections) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [Host] Maximum connections {0} reached, cannot connect to '{1}'.",
							MaxConnections, remote
						));
					}
					if (rethrow) {
						throw new InvalidOperationException("Maximum connections reached");
					} else {
						return null;
					}
				}

				// Check if peer already exists
				bool found = Peers.TryGetValue(remote, out Peer existing);
				if (found && existing.Connected) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format("[SuperNet] [Host] Already connected to '{0}'.", remote));
					}
					return existing;
				} else if (found && existing.Connecting) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format("[SuperNet] [Host] Already connecting to '{0}'.", remote));
					}
					return existing;
				}

				// Dispose any existing connections
				if (existing != null) existing.Dispose();

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format("[SuperNet] [Host] Connecting to '{0}'.", remote));
				}

				// Start connecting
				Peer peer = Host.Connect(remote, PeerConfiguration, this, message);

				// Save peer & listener
				Peers[remote] = peer;
				Listeners[remote] = listener;
				Debug.Log("事件添加成功 ：" + remote.Address);

				// Replace peer array
				Peer[] peers = new Peer[Peers.Count];
				Peers.Values.CopyTo(peers, 0);
				Interlocked.Exchange(ref PeersArray, peers);

				// Return peer
				return peer;

			} catch (Exception exception) {

				// Exception occured
				if (NetworkManager.LogExceptions) Debug.LogException(exception);
				if (rethrow) throw;
				return null;

			} finally {
				Lock.ExitWriteLock();
			}

		}

		void IHostListener.OnHostReceiveRequest(ConnectionRequest request, Reader message) {

			// Invoke event
			try {
				(HostEvents as IHostListener).OnHostReceiveRequest(request, message);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// If request has been disposed or rejected, do nothing
			if (request.Disposed || request.Rejected) {
				return;
			}

			// Get already accepted peer
			bool accepted = request.Accepted;
			Peer peer = accepted ? request.Peer : null;

			try {
				Lock.EnterWriteLock();

				if (peer == null) {

					// Check if maximum connections was reached
					if (Peers.Count >= MaxConnections) {
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [Host] Maximum connections {0} reached. Connection request from '{1}' rejected.",
								MaxConnections, request.Remote
							));
						}
						request.Reject();
						return;
					}

					// Check if already connected
					bool found = Peers.TryGetValue(request.Remote, out Peer existing);
					if (found && (existing.Connected || existing.Connecting)) {
						if (NetworkManager.LogExceptions) {
							Debug.LogWarning(string.Format(
								"[SuperNet] [Host] Duplicated connection request from '{0}'. Ignoring.",
								request.Remote
							));
						}
						return;
					}

					// Accept the connection
					peer = request.Accept(PeerConfiguration, this);

					// Log
					if (NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [Host] Connection request from '{0}' accepted.", request.Remote
						));
					}

				} else {

                    // Replace listener
                    Debug.Log("事件修改！");
                    IPeerListener old = peer.SetListener(this);
					Listeners[peer.Remote] = old;
                    // Log
                    if (NetworkManager.LogEvents) {
						Debug.Log(string.Format(
							"[SuperNet] [Host] Connection request from '{0}' already accepted.", request.Remote
						));
					}

				}

				// Save peer
				Peers[request.Remote] = peer;

				// Replace peer array
				Peer[] peers = new Peer[Peers.Count];
				Peers.Values.CopyTo(peers, 0);
				Interlocked.Exchange(ref PeersArray, peers);

			} finally {
				Lock.ExitWriteLock();
			}

			// Invoke connect if needed
			if (accepted && peer.Connected) {
				(this as IPeerListener).OnPeerConnect(peer);
			}

		}

		void IPeerListener.OnPeerConnect(Peer peer) {

			// Log
			if (NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Connection to '{0}' established.", peer.Remote
				));
			}

			try {
				Lock.EnterWriteLock();

				// Add peer if not added yet
				Peers[peer.Remote] = peer;

				// Replace peer array
				Peer[] peers = new Peer[Peers.Count];
				Peers.Values.CopyTo(peers, 0);
				Interlocked.Exchange(ref PeersArray, peers);

			} finally {
				Lock.ExitWriteLock();
			}

			// Notify tracker
			if (AutoRegister) {
				NetworkManager.Register(peer);
			}

			// Invoke peer listener
			try {
                IPeerListener listener = FindListener(peer);
				listener?.OnPeerConnect(peer);
				
            } catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Invoke listener
			try {
                Debug.Log("第二次----");
                (PeerEvents as IPeerListener)?.OnPeerConnect(peer);
            } catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

		}

		void IPeerListener.OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {

			// Log exception
			if (exception != null && NetworkManager.LogExceptions) {
				Debug.LogException(exception);
			}

			// Log event
			if (NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Disconnected from '{0}': {1}",
					peer.Remote, reason
				));
			}

			// Invoke peer listener
			try {
				IPeerListener listener = FindListener(peer);
				listener?.OnPeerDisconnect(peer, message, reason, exception);
			} catch (Exception e) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(e);
				}
			}

			// Invoke listener
			try {
				(PeerEvents as IPeerListener)?.OnPeerDisconnect(peer, message, reason, exception);
			} catch (Exception e) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(e);
				}
			}

			try {
				Lock.EnterWriteLock();

				// Remove peer & listener
				Peers.Remove(peer.Remote);
				Listeners.Remove(peer.Remote);

				// Replace peer array
				Peer[] peers = new Peer[Peers.Count];
				Peers.Values.CopyTo(peers, 0);
				Interlocked.Exchange(ref PeersArray, peers);

			} finally {
				Lock.ExitWriteLock();
			}

			// Notify tracker
			NetworkManager.Unregister(peer);

		}

		void IHostListener.OnHostShutdown() {

			// Log
			if (NetworkManager.LogEvents) {
				Debug.Log(string.Format(
					"[SuperNet] [Host] Host listening on '{0}' was shut down.",
					Host?.BindAddress
				));
			}

			// Invoke event
			try {
				(HostEvents as IHostListener).OnHostShutdown();
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Remove all peers, listeners and host
			try {
				Lock.EnterWriteLock();
				Host = null;
				Peers.Clear();
				Listeners.Clear();
				PeersArray = new Peer[0];
			} finally {
				Lock.ExitWriteLock();
			}

		}

		void IPeerListener.OnPeerReceive(Peer peer, Reader reader, MessageReceived info) {

			// Notify tracker
			NetworkManager.Receive(peer, reader, info);

			// Invoke peer listener
			try {
				reader.Reset(reader.First);
				IPeerListener listener = FindListener(peer);
				listener?.OnPeerReceive(peer, reader, info);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Invoke listener
			try {
				reader.Reset(reader.First);
				(PeerEvents as IPeerListener)?.OnPeerReceive(peer, reader, info);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

		}

		void IPeerListener.OnPeerUpdateRTT(Peer peer, ushort rtt) {

			// Invoke peer listener
			try {
				IPeerListener listener = FindListener(peer);
				listener?.OnPeerUpdateRTT(peer, rtt);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

			// Invoke listener
			try {
				(PeerEvents as IPeerListener)?.OnPeerUpdateRTT(peer, rtt);
			} catch (Exception exception) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(exception);
				}
			}

		}

		void IHostListener.OnHostException(IPEndPoint remote, Exception exception) {

			// Log
			if (NetworkManager.LogExceptions) {
				Debug.LogException(exception);
			}

			// Invoke event
			try {
				(HostEvents as IHostListener).OnHostException(remote, exception);
			} catch (Exception e) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(e);
				}
			}

		}

		void IPeerListener.OnPeerException(Peer peer, Exception exception) {

			// Log
			if (NetworkManager.LogExceptions) {
				Debug.LogException(exception);
			}

			// Invoke peer listener
			try {
				IPeerListener listener = FindListener(peer);
				listener?.OnPeerException(peer, exception);
			} catch (Exception e) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(e);
				}
			}

			// Invoke listener
			try {
				(PeerEvents as IPeerListener)?.OnPeerException(peer, exception);
			} catch (Exception e) {
				if (NetworkManager.LogExceptions) {
					Debug.LogException(e);
				}
			}

		}

		void IHostListener.OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length) {
			(HostEvents as IHostListener).OnHostReceiveSocket(remote, buffer, length);
		}

		void IHostListener.OnHostReceiveUnconnected(IPEndPoint remote, Reader message) {
			(HostEvents as IHostListener).OnHostReceiveUnconnected(remote, message);
		}
		
		void IHostListener.OnHostReceiveBroadcast(IPEndPoint remote, Reader message) {
			(HostEvents as IHostListener).OnHostReceiveBroadcast(remote, message);
		}

		private IPeerListener FindListener(Peer peer) {
			try {
				Lock.EnterReadLock();
				Listeners.TryGetValue(peer.Remote, out IPeerListener listener);
                return listener ?? PeerEvents;
			} finally {
				Lock.ExitReadLock();
			}
		}

	}

}
