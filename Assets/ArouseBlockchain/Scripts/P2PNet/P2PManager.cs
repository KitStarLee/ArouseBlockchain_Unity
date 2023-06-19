using System;
using System.Net;
using ArouseBlockchain.Common;
using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using UnityEngine;
using EasyButtons;
using ArouseBlockchain.Solve;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

using static ArouseBlockchain.P2PNet.NetTransaction;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;

namespace ArouseBlockchain.P2PNet
{
    /// <summary>
    /// 用于记录Peer的额外数据，来区分和辨析Peer
    /// 1. 区分中继Peer：中继用于最新块的同步，以及Peer直接的网络桥接工作（如果我需要链接那个Peer无法通过自己链接，需要通过中继进行桥接）
    /// 2. 无论是它方主动请求 还是 我方主动请求的Peer都一视同仁。只是会根据其请求的类型，进行处理。每一个类型需要考虑 中继、我、它三者。
    /// 3. 可以先用自己访问自己的方式开发和调试接口，用来看数据通畅情况。
    /// 业务流程：
    /// 1. 获取中继服务，中继返回目前正在链接的Peer （中继的定义：我司放在公网IP上的中继系统和客户端代码基本一致，以及那些系统多次后判断网络稳定的Peer）
    /// 2. 且只有中继网络才会被保存到本地数据，用于下次进行访问 【我司提送中继服务IP也可能发生变化，但是会在软件启动时候更新】
    /// 3. 当我方获取到其他Peer 交接信息好，先对其块版本进行判断，如果是 高于 我们的目标版本（预留目标版本会变，因为一次可能通过好几个Peer进行更新，更新后目前会更高，如果失败则会回退）则进行访问，
    /// 访问方式需要通过数据源头的中继服务来桥接。
    /// 4. 当桥接链接到之后，我发记录我们当前建立的Peer数量，如果数量为10，此其他新的链接请求，进行拒绝。
    /// 5. 确保性能、网络不断、链接进行中 等环境后，先同步区块信息 和 接口
    /// 
    /// 经过上面部分，peer的启示状态同步完成，剩下的是启动后的事件和数据同步
    /// 1. 进行了新的交易，同步给中继和目前建立链接的Peer，进行全网验证和保存
    /// 2. 当交易到块的条件达成后，进行块化，然后同步块，全Peer收到块之后，进行验证和交易，如果里面的交易和自己当下交易池中的类同，则删除自己交易池中的。
    /// 3. 如果有两个块里面的交易内容有重合，则保存最先时间建立（在线时间最高的用户）的块，并且给予建立块的Peer费用。
    /// 4. 以及多人小游戏的同步（看时间情况来定，越小越好，比如就传输用户的屏幕点击数据，做游戏）
    /// 5. 
    /// </summary>
    public class NodePeerNet
    {
        public Peer Peer;
        public NodePeer NodePeer;
    }

   
    [RequireComponent(typeof(ANetworkHost))]
    public class P2PManager: MonoBehaviour
    {
        #region p2p service manager of instance

        static P2PManager instance;
        public static P2PManager Instance => instance;

        public static P2PManager Init(GameObject obj)
        {
            if (instance == null && obj != null)
            {
                instance = FindObjectOfType(typeof(P2PManager)) as P2PManager;

                if (instance == null)
                {
                    instance = obj.AddComponent<P2PManager>();
                }
            }

            return instance;
        }

        #endregion

        /// <summary>
        /// 公网获取的地址
        /// </summary>
        private static string[] PUBLIC_IP_SERVICES = new string[] {
            "http://120.26.66.97:43023/Pip",
            "https://ipinfo.io/ip",
            "https://icanhazip.com",
            "https://bot.whatismyipaddress.com/",
            "https://api.ipify.org/",
            "https://checkip.amazonaws.com/",
            "https://wtfismyip.com/text",
        };

