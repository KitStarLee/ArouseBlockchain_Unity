namespace SuperNet.Netcode.Transport {
	
	/// <summary>
	/// Extra information for a network message that has been received by a connected peer.
	/// </summary>
	public class MessageReceived {

		/// <summary>Received message is timed.</summary>
		public bool Timed => Flags.HasFlag(MessageFlags.Timed);

		/// <summary>Received message is reliable.</summary>
		public bool Reliable => Flags.HasFlag(MessageFlags.Reliable);

		/// <summary>Received message is ordered.</summary>
		public bool Ordered => Flags.HasFlag(MessageFlags.Ordered);

		/// <summary>Received message is unique.</summary>
		public bool Unique => Flags.HasFlag(MessageFlags.Unique);

		/// <summary>Internal message type.</summary>
		internal readonly MessageType Type;

		/// <summary>Internal message flags.</summary>
		internal readonly MessageFlags Flags;

		/// <summary>Data channel the message was sent over.</summary>
		public readonly byte Channel;

		/// <summary>How many times the message was previously sent before.</summary>
		public readonly byte Attempt;

		/// <summary>Received sequence number.</summary>
		internal readonly ushort? Sequence;

		/// <summary>Received remote timestamp ticks.</summary>
		internal readonly ushort? TicksTimestamp;

		/// <summary>Received remote sent ticks.</summary>
		internal readonly ushort? TicksSent;

		/// <summary>
		/// Timestamp included in the message.
		/// <para>If message was not timed, this is approximated using round trip time.</para>
		/// </summary>
		public readonly HostTimestamp Timestamp;

		/// <summary>Used internally by the netcode to create a new received message.</summary>
		internal MessageReceived(
			MessageType type,
			MessageFlags flags,
			byte channel,
			byte attempt,
			ushort? sequence,
			ushort? ticksTimestamp,
			ushort? ticksSent,
			HostTimestamp timestamp
		) {
			Type = type;
			Flags = flags;
			Channel = channel;
			Attempt = attempt;
			Sequence = sequence;
			TicksTimestamp = ticksTimestamp;
			TicksSent = ticksSent;
			Timestamp = timestamp;
		}

	}

}
