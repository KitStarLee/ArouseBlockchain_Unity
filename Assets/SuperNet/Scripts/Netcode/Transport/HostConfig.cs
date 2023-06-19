using System;
using System.Net;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Holds configuration values for hosts.
	/// </summary>
	[Serializable]
	public class HostConfig {

		/// <summary>
		/// UDP port to listen on or zero for random.
		/// <para>Must be between 0 and 65536.</para>
		/// </summary>
		public int Port = 0;

		/// <summary>
		/// Bind socket to a specific address or null for any.
		/// </summary>
		public IPAddress BindAddress = null;

		/// <summary>
		/// Set <c>Socket.DualMode</c> when creating a socket.
		/// <para>If true, accept both IPv6 and IPv4 connections.</para>
		/// </summary>
		public bool DualMode = false;

		/// <summary>
		/// Set <c>Socket.EnableBroadcast</c> when creating a socket.
		/// <para>If true, allow broadcast messages to be sent.</para>
		/// </summary>
		public bool Broadcast = true;

		/// <summary>
		/// Set <c>Socket.Ttl</c> when creating a socket.
		/// <para>Maximum number of hops packets can take before being dropped.</para>
		/// </summary>
		public short TTL = 128;

		/// <summary>
		/// Set <c>Socket.SendBufferSize</c> when creating a socket.
		/// <para>Maximum socket send buffer in bytes.</para>
		/// </summary>
		public int SendBufferSize = 32768;

		/// <summary>
		/// Set <c>Socket.ReceiveBufferSize</c> when creating a socket.
		/// <para>Maximum socket receive buffer in bytes.</para>
		/// </summary>
		public int ReceiveBufferSize = 32768;

		/// <summary>
		/// Maximum number of possible concurrent read operations.
		/// </summary>
		public int ReceiveCount = 8;

		/// <summary>
		/// Maximum number of bytes in a single received UDP packet.
		/// <para>This is used to allocate appropriately sized receive buffers.</para>
		/// </summary>
		public int ReceiveMTU = 1500;

		/// <summary>
		/// Enable CRC32 error checking to make sure packets don't get corrupted in transit. 
		/// <para>If false and a CRC32 code is received, it is ignored.</para>
		/// </summary>
		public bool CRC32 = true;

		/// <summary>
		/// Enable compression of outgoing network packets.
		/// <para>If false and a compressed packet is received, it is still decompressed.</para>
		/// </summary>
		public bool Compression = true;

		/// <summary>
		/// Enable end to end encryption between peers.
		/// <para>A connection request without encryption can still be accepted.</para>
		/// </summary>
		public bool Encryption = true;

		/// <summary>
		/// Private key to use when authenticating this host.
		/// <para>If null, this host cannot be authenticated.</para>
		/// </summary>
		public string PrivateKey = null;

		/// <summary>
		/// Number of pooled arrays.
		/// </summary>
		public int AllocatorCount = 256;

		/// <summary>
		/// Maximum length an array can still be to be pooled.
		/// </summary>
		public int AllocatorPooledLength = 32768;

		/// <summary>
		/// Number of bytes to add when a pooled array becomes too small.
		/// </summary>
		public int AllocatorPooledExpandLength = 1024;

		/// <summary>
		/// Number of bytes to add when a non-pooled array becomes too small
		/// </summary>
		public int AllocatorExpandLength = 65536;

		/// <summary>
		/// Maximum length of allocated arrays.
		/// </summary>
		public int AllocatorMaxLength = 16777216;

		/// <summary>
		/// Enable latency and packet loss simulation.
		/// </summary>
		public bool SimulatorEnabled = false;

		/// <summary>
		/// Simulated packet loss % when sending. Must be between 0 and 1.
		/// <para>Value of 0 means no packet loss, value of 1 means all packets are dropped.</para>
		/// </summary>
		public float SimulatorOutgoingLoss = 0f;

		/// <summary>
		/// Simulated delay in milliseconds when sending packets.
		/// </summary>
		public float SimulatorOutgoingLatency = 0f;

		/// <summary>
		/// Randomly added delay in milliseconds when sending packets.
		/// <para>Making this non-zero can scramble the order of packets.</para>
		/// </summary>
		public float SimulatorOutgoingJitter = 0f;

		/// <summary>
		/// Simulated packet loss % when receiving. Must be between 0 and 1.
		/// <para>Value of 0 means no packet loss, value of 1 means all packets are dropped.</para>
		/// </summary>
		public float SimulatorIncomingLoss = 0f;

		/// <summary>
		/// Simulated delay in milliseconds when receiving packets.
		/// </summary>
		public float SimulatorIncomingLatency = 0f;

		/// <summary>
		/// Randomly added delay in milliseconds when receiving packets.
		/// <para>Making this non-zero can scramble the order of packets.</para>
		/// </summary>
		public float SimulatorIncomingJitter = 0f;

	}

}