        public static string[] BootSrtapPeers { get => Instance.BOOTSRTAP_ADDRESS; }
        private string[] BOOTSRTAP_ADDRESS = new string[] { };

        // Host服务
        public static P2PService _P2PService { get; private set; }
        // public static P2PClient _P2PClient { get; private set; }
        // public static P2PRelay _P2PRelay{ get; private set; }

        //业务流程过程中间服务类
        public static NodePeerSync _NodePeerSync { get; private set; }

        // Netcode
        private ANetworkHost _Host;
        public static ANetworkHost GetNetHost => instance._Host;
        public static Host GetHost => instance._Host.GetHost();

        public List<INetServer> NetServerlmpl = new List<INetServer>();

        [Obsolete("需要提出SuperNet创建房间那套代码，后期使用 SolveManager.BlockChain.NodePeer.m_NodePeer")]
        public RelayPeerNetInfoEntry _SelfNetInfoEntry;

        public static Dictionary<IPEndPoint, NodePeerNet> PeersConnected { get => GetNetHost.PeersConnected; }
        public static int PeersConnectedCount { get => GetNetHost.PeersConnected.Count; }

        public static IPAddress PublicIP { get; private set; }


        private void Awake()
        {
            _Host = GetComponent<ANetworkHost>();

            // Initialize network manager
            NetworkManager.Initialize();

            // Reduce the frame rate to use less CPU
            Application.targetFrameRate = 60;

            #region HostConfig and PeerConfig setting

            _Host.MaxConnections = 32;//最大并发网络连接数。
            _Host.PersistAcrossScenes = true;//确保加载新场景的时候Host不被销毁
            _Host.AutoStartup = false;//启动时自动监听
            _Host.AutoRegister = true;//自动将所有的Peer进行network注册
            _Host.AutoConnect = "";//Startup时需要链接的Remote地址，空则关闭
        

            HostConfig hostConfig = _Host.HostConfiguration;

            // 如果是节点中继服务
            if (AUtils.IRS) {

                hostConfig.Port = 44824;
            }
            else {
                hostConfig.Port = 0;
            }
           

            
            hostConfig.DualMode =  false; // 同时接受IP4和IP6链接
            hostConfig.Broadcast = true; //允许广播消息
            hostConfig.TTL = 128;//允许数据包丢失的最大次数
            hostConfig.SendBufferSize = 32768; //Socket发送的数据最大限制
            hostConfig.ReceiveBufferSize = 32768; //Socket接受的数据最大限制
            hostConfig.ReceiveCount = 8;//读取操作的最大并发数
            hostConfig.CRC32 = true; //CRC32错误检查，确保包不会损坏
            hostConfig.Compression = false;//数据包压缩处理
            hostConfig.Encryption = true;//数据包进行加密，但也不阻止加密的包
            hostConfig.SimulatorEnabled = false;//延时和丢包的模拟
            hostConfig.Broadcast = true;
            //需要私钥进行对Host的验证
            //  hostConfig.PrivateKey ="";
          //  hostConfig.PrivateKey = "eLuzMOg2n3nmOeJQaxYf73Ix+vOfd7VE+GOXGXYBnXJJHHM9BVadqbLvlpAAqiNkFr0Lbnio41lZ6p3QXTT7aRUOcLTH5KyN19pIBgkShbrrK5OaEkktL099sXbwSBhsNWIFAd+d2qIMl0JrrQXhinUv5966Dj8vF/I5PXKqv+UNcR65zUvZmFvfzAAs1Cl4HAec9fnWqyGUgsVLuyXao9M5H1iVTNv0Oje529W0Zm4LKtKuxv+rIRVf29JIYCXhAGQWZO/cVZ5N+iBe4g73ymJRpyTtscSAgtWVPpp32rXhwHqkXLVXD8U84YMBJpf8UT9We5C53C06UEWIDFP+pYLntuAnemVAb6l041b2PXJcxgmHmt51J/afddknFXYaiPCxv2rg8+vAx0c+gbKuSDAzp0RLgi+C9aXho0zvcQgga28y8EFfvwGOfvAY5txO1ZNrCFyhIjzaW2Kdsge+OMmneH5FZ4WLe204GSlQx4HUs0lqh0qqBY41PxKJx1rtcnsv1Fhwh+B7ZcAqLjRF1qvuD47aVZ6iEQGut/Uze5tO4nXSe2TgmnsqYbi/ZWkOYC7IEyaseoBWJ3D6Qoq72XHKXl18kR6RWQMEx9SBeTqHRzc/fJoF2d487y3KXIKDl2ldn4Jz7dXngtkfgJ1bcnzHdGRFEShRvw9Wd6rjzgExOc2mgpHhiuyPinU0j6DpKMCsX8v30qrWEprqHwThawEedY9hgmU3sXKcnZkp79rw2F2Iu9F+YdAbP5tBEeLc1VimtsKAjDNE8typhvGjZoNUvKCkNqUPtojhs3qRCf1rPVptYhXW3w2ljb/PECU2JduE+283QX4YU+ab3SgFC4KAnkUVB3Nzl3SzjqWIKNx6iTygM+5WUHmR2xw9KxGpS0GpcP6RLrh9dRGM2wsKnIuPGxnE05YH2a25TnvG7zvRmpA4g2cn7p91X8iKfhjXaCCXNvGeuav8/3wVEWmqQ5bTDBZ/tJU/hUoo7RYNDr/ydwexAEuiaBM5rUjUVHkVHqWb1CjlJT6GuZ1poOiYgo5HhbxxcSCA7i1gzsLPOGKIjNhpsX1rpXEa0beUSqfU2qBCoT+gHanvp14lVjRWXTQC8s07JwolrGzoVyEAf5nOVuJ9DsUdqvaj5jIxzmU16goGFmBBJhZ7vZ+HGheMLkPG6h8xlk+KoWylw8PBO63nZ9LeB4Cz1B4QaQvHcbrexi3ryw3rz0RDKz44IZOuTHlxFTMPif9JZ/huodWqg73dv+OpPFqdqlRUvAxPEZD9kMwOoKoPqSn6bljYYJvs+zF4ThTZ4VxyDIOB8qLcKgq+VY4pHMBee42LtvXb9/7px+IL46qUBugYmf4l351DB5BfYX7HlyyJeWj2ns9IdBLYnUxHxdrDbVGEGOAE8Ww19ofnq28/geNyd6FGyKkvOwBNH9oVHw9akJcMxt/d8VxIsFDn21fHa8f7au4ndGcS6zNhxFqbwYRGu6b9IXgugMIc827Rb4dMt0taVjIc0/CryvYgcNCeH2fjGeMAc1qr";


            PeerConfig peerConfig = _Host.PeerConfiguration;
            peerConfig.ConnectAttempts = 24;//尝试链接的最大次数，如果超过这个数则放弃链接，不能太小，以便有足够的时间进行UDP内网穿透。
            peerConfig.ConnectDelay = 550;//能接受的链接延时限度
            peerConfig.MTU = 1350; //一个UDP数据包中要发送的最大字节数,以太网上的MTU为1500字节
            peerConfig.PingDelay = 2000;//Ping消息的延时限度
            peerConfig.SendDelay = 15;//发送消息之前，整合消息数据的时间限度
            peerConfig.ResendCount = 12;//重新发送的次数，确保小于链接超时的时间
            peerConfig.DisconnectDelay = 300; //收到断开连接请求时延迟关闭连接的时间（毫秒）。
            //用于验证身份的Remote公钥。
          //  peerConfig.RemotePublicKey = "goCeRRUHc3OXdLOOpYgo3HqJPKAz7lZQeZHbHD0rEalLQalw/pEuuH11EYzbCwqci48bGcTTlgfZrblOe8bvO9GakDiDZyfun3VfyIp+GNdoIJc28Z65q/z/fBURaapDltMMFn+0lT+FSijtFg0Ov/J3B7EAS6JoEzmtSNRUeRUepZvUKOUlPoa5nWmg6JiCjkeFvHFxIIDuLWDOws84YoiM2GmxfWulcRrRt5RKp9TaoEKhP6Adqe+nXiVWNFZdNALyzTsnCiWsbOhXIQB/mc5W4n0OxR2q9qPmMjHOZTXqCgYWYEEmFnu9n4caF4wuQ8bqHzGWT4qhbKXDw8E7rQ==";

            _Host.HostConfiguration = hostConfig;
            _Host.PeerConfiguration = peerConfig;

            #endregion


          //if(AUtils.IRS)
          //  {
          //      Destroyer destroyer = new Destroyer();
          //  }

        }

