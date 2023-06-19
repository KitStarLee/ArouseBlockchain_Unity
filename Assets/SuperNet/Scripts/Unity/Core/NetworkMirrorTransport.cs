#if SUPERNET_MIRROR

using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace SuperNet.Unity.Core {

	[AddComponentMenu("SuperNet/SuperNetMirrorTransport")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Core.NetworkMirrorTransport.html")]
	public class NetworkMirrorTransport : Mirror.Transport, IHostListener, IPeerListener {

		/// <summary>
		/// The maximum number of concurrent network connections to support.
		/// </summary>
		[FormerlySerializedAs("MaxConnections")]
		[Tooltip("Maximum number of concurrent connections.")]
		public int MaxConnections;

		/// <summary>
		/// Host configuration values.
		/// </summary>
		[FormerlySerializedAs("HostConfiguration")]
		[Tooltip("Host configuration values.")]
		public HostConfig HostConfiguration;

		/// <summary>
		/// Peer configuration values.
		/// </summary>
		[FormerlySerializedAs("HostConfiguration")]
		[Tooltip("Peer Configuration values.")]
		public PeerConfig PeerConfiguration;

		/// <summary>
		/// Channel configuration values.
		/// </summary>
		[FormerlySerializedAs("Channels")]
		[Tooltip("Channel configuration values.")]
		public ChannelConfiguration[] Channels;

		[Serializable]
		public class ChannelConfiguration {
			public bool Timed;
			public bool Reliable;
			public bool Ordered;
			public bool Unique;
			public byte Channel;
		}

		/// <summary>Main unity thread.</summary>
		private Thread RunThread = Thread.CurrentThread;

		/// <summary>Queue for running actions on the main unity thread.</summary>
		private readonly ConcurrentQueue<Action> RunQueueServer = new ConcurrentQueue<Action>();

		/// <summary>Queue for running actions on the main unity thread.</summary>
		private readonly ConcurrentQueue<Action> RunQueueClient = new ConcurrentQueue<Action>();

		/// <summary>Lock used for resource access.</summary>
		private readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		/// <summary>Convert server peer to connection ID.</summary>
		private readonly Dictionary<Peer, int> PeerToID = new Dictionary<Peer, int>();

		/// <summary>Convert connection ID to server peer.</summary>
		private readonly Dictionary<int, Peer> IDToPeer = new Dictionary<int, Peer>();

		/// <summary>Host used for both client and server.</summary>
		private Host Host = null;

		/// <summary>Client peer.</summary>
		private Peer Peer = null;

		/// <summary>True if host is a server.</summary>
		private bool Server = false;

		/// <summary>Next server peer ID.</summary>
		private int PeersID = 1;

		/// <summary>Semaphore used for access to receive buffer.</summary>
		private readonly SemaphoreSlim BufferSemaphore = new SemaphoreSlim(1, 1);

		/// <summary>Receive buffer to copy incoming data to.</summary>
		private byte[] Buffer = new byte[0];

		private class SegmentMessage : IMessage {

			public HostTimestamp Timestamp => Host.Now;
			public bool Timed => Configuration?.Timed ?? false;
			public bool Reliable => Configuration?.Reliable ?? true;
			public bool Ordered => Configuration?.Ordered ?? false;
			public bool Unique => Configuration?.Unique ?? true;
			public byte Channel { get; private set; }
			
			public readonly ArraySegment<byte> Segment;
			public readonly ChannelConfiguration Configuration;

			public SegmentMessage(ArraySegment<byte> segment, ChannelConfiguration config, byte channel) {
				Segment = segment;
				Configuration = config;
				Channel = channel;
			}

			public void Write(Writer writer) {
				writer.WriteBytes(Segment);
			}

		}

		private void Reset() {
			MaxConnections = 32;
			HostConfiguration = new HostConfig();
			PeerConfiguration = new PeerConfig();
		}

		private void Awake() {
			RunThread = Thread.CurrentThread;
		}

		public override void ClientEarlyUpdate() {
			while (RunQueueClient.TryDequeue(out Action action)) {
				try {
					action.Invoke();
				} catch (Exception exception) {
					Debug.LogException(exception, this);
				}
			}
		}

		public override void ServerEarlyUpdate() {
			while (RunQueueServer.TryDequeue(out Action action)) {
				try {
					action.Invoke();
				} catch (Exception exception) {
					Debug.LogException(exception, this);
				}
			}
		}

		private void RunServer(Action action) {
			if (Thread.CurrentThread == RunThread) {
				action.Invoke();
			} else {
				RunQueueServer.Enqueue(action);
			}
		}

		private void RunClient(Action action) {
			if (Thread.CurrentThread == RunThread) {
				action.Invoke();
			} else {
				RunQueueClient.Enqueue(action);
			}
		}

		public override bool Available() {
			return Application.platform != RuntimePlatform.WebGLPlayer;
		}

		public override int GetMaxPacketSize(int channelId) {
			int mtu = PeerConfiguration.MTU - 52;
			return mtu * ushort.MaxValue;
		}

		public override void Shutdown() {
			try {
				Lock.EnterWriteLock();
				try { BufferSemaphore.Release(); } catch { }
				Host?.Dispose();
				Peer?.Dispose();
				PeerToID.Clear();
				IDToPeer.Clear();
				Host = null;
				Peer = null;
				Server = false;
				PeersID = 1;
			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override void ClientConnect(string address) {
			IPResolver.Resolve(address, OnClientConnectResolve);
		}

		private void OnClientConnectResolve(IPEndPoint address, Exception exception) {

			if (address == null) {
				RunClient(() => OnClientDisconnected?.Invoke());
				Debug.LogException(exception, this);
				return;
			}

			try {
				Lock.EnterWriteLock();

				if (Peer != null && Peer.Remote == address) {
					if (Peer.Connected) {
						Debug.LogWarning("[SuperNet] [Mirror] Client already connected.", this);
						return;
					} else if (Peer.Connecting) {
						Debug.LogWarning("[SuperNet] [Mirror] Client already connecting.", this);
						return;
					}
				}

				if (Peer != null) {
					if (Peer.Connected) {
						Debug.LogWarning("[SuperNet] [Mirror] Client already connected to '" + Peer.Remote + "'. Disconnecting.", this);
					} else if (Peer.Connecting) {
						Debug.LogWarning("[SuperNet] [Mirror] Client already connecting to '" + Peer.Remote + "'. Disconnecting.", this);
					}
					Peer.Dispose();
					Peer = null;
				}

				try {
					if (Host == null) Host = new Host(HostConfiguration, this);
				} catch (Exception e) {
					Debug.LogException(e, this);
				}

				Peer = Host.Connect(address, PeerConfiguration, this);

			} finally {
				Lock.ExitWriteLock();
			}

		}

		public override bool ClientConnected() {
			try {
				Lock.EnterReadLock();
				return Peer != null && Peer.Connected;
			} finally {
				Lock.ExitReadLock();
			}
		}

		public override void ClientDisconnect() {
			try {
				Lock.EnterWriteLock();
				if (Peer != null) {
					Peer.Disconnect();
					Peer = null;
				}
				if (!Server && Host != null) {
					Host.Shutdown();
					Host = null;
				}
			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override void ClientSend(ArraySegment<byte> segment, int channelId) {

			if (channelId < byte.MinValue || channelId > byte.MaxValue) {
				Debug.LogWarning("[SuperNet] [Mirror] Invalid channel ID " + channelId + ".", this);
				return;
			}

			ChannelConfiguration config = null;
			foreach (ChannelConfiguration channel in Channels) {
				if (channel.Channel == channelId) {
					config = channel;
					break;
				}
			}

			if (config == null) {
				Debug.LogWarning("[SuperNet] [Mirror] Channel ID " + channelId + " is not configured.", this);
			}

			try {
				Lock.EnterReadLock();

				if (Peer == null) {
					Debug.LogWarning("[SuperNet] [Mirror] Client not connected while sending.", this);
					return;
				}

				SegmentMessage message = new SegmentMessage(segment, config, (byte)channelId);
				Peer.Send(message);

			} finally {
				Lock.ExitReadLock();
			}

		}

		public override void ServerStart() {
			try {
				Lock.EnterWriteLock();

				if (Server && Host != null) {
					Debug.LogWarning("[SuperNet] [Mirror] Server already started.", this);
					return;
				}

				try {
					if (Host == null) Host = new Host(HostConfiguration, this);
				} catch (Exception e) {
					Debug.LogException(e, this);
				}

				PeerToID.Clear();
				IDToPeer.Clear();
				Server = true;
				PeersID = 1;

			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override Uri ServerUri() {
			UriBuilder builder = new UriBuilder {
				Scheme = "supernet",
				Host = Dns.GetHostName(),
				Port = HostConfiguration.Port
			};
			return builder.Uri;
		}

		public override bool ServerActive() {
			try {
				Lock.EnterReadLock();
				return Server && Host != null && !Host.Disposed;
			} finally {
				Lock.ExitReadLock();
			}
		}

		public override void ServerStop() {
			try {
				Lock.EnterWriteLock();

				try { BufferSemaphore.Release(); } catch { }

				foreach (Peer peer in PeerToID.Keys) {
					peer.Disconnect();
				}

				PeerToID.Clear();
				IDToPeer.Clear();
				Server = false;
				PeersID = 1;

				if (Peer == null && Host != null) {
					Host.Shutdown();
					Host = null;
				}

			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override void ServerDisconnect(int connectionID) {
			try {
				Lock.EnterWriteLock();

				if (!Server || Host == null || Host.Disposed) {
					Debug.LogWarning("[SuperNet] [Mirror] Server not running while disconnecting client " + connectionID + ".", this);
					return;
				}

				IDToPeer.TryGetValue(connectionID, out Peer peer);
				if (peer == null) {
					Debug.LogWarning("[SuperNet] [Mirror] Server client " + connectionID + " doesn't exist.", this);
					return;
				}

				IDToPeer.Remove(connectionID);
				PeerToID.Remove(peer);
				peer.Disconnect();
				return;

			} finally {
				Lock.ExitWriteLock();
			}
		}

		public override string ServerGetClientAddress(int connectionID) {
			try {
				Lock.EnterReadLock();

				if (!Server || Host == null || Host.Disposed) {
					Debug.LogWarning("[SuperNet] [Mirror] Server not running while getting client " + connectionID + " address.", this);
					return "unknown";
				}

				IDToPeer.TryGetValue(connectionID, out Peer peer);
				if (peer == null) {
					Debug.LogWarning("[SuperNet] [Mirror] Server client " + connectionID + " doesn't exist.", this);
					return "unknown";
				}

				return peer.Remote.ToString();

			} finally {
				Lock.ExitReadLock();
			}
		}

		public override void ServerSend(int connectionID, ArraySegment<byte> segment, int channelId) {

			if (channelId < byte.MinValue || channelId > byte.MaxValue) {
				Debug.LogWarning("[SuperNet] [Mirror] Invalid channel ID " + channelId + ".", this);
				return;
			}

			ChannelConfiguration config = null;
			foreach (ChannelConfiguration channel in Channels) {
				if (channel.Channel == channelId) {
					config = channel;
					break;
				}
			}

			if (config == null) {
				Debug.LogWarning("[SuperNet] [Mirror] Channel ID " + channelId + " is not configured.", this);
			}

			try {
				Lock.EnterReadLock();

				if (!Server || Host == null || Host.Disposed) {
					Debug.LogWarning("[SuperNet] [Mirror] Server not running while sending client data.", this);
					return;
				}

				SegmentMessage message = new SegmentMessage(segment, config, (byte)channelId);
				IDToPeer.TryGetValue(connectionID, out Peer peer);
				if (peer != null) {
					peer.Send(message);
				} else {
					Debug.LogWarning("[SuperNet] [Mirror] Server client " + connectionID + " doesn't exist.", this);
				}

			} finally {
				Lock.ExitReadLock();
			}

		}

		void IHostListener.OnHostException(IPEndPoint remote, Exception exception) {
			try {
				Lock.EnterReadLock();

				if (remote == null) {
					Debug.LogException(exception, this);
				} else if (Peer != null && remote == Peer.Remote) {
					RunClient(() => OnClientError?.Invoke(exception));
				} else if (Server) {
					foreach (KeyValuePair<Peer, int> pair in PeerToID) {
						if (pair.Key.Remote == remote) {
							RunServer(() => OnServerError?.Invoke(pair.Value, exception));
							return;
						}
					}
					Debug.LogException(exception, this);
				} else {
					Debug.LogException(exception, this);
				}

			} finally {
				Lock.ExitReadLock();
			}
		}

		void IHostListener.OnHostReceiveBroadcast(IPEndPoint remote, Reader message) {
			Debug.LogWarning("[SuperNet] [Mirror] Broadcast message received from '" + remote + "'.", this);
		}

		void IHostListener.OnHostReceiveRequest(ConnectionRequest request, Reader message) {
			try {
				Lock.EnterWriteLock();

				if (!Server || Host == null || Host.Disposed) {
					Debug.LogWarning("[SuperNet] [Mirror] Request from '" + request.Remote + "' while server not running.", this);
					return;
				}

				if (PeerToID.Count >= MaxConnections) {
					Debug.LogWarning("[SuperNet] [Mirror] Maximum connections " + MaxConnections + " reached. Request from '" + request.Remote + "' rejected.", this);
					request.Reject();
					return;
				}

				Peer peer = request.Accept(PeerConfiguration, this);
				PeerToID[peer] = PeersID;
				IDToPeer[PeersID] = peer;
				PeersID++;

			} finally {
				Lock.ExitWriteLock();
			}
		}

		void IHostListener.OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length) { }

		void IHostListener.OnHostReceiveUnconnected(IPEndPoint remote, Reader message) {
			Debug.LogWarning("[SuperNet] [Mirror] Unconnected message received from '" + remote + "'.", this);
		}

		void IHostListener.OnHostShutdown() { }

		void IPeerListener.OnPeerConnect(Peer peer) {
			try {
				Lock.EnterReadLock();
				if (Peer == peer) {

					RunClient(() => OnClientConnected?.Invoke());

				} else if (Server) {

					bool found = PeerToID.TryGetValue(peer, out int id);
					if (!found) {
						Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' not found on server on connect.", this);
						return;
					}

					RunServer(() => OnServerConnected?.Invoke(id));

				} else {

					Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' connected but server is not running.", this);

				}
			} finally {
				Lock.ExitReadLock();
			}
		}

		void IPeerListener.OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {
			try {
				Lock.EnterWriteLock();

				if (Peer == peer) {

					Peer = null;
					if (!Server && Host != null) {
						Host.Shutdown();
						Host = null;
					}

					RunClient(() => OnClientDisconnected?.Invoke());

				} else if (Server) {

					bool found = PeerToID.TryGetValue(peer, out int id);
					if (!found) {
						Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' not found on server on disconnect.", this);
						return;
					}

					PeerToID.Remove(peer);
					IDToPeer.Remove(id);
					RunServer(() => OnServerDisconnected?.Invoke(id));

				} else {

					Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' disconnected while server not running.", this);

				}

			} finally {
				Lock.ExitWriteLock();
			}
		}

		void IPeerListener.OnPeerException(Peer peer, Exception exception) {
			try {
				Lock.EnterReadLock();

				if (Peer == peer) {
					RunClient(() => OnClientError?.Invoke(exception));
				} else if (Server) {
					bool found = PeerToID.TryGetValue(peer, out int id);
					if (found) {
						RunServer(() => OnServerError?.Invoke(id, exception));
					} else {
						Debug.LogException(exception, this);
					}
				} else {
					Debug.LogException(exception, this);
				}

			} finally {
				Lock.ExitReadLock();
			}
		}

		void IPeerListener.OnPeerReceive(Peer peer, Reader message, MessageReceived info) {
			try {
				Lock.EnterReadLock();
				BufferSemaphore.Wait();
				try {
					if (Buffer.Length < message.Available) Array.Resize(ref Buffer, message.Available);
					Array.Copy(message.Buffer, message.Position, Buffer, 0, message.Available);
					ArraySegment<byte> segment = new ArraySegment<byte>(Buffer, 0, message.Available);
					if (Peer == peer) {
						RunClient(() => {
							OnClientDataReceived?.Invoke(segment, info.Channel);
							BufferSemaphore.Release();
						});
					} else if (Server) {
						bool found = PeerToID.TryGetValue(peer, out int id);
						if (found) {
							RunServer(() => {
								OnServerDataReceived?.Invoke(id, segment, info.Channel);
								BufferSemaphore.Release();
							});
						} else {
							Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' not found on server on receive.", this);
							BufferSemaphore.Release();
						}
					} else {
						Debug.LogWarning("[SuperNet] [Mirror] Peer '" + peer.Remote + "' sent data while server not running.", this);
						BufferSemaphore.Release();
					}
				} catch {
					BufferSemaphore.Release();
				}
			} finally {
				Lock.ExitReadLock();
			}
		}

		void IPeerListener.OnPeerUpdateRTT(Peer peer, ushort rtt) { }

	}

}

#endif
