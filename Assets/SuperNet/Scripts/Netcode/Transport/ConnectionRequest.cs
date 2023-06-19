using System;
using System.Net;
using SuperNet.Netcode.Util;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// A connection request received by an active host.
	/// </summary>
	public class ConnectionRequest : IDisposable {

		/// <summary>The host that received this request.</summary>
		public readonly Host Host;

		/// <summary>Remote address that the request was received from.</summary>
		public readonly IPEndPoint Remote;

		/// <summary>Exchange key received in the request or empty if no encryption.</summary>
		internal readonly ArraySegment<byte> Key;

		/// <summary>Random data to be signed or empty if authentication is disabled.</summary>
		internal readonly ArraySegment<byte> Random;

		/// <summary>True if the underlying buffers for the request have been repurposed for something else.</summary>
		public bool Disposed { get; private set; }

		/// <summary>True if the request has been rejected.</summary>
		public bool Rejected { get; private set; }

		/// <summary>True if the request has been accepted.</summary>
		public bool Accepted { get; private set; }

		/// <summary>Peer accepted or null if none.</summary>
		public Peer Peer { get; private set; }

		/// <summary>True if remote peer requires encryption.</summary>
		public bool Encrypted => Key.Count > 0;

		/// <summary>True if remote peer requires us to authenticate.</summary>
		public bool Authenticate => Random.Count > 0;

		/// <summary>Used internally by the netcode to create a new connection request.</summary>
		/// <param name="host">Host that received the request.</param>
		/// <param name="remote">Remote address that the request was received from.</param>
		/// <param name="key">Exchange key received in the request.</param>
		/// <param name="random">Random data to be signed.</param>
		internal ConnectionRequest(Host host, IPEndPoint remote, ArraySegment<byte> key, ArraySegment<byte> random) {
			Host = host;
			Remote = remote;
			Key = key;
			Random = random;
			Disposed = false;
			Rejected = false;
			Accepted = false;
			Peer = null;
		}

		/// <summary>
		/// Used internally by the netcode to invalidate the request, making it unable to be accepted.
		/// <para>This is called when the underlying buffers have been repurposed for something else.</para>
		/// </summary>
		public void Dispose() {
			Disposed = true;
		}

		/// <summary>Reject the request by sending a reject message.</summary>
		/// <param name="message">Message to reject with.</param>
		public void Reject(IWritable message = null) {

			// Validate
			if (Disposed) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} is disposed", Remote
				));
			} else if (Accepted) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} already accepted", Remote
				));
			} else if (Rejected) {
				return;
			}

			// Reject
			Host.Reject(this, message);
			Rejected = true;

		}

		/// <summary>Accept the request, create a new peer and establish a connection.</summary>
		/// <param name="config">Peer configuration values. If null, default is used.</param>
		/// <param name="listener">Peer listener. If null, event based listener is created.</param>
		/// <returns>The created peer.</returns>
		public Peer Accept(PeerConfig config = null, IPeerListener listener = null) {

			// Validate
			if (Disposed) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} is disposed", Remote
				));
			} else if (Rejected) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} already rejected", Remote
				));
			} else if (Accepted && Peer?.Listener == listener && Peer?.Config == config) {
				return Peer;
			} else if (Accepted) {
				throw new InvalidOperationException(string.Format(
					"Connection request from {0} already accepted", Remote
				));
			}

			// Accept
			Peer = Host.Accept(this, config, listener);
			Accepted = true;
			return Peer;

		}

	}

}
