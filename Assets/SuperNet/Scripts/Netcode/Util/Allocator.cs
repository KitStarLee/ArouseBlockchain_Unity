using SuperNet.Netcode.Transport;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SuperNet.Netcode.Util {

	public sealed class Allocator {

		// Config
		private readonly int Count;
		private readonly int PooledLength;
		private readonly int PooledExpandLength;
		private readonly int ExpandLength;
		private readonly int MaxLength;

		/// <summary>
		/// Create a new allocator for a host.
		/// </summary>
		/// <param name="config">Configuration to use.</param>
		public Allocator(HostConfig config) {
			Count = config.AllocatorCount;
			PooledLength = config.AllocatorPooledLength;
			PooledExpandLength = config.AllocatorPooledExpandLength;
			ExpandLength = config.AllocatorExpandLength;
			MaxLength = config.AllocatorMaxLength;
			IV = new ArrayPool<byte>(Count, PooledLength);
			Key = new ArrayPool<byte>(Count, PooledLength);
			SocketArgs = new ObjectPool<SocketAsyncEventArgs>(Count);
			Packets = new ArrayPool<byte>(Count, PooledLength);
			Messages = new ArrayPool<byte>(Count, PooledLength);
			Sequences = new ArrayPool<int>(Count, PooledLength);
			Tokens = new ArrayPool<CancellationTokenSource>(Count, PooledLength);
			LZF = new ArrayPool<long>(Count, PooledLength);
			HashSets = new ObjectPool<HashSet<Tuple<byte, ushort>>>(Count);
			Sent = new ObjectPool<Dictionary<Tuple<byte, ushort>, MessageSent>>(Count);
		}

		/// <summary>
		/// Create a new allocator without any pooling.
		/// </summary>
		public Allocator() {
			Count = 0;
			PooledLength = 0;
			PooledExpandLength = 1;
			ExpandLength = 1;
			MaxLength = int.MaxValue;
			IV = new ArrayPool<byte>(Count, PooledLength);
			Key = new ArrayPool<byte>(Count, PooledLength);
			Packets = new ArrayPool<byte>(Count, PooledLength);
			Messages = new ArrayPool<byte>(Count, PooledLength);
			Sequences = new ArrayPool<int>(Count, PooledLength);
			Tokens = new ArrayPool<CancellationTokenSource>(Count, PooledLength);
			LZF = new ArrayPool<long>(Count, PooledLength);
			HashSets = new ObjectPool<HashSet<Tuple<byte, ushort>>>(Count);
			Sent = new ObjectPool<Dictionary<Tuple<byte, ushort>, MessageSent>>(Count);
		}

		//////////////////////////////// Fixed 16 bytes ///////////////////////////////

		private readonly ArrayPool<byte> IV;

		/// <summary>Allocate a new IV array for crypto.</summary>
		/// <param name="length">Length of the array.</param>
		/// <returns>A new unused array.</returns>
		public byte[] CreateIV(int length) {
			return IV.Rent(length);
		}

		/// <summary>Return an IV array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void ReturnIV(ref byte[] array) {
			byte[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			IV.Return(copy);
		}

		//////////////////////////////// Fixed 32 bytes ///////////////////////////////

		private readonly ArrayPool<byte> Key;

		/// <summary>Allocate a new key array for crypto.</summary>
		/// <param name="length">Length of the array.</param>
		/// <returns>A new unused array.</returns>
		public byte[] CreateKey(int length) {
			return Key.Rent(length);
		}

		/// <summary>Return a key array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void ReturnKey(ref byte[] array) {
			byte[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			Key.Return(copy);
		}

		//////////////////////////////// SocketAsyncEventArgs ///////////////////////////////

		private readonly ObjectPool<SocketAsyncEventArgs> SocketArgs;

		public SocketAsyncEventArgs CreateSocketArgs(EventHandler<SocketAsyncEventArgs> callback) {
			SocketAsyncEventArgs args = SocketArgs.Rent();
			if (args == null) {
				args = new SocketAsyncEventArgs();
				args.Completed += callback;
			}
			return args;
		}

		public void ReturnSocketArgs(ref SocketAsyncEventArgs args) {
			SocketAsyncEventArgs copy = Interlocked.Exchange(ref args, null);
			if (copy != null) SocketArgs.Return(copy);
		}

		//////////////////////////////// Packet buffers ///////////////////////////////

		private readonly ArrayPool<byte> Packets;

		/// <summary>Allocate a new short array to store a single packet.</summary>
		/// <param name="minimumLength">Minimum length of the returned array.</param>
		/// <returns>A new unused array.</returns>
		public byte[] CreatePacket(int minimumLength) {
			return Packets.Rent(minimumLength);
		}

		/// <summary>Return a packet array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void ReturnPacket(ref byte[] array) {
			byte[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			Packets.Return(copy);
		}

		//////////////////////////////// Message buffers ///////////////////////////////

		private readonly ArrayPool<byte> Messages;

		/// <summary>Allocate a new resizable array to store a single message.</summary>
		/// <param name="minimumLength">Minimum length of the returned array.</param>
		/// <returns>A new unused array.</returns>
		public byte[] CreateMessage(int minimumLength) {
			if (minimumLength > MaxLength) {
				throw new InvalidOperationException(string.Format(
					"Message length {0} is larger than {1}",
					minimumLength, MaxLength
				));
			} else {
				return Messages.Rent(minimumLength);
			}
		}

		/// <summary>Return a message array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void ReturnMessage(ref byte[] array) {
			byte[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			Messages.Return(copy);
		}

		/// <summary>Resize a message array to a larger size.</summary>
		/// <param name="array">Array to resize.</param>
		/// <param name="offset">Current array offset.</param>
		/// <param name="length">Length beyond the array offset to add.</param>
		/// <returns>A new resized array with copied data.</returns>
		public byte[] ExpandMessage(byte[] array, int offset, int length = 1) {
			if (offset + length > MaxLength) {
				throw new InvalidOperationException(string.Format(
					"Message length {0} with offset {1} is larger than {2}",
					length, offset, MaxLength
				));
			} else if (offset + length < PooledLength) {
				return Messages.Expand(array, offset, length, PooledExpandLength);
			} else {
				return Messages.Expand(array, offset, length, ExpandLength);
			}
		}

		//////////////////////////////// Sequences ///////////////////////////////

		private readonly ArrayPool<int> Sequences;

		/// <summary>Allocate a new array to store message sequence for each channel.</summary>
		/// <param name="channels">Number of channels.</param>
		/// <returns>A new unused array.</returns>
		public int[] SequenceNew(int channels) {
			return Sequences.Rent(channels);
		}

		/// <summary>Return a sequence array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void SequenceReturn(ref int[] array) {
			int[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			Sequences.Return(copy);
		}

		//////////////////////////////// Tokens ///////////////////////////////

		private readonly ArrayPool<CancellationTokenSource> Tokens;

		/// <summary>Allocate a new cancellation token array for each channel.</summary>
		/// <param name="channels">Number of channels.</param>
		/// <returns>A new unused array.</returns>
		public CancellationTokenSource[] TokensNew(int channels) {
			CancellationTokenSource[] copy = Tokens.Rent(channels);
			for (int i = 0; i < copy.Length; i++) {
				try { copy[i]?.Cancel(); } catch { }
				try { copy[i]?.Dispose(); } catch { }
				try { copy[i] = null; } catch { }
			}
			return copy;
		}

		/// <summary>Return a cancellation token array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void TokensReturn(ref CancellationTokenSource[] array) {
			CancellationTokenSource[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			for (int i = 0; i < copy.Length; i++) {
				try { copy[i]?.Cancel(); } catch { }
				try { copy[i]?.Dispose(); } catch { }
				try { copy[i] = null; } catch { }
			}
			Tokens.Return(copy);
		}

		//////////////////////////////// LZF ///////////////////////////////

		private readonly ArrayPool<long> LZF;

		/// <summary>Allocate a new hash table array for the LZF compressor.</summary>
		/// <param name="length">Length of the array.</param>
		/// <returns>A new unused array.</returns>
		public long[] HashTableCreate(int length) {
			return LZF.Rent(length);
		}

		/// <summary>Return a hash table array back to the pool.</summary>
		/// <param name="array">Array to return.</param>
		public void HashTableReturn(long[] array) {
			long[] copy = Interlocked.Exchange(ref array, null);
			if (copy == null) return;
			LZF.Return(copy);
		}

		//////////////////////////////// HashSet ///////////////////////////////

		private readonly ObjectPool<HashSet<Tuple<byte, ushort>>> HashSets;

		/// <summary>Allocate a new HashSet used by peers.</summary>
		/// <returns>A new unused HashSet.</returns>
		public HashSet<Tuple<byte, ushort>> CreateSet() {
			HashSet<Tuple<byte, ushort>> set = HashSets.Rent();
			if (set == null) return new HashSet<Tuple<byte, ushort>>();
			set.Clear();
			return set;
		}

		/// <summary>Return a HashSet back to the pool. </summary>
		/// <param name="set">HashSet to return.</param>
		public void ReturnSet(ref HashSet<Tuple<byte, ushort>> set) {
			HashSet<Tuple<byte, ushort>> copy = Interlocked.Exchange(ref set, null);
			if (copy == null) return;
			copy.Clear();
			HashSets.Return(copy);
		}

		//////////////////////////////// Sent messages ///////////////////////////////

		private readonly ObjectPool<Dictionary<Tuple<byte, ushort>, MessageSent>> Sent;

		/// <summary>Allocate a new sent message storage used by peers.</summary>
		/// <returns>A new unused sent message storage.</returns>
		public Dictionary<Tuple<byte, ushort>, MessageSent> CreateSent() {
			Dictionary<Tuple<byte, ushort>, MessageSent> set = Sent.Rent();
			if (set == null) return new Dictionary<Tuple<byte, ushort>, MessageSent>();
			foreach (MessageSent sent in set.Values) sent.StopResending();
			set.Clear();
			return set;
		}

		/// <summary>Return a sent message storage back to the pool.</summary>
		/// <param name="set">Sent message storage to return.</param>
		public void ReturnSent(ref Dictionary<Tuple<byte, ushort>, MessageSent> set) {
			Dictionary<Tuple<byte, ushort>, MessageSent> copy = Interlocked.Exchange(ref set, null);
			if (copy == null) return;
			foreach (MessageSent sent in copy.Values) sent.StopResending();
			copy.Clear();
			Sent.Return(copy);
		}

	}

}
