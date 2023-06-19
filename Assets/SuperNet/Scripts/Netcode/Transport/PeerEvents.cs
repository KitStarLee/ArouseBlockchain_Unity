using System;
using SuperNet.Netcode.Util;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Implements a peer listener.
	/// </summary>
	public interface IPeerListener {

		/// <summary>Called when a peer successfully connects.</summary>
		/// <param name="peer">Connected peer.</param>
		void OnPeerConnect(Peer peer);

		/// <summary>Called when a peer receives a connected message.</summary>
		/// <param name="peer">Receiver of the message.</param>
		/// <param name="reader">Message that was received.</param>
		/// <param name="info">Extra message information.</param>
		void OnPeerReceive(Peer peer, Reader reader, MessageReceived info);

		/// <summary>Called when round trip time (ping) is updated.</summary>
		/// <param name="peer">Updated peer.</param>
		/// <param name="rtt">New RTT value.</param>
		void OnPeerUpdateRTT(Peer peer, ushort rtt);

		/// <summary>Called when a peer disconnects.</summary>
		/// <param name="peer">Disconnected peer.</param>
		/// <param name="reader">Disconnect message or null if not included.</param>
		/// <param name="reason">Disconnect reason.</param>
		/// <param name="exception">Exception associated with the disconnect or null if none.</param>
		void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception);

		/// <summary>Called when an exception occurs internally. Can be ignored.</summary>
		/// <param name="peer">Peer involved.</param>
		/// <param name="exception">Exception that was thrown.</param>
		void OnPeerException(Peer peer, Exception exception);

	}

	/// <summary>
	/// Event based implementation of a peer listener.
	/// </summary>
	public class PeerEvents : IPeerListener {

		public delegate void OnConnectHandler(Peer peer);
		public delegate void OnReceiveHandler(Peer peer, Reader reader, MessageReceived info);
		public delegate void OnUpdateRTTHandler(Peer peer, ushort rtt);
		public delegate void OnDisconnectHandler(Peer peer, Reader reader, DisconnectReason reason, Exception exception);
		public delegate void OnExceptionHandler(Peer peer, Exception exception);

		/// <summary>Called when a peer successfully connects.</summary>
		public event OnConnectHandler OnConnect;

		/// <summary>Called when a peer receives a connected message.</summary>
		public event OnReceiveHandler OnReceive;

		/// <summary>Called when round trip time (ping) is updated.</summary>
		public event OnUpdateRTTHandler OnUpdateRTT;

		/// <summary>Called when a peer disconnects.</summary>
		public event OnDisconnectHandler OnDisconnect;

		/// <summary>Called when an exception occurs internally. Can be ignored.</summary>
		public event OnExceptionHandler OnException;

		void IPeerListener.OnPeerConnect(Peer peer) {
			OnConnect?.Invoke(peer);
		}

		void IPeerListener.OnPeerReceive(Peer peer, Reader reader, MessageReceived info) {
			OnReceive?.Invoke(peer, reader, info);
		}

		void IPeerListener.OnPeerUpdateRTT(Peer peer, ushort rtt) {
			OnUpdateRTT?.Invoke(peer, rtt);
		}

		void IPeerListener.OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {
			OnDisconnect?.Invoke(peer, reader, reason, exception);
		}

		void IPeerListener.OnPeerException(Peer peer, Exception exception) {
			OnException?.Invoke(peer, exception);
		}

	}

}
