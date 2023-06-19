using System;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Reason provided when a connection ends.
	/// </summary>
	public enum DisconnectReason : byte {

		/// <summary>
		/// An exception has caused the peer to be disconnected.
		/// <para>Always includes the actual exception.</para>
		/// </summary>
		Exception,

		/// <summary>Graceful disconnect after requested.</summary>
		Disconnected,

		/// <summary>Graceful disconnect after the remote peer has requested it.</summary>
		Terminated,

		/// <summary>Connection request has been rejected by the remote peer.</summary>
		Rejected,

		/// <summary>Remote host failed to authenticate.</summary>
		BadSignature,

		/// <summary>Remote host has stopped responding to messages.</summary>
		Timeout,

		/// <summary>Peer or host has been disposed.</summary>
		Disposed,
		
	}

	/// <summary>
	/// Used internally by the netcode to encode packet flags.
	/// </summary>
	[Flags]
	internal enum PacketFlags : byte {

		/// <summary>Packet includes remote ticks at send time.</summary>
		Timed = 0b00000001,

		/// <summary>Packet is a fragment and includes fragment information.</summary>
		Fragmented = 0b00000010,

		/// <summary>Packet is compressed.</summary>
		Compressed = 0b00000100,

		/// <summary>Packet includes multiple messages.</summary>
		Combined = 0b00001000,

		/// <summary>Packet includes a CRC32 code.</summary>
		Verified = 0b00010000,

		/// <summary>No flags.</summary>
		None = 0b00000000,

		/// <summary>Mask used to extract flags.</summary>
		Mask = 0b00011111,

	}

	/// <summary>
	/// Used internally by the netcode to encode packet type.
	/// </summary>
	internal enum PacketType : byte {

		/// <summary>Unused.</summary>
		Unused1 = 0b00000000,

		/// <summary>Unused.</summary>
		Unused2 = 0b00100000,

		/// <summary>Packet is a connection request.</summary>
		Request = 0b01000000,

		/// <summary>Packet rejects a previously received connection request.</summary>
		Reject = 0b01100000,

		/// <summary>Packet accepts a previously received connection request.</summary>
		Accept = 0b10000000,

		/// <summary>Packet contains a connected message (usually encrypted).</summary>
		Connected = 0b10100000,

		/// <summary>Packet contains an unconnected message.</summary>
		Unconnected = 0b11000000,

		/// <summary>Packet contains a broadcast message.</summary>
		Broadcast = 0b11100000,

		/// <summary>Mask used to extract type.</summary>
		Mask = 0b11100000,

	}

	/// <summary>
	/// Used internally by the netcode to encode message flags.
	/// </summary>
	[Flags]
	internal enum MessageFlags : byte {

		/// <summary>Message includes remote ticks at message creation.</summary>
		Timed = 0b00000001,

		/// <summary>Message includes a sequence number.</summary>
		Sequenced = 0b00000010,

		/// <summary>Message requires an acknowledgment and includes attempt number.</summary>
		Reliable = 0b00000100,

		/// <summary>Message requires ordered delivery.</summary>
		Ordered = 0b00001000,

		/// <summary>Message must not be duplicated.</summary>
		Unique = 0b00010000,

		/// <summary>Message includes a channel.</summary>
		Channeled = 0b00100000,

		/// <summary>No flags.</summary>
		None = 0b00000000,

		/// <summary>Mask used to extract flags.</summary>
		Mask = 0b00111111,

	}

	/// <summary>
	/// Used internally by the netcode to encode message type.
	/// </summary>
	internal enum MessageType : byte {

		/// <summary>Message is a disconnect request.</summary>
		Disconnect = 0b00000000,

		/// <summary>Message acknowledges a previously received reliable message.</summary>
		Acknowledge = 0b01000000,

		/// <summary>Message is a custom message sent by the user.</summary>
		Custom = 0b10000000,

		/// <summary>Message is a keep-alive ping used for timings and keeping the connection active.</summary>
		Ping = 0b11000000,

		/// <summary>Mask used to extract type.</summary>
		Mask = 0b11000000,

	}
	
}
