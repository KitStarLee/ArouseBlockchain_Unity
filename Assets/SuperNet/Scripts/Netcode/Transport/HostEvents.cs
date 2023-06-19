using System;
using System.Net;
using SuperNet.Netcode.Util;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Implements a host listener.
	/// </summary>
	public interface IHostListener {

		/// <summary>Called for every raw packet the host receives.</summary>
		/// <param name="remote">Remote address packet was received from.</param>
		/// <param name="buffer">Receive buffer the packet is written on.</param>
		/// <param name="length">Number of bytes in the packet.</param>
		void OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length);

		/// <summary>Called for every unconnected message the host receives.</summary>
		/// <param name="remote">Remote address message was received from.</param>
		/// <param name="message">Message that was received.</param>
		void OnHostReceiveUnconnected(IPEndPoint remote, Reader message);

		/// <summary>Called for every broadcast message the host receives.</summary>
		/// <param name="remote">Remote address message was received from.</param>
		/// <param name="message">Message that was received.</param>
		void OnHostReceiveBroadcast(IPEndPoint remote, Reader message);

		/// <summary>
		/// Called when a connection request is received.
		/// <para>The request can only be accepted during this call.</para>
		/// </summary>
		/// <param name="request">Connection request received.</param>
		/// <param name="message">Message sent with the connection request.</param>
		void OnHostReceiveRequest(ConnectionRequest request, Reader message);

		/// <summary>
		/// Called when an exception occurs internally.
		/// <para>This does not usually indicate any fatal errors and can be ignored.</para>
		/// </summary>
		/// <param name="remote">Remote address associated with this exception or null.</param>
		/// <param name="exception">Exception that was thrown.</param>
		void OnHostException(IPEndPoint remote, Exception exception);

		/// <summary>Called when the host shuts down.</summary>
		void OnHostShutdown();

	}

	/// <summary>
	/// Event based implementation of a host listener.
	/// </summary>
	public class HostEvents : IHostListener {

		public delegate void OnReceiveSocketHandler(IPEndPoint remote, byte[] buffer, int length);
		public delegate void OnReceiveUnconnectedHandler(IPEndPoint remote, Reader message);
		public delegate void OnReceiveBroadcastHandler(IPEndPoint remote, Reader message);
		public delegate void OnReceiveRequestHandler(ConnectionRequest request, Reader message);
		public delegate void OnExceptionHandler(IPEndPoint remote, Exception exception);
		public delegate void OnShutdownHandler();

		/// <summary>Called for every raw packet the host receives.</summary>
		public event OnReceiveSocketHandler OnReceiveSocket;

		/// <summary>Called for every unconnected message the host receives.</summary>
		public event OnReceiveUnconnectedHandler OnReceiveUnconnected;

		/// <summary>Called for every broadcast message the host receives.</summary>
		public event OnReceiveBroadcastHandler OnReceiveBroadcast;

		/// <summary>Called when a connection request is received.</summary>
		public event OnReceiveRequestHandler OnReceiveRequest;

		/// <summary>Called when an exception occurs internally. Can be ignored.</summary>
		public event OnExceptionHandler OnException;

		/// <summary>Called when the host shuts down.</summary>
		public event OnShutdownHandler OnShutdown;

		void IHostListener.OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length) {
			OnReceiveSocket?.Invoke(remote, buffer, length);
		}

		void IHostListener.OnHostReceiveUnconnected(IPEndPoint remote, Reader message) {
			OnReceiveUnconnected?.Invoke(remote, message);
		}

		void IHostListener.OnHostReceiveBroadcast(IPEndPoint remote, Reader message) {
			OnReceiveBroadcast?.Invoke(remote, message);
		}

		void IHostListener.OnHostReceiveRequest(ConnectionRequest request, Reader message) {
			OnReceiveRequest?.Invoke(request, message);
		}

		void IHostListener.OnHostException(IPEndPoint remote, Exception exception) {
			OnException?.Invoke(remote, exception);
		}

		void IHostListener.OnHostShutdown() {
			OnShutdown?.Invoke();
		}

	}

}
