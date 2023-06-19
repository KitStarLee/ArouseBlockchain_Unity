using System.Threading;
using SuperNet.Netcode.Util;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Network message that has been sent to a connected peer.
	/// </summary>
	public class MessageSent {

		/// <summary>Sent message is timed.</summary>
		public bool Timed => Flags.HasFlag(MessageFlags.Timed);

		/// <summary>Sent message is reliable.</summary>
		public bool Reliable => Flags.HasFlag(MessageFlags.Reliable);

		/// <summary>Sent message is ordered.</summary>
		public bool Ordered => Flags.HasFlag(MessageFlags.Ordered);

		/// <summary>Sent message is unique.</summary>
		public bool Unique => Flags.HasFlag(MessageFlags.Unique);

		/// <summary>Listener used for this message or null if not provided.</summary>
		public readonly IMessageListener Listener;

		/// <summary>Message payload that is used to write to internal buffers.</summary>
		public readonly IWritable Payload;

		/// <summary>Internal message type.</summary>
		internal readonly MessageType Type;

		/// <summary>Internal message flags.</summary>
		internal readonly MessageFlags Flags;

		/// <summary>Data channel this message is sent over.</summary>
		public readonly byte Channel;

		/// <summary>Internal sequence number of the message.</summary>
		public readonly ushort Sequence;

		/// <summary>Number of times this message has been sent.</summary>
		internal byte Attempt;

		/// <summary>Timestamp at the sending of the first attempt.</summary>
		internal HostTimestamp? TimestampFirst;

		/// <summary>>Timestamp at the sending of the last attempt.</summary>
		internal HostTimestamp? TimestampLast;

		/// <summary>Timestamp included in the message.</summary>
		public readonly HostTimestamp Timestamp;

		/// <summary>Cancellation token used to cancel resending of this message.</summary>
		internal readonly CancellationTokenSource Token;

		/// <summary>Used internally by the netcode to create a new sent message.</summary>
		internal MessageSent(
			IMessageListener listener,
			IWritable payload,
			MessageType type,
			MessageFlags flags,
			HostTimestamp timestamp,
			ushort sequence,
			byte channel
		) {
			Listener = listener;
			Payload = payload;
			Type = type;
			Flags = flags;
			Timestamp = timestamp;
			Sequence = sequence;
			Channel = channel;
			Attempt = 0;
			TimestampFirst = null;
			TimestampLast = null;
			Token = flags.HasFlag(MessageFlags.Reliable) ? new CancellationTokenSource() : null;
		}

		/// <summary>Stop resending this message if reliable. May cause the message to be lost.</summary>
		public void StopResending() {
			try { Token?.Cancel(); } catch { }
			try { Token?.Dispose(); } catch { }
		}
		
		/// <summary>Used internally by the netcode to notify listener.</summary>
		internal void OnMessageSend(Peer peer, byte attempt) {
			Attempt = attempt;
			TimestampLast = Host.Now;
			if (attempt == 0) TimestampFirst = TimestampLast;
			Listener?.OnMessageSend(peer, this);
		}

		/// <summary>Used internally by the netcode to notify listener.</summary>
		internal void OnMessageAcknowledge(Peer peer) {
			Listener?.OnMessageAcknowledge(peer, this);
		}

	}

}