        public bool InitNetworkEnv(AConfig.NetConfig netConfig)
        {
            int okCount = 0;
            if (netConfig.BootSrtapPeers != null && netConfig.BootSrtapPeers.Count > 0)
            {
                BOOTSRTAP_ADDRESS = netConfig.BootSrtapPeers.ToArray();

                okCount += 1;
            }
            if (netConfig.PublicIPServices != null && netConfig.PublicIPServices.Count > 0)
            {
                PUBLIC_IP_SERVICES = netConfig.PublicIPServices.ToArray();
                okCount += 1;
            }

            return (okCount >= 2) ? true : false;

        }

        public void StartServer()
        {
            // Create host
            try
            {
                Shutdown();
                _Host.Startup(true);
                
            }
            catch (Exception exception)
            {
                ALog.Error(exception);
            }

            if (_P2PService != null) _P2PService.Shutdown();
            _P2PService = new P2PService();
            //if (_P2PClient != null) _P2PClient.Shutdown();
            //_P2PClient = new P2PClient();
            //if (_P2PRelay != null) _P2PRelay.Shutdown();
            //_P2PRelay = new P2PRelay();


            _P2PService.Init(_Host);
           // _P2PClient.Init(_Host);
           // _P2PRelay.Init(_Host);

            if (_NodePeerSync != null) _NodePeerSync.Shutdown();
            _NodePeerSync = new NodePeerSync();
            _NodePeerSync.Start();

            if (NetServerlmpl != null && NetServerlmpl.Count > 0)
            {
                for (int i = 0; i < NetServerlmpl.Count; i++)
                {
                    NetServerlmpl[i].Start();
                }
            }
        }

