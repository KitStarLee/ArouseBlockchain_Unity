using System;
using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using System.Net;
using ArouseBlockchain.Common;

namespace ArouseBlockchain.P2PNet
{
    public class P2PRelay: IPeerListener
    {
        //127.0.0.1:44015
        //54.169.217.20:44015
        private const string NET_RELAY_IP = "172.20.10.2:44015";

        //中继
        public event Action OnRelayConnect;
        public event Action<Exception> OnRelayDisconnect;


        //中继服务更新了Response列表
        public event Action<RelayListResponse> OnRelayRefresh;
        public bool ConnectedToRelay => Peer?.Connected ?? false;

        private ANetworkHost _Host;
        public Peer Peer { get; private set; }

        //是否已经开过房间
        protected bool ServerAdded;
        public bool IsServer => ServerAdded;


        public void Init(ANetworkHost host)
        {
            _Host = host;
            Peer = null;
        }

        public void Shutdown()
        {
            //关闭全部的链接
            ServerAdded = false;
            Peer?.Send(new RelayServerRemove());
            Peer?.Disconnect();
            Peer = null;

            ALog.Log("Relay Server Stop");
        }

        #region 中继 Relay Server code

        /// <summary>
        /// First connect to register in relay server
        /// </summary>
        /// <returns></returns>
        public bool ConnectToRelay()
        {
            // Make sure host is running

           // _Host.HostConfiguration.Port = 0;
            _Host.Startup(true);

            // If already connected to relay, return true
            if (Peer != null && !Peer.Disposed)
            {
                return true;
            }

            // Start connecting to relay
            IPEndPoint address = IPResolver.Resolve(NET_RELAY_IP);
            Peer = _Host.Connect(address, this, null, true);

            //返回False,证明链接中
            return false;

        }


        /// <summary>
        /// 创建房间，并且通知中继服务
        /// </summary>
        /// <param name="serverName"></param>
        /// <exception cref="Exception"></exception>
        public void CreateServer(string serverName)
        {
            ALog.Log("去创建房间服务");

            if (ServerAdded) return;

            // Create or update server
            if (Peer == null || !Peer.Connected)
                throw new Exception("Not connected to relay");
            //else if (PeerClient != null)
            //    throw new Exception("Already connected to a server");
            else
            {
                string port = _Host.GetBindAddress().Port.ToString();
                IPEndPoint local = _Host.GetLocalAddress();

                RelayPeerNetInfoEntry entry = new RelayPeerNetInfoEntry();

                entry.AddressLocal = local.ToString();
                entry.AddressRemote = Peer.Remote.ToString();
                entry.AddressPublic = P2PManager.PublicIP == null ? null : (P2PManager.PublicIP.ToString() + ":" + port);
                entry.Name = local.ToString();
                entry.Players = _Host.GetPeers().Count;

              //  P2PManager.Instance._SelfNetInfoEntry = entry;

                ALog.Log("AAA-1!!!! Connected to " + entry.Name + "\r\n" + entry.AddressLocal + "\r\n" + entry.AddressRemote + "\r\n" + entry.AddressPublic);


               // Peer.Send(new RelayServerUpdate() { Entry = P2PManager.Instance._SelfNetInfoEntry });
                ServerAdded = true;
            }


        }


        /// <summary>
        /// 请求中继服务器刷新Peer列表
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Refresh()
        {
            ALog.Log("去刷新房间列表");

            if (Peer == null || !Peer.Connected)
                throw new Exception("Not connected to relay");
            else
            {
                Peer.Send(new RelayListRequest());
            }
        }

        public void ConnectToServerByRelay(RelayPeerNetInfoEntry server)
        {
            ALog.Log("去链接服务通过房间地址");

            // Connect to the chosen server
            if (Peer == null || !Peer.Connected)
                throw new Exception("Not connected to relay");

            //P2P模式下，PeerClient需要链接的是多个客户端Host
            //   else if (PeerClient != null)
            //   {
            //      throw new Exception("Already connected to a server");
            //   }
            else
            {
                IPEndPoint address;
                IPEndPoint address_local = server.AddressLocal == null ? null : IPResolver.Resolve(server.AddressLocal);
                IPEndPoint address_remote = server.AddressRemote == null ? null : IPResolver.Resolve(server.AddressRemote);
                IPEndPoint address_public = server.AddressPublic == null ? null : IPResolver.Resolve(server.AddressPublic);
                if (P2PManager.PublicIP != null && P2PManager.PublicIP.Equals(address_public?.Address))
                {
                    //如果公网地址相同，那肯定是本地局域网环境，则不需要公网IP链接
                    address = address_local ?? address_remote ?? throw new Exception("No local address");
                }
                else
                {
                    //否则需要通过public id 进行链接
                    address = address_public ?? address_remote ?? throw new Exception("No public address");
                }

                //主动去链接对方
                //### P2PManager._P2PClient.Connect(address); //_Host.Connect(address, null, null, true);

                //if (P2PManager._P2PClient.Peer == null)
                //{
                //    ALog.Error("!!! Peer created bad " + address);
                //}
               
                //防止此用户之前创建的房间没有移除，然后链接了其他房间
                Peer.Send(new RelayServerRemove());

                //告诉中继，我需要去链接这个Peer
                Peer.Send(new RelayConnect() { Address = address.ToString() });

                ServerAdded = false;
            }
        }

