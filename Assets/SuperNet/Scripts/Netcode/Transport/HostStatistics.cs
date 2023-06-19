using System.Threading;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Stores packet statistics for hosts.
	/// </summary>
	public class HostStatistics {

		/// <summary>Host ticks at the moment of the last socket send operation.</summary>
		public long SocketSendTicks => _SocketSendTicks;

		/// <summary>Total number of bytes sent.</summary>
		public long SocketSendBytes => _SocketSendBytes;

		/// <summary>Total number of packets sent.</summary>
		public long SocketSendCount => _SocketSendCount;

		/// <summary>Host ticks at the moment of the last socket receive operation.</summary>
		public long SocketReceiveTicks => _SocketReceiveTicks;

		/// <summary>Total number of bytes received.</summary>
		public long SocketReceiveBytes => _SocketReceiveBytes;

		/// <summary>Total number of packets received.</summary>
		public long SocketReceiveCount => _SocketReceiveCount;
		
		// Interlocked values
		private long _SocketSendTicks;
		private long _SocketSendBytes;
		private long _SocketSendCount;
		private long _SocketReceiveTicks;
		private long _SocketReceiveBytes;
		private long _SocketReceiveCount;

		/// <summary>Used internally by the netcode to set <see cref="SocketSendTicks"/>.</summary>
		internal void SetSocketSendTicks(long ticks) {
			Interlocked.Exchange(ref _SocketSendTicks, ticks);
		}

		/// <summary>Used internally by the netcode to set <see cref="SocketSendBytes"/>.</summary>
		internal void AddSocketSendBytes(long bytes) {
			Interlocked.Add(ref _SocketSendBytes, bytes);
		}

		/// <summary>Used internally by the netcode to set <see cref="SocketReceiveTicks"/>.</summary>
		internal void SetSocketReceiveTicks(long ticks) {
			Interlocked.Exchange(ref _SocketReceiveTicks, ticks);
		}

		/// <summary>Used internally by the netcode to set <see cref="SocketSendCount"/>.</summary>
		internal void IncrementSocketSendCount() {
			Interlocked.Increment(ref _SocketSendCount);
		}

		/// <summary>Used internally by the netcode to set <see cref="SocketReceiveBytes"/>.</summary>
		internal void AddSocketReceiveBytes(long bytes) {
			Interlocked.Add(ref _SocketReceiveBytes, bytes);
		}

		/// <summary>Used internally by the netode to set <see cref="SocketReceiveCount"/>.</summary>
		internal void IncrementSocketReceiveCount() {
			Interlocked.Increment(ref _SocketReceiveCount);
		}

		/// <summary>Reset all statistics back to zero.</summary>
		public void Reset() {
			Interlocked.Exchange(ref _SocketSendTicks, 0);
			Interlocked.Exchange(ref _SocketSendBytes, 0);
			Interlocked.Exchange(ref _SocketSendCount, 0);
			Interlocked.Exchange(ref _SocketReceiveTicks, 0);
			Interlocked.Exchange(ref _SocketReceiveBytes, 0);
			Interlocked.Exchange(ref _SocketReceiveCount, 0);
		}

	}

}
