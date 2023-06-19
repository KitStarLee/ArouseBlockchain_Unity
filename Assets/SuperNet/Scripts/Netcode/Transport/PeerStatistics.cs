using System.Threading;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Stores packet statistics for peers.
	/// </summary>
	public class PeerStatistics {

		/// <summary>Host ticks at the moment of the last receive operation.</summary>
		public long PacketReceiveTicks => _PacketReceiveTicks;

		/// <summary>Total number of bytes received.</summary>
		public long PacketReceiveBytes => _PacketReceiveBytes;

		/// <summary>Total number of packets received.</summary>
		public long PacketReceiveCount => _PacketReceiveCount;

		/// <summary>Host ticks at the moment of the last send operation.</summary>
		public long PacketSendTicks => _PacketSendTicks;

		/// <summary>Total number of bytes sent.</summary>
		public long PacketSendBytes => _PacketSendBytes;

		/// <summary>Total number of sent packets.</summary>
		public long PacketSendCount => _PacketSendCount;

		/// <summary>Total number of bytes recieved in messages after decryption and decompression.</summary>
		public long MessageReceiveBytes => _MessageReceiveBytes;

		/// <summary>Total number of lost messages.</summary>
		public long MessageReceiveLost => _MessageReceiveLost;

		/// <summary>Total number of received messages.</summary>
		public long MessageReceiveTotal => _MessageReceiveTotal;

		/// <summary>Total number of received reliable messages.</summary>
		public long MessageReceiveReliable => _MessageReceiveReliable;

		/// <summary>Total number of received unreliable messages.</summary>
		public long MessageReceiveUnreliable => _MessageReceiveUnreliable;

		/// <summary>Total number of received duplicated messages.</summary>
		public long MessageReceiveDuplicated => _MessageReceiveDuplicated;

		/// <summary>Total number of received acknowledgements.</summary>
		public long MessageReceiveAcknowledge => _MessageReceiveAcknowledge;

		/// <summary>Total number of received pings.</summary>
		public long MessageReceivePing => _MessageReceivePing;

		/// <summary>Total number of sent bytes in messages before compression and encryption.</summary>
		public long MessageSendBytes => _MessageSendBytes;

		/// <summary>Total number of sent messages.</summary>
		public long MessageSendTotal => _MessageSendTotal;

		/// <summary>Total number of sent reliable messages.</summary>
		public long MessageSendReliable => _MessageSendReliable;

		/// <summary>Total number of sent unreliable messages.</summary>
		public long MessageSendUnreliable => _MessageSendUnreliable;

		/// <summary>Total number of sent duplicated messages.</summary>
		public long MessageSendDuplicated => _MessageSendDuplicated;

		/// <summary>Total number of sent acknowledgements.</summary>
		public long MessageSendAcknowledge => _MessageSendAcknowledge;

		/// <summary>Total number of sent pings.</summary>
		public long MessageSendPing => _MessageSendPing;

		// Interlocked values
		private long _PacketReceiveTicks;
		private long _PacketReceiveBytes;
		private long _PacketReceiveCount;
		private long _PacketSendTicks;
		private long _PacketSendBytes;
		private long _PacketSendCount;
		private long _MessageReceiveBytes;
		private long _MessageReceiveLost;
		private long _MessageReceiveTotal;
		private long _MessageReceiveReliable;
		private long _MessageReceiveUnreliable;
		private long _MessageReceiveDuplicated;
		private long _MessageReceiveAcknowledge;
		private long _MessageReceivePing;
		private long _MessageSendBytes;
		private long _MessageSendTotal;
		private long _MessageSendReliable;
		private long _MessageSendUnreliable;
		private long _MessageSendDuplicated;
		private long _MessageSendAcknowledge;
		private long _MessageSendPing;

		/// <summary>Used internally by the netcode to set <see cref="PacketReceiveTicks"/>.</summary>
		internal void SetPacketReceiveTicks(long ticks) {
			Interlocked.Exchange(ref _PacketReceiveTicks, ticks);
		}

		/// <summary>Used internally by the netcode to set <see cref="PacketReceiveBytes"/>.</summary>
		internal void AddPacketReceiveBytes(long bytes) {
			Interlocked.Add(ref _PacketReceiveBytes, bytes);
		}

		/// <summary>Used internally by the netcode to set <see cref="PacketReceiveCount"/>.</summary>
		internal void IncrementPacketReceiveCount() {
			Interlocked.Increment(ref _PacketReceiveCount);
		}

		/// <summary>Used internally by the netcode to set <see cref="PacketSendTicks"/>.</summary>
		internal void SetPacketSendTicks(long ticks) {
			Interlocked.Exchange(ref _PacketSendTicks, ticks);
		}

		/// <summary>Used internally by the netcode to set <see cref="PacketSendBytes"/>.</summary>
		internal void AddPacketSendBytes(long bytes) {
			Interlocked.Add(ref _PacketSendBytes, bytes);
		}

		/// <summary>Used internally by the netcode to set <see cref="PacketSendCount"/>.</summary>
		internal void IncrementPacketSendCount() {
			Interlocked.Increment(ref _PacketSendCount);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveBytes"/>.</summary>
		internal void AddMessageReceiveBytes(long bytes) {
			Interlocked.Add(ref _MessageReceiveBytes, bytes);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveLost"/>.</summary>
		internal void AddMessageReceiveLost(long amount) {
			Interlocked.Add(ref _MessageReceiveLost, amount);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveLost"/>.</summary>
		internal void DecrementMessageReceiveLost() {
			Interlocked.Decrement(ref _MessageReceiveLost);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveTotal"/>.</summary>
		internal void IncrementMessageReceiveTotal() {
			Interlocked.Increment(ref _MessageReceiveTotal);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveReliable"/>.</summary>
		internal void IncrementMessageReceiveReliable() {
			Interlocked.Increment(ref _MessageReceiveReliable);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveUnreliable"/>.</summary>
		internal void IncrementMessageReceiveUnreliable() {
			Interlocked.Increment(ref _MessageReceiveUnreliable);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveDuplicated"/>.</summary>
		internal void IncrementMessageReceiveDuplicated() {
			Interlocked.Increment(ref _MessageReceiveDuplicated);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceiveAcknowledge"/>.</summary>
		internal void IncrementMessageReceiveAcknowledge() {
			Interlocked.Increment(ref _MessageReceiveAcknowledge);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageReceivePing"/>.</summary>
		internal void IncrementMessageReceivePing() {
			Interlocked.Increment(ref _MessageReceivePing);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendBytes"/>.</summary>
		internal void AddMessageSendBytes(long bytes) {
			Interlocked.Add(ref _MessageSendBytes, bytes);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendTotal"/>.</summary>
		internal void IncrementMessageSendTotal() {
			Interlocked.Increment(ref _MessageSendTotal);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendReliable"/>.</summary>
		internal void IncrementMessageSendReliable() {
			Interlocked.Increment(ref _MessageSendReliable);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendUnreliable"/>.</summary>
		internal void IncrementMessageSendUnreliable() {
			Interlocked.Increment(ref _MessageSendUnreliable);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendDuplicated"/>.</summary>
		internal void IncrementMessageSendDuplicated() {
			Interlocked.Increment(ref _MessageSendDuplicated);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendAcknowledge"/>.</summary>
		internal void IncrementMessageSendAcknowledge() {
			Interlocked.Increment(ref _MessageSendAcknowledge);
		}

		/// <summary>Used internally by the netcode to set <see cref="MessageSendPing"/>.</summary>
		internal void IncrementMessageSendPing() {
			Interlocked.Increment(ref _MessageSendPing);
		}

		/// <summary>Reset all statistics to zero.</summary>
		public void Reset() {
			Interlocked.Exchange(ref _PacketReceiveTicks, 0);
			Interlocked.Exchange(ref _PacketReceiveBytes, 0);
			Interlocked.Exchange(ref _PacketReceiveCount, 0);
			Interlocked.Exchange(ref _PacketSendTicks, 0);
			Interlocked.Exchange(ref _PacketSendBytes, 0);
			Interlocked.Exchange(ref _PacketSendCount, 0);
			Interlocked.Exchange(ref _MessageReceiveBytes, 0);
			Interlocked.Exchange(ref _MessageReceiveLost, 0);
			Interlocked.Exchange(ref _MessageReceiveTotal, 0);
			Interlocked.Exchange(ref _MessageReceiveReliable, 0);
			Interlocked.Exchange(ref _MessageReceiveUnreliable, 0);
			Interlocked.Exchange(ref _MessageReceiveDuplicated, 0);
			Interlocked.Exchange(ref _MessageReceiveAcknowledge, 0);
			Interlocked.Exchange(ref _MessageReceivePing, 0);
			Interlocked.Exchange(ref _MessageSendBytes, 0);
			Interlocked.Exchange(ref _MessageSendTotal, 0);
			Interlocked.Exchange(ref _MessageSendReliable, 0);
			Interlocked.Exchange(ref _MessageSendUnreliable, 0);
			Interlocked.Exchange(ref _MessageSendDuplicated, 0);
			Interlocked.Exchange(ref _MessageSendAcknowledge, 0);
			Interlocked.Exchange(ref _MessageSendPing, 0);
		}

	}

}