        public void FitParseModule()
        {
            if (NetServerlmpl != null && NetServerlmpl.Count > 0)
            {
                for (int i = 0; i < NetServerlmpl.Count; i++)
                {
                    NetServerlmpl[i].Shutdown();
                }
                NetServerlmpl = null;
            }

            Type[] typess = typeof(INetServer).Assembly.GetTypes();
            foreach (Type type in typess
              .Where(myType => myType.IsClass && !myType.IsAbstract && typeof(INetServer).IsAssignableFrom(myType)))
            {
                INetServer netlmpl = type.Instantiate() as INetServer;
                NetServerlmpl.Add(netlmpl);
            }

        }

        
        public bool SetNodePeerState(IPEndPoint address, NodePeer nodePeer,bool isCacheToData = true)
        {
            if (_Host.PeersConnected[address] != null)
            {
                _Host.PeersConnected[address].NodePeer = nodePeer;
                if(isCacheToData)SolveManager.BlockChain.NodePeer.AddToCache(nodePeer);
                _Host.PeersConnected[address].NodePeer.is_online = true;
                return true;
            }
            else
            {
                ALog.Error("没有在链接中的list中找到这个Peer ：" + address);
            }

            return false;
        }
        public bool SetNodePeerState(Peer peer, NodePeer nodePeer, bool isCacheToData = true)
        {
            if (_Host.PeersConnected[peer.Remote] != null)
            {
                _Host.PeersConnected[peer.Remote].NodePeer = nodePeer;
                if (isCacheToData) SolveManager.BlockChain.NodePeer.AddToCache(nodePeer);
                _Host.PeersConnected[peer.Remote].NodePeer.is_online = true;
                return true;
            }
            else {
                ALog.Error("没有在链接中的list中找到这个Peer ：" + peer.Remote);
            }

            return false;
        }


        public NodePeerNet IsContainsInConnected(IPEndPoint address)
        {
            if (PeersConnected.ContainsKey(address))
            {
                return PeersConnected[address];
            }
            else return null;
        }


        public void Shutdown()
        {
            _P2PService?.Shutdown();
            //_P2PClient?.Shutdown();
            //_P2PRelay?.Shutdown();
            _NodePeerSync?.Shutdown();
         
           // _Host?.Dispose();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
           // _Host?.Dispose();
            Debug.Log(string.Format("[0 1-1 Host 退出关闭]"));
            Console.WriteLine("[0 1-1 Host 退出关闭]");
            GC.Collect();
        }

        //private void OnDestroy()
        //{
        //    Shutdown();
        //    _Host?.Dispose();
        //    Debug.Log(string.Format("[0 1-2 Host 关闭]"));
        //    Console.WriteLine("[0 1-2 Host 退出关闭]");
        //    GC.Collect();
        //}




        static int timerTemp = 0;
        static List<NodePeerNet> stabilzeNodeTemp;
        /// <summary>
        /// 获取所有可能或可以访问的节点，包括缓存的以及在线链接,毕竟数据库读取，限制操作太过频繁
        /// </summary>
        public static List<NodePeerNet> StabilizeNodePeers
        {
            get
            {
                var dateTime = AUtils.NowUtcTime;
                var timeGap = (dateTime.Second - timerTemp);
                var waitTime = (timeGap >= 0) ? timeGap : timeGap + 60;

                if (waitTime >= 1) stabilzeNodeTemp = null;

                if (stabilzeNodeTemp != null) return stabilzeNodeTemp;
                timerTemp = dateTime.Second;

                var _NodedPeers = SolveManager.BlockChain.NodePeer.StabilizePeersInCache()
              .Select(x => new NodePeerNet() { NodePeer = x, Peer = null }).ToList();

                //获取正在链接的节点
                var connectedPeer = P2PManager.PeersConnected.Select(x => x.Value).ToList();

                _NodedPeers.AddRange(connectedPeer);

                _NodedPeers = NetNodePeer.NodePeerDeduplication(_NodedPeers);

                stabilzeNodeTemp = _NodedPeers;
                return stabilzeNodeTemp;
            }
        }





#if UNITY_EDITOR

        [Button]
        public void SendBroadcast()
        {
            _P2PService.SendBroadcast();
        }
#endif


        public static async UniTask<IPAddress> GetPublicIP(string[] public_ip_services)
        {
            if (public_ip_services == null || public_ip_services.Length <= 0) public_ip_services = PUBLIC_IP_SERVICES;

            int count = 0;
            foreach (string url in public_ip_services)
            {
               // ALog.Log("Obtaining public IP from " + url);
                count += 1;
                try
                {
                    string ip = await AUtils.AsyncLoadTextFile(url,4);

                    ALog.Log("第{0}次 Request Public IP form {1} , Result => {2}" , count , url , ip);

                    if (IPAddress.TryParse(ip, out IPAddress parsed))
                    {
                        if (parsed != null) PublicIP = parsed;
                        else ALog.Error("[Arena] Failed to get public IP. No internet connection?");

                        return parsed;
                    }

                }
                catch (Exception ex)
                {
                    ALog.Error("Error " + "Obtaining public IP from " + url + " : " + ex);
                }

               

            }
            return null;
        }


        /// <summary>
        /// 客户端向和已知的Peep同步所有块信息
        /// </summary>
        /// <param name="blockService"></param>
        /// <param name="lastBlockHeight"></param>
        /// <param name="peerHeight"></param>
        private void DownloadBlocks(NetBlock.BlockClient blockService, long lastBlockHeight, long peerHeight)
        {
            // BlockList response = blockService.GetRemains(new StartingParam { height = lastBlockHeight });
            //bl blocks = null;//response.blocks;
            ////把块进行反转
            //blocks.Reverse();

            //var lastHeight = 0L;
            //foreach (var block in blocks)
            //{
            //    try
            //    {
            //        Console.WriteLine("==== Download block: {0}", block.height);
            //        //var status = ServicePool.DbService.BlockDb.Add(block);
            //        lastHeight = block.height;
            //        Console.WriteLine("==== Done");
            //    }
            //    catch
            //    {
            //        Console.WriteLine("==== Fail");
            //    }
            //}

            //if (lastHeight < peerHeight)
            //{
            //    DownloadBlocks(blockService, lastHeight, peerHeight);
            //}
        }

        ///// <summary>
        ///// 判断新的Peep，检查Peep是否在数据库中
        ///// </summary>
        ///// <param name="address"></param>
        ///// <returns></returns>
        //private bool IsNewPeer(string address)
        //{
        //    var node_peer = StabilizePeersInCache(false);
        //    foreach (var peer in node_peer.knownPeers)
        //    {
        //        if (address == peer.address_public)
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}




#region Message Broadcast


        /// <summary>
        /// 客户端向所有已知的Peep广播添加块
        /// </summary>
        /// <param name="block"></param>
        public void BroadcastBlock(Block block)
        {
        }

        /// <summary>
        /// 客户端向所有已知的Peep广播Stake
        /// </summary>
        /// <param name="stake"></param>
        public void BroadcastStake(Stake stake)
        {
            //var node_peer = KnownPeersInCache();

            //Parallel.ForEach(node_peer.knownPeers, peer =>
            //{
            //    if (!node_peer.m_NodePeer.Equals(peer.address_public))
            //    {
            //        Console.WriteLine("-- BroadcastStake to {0}", peer.address_public);

            //        IPEndPoint address = null;
            //        //var stakeService = new NetStake.StakeClient(address);
            //        //try
            //        //{
            //        //    var response = stakeService.Add(stake);
            //        //    Console.WriteLine("--- Done");
            //        //}
            //        //catch
            //        //{
            //        //    Console.WriteLine("--- Fail");
            //        //}
            //    }
            //});
        }

        /// <summary>
        ///客户端向所有已知的Peep广播一条交易信息
        /// </summary>
        /// <param name="tx"></param>
        public void BroadcastTransaction(Transaction tx)
        {
            //var node_peer = KnownPeersInCache();

            //Parallel.ForEach(node_peer.knownPeers, peer =>
            //{
            //    if (!node_peer.m_NodePeer.Equals(peer.address_public))
            //    {
            //        Console.WriteLine("-- BroadcastTransaction to {0}", peer.address_public);

            //        IPEndPoint address = null;
            //        //var txnService = new NetTransaction.TransactionClient(address);
            //        //try
            //        //{
            //        //    var response = txnService.Receive(new TransactionPost
            //        //    {
            //        //        // SendingFrom = nodeAddress,
            //        //        Transaction = tx
            //        //    });
            //        //    if (response.status == AConstants.TXN_STATUS_SUCCESS)
            //        //    {
            //        //        Console.WriteLine(".. Done");
            //        //    }
            //        //    else
            //        //    {
            //        //        Console.WriteLine(".. Fail");
            //        //    }
            //        //}
            //        //catch
            //        //{
            //        //    Console.WriteLine(".. Fail");
            //        //}
            //    }
            //});
        }

#endregion


    }
}

