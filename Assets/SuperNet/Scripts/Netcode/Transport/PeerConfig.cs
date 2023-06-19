using System;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Holds configuration values for peers.
	/// </summary>
	[Serializable]
	public class PeerConfig {

		/// <summary>
		/// Number of connection requests to send before giving up.
		/// This is a high number to allow enough time for UDP hole punching.
		/// </summary>
		public int ConnectAttempts = 24;

		/// <summary>
		/// Delay in milliseconds between connection requests.
		/// </summary>
		public int ConnectDelay = 250;

		/// <summary>
		/// Maximum bytes to send in one UDP packet.
		/// <para>MTU on ethernet is 1500 bytes - 20 bytes for IP header - 8 bytes for UDP header.</para>
		/// </summary>
		public int MTU = 1350;
		
		/// <summary>
		/// Delay in milliseconds between ping messages.
		/// </summary>
		public int PingDelay = 1000;

		/// <summary>
		/// Delay in milliseconds before combining and sending messages to the socket.
		/// </summary>
		public int SendDelay = 15;

		/// <summary>
		/// Maximum number of times a reliable message is resent
		/// without being acknowledged before the connection times out.
		/// </summary>
		public byte ResendCount = 12;

		/// <summary>
		/// Maximum number of milliseconds to wait before declaring a reliable message as lost.
		/// <para>
		/// When a reliable message is sent, peer waits RTT + ResendDelayJitter milliseconds for an acknowledgment.
		/// If no acknowledgment is received within that time, the message is resent.
		/// A small value can result in unnecessary duplicated messages wasting networking bandwidth.
		/// </para>
		/// </summary>
		public int ResendDelayJitter = 40;

		/// <summary>
		/// Minimum delay in milliseconds before resending unacknowledged reliable messages.
		/// </summary>
		public int ResendDelayMin = 120;

		/// <summary>
		/// Maximum delay in milliseconds before resending unacknowledged reliable messages.
		/// </summary>
		public int ResendDelayMax = 800;

		/// <summary>
		/// Timeout in milliseconds until a received incompleted fragmented packet times out.
		/// </summary>
		public int FragmentTimeout = 16000;

		/// <summary>
		/// How long in milliseconds to keep received reliable messages for.
		/// If the same reliable message is received during this timeout, it is ignored.
		/// </summary>
		public int DuplicateTimeout = 2000;

		/// <summary>
		/// Maximum number of consecutive unsequenced messages to send.
		/// <para>
		/// All reliable messages include a sequence number.
		/// Unreliable messages don't need a sequence number but can include it.
		/// This value controls how often to include a sequence number.
		/// Sending a sequence number every so often is important to check for lost messages.
		/// </para>
		/// </summary>
		public int UnsequencedMax = 64;

		/// <summary>
		/// Maximum number of messages to wait for before processing a reliable ordered message that came out of order.
		/// <para>
		/// If an ordered reliable message comes late, it is delayed until all missing messages are received.
		/// This value controls maximum number of missing messages to wait for.
		/// If this is zero, delaying is disabled.
		/// </para>
		/// </summary>
		public int OrderedDelayMax = 8;

		/// <summary>
		/// Maximum number of milliseconds to wait for before processing a reliable ordered message that came out of order.
		/// <para>
		/// If an ordered reliable message comes late, it is delayed until all missing messages are received.
		/// This controls maximum number of milliseconds to wait for.
		/// If this is zero, delaying is disabled.
		/// </para>
		/// </summary>
		public int OrderedDelayTimeout = 4000;

		/// <summary>
		/// Number of milliseconds to delay closing the connection when a disconnect request is received.
		/// <para>
		/// This is useful in cases where both peers disconnect at the same time.
		/// It is also useful for when a disconnect acknowledge gets lost.
		/// Set to zero to disable this delay.
		/// </para>
		/// </summary>
		public int DisconnectDelay = 300;

		/// <summary>
		/// How fast the clock tick difference can change. Must be between 0 and 1.
		/// <para>
		/// Clock tick difference is an exponential moving average.
		/// If this value is 1, clock drifting is completely ignored once calculated.
		/// if this value is 0, latest calculated value is always used.
		/// </para>
		/// </summary>
		public float TimeStability = 0.98f;

		/// <summary>
		/// Remote public key to verify authentication signature against.
		/// <para>If provided and remote peer has authentication disabled, they will ignore connection requests.</para>
		/// </summary>
		public string RemotePublicKey = null;

	}

}
