using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Examples.BPP {

	public class BPP_RelayProgram : IHostListener, IPeerListener {

		// This is a standalone relay program that can be compiled without Unity
		// Once running, it will receive component messages and resend them to all connected peers
		// Any kind of a game you create that will work on P2P networks will also work via this relay
		// This relay is useful if the P2P connection fails or if the bandwidth requirements are too great for P2P
		// To use it, make sure the UDP port (by default 44016) is forwarded

		// The Unity build equivalent to this program would be an empty scene with just a NetworkHost
		// By default, the netcode resends all component messages even if the component is not on the scene

		public static void Main(string[] args) {

			// Get port from command line arguments
			int port = 44016;
			if (args.Length == 1) {
				int.TryParse(args[0], out port);
			}

			// Initialize program
			new BPP_RelayProgram(port);
			Thread.Sleep(Timeout.Infinite);

		}

		private static string Date => DateTime.Now.ToString("yyyy-MM-dd H:mm:ss");
		private readonly Host Host;
		private readonly object PeersLock;
		private readonly HashSet<Peer> PeersSet;
		private Peer[] PeersArray;
		
		public BPP_RelayProgram(int port) {

			// Create host config
			HostConfig config = new HostConfig() {
				Port = port,
				SendBufferSize = 65536,
				ReceiveBufferSize = 65536,
				ReceiveCount = 32,
				ReceiveMTU = 2048,
				AllocatorCount = 16384,
				AllocatorPooledLength = 8192,
				AllocatorPooledExpandLength = 1024,
				AllocatorExpandLength = 1024,
				AllocatorMaxLength = 65536,
			};

			// Set thread limits
			ThreadPool.SetMinThreads(32, 1);

			// Get thread limits
			ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIOThreads);
			ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIOThreads);
			ThreadPool.GetAvailableThreads(out int availWorkerThreads, out int availIOThreads);
			Console.WriteLine("Max worker threads = " + maxWorkerThreads + ", max I/O threads = " + maxIOThreads);
			Console.WriteLine("Min worker threads = " + minWorkerThreads + ", min I/O threads = " + minIOThreads);
			Console.WriteLine("Available worker threads = " + availWorkerThreads + ", available I/O threads = " + availIOThreads);

			// Create host
			Host = new Host(config, this);
			Console.WriteLine("[" + Date + "] Relay server started on " + Host.BindAddress);

			// Create peers array
			PeersLock = new object();
			PeersSet = new HashSet<Peer>();
			PeersArray = new Peer[0];

		}

		void IHostListener.OnHostReceiveRequest(ConnectionRequest request, Reader message) {
			// Accept the connection
			request.Accept(new PeerConfig() {
				PingDelay = 2000,
				SendDelay = 0,
				ResendCount = 12,
				ResendDelayJitter = 80,
				ResendDelayMin = 200,
				ResendDelayMax = 800,
				FragmentTimeout = 16000,
				DuplicateTimeout = 2000,
				DisconnectDelay = 500,
			}, this);
		}

		void IPeerListener.OnPeerConnect(Peer peer) {

			// Log
			Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Connected.");

			// Add peer
			lock (PeersLock) {
				bool added = PeersSet.Add(peer);
				if (added) {
					Peer[] array = new Peer[PeersSet.Count];
					PeersSet.CopyTo(array, 0);
					Interlocked.Exchange(ref PeersArray, array);
				} else {
					return;
				}
			}

		}

		void IPeerListener.OnPeerReceive(Peer peer, Reader reader, MessageReceived info) {

			// Get the peers array
			Peer[] peers = Interlocked.CompareExchange(ref PeersArray, null, null);
			MessageCopy copy = null;
			
			// Resend to all other peers
			foreach (Peer other in peers) {

				// Dont resend to yourself or to disconnected peers
				if (other == peer) continue;
				if (!other.Connected) continue;

				// Create copy
				if (copy == null) {
					copy = new MessageCopy(Host.Allocator, reader, info);
				}

				// Send the copy
				copy.Send(other);

			}

		}

		void IPeerListener.OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {

			// Log
			Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Disconnected: " + reason);

			// Remove peer
			lock (PeersLock) {
				bool removed = PeersSet.Remove(peer);
				if (removed) {
					Peer[] array = new Peer[PeersSet.Count];
					PeersSet.CopyTo(array, 0);
					Interlocked.Exchange(ref PeersArray, array);
				} else {
					return;
				}
			}

		}

		void IHostListener.OnHostException(IPEndPoint remote, Exception exception) {
			Console.WriteLine("[" + Date + "] Host exception: " + exception.Message);
		}

		void IHostListener.OnHostShutdown() {
			Console.WriteLine("[" + Date + "] Host shut down.");
		}

		void IPeerListener.OnPeerException(Peer peer, Exception exception) {
			Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Peer exception: " + exception.ToString());
		}

		void IPeerListener.OnPeerUpdateRTT(Peer peer, ushort rtt) {
			// We don't care about RTT
		}

		void IHostListener.OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length) {
			// We don't care about raw socket packets
		}

		void IHostListener.OnHostReceiveUnconnected(IPEndPoint remote, Reader message) {
			// We don't care about unconnected packets
		}

		void IHostListener.OnHostReceiveBroadcast(IPEndPoint remote, Reader message) {
			// We don't care about broadcast packets
		}

		internal class MessageCopy : IMessage, IMessageListener {

			HostTimestamp IMessage.Timestamp => Info.Timestamp;
			byte IMessage.Channel => Info.Channel;
			bool IMessage.Timed => Info.Timed;
			bool IMessage.Reliable => Info.Reliable;
			bool IMessage.Ordered => Info.Ordered;
			bool IMessage.Unique => Info.Unique;

			public readonly Allocator Allocator;
			public readonly MessageReceived Info;
			public readonly int Length;
			public byte[] Buffer;
			public int Count;

			public MessageCopy(Allocator allocator, Reader reader, MessageReceived info) {
				Allocator = allocator;
				Info = info;
				Length = reader.Last - reader.First;
				Buffer = allocator.CreateMessage(Length);
				Array.Copy(reader.Buffer, reader.First, Buffer, 0, Length);
				Count = 0;
			}

			public void Send(Peer peer) {
				Interlocked.Increment(ref Count);
				peer.Send(this, this);
			}

			void IWritable.Write(Writer writer) {
				byte[] buffer = Interlocked.CompareExchange(ref Buffer, null, null);
				if (buffer != null) {
					writer.WriteBytes(buffer, 0, Length);
				}
			}

			void IMessageListener.OnMessageSend(Peer peer, MessageSent message) {
				if (Info.Reliable) return;
				int count = Interlocked.Decrement(ref Count);
				if (count == 1) Allocator.ReturnMessage(ref Buffer);
			}

			void IMessageListener.OnMessageAcknowledge(Peer peer, MessageSent message) {
				int count = Interlocked.Decrement(ref Count);
				if (count == 1) Allocator.ReturnMessage(ref Buffer);
			}

		}

	}

}
