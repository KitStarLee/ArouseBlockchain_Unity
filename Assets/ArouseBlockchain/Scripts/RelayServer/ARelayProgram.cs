using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using Peer = SuperNet.Netcode.Transport.Peer;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain.NetService
{

    public class ARelayProgram : IHostListener, IPeerListener
    {

        // This is a standalone relay program that can be compiled without Unity
        // Once running, it will keep track of a server list and facilitate P2P connections
        // To use it, make sure the UDP port (by default 44015) is forwarded
        // You can use this relay for your own server list on your own server

        // Relay running on "superversus.com:44015" should not be used in production

        public static void Main(string[] args)
        {

            string address = "";

            if (args != null && args.Length > 0)
            {
                foreach (var item in args)
                {
                    if (item.Contains("IP"))
                    {
                        try
                        {
                            string[] add = item.Split(":");
                            address = add[1];
                        }
                        catch
                        {

                        }
                    }
                    Console.WriteLine(args.Length + " : " + item);
                }
            }

            // Get port from command line arguments
            int port = 44015;
            if (args.Length == 1)
            {
                int.TryParse(args[0], out port);
            }

            // Initialize program
            new ARelayProgram(port);
            Thread.Sleep(Timeout.Infinite);

        }

        private class Server
        {
            public Peer Peer;
            public RelayPeerNetInfoEntry Entry;
        }

        private static string Date => DateTime.Now.ToString("yyyy-MM-dd H:mm:ss");
        private readonly Dictionary<IPEndPoint, Server> Servers;
        private readonly ReaderWriterLockSlim Lock;
        private readonly Host Host;

        public ARelayProgram(int port)
        {

            // Initialize variables
            Servers = new Dictionary<IPEndPoint, Server>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            // Create host config
            HostConfig config = new HostConfig()
            {
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

        }

        void IHostListener.OnHostReceiveRequest(ConnectionRequest request, Reader message)
        {
            // Accept the connection
            request.Accept(new PeerConfig()
            {
                PingDelay = 2000,
                SendDelay = 15,
                ResendCount = 12,
                ResendDelayJitter = 80,
                ResendDelayMin = 200,
                ResendDelayMax = 800,
                FragmentTimeout = 16000,
                DuplicateTimeout = 2000,
                DisconnectDelay = 500,
            }, this);
        }

        void IPeerListener.OnPeerConnect(Peer peer)
        {
            Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Connected.");
        }

        void IPeerListener.OnPeerReceive(Peer peer, Reader message, MessageReceived info)
        {

            // Convert channel to message type
            RelayMessageType type = (RelayMessageType)info.Channel;

            // Process the message based on type
            switch (type)
            {
                case RelayMessageType.PeerListRequest:
                    // Peer has requested current server list
                    OnPeerReceiveListRequest(peer, message, info);
                    break;
                case RelayMessageType.ServerUpdate:
                    // Peer has request to add or update the server
                    OnPeerReceiveServerUpdate(peer, message, info);
                    break;
                case RelayMessageType.ServerRemove:
                    // Peer has request to remove the server
                    OnPeerReceiveServerRemove(peer, message, info);
                    break;
                case RelayMessageType.Connect:
                    // Peer has requested to connect to a server on this relay
                    // Notify the server if it exists
                    OnPeerReceiveConnect(peer, message, info);
                    break;
                default:
                    Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Unknown message type " + info.Channel + " received.");
                    break;
            }

        }

        private void OnPeerReceiveListRequest(Peer peer, Reader reader, MessageReceived info)
        {

            // Read the message
            RelayListRequest message = new RelayListRequest();
            message.Read(reader);

            // Copy server list
            List<RelayPeerNetInfoEntry> servers = new List<RelayPeerNetInfoEntry>();
            try
            {
                Lock.EnterReadLock();
                foreach (Server server in Servers.Values)
                {
                    servers.Add(server.Entry);
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }

            // Send server list
            peer.Send(new RelayListResponse() { Servers = servers });

            // Notify console
            Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Sending server list. Servers = " + servers.Count);

        }

        private void OnPeerReceiveServerRemove(Peer peer, Reader reader, MessageReceived info)
        {

            // Read the message
            RelayServerRemove message = new RelayServerRemove();
            message.Read(reader);

            // Remove from server list if exists
            bool removed = false;
            try
            {
                Lock.EnterWriteLock();
                removed = Servers.Remove(peer.Remote);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // Notify console
            if (removed)
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Removed from server list.");
            }
            else
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] No server to remove.");
            }

        }

        private void OnPeerReceiveServerUpdate(Peer peer, Reader reader, MessageReceived info)
        {

            // Read the message
            RelayServerUpdate message = new RelayServerUpdate();
            message.Read(reader);

            // Create entry
            RelayPeerNetInfoEntry entry = message.Entry;
            string remote = peer.Remote.ToString();
            entry.AddressRemote = remote;
            if (entry.AddressPublic != remote)
            {
                if (IsInternal(peer.Remote.Address))
                {
                    Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Received public IP " + entry.AddressPublic + " is not " + remote + ". Keeping.");
                }
                else
                {
                    Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Received public IP " + entry.AddressPublic + " is not " + remote + ". Deleting.");
                    entry.AddressPublic = null;
                }
            }

            // Add to server list
            try
            {
                Lock.EnterWriteLock();
                Servers[peer.Remote] = new Server()
                {
                    Peer = peer,
                    Entry = entry,
                };
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // Notify console
            Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Server added or updated.");

        }

        private void OnPeerReceiveConnect(Peer peer, Reader reader, MessageReceived info)
        {

            // Read the message
            RelayConnect message = new RelayConnect();
            message.Read(reader);

            // Parse connect address
            IPEndPoint remote = IPResolver.TryParse(message.Address);
            if (remote == null)
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Bad connect address '" + message.Address + "' received.");
                return;
            }

            // Find server if it exists
            Server server = null;
            try
            {
                Lock.EnterReadLock();
                Servers.TryGetValue(remote, out server);
            }
            finally
            {
                Lock.ExitReadLock();
            }

            // Notify server about the connection
            if (server == null)
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Server " + remote + " is not on this relay.");
            }
            else
            {
                server.Peer.Send(new RelayConnect() { Address = peer.Remote.ToString() });
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Notifying server " + remote + " about the connection attempt.");
            }

        }

        void IPeerListener.OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception)
        {

            // Remove from server list if exists
            bool removed = false;
            try
            {
                Lock.EnterWriteLock();
                removed = Servers.Remove(peer.Remote);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // Notify console
            if (removed)
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Removed from server list and disconnected: " + reason);
            }
            else
            {
                Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Disconnected: " + reason);
            }

        }

        void IHostListener.OnHostException(IPEndPoint remote, Exception exception)
        {
            Console.WriteLine("[" + Date + "] Host exception: " + exception.Message);
        }

        void IHostListener.OnHostShutdown()
        {
            Console.WriteLine("[" + Date + "] Host shut down.");
        }

        void IPeerListener.OnPeerException(Peer peer, Exception exception)
        {
            Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Peer exception: " + exception.ToString());
        }

        void IPeerListener.OnPeerUpdateRTT(Peer peer, ushort rtt)
        {
            // We don't care about RTT
        }

        void IHostListener.OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length)
        {
            // We don't care about raw socket packets
        }

        void IHostListener.OnHostReceiveUnconnected(IPEndPoint remote, Reader message)
        {
            // We don't care about unconnected packets
        }

        void IHostListener.OnHostReceiveBroadcast(IPEndPoint remote, Reader message)
        {
            // We don't care about broadcast packets
        }

        /// <summary>
        /// 是否是内网
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static bool IsInternal(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.ToString() == "::1") return true;
            byte[] ip = address.GetAddressBytes();
            switch (ip[0])
            {
                case 10: return true;
                case 127: return true;
                case 172: return ip[1] >= 16 && ip[1] < 32;
                case 192: return ip[1] == 168;
                default: return false;
            }
        }

    }

}