        /// <summary>
        /// 接受到一个数据列表在中继服务
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        private void OnRelayReceiveList(Peer peer, Reader reader)
        {
            ALog.Log("接收到服务器传来的房间地址列表");

            // We received a list of servers from the relay, notify the menu
            RelayListResponse message = new RelayListResponse();
            message.Read(reader);
            NetworkManager.Run(() =>
            {
                OnRelayRefresh?.Invoke(message);
            });
        }

        /// <summary>
        /// 链接到接受到的地址
        /// </summary>
        /// <param name="reader"></param>
        private void OnRelayReceiveConnect(Reader reader)
        {
           

            RelayConnect message = new RelayConnect();
            message.Read(reader);
            IPEndPoint address = IPResolver.TryParse(message.Address);

            ALog.Log("直接链接到这个地址： " + address);

            if (address == null)
            {
                ALog.Error("[Arena] Invalid address from relay: " + message.Address);
            }
            else
            {
                _Host.Connect(address);
            }
        }



        #endregion


        #region Peer Listener
        void IPeerListener.OnPeerConnect(Peer peer)
        {
            ALog.Log("[P2PRelay :][New Peer] Connected to " + peer.Remote + ".");

            NetworkManager.Run(() =>
            {
                if (peer == Peer)
                {

                    // We just finished connecting to the relay
                    // Unregister the peer, refresh the list of servers, and notify the menu
                    // We unregister the peer because it isn't used in the component system
                    NetworkManager.Unregister(peer);
                    peer.Send(new RelayListRequest());
                    OnRelayConnect?.Invoke();
                    return;
                }
                else
                {
                    ALog.Log("ERROR !!! : " + "Not Relay Peer dont Connect");
                }

                NetworkManager.Register(peer);
                if (ServerAdded)
                {
                    // We are a server and a new client just connected
                    // Update the server entry on the relay
                    RelayPeerNetInfoEntry entry = P2PManager.Instance._SelfNetInfoEntry;
                    entry.Players = _Host.GetPeers().Count;
                    P2PManager.Instance._SelfNetInfoEntry = entry;

                    peer?.Send(new RelayServerUpdate() { Entry = entry });
                }
            });
        }

        public void OnPeerReceive(Peer peer, Reader reader, MessageReceived info)
        {
            RelayMessageType type = (RelayMessageType)info.Channel;
            switch (type)
            {
                case RelayMessageType.PeerListResponse:
                    OnRelayReceiveList(peer, reader);
                    break;
                case RelayMessageType.Connect:
                    OnRelayReceiveConnect(reader);
                    break;
            }
        }

        public void OnPeerUpdateRTT(Peer peer, ushort rtt)
        {
            //throw new NotImplementedException();
        }

        void IPeerListener.OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception)
        {
            ALog.Log("[P2PRelay :][Arena] Disconnected from " + peer.Remote + ": " + reason);

            NetworkManager.Run(() =>
            {

                // Unregister the peer because it is no longer active
                NetworkManager.Unregister(peer);

                if (peer == Peer)
                {
                    // We just disconnected from the relay, notify the menu
                    Peer = null;
                    if (exception == null)
                    {
                        OnRelayDisconnect?.Invoke(new Exception(reason.ToString()));
                    }
                    else
                    {
                        OnRelayDisconnect?.Invoke(exception);
                    }
                    return;
                }


                if (ServerAdded)
                {
                    // We are a server and a client just disconnected
                    // Update the server entry on the relay

                    RelayPeerNetInfoEntry entry = P2PManager.Instance._SelfNetInfoEntry;
                    entry.Players = _Host.GetPeers().Count;
                    P2PManager.Instance._SelfNetInfoEntry = entry;

                    Peer?.Send(new RelayServerUpdate() { Entry = entry });
                }


            });
        }

        public void OnPeerException(Peer peer, Exception exception)
        {
            ALog.Error("[P2PRelay :] Peer exception :" + exception);
        }

        #endregion

    }
}