using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ArouseBlockchain.Common;
using ArouseBlockchain.Solve;
using Cysharp.Threading.Tasks;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using static ArouseBlockchain.Common.AUtils;
using static ArouseBlockchain.P2PNet.NetNodePeer;

namespace ArouseBlockchain.P2PNet
{
    public class NodePeerSync : IDefaultLife
    {
        public bool IsSearching => IsSearching;
        /// <summary>
        /// 当搜索停止，一般来说手动停止和错误导致的停止
        /// </summary>
        public Action OnSearchSopt;
        /// <summary>
        /// 当成功搜索到一个Peer
        /// </summary>
        public Action<NodePeerNet> OnPeerFind;
        /// <summary>
        /// 当一个Peer丢失了
        /// </summary>
        public Action<NodePeerNet> OnPeerLoss;

        /// <summary>
        /// 需要去轮训访问的自己中继（纯中继和虚中继）节点列表 和 它方回复的中继节点
        /// </summary>
        List<NodePeerNet> _TempSyncStateRelayPeer = null;
        /// <summary>
        /// 已经访问过的中继Node,包含通过和没有通过的
        /// </summary>
        List<NodePeerNet> _AlreadySyncPeer = new List<NodePeerNet>();
        /// <summary>
        /// 负责和Host通信的PeerClient，为了性能，限制为10个
        /// </summary>
        NodePeerClient[] _FreeRelayPeerClients;


        /// <summary>
        /// 当前正在链接的玩家Peer，不包括同步块信息的中继（纯）
        /// </summary>
        List<NodePeerNet> WaitPeerConnecting = new List<NodePeerNet>();

        /// <summary>
        /// 链接通过的GamePeer
        /// </summary>
        List<NodePeerNet> PeerConnected = new List<NodePeerNet>();
        // bool isSycnBlocking = false;



        //1. 添加TokenSource 的时候需要考虑，服务的Task是否需要被取消
        //2. 以及使用的三段式：当开始时候需要关闭上一个并New新的，当取消的时候需要Cancel，当杀死彻底不用时需要Cancel并Dispose
        //3. 服务的Task事务不能跨度太大，时间周期太长。

        /// <summary>
        /// 访问和同步状态的TokenSource，由于循环的超时和访问的超时不同，需要分别控制
        /// </summary>
        CancellationTokenSource _VisitDCTS;
        TimeoutController _VisitTimeoutController;

        CancellationTokenSource timeoutDCTS;
        TimeoutController timeoutController;

        int maxLoopTime = 0; // 最大循环周期的时间间隔
        int minTimeoutOfPrime = 8;//不能小于8; // 最初的时候，每次访问的允许的超时时间
        DateTime startTime = DateTime.Now;



        bool IsSyncing = false;
        int maxConnetCount = 5;




        public NodePeerSync()
        {
            _FreeRelayPeerClients = new NodePeerClient[maxConnetCount];
            for (int i = 0; i < _FreeRelayPeerClients.Length; i++)
            {
                _FreeRelayPeerClients[i] = new NodePeerClient(null);
            }
        }


        public void Start()
        {
            //获取本地缓存的稳定节点，作为第一批需要同步的对象
            if (AUtils.IRS) return;//非客户端不监听请求的Peer链接

            P2PManager._P2PService.OnConnect += OnPeerConnect;
            P2PManager._P2PService.OnDisconnect += OnDisconnect;
            P2PManager._P2PService.OnException += OnException;
            IsSyncing = false;

        }

        public void SearchValidNode()
        {
            if (!P2PManager.GetNetHost.Listening)
            {
                ALog.Log("本地服务没有正常启动！！");
                return ;
            }

            if (AUtils.IRS) return;//非客户端不去同步

            _TempSyncStateRelayPeer = SolveManager.BlockChain.NodePeer.StabilizePeersInCache()
                .Select(x=> new NodePeerNet() { NodePeer =x,Peer =null}).ToList();

            ALog.Log("00 获取稳定的节点 {0} ", _TempSyncStateRelayPeer.Count);

            rebornCount = 0;
            _AlreadySyncPeer?.Clear();

            AsyncSearcPeer();
        }

        public void StopSearchNode()
        {
            if (AUtils.IRS) return;//非客户端不监听请求的Peer链接

            IsSyncing = false;
            CloseCancellationTokenSource();
        }

        public void Shutdown()
        {
            if (AUtils.IRS) return;//非客户端不监听请求的Peer链接

            IsSyncing = false;
            CloseCancellationTokenSource();
            P2PManager._P2PService.OnConnect -= OnPeerConnect;
            P2PManager._P2PService.OnDisconnect -= OnDisconnect;
            P2PManager._P2PService.OnException -= OnException;
        }

        void CloseCancellationTokenSource()
        {
            try
            {
                _VisitDCTS?.Cancel();
                _VisitDCTS?.Dispose();
                _VisitDCTS = null;

                timeoutDCTS?.Cancel();
                timeoutDCTS?.Dispose();
                timeoutDCTS = null;

            }catch(Exception ex) {

                ALog.Error("释放同步Task错误：" + ex);
            }
        }


        void OnPeerConnect(Peer peer)
        {
            var waitPeer = WaitPeerConnecting.Find(x=>x.Peer == peer);
            if (waitPeer != null)
            {
                P2PManager.Instance.SetNodePeerState(peer, waitPeer.NodePeer);//关联正在链接的Peer对应的NodePeer
                PeerConnected.Add(waitPeer);
                ALog.Log("成功链接一个游戏 PEER，可以进行数据块同步的工作和其他游戏工作了！！！");
            }
            if (PeerConnected.Count > maxConnetCount)
            {

            }
        }

        void OnDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception)
        {
            OnPeerDisconnectOrError(peer, exception);

        }
        public void OnException(Peer peer, Exception exception)
        {
            OnPeerDisconnectOrError(peer, exception);
        }

        public NodePeerClient GetFreeNodePeerClient
        {
            get
            {
                for (int i = 0; i < _FreeRelayPeerClients.Length; i++)
                {
                    if (_FreeRelayPeerClients[i].IsFreePeer)
                    {
                        return _FreeRelayPeerClients[i];
                    }
                }
                return null;
            }
        }

        public void OnPeerDisconnectOrError(Peer peer, Exception exception)
        {
            var waitPeer = WaitPeerConnecting.Find(x => x.Peer == peer);
            if (waitPeer != null)
            {
                WaitPeerConnecting.Remove(waitPeer);
            }
            var connPeer = PeerConnected.Find(x => x.Peer == peer);
            if (connPeer != null)
            {
                PeerConnected.Remove(connPeer);
                OnPeerLoss?.Invoke(connPeer);
            }

            if (_TempSyncStateRelayPeer == null || _TempSyncStateRelayPeer.Count <= 0) return;


            foreach (var item in _TempSyncStateRelayPeer)
            {
                if(item.Peer != null && item.Peer == peer)
                {
                    if (!_AlreadySyncPeer.Exists(x => x.Peer == item.Peer))
                    {

                        _AlreadySyncPeer.Add(item); // 添加到已链接的列表中
                        ALog.Log("03 移除这个Peer{0}，IP协议 {1} 断开或者出错：{2}", peer.Remote, peer.Remote.AddressFamily, exception);
                        
                    }
                    // item.Peer?.Disconnect();
                    ALog.Log("04 这个Peer{0}， 断开或者出错：{2}", peer.Remote, peer.Remote.AddressFamily, exception);
                    break;
                }

                //IPEndPoint iPEnd1 = IPResolver.TryParse(item.NodePeer.address_public);
                //// ALog.Log("01 这个Peer{0} 和 存储的IP {1} 断开或者出错：{2} ; 是否相同 {3}", peer.Remote, iPEnd1, exception,  Net.IsIPSame(iPEnd1, peer.Remote));
                ////  ALog.Log("02 地址类型  {0}  {1} ", peer.Remote.AddressFamily, iPEnd1.AddressFamily );
                //if (Net.IsIPSame(iPEnd1, peer.Remote))
                //{ //|| Net.IsIPSame(item.address_remote, peer.Remote)) {

                //    if (!_AlreadySyncPeer.Exists(x => x.NodePeer.address_public == item.NodePeer.address_public))
                //    {

                //        _AlreadySyncPeer.Add(item); // 添加到已链接的列表中
                //        ALog.Log("03 移除这个Peer{0}，IP协议 {1} 断开或者出错：{2}", peer.Remote, peer.Remote.AddressFamily, exception);
                //    }
                //    // peer?.Disconnect();
                //    break;
                //}
            }


            if (PeerConnected.Count >= (maxConnetCount - 1))
            {
                if (!IsSyncing) AsyncSearcPeer();
            }

        }

        int rebornCount = 0; //轮训周期的次数
       // int rebornCountTem = 0; //轮训周期次数的缓存

        int syncCount = 0; //同步的总次数
        int successCount = 0; //同步的成功次数



        async UniTaskVoid AsyncSearcPeer()
        {

            if (PeerConnected.Count > maxConnetCount)
            {

                ALog.Log("已经链接了足够的Peer了，不需要搜索了");
                return;
            }
            if (IsSyncing)
            {

                ALog.Log("搜索中... 千万不要重复多次搜索");
                return;
            }

            IsSyncing = true;

            IsSyncing = await AsyncSearcPeerWhilePart();
        }




        /// <summary>
        /// 最大的等待时间
        /// 1. 刚开始，快速查找,如果访问失败，则直接下一个
        /// 2. 找到一定数量后，慢速查询剩余的，然后现有的先去同步块
        /// 3. 一直找不到8次后，就低速1分钟慢慢查询
        /// </summary>
        async UniTask<bool> AsyncSearcPeerWhilePart()
        {
            if (timeoutDCTS == null || timeoutController == null|| timeoutDCTS.IsCancellationRequested)
            {
                timeoutDCTS = new CancellationTokenSource();
                timeoutController = new TimeoutController(timeoutDCTS);
            }

            syncCount = 0;
            successCount = 0;

            while (IsSyncing)
            {
                await UniTask.Yield();
                try
                {
                    ALog.Log("01 AAA 开始新的访问，上一个" + (_VisitDCTS != null && _VisitDCTS.IsCancellationRequested ? "因为超时或错误Task取消" : "正常访问"));
                    if (_VisitDCTS != null && _VisitDCTS.IsCancellationRequested)
                    {//上一个要Task要清楚完，才能进去下一个
                        _VisitDCTS?.Cancel();
                        _VisitDCTS?.Dispose();
                        _VisitDCTS = null;
                        _VisitDCTS = new CancellationTokenSource();
                    }
                    if (_VisitDCTS == null) _VisitDCTS = new CancellationTokenSource();


                    _TempSyncStateRelayPeer = NetNodePeer.NodePeerDeduplication(_TempSyncStateRelayPeer);
                    _AlreadySyncPeer = NetNodePeer.NodePeerDeduplication(_AlreadySyncPeer);
                    List<NodePeerNet> _TheRestPeer = _TempSyncStateRelayPeer.Except(_AlreadySyncPeer).ToList();

                    //当全部遍历完之后
                    if (_TheRestPeer.Count <= 0)
                    {
                        ALog.Log("重制 大循环{0}次", rebornCount);
                        if (_TempSyncStateRelayPeer!=null && _TempSyncStateRelayPeer.Count >0) {

                            NodePeer[] tempNodes = new NodePeer[_TempSyncStateRelayPeer.Count];
                            _TempSyncStateRelayPeer
                                .Select(x=>x.NodePeer)
                                .ToArray()
                                .CopyTo(tempNodes, 0);

                            NetworkManager.Run(() =>
                            {
                                SolveManager.BlockChain.NodePeer.ReplaceAll(tempNodes.ToList());//缓存一下当前Node数据
                            });

                        }

                        rebornCount += 1;
                        _AlreadySyncPeer.Clear();
                        _TheRestPeer = _TempSyncStateRelayPeer;
                    }

                    foreach (var item in _TempSyncStateRelayPeer)
                    {
                        ALog.Log("所有中继 ：" + item.NodePeer.ToString());
                    }

                    foreach (var item in _TheRestPeer)
                    {
                        ALog.Log("剩余 ：" + item.NodePeer.ToString());
                    }

                    ALog.Log("剩余尝试链接{0} ； 访问过{1} ； 准备的{2}； 通过等待链接的{3} ； 已链接的{4}", _TempSyncStateRelayPeer.Count, _AlreadySyncPeer.Count, _TheRestPeer.Count, WaitPeerConnecting.Count, PeerConnected.Count);


                    if (rebornCount < 5)
                    {
                        bool max2 = (PeerConnected.Count > (maxConnetCount / 2));
                        maxLoopTime = max2 ? 14 : minTimeoutOfPrime;
                        ALog.Log("链接的Peer有 {0}个，{1} ,等待 {2}秒 进行下个Node, 距离上次有 {3} 秒", PeerConnected.Count, max2 ? "大于2个" : "不足2个", maxLoopTime, (DateTime.Now - startTime).Seconds);
                    }
                    else
                    {
                        maxLoopTime = 60;
                        ALog.Log("多次轮训了所有Node，将进行慢速搜素 (1分钟)，距离上次有{0}秒", (DateTime.Now - startTime).Seconds);
                    }

                    _VisitDCTS.Token.ThrowIfCancellationRequested();
                    _VisitDCTS.CancelAfterSlim(TimeSpan.FromSeconds(MathF.Min(10, maxLoopTime) - 0.5f)); // 8sec timeout，8秒没有给出反馈的终止访问

                    //如果查询足够了
                    if (PeerConnected.Count > maxConnetCount)
                    {
                        //如果找到足够多的game peer，则取消和中继的链接，保持中继的性能稳定
                        foreach (var item in _FreeRelayPeerClients)
                        {
                            item.SetNetPeer(null);
                        }

                        return false;
                    }

                    syncCount += 1;

                    NodePeerClient relayNodeClient = GetFreeNodePeerClient;
                    if (relayNodeClient != null)
                    {
                        var reslut1 = await FindStateWithOnlyRelay(_TheRestPeer[0], relayNodeClient, _VisitDCTS.Token).SuppressCancellationThrow();

                        if (reslut1.IsCanceled && !IsSyncing) return false;
                        if (reslut1.Result) successCount += 1;
                        else relayNodeClient.SetNetPeer(null);
                    }
                    else ALog.Log("暂时并未有空闲的Peer同步Client,它们目前都可能在通讯中，如果可同步的Peer一直不存在，请稍后再次尝试");


                    ALog.Log("-- |结束| --- 第{0}轮，共{1}次的访问结束,中继总数：{2}，剩余访问{3}，链接成功率：{4}%", rebornCount, syncCount, _TempSyncStateRelayPeer.Count, _TheRestPeer.Count -1, (syncCount > 0) ? (successCount / (float)syncCount) : 0);



                    if(timeoutDCTS== null || timeoutDCTS.IsCancellationRequested) return false;

                    timeoutController.Reset();
                    //这个时间会导致之前本来执行中的Task时间增加，所以千万不能重复调用此函数
                    CancellationToken timeout_token = timeoutController.Timeout(TimeSpan.FromSeconds(maxLoopTime + 2));
                   

                    var isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(maxLoopTime), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, timeout_token).SuppressCancellationThrow();

                    if (isCanceled && !IsSyncing) return false;

                    startTime = DateTime.Now;

                }
                catch (Exception ex)
                {
                    ALog.Error("错误：" + ex);

                    if (timeoutController!=null && timeoutController.IsTimeout())
                    {
                        ALog.Error("理论上这个 周期循环中间时常，时间留着很够 不可能超时timeout。这个很严重，因为超时会取消，取消后这个等待Task就不会等待了");
                    }

                    return false;
                }

            }

            return false;
        }

        /// <summary>
        /// 向单个中继获取状态，找到在线的Peer
        /// </summary>
        /// <returns>返回允许同步的数量</returns>
        async UniTask<bool> FindStateWithOnlyRelay(NodePeerNet try_sync_node, NodePeerClient relayNodeClient, CancellationToken _token)
        {
            IPEndPoint sync_remote = IPResolver.TryParse(try_sync_node.NodePeer.address_public);

            if (string.IsNullOrEmpty(try_sync_node.NodePeer.address_public) || sync_remote == null)
            {
                ALog.Error("这个Peer没有网络地址,缓存了么？");
                _AlreadySyncPeer.Add(try_sync_node); //记录在访问过的Node列表中
                if (_TempSyncStateRelayPeer != null) _TempSyncStateRelayPeer.Remove(try_sync_node);

                return false;
            }

            var my_bing_adrs = P2PManager.GetNetHost.GetBindAddress();
            var my_local_adrs = P2PManager.GetNetHost.GetLocalAddress();

            if (Net.IsInternal(sync_remote.Address)) ALog.Log("是局域网:" + try_sync_node.ToString() + " ; " + my_bing_adrs.AddressFamily);

            if (my_bing_adrs.AddressFamily == AddressFamily.InterNetworkV6)
            {
                my_bing_adrs = new IPEndPoint(my_bing_adrs.Address.MapToIPv4(), my_bing_adrs.Port);
                my_local_adrs = new IPEndPoint(my_local_adrs.Address.MapToIPv4(), my_local_adrs.Port);
            }

            if (try_sync_node.NodePeer.address_public == my_bing_adrs.ToString() || try_sync_node.NodePeer.address_public == my_local_adrs.ToString() || try_sync_node.NodePeer.address_public == "127.0.0.1:" + my_bing_adrs.Port)
            {
                ALog.Error("不能和自己的Host进行链接！");
                _AlreadySyncPeer.Add(try_sync_node); //记录在访问过的Node列表中
                if (_TempSyncStateRelayPeer != null) _TempSyncStateRelayPeer.Remove(try_sync_node);

                return false;
            }


            IPEndPoint address = Net.FindRightNetAddress(try_sync_node.NodePeer);
            if (!P2PManager.GetNetHost.Listening)
            {

                ALog.Log("本地服务没有正常启动！！");
                return false;
            }

            if (_token.IsCancellationRequested) return false;
            _token.ThrowIfCancellationRequested();

            ALog.Log("开始访问中继 最终地址 {0} ；Node信息{1} ; 当前Host链接数量 {2} ", address.ToString(), try_sync_node.ToString(), P2PManager.PeersConnectedCount);

            Peer sync_try_peer = null;

            int tempIndex = _TempSyncStateRelayPeer.FindIndex(x => x == try_sync_node);
            if (tempIndex >= 0)
            {
                Peer peer2 = _TempSyncStateRelayPeer[tempIndex].Peer;
                if(peer2!=null && (peer2.Connected || peer2.Connecting))
                {
                    sync_try_peer = peer2;
                }
                else
                {
                    if(peer2 != null) ALog.Log("这个Peer{0}已经链接过了，状态Connected = {1} ；Connecting={2} ； 再次链接", peer2.Remote, peer2.Connected, peer2.Connecting);
                    
                    sync_try_peer = P2PManager.GetNetHost.Connect(address, null, null, true);
                    _TempSyncStateRelayPeer[tempIndex].Peer = sync_try_peer;
                }
               
            }
            else
            {
                ALog.Error("Error!! : 严重错误，再链接的这个Peer,在_TempSyncStateRelayPeer中没有存在");
                return false;
            }

            var IsCanceled = await UniTask.WaitUntil(() => sync_try_peer.Connected && !sync_try_peer.Connecting, PlayerLoopTiming.FixedUpdate, _token).SuppressCancellationThrow();
            if (IsCanceled) //如果取消超时
            {
                _AlreadySyncPeer.Add(try_sync_node); //记录在访问过的Node列表中
                sync_try_peer?.Disconnect();
                sync_try_peer?.Dispose();
                ALog.Log("---------^^^^^链接超时或Task取消 {0} ,{1}^^^^^-------", _token.IsCancellationRequested, sync_remote);
                return false;
            }

            relayNodeClient.SetNetPeer(sync_try_peer);


            NodePeer m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;
            m_NodePeer.address_remote = sync_remote.ToString();
            ALog.Log("给{0}发送消息并等待响应----", address);


            var isCanclea_and_Response = await relayNodeClient.NodeSyncStateRequest(m_NodePeer, _token).SuppressCancellationThrow();
            if (isCanclea_and_Response.IsCanceled)
            {
                _AlreadySyncPeer.Add(try_sync_node); //记录在访问过的Node列表中
                sync_try_peer?.Disconnect();
                sync_try_peer?.Dispose();
                ALog.Log("---------^^^^^请求同步State超时或Task取消 {0} ,{1}^^^^^-------", _token.IsCancellationRequested, sync_remote);
                return false;
            }

            NodeSyncStateResponse response = isCanclea_and_Response.Result;
            var remoteCachePeers = response.NodePeers; //  NodeSyncStateResponse

            ALog.Log("接收到回复: Status:{0} ;Message: {1}; CachePeers isNull:{2} ; CachePeersNumber {3}", response._ReStatus._Status, response._ReStatus._Message, (remoteCachePeers == null), remoteCachePeers?.Count);

            //如果是无效(异常、满载)的中继，则剔除，进入下一次循环
            if (response._ReStatus._Status == MESSatus.NONE || response._ReStatus.IsError(this.GetType().ToString())
                || remoteCachePeers == null || remoteCachePeers?.Count <= 0)
            {
                _AlreadySyncPeer.Add(try_sync_node); //记录在访问过的Node列表中

                ALog.Warning("无效的中继 -> 是否是中继{0}，是否是稳定节点{1}", try_sync_node.NodePeer.is_bootstrap, try_sync_node.NodePeer.IsStabilizeExcludeBoot);
                AUtils.BC.PrintNodePeers(try_sync_node.NodePeer);
                ALog.Log("---------^^^^无效Node的信息^^^^^---------------");
                sync_try_peer?.Disconnect();
                sync_try_peer?.Dispose();
                return false;
            }
            ///走到下面就算返回成功了

            //中继通过后页作为备选同步对象
           
            _AlreadySyncPeer.Add(try_sync_node);
            int orginCount = _TempSyncStateRelayPeer.Count;
            foreach (var peer in remoteCachePeers)
            {
                if (peer.is_bootstrap) {
                    //int tempIndex2 = _TempSyncStateRelayPeer.FindIndex(x => Net.IsSameNodePeer(x.NodePeer, peer));

                    //直接添加，会自己根据中继的最后上线时间去重
                    _TempSyncStateRelayPeer.Add(new NodePeerNet() { NodePeer = peer });
 
                     ALog.Log("添加到到一个中继的Node Peer {0}", peer.ToString());
                    //}
                    //else
                    //{
                    //    ALog.Log("已经有这个中继的Node Peer {0}", peer.ToString());
                    //}
                }
            }
           
           // _TempSyncStateRelayPeer.AddRange(remoteCachePeers);//返回的列表有中继的数据，中继会同步它当前的块状态
            //_TempSyncStateRelayPeer = NetNodePeer.NodePeerDeduplication(_TempSyncStateRelayPeer);

            ALog.Log("添加到了中继给予的Node Peer节点共 ：{0}", (_TempSyncStateRelayPeer.Count - orginCount));

            for (int i = 0; i < remoteCachePeers.Count; i++)
            {
                var no = remoteCachePeers[i];
                ALog.Log("中继返回的节点 第{0}个 是否是中继{1} ：{2}", i, no.is_bootstrap, no.ToString());
            }

            //中继链接到之后，也会同步到UI中...
            OnPeerFind?.Invoke(try_sync_node);


            bool isSuccess = false;
            var temp = remoteCachePeers;
            //successCount += 1;
            for (int i = 0; i < temp.Count; i++)
            {
                //判断是否是自己的Node信息
                if (Net.IsSameNodePeer(temp[i], m_NodePeer))
                {
                    _TempSyncStateRelayPeer.RemoveAt(i);//移除自己
                    ALog.Log("中继给了我自己的Node信息 ：{0} , 我自己本地的 {1}", temp[i].ToString(), m_NodePeer.ToString()) ;
                    continue;
                }

                //判断是否是这个中继的Node信息
                if (Net.IsSameNodePeer(temp[i], try_sync_node.NodePeer))
                {
                    _TempSyncStateRelayPeer.RemoveAt(i);//移除自己
                    ALog.Log("中继发来了它自己的Node信息 ：{0} , 它最新的信息{1}", m_NodePeer.ToString() , temp[i].ToString());
                    continue;
                }

                //优先在线并且不是纯中继的Peer
                //如果链接的Peer超过3个，则选择块高我的进行同步，其他角色用户游戏
                if (temp[i].is_online && !temp[i].is_bootstrap)
                {
                    //当我收到中继的反馈，然后去链接这些 这些 真是用户Peer进来，链接之前需要看地址找到合适的地址，比如是否本地局域网

                    ALog.Log(" --- ｜开始链接客户端Peer｜ ----通知中继 {0}去链接Peer {1}", sync_remote, temp[i].address_public);

                    var p2pRelust = await ToTryP2PConnect(relayNodeClient, temp[i], try_sync_node, _token).SuppressCancellationThrow();
                    if (p2pRelust.IsCanceled) return false;
                    if (p2pRelust.Result) isSuccess = true; //有一次成功就算成功

                }
                else
                {
                    ALog.Log(" [CP]不是客户端！是中继 不访问 ：{0}", temp[i].ToString());
                }
            }

            return isSuccess;
        }



        public async UniTask<bool> ToTryP2PConnect(NodePeerClient relayNodeClient ,NodePeer needConnectPeer, NodePeerNet _relayNode,  CancellationToken _token)
        {
            IPEndPoint gamepeer_address = Net.FindRightNetAddress(needConnectPeer);

            if (_AlreadySyncPeer.Exists(x => Net.FindRightNetAddress(x.NodePeer) == gamepeer_address))
            {
                //已经访问过了
                ALog.Log(" [CP]已经访问过的客户端！{0}", needConnectPeer.ToString());
                return false;
            }


            NodePeerNet connetPeerNet = null;
            P2PManager.PeersConnected.TryGetValue(gamepeer_address, out connetPeerNet);
            Peer peer1 = connetPeerNet.Peer;
            if (connetPeerNet != null && peer1!=null &&(peer1.Connected || peer1.Connecting))
            {
                ALog.Log(" [CP]已经建立链接了，请勿重复链接 {0}", needConnectPeer.ToString());
                return false;
            }

            if (_token.IsCancellationRequested) return false;
            _token.ThrowIfCancellationRequested();


            Peer gamepeer = P2PManager.GetNetHost.Connect(gamepeer_address, null, null, true);

            //我已经主动链接了，请求中继帮助转发，达成P2P链接
            var isCanclea_and_P2PResponse = await relayNodeClient.P2PConnectRequest(gamepeer_address, _token).SuppressCancellationThrow();

            if (isCanclea_and_P2PResponse.IsCanceled)
            {
                _AlreadySyncPeer.Add(_relayNode); //记录在访问过的Node列表中
               // relayNodeClient.SetNetPeer(null); //空闲
                gamepeer?.Dispose();

                ALog.Log(" [CP] ----请求同步State超时或Task取消 :{0}", needConnectPeer.ToString());
                return false;
            }


            ResponseStatus response_St = isCanclea_and_P2PResponse.Result;

            ALog.Log(" [CP] 是否已经链接上了 {0} -> {1}", gamepeer.Connected, needConnectPeer.ToString());

            if (response_St._Status != MESSatus.NONE && !response_St.IsError(this.GetType().ToString()))
            {
                _AlreadySyncPeer.Add(_relayNode); //记录在访问过的Node列表中
               // WaitPeerConnecting[gamepeer] = needConnectPeer; 
                ALog.Log("中继正在帮助建立桥接，目标是：{0} , 回复 {1}", gamepeer_address.ToString(), response_St._Message);

                var waitPeer = WaitPeerConnecting.Find(x => x.Peer == gamepeer);
                if(waitPeer == null) {
                    //如果返回成功，则添加到链接列表中，但是否链接成功还需要验证
                    NodePeerNet newPeerNet = new NodePeerNet()
                    {
                        Peer = gamepeer,
                        NodePeer = needConnectPeer
                    };
                    WaitPeerConnecting.Add(newPeerNet);
                    OnPeerFind?.Invoke(newPeerNet);
                }
                else {
                    ALog.Log("已经有了这个Peer{0}应该在一开始就识别，不能重复链接！！", gamepeer.Remote);
                }

                return true;
            }
            else
            {
                _AlreadySyncPeer.Add(_relayNode); //记录在访问过的Node列表中
               // WaitPeerConnecting.Remove(gamepeer);
                gamepeer?.Dispose();
               // relayNodeClient.SetNetPeer(null); //空闲
                ALog.Log("中继无法和目标{0}建立桥接！！原因：{1}, {2}", gamepeer_address.ToString(), response_St._Status, response_St._Message);

                return false;
            }

        }

        //AsyncWaitTrue WaitTrue_P2PConnect = new AsyncWaitTrue(8);
        //public async bool ToP2PConnectAsync((Peer, NodePeer) _NodePeer)
        //{
        //    if (WaitTrue_P2PConnect.IsUseding)
        //    {
        //        ALog.Error("请勿重复访问！！");
        //        return false;
        //    }

        //    IPEndPoint address = AUtils.FindRightNetAddress(_NodePeer.Item2);
        //    Peer sync_try_peer = P2PManager.GetNetHost.Connect(address, null, null, true);

        //    await WaitTrue_SyncState.Start(() => sync_try_peer.Connected == true);

        //    if (P2PManager._P2PClient.Peer == null)
        //    {
        //        ALog.Error("!!! Peer created bad " + address);
        //    }

        //    //防止此用户之前创建的房间没有移除，然后链接了其他房间
        //    Peer.Send(new RelayServerRemove());

        //    //告诉中继，我需要去链接这个Peer
        //    Peer.Send(new RelayConnect() { Address = address.ToString() });


        //    freeNodeClient.SetNetPeer(sync_try_peer);
        //    NodePeer m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;
        //    m_NodePeer.address_remote = sync_try_peer.Remote.ToString();
        //    List<NodePeer> remoteCachePeers = await freeNodeClient.NodeSyncStateRequest(m_NodePeer);

        //    ALog.Log("接收到回复 " + (remoteCachePeers == null) + remoteCachePeers?.Count);
        //}


        public async void SyncBlock()
        {

        }

    }

}