using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using Newtonsoft.Json;
using ArouseBlockchain.NetService;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Threading;
using ArouseBlockchain.Solve;
using System.Net;
using ArouseBlockchain.P2PNet;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using SuperNet.Unity.Core;
using static ArouseBlockchain.Common.AUtils;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Net;
using Cysharp.Threading.Tasks;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls;

namespace ArouseBlockchain.P2PNet
{

    /// <summary>
    /// NodePeer数据同步相关服务处理
    /// 1. NodePeer分为bootstrap节点 和 client节点。
    /// 2. 这些节点数据都可以通过KnownPeersInCache在数据库中获取
    /// 3. bootstrap节点 和 client节点区别是，bootstrap节点公网地址应该保存不变的，client节点由于内网问题会不断变化，没有缓存的价值
    ///
    /// 业务流程：
    /// 1. 访问本地缓存的稳定节点，更新同步NodePeerList(网络节点、当前块状态)
    /// 2. 把已经访问的节点缓存起来，把新获取到的节点网络信息缓存起来（其中稳定次数只保存最大的记录）。
    /// 3. 如果当前节点返回的数据表明，我有10个以上的块需要同步，则同步块，并且缓存目前获取到的节点网络信息（其中稳定次数只保存最大的记录）。
    /// 4. 否则，排除已经访问的去访问新获取到的节点网络信息，更新同步NodePeerList(网络节点、当前块状态)
    /// 5. 直至可以获取到10个最新块数据，则停止同步，开始同步块数据。
    /// </summary>
	public class PeerServiceImpl : NodePeerServer
    {
        //private readonly System.Threading.ReaderWriterLockSlim Lock;

        public PeerServiceImpl()
        {
          //  Lock = new System.Threading.ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        /// 中继可以是虚中继也可以是全中继，全中继的工作就只有转发(P2P链接)和同步(块和交易)
        /// 全中继是服务器本身可以定夺固定的，虚中继是P端用户自己通过链接习惯定夺的。
        /// 
        /// 作为链接中转站，如果收到P2P中A请求链接B，则本Host帮助进行通知
        /// 前提是确保我和目前这个B Peer链接着才能帮助进行互通
        /// </summary>
        /// <param name="peer">A Peer</param>
        /// <param name="reader">A 的IP</param>
        /// <param name="info"></param>
        /// <returns></returns>
        public override async UniTaskVoid P2PConnectResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken)
        {
            P2PConnectResponse response = new P2PConnectResponse();


            P2PConnectRequest message = new P2PConnectRequest();
            message.Read(reader);
            IPEndPoint target_remote = IPResolver.TryParse(message.ToAddress);

            if (target_remote == null)
            {
                SendError(peer, response, MESSatus.STATUS_ERROR_BAD, "非法IP");

                return;
            }
        
               

            NodePeerNet nodePeerNet = null;
            P2PManager.PeersConnected.TryGetValue(target_remote, out nodePeerNet);

            NodePeer m_Peer = SolveManager.BlockChain.NodePeer.m_NodePeer;
            IPEndPoint my_PublicIP = IPResolver.TryParse(m_Peer.address_public);
            IPEndPoint my_LocalIP = IPResolver.TryParse(m_Peer.address_local);

            //如果链接的不是自己，就是需要我转发
            if (! (Net.IsIPSame(my_PublicIP, target_remote) || Net.IsIPSame(my_LocalIP, target_remote) ))
            {
                //确保我链接着的，否则无法帮助他们建立其通讯
                if (nodePeerNet == null)
                {
                    SendError(peer, response, MESSatus.STATUS_ERROR_NOT_FOUND, "无法桥接");

                    return ;
                }
                else
                {
                    SendSucc(peer, response, MESSatus.STATUS_SUC_NONE, "正在帮你桥接Peer：" + target_remote.ToString());


                    //转发桥接请求
                    message.FormAddress = peer.Remote.ToString();
                    nodePeerNet.Peer.Send(message);
                    return ;
                }
            }
            else // 如果对象想链接的就是我，则我和其他Peer建立链接
            {
                IPEndPoint form_remote = IPResolver.TryParse(message.FormAddress);
                if (form_remote == null) {

                    SendError(peer, response, MESSatus.STATUS_ERROR_BAD, "非法IP");

                    return;
                }
                NodePeerNet formPeerNet = null;
                P2PManager.PeersConnected.TryGetValue(form_remote, out formPeerNet);

                if (formPeerNet != null)
                {
                    SendWarn(peer, response, "链接中，勿重复");
                    return;
                }
                else
                {

                    Dictionary<IPEndPoint, NodePeerNet> allLinkPeer = P2PManager.PeersConnected;

                    Peer newPeer = P2PManager.GetNetHost.Connect(form_remote);

                    var wait_result = await UniTask.WaitUntil(() => newPeer.Connected == true, PlayerLoopTiming.FixedUpdate, _CancelToken.Token).SuppressCancellationThrow();
                    if (wait_result)
                    {
                        SendError(peer, response, MESSatus.STATUS_ERROR_TIMEOUT);
                        return;
                    }

                    var isCancel = await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, PlayerLoopTiming.Update, _CancelToken.Token).SuppressCancellationThrow();
                    if (isCancel)
                    {
                        SendError(peer, response, MESSatus.STATUS_ERROR_TIMEOUT);
                        return;
                    }

                    if (newPeer.Connected)
                    {
                        //和对方建立链接
                        SendSucc(newPeer,response, MESSatus.STATUS_SUC_NONE,
                            (allLinkPeer.Count > 10)? "和你链接中，但Host近满载网络可能不稳定,链接数量" + allLinkPeer.Count
                            : "和你P2P中，勿重复");
                        //newPeer.Send(response);
                    }
                    else
                    {
                        SendError(peer, response, MESSatus.STATUS_ERROR_NOT_FOUND, "目标Peer无响应，我方链接数：" + allLinkPeer.Count);
                    }
                       
                    return;
                }

            }
           
        }



        /// <summary>
        /// 当中继（纯中继的多）Host接受到 一个Peer的 PeerList请求的消息，
        /// </summary>
        public override void NodeSyncStateResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken)
        {
            NodeSyncStateResponse response = new NodeSyncStateResponse();

            ResponseStatus status = new ResponseStatus();

            NodeSyncStateRequest message = new NodeSyncStateRequest();
            message.Read(reader);
            NodePeer remote_peer = message.NodePeer;

            //更新请求Peer的Node地址情况
            string remote = peer.Remote.ToString();
            remote_peer.address_remote = remote;
            if (remote_peer.address_public != remote)
            {
                if (Net.IsInternal(peer.Remote.Address))
                {
                    ALog.Log("[" + peer.Remote + "] Received public IP " + remote_peer.address_public + " is not " + remote + ". Keeping.");
                }
                else
                {
                    ALog.Log("[" + peer.Remote + "] Received public IP " + remote_peer.address_public + " is not " + remote + ". Deleting.");
                    remote_peer.address_public = null;
                }
            }



            remote_peer.report_reach = AUtils.NowUtcTime_Unix;
            remote_peer.is_online = false; //在线离线不应该保存在数据库，所以防止错误，默认为否

            //更新客户端节点的稳定率,但是稳定节点应该很少很少很少
            var noneBootPeers = SolveManager.BlockChain.NodePeer.NoneBootStrapPeersInCache();

            //Mac地址可能获取失败，失败导致有很大问题
            int cacheIndex = noneBootPeers.FindIndex(p => (p.address_public == remote_peer.address_public)||
            (p.mac_address == remote_peer.mac_address));

            if (cacheIndex >= 0)
            {
                DateTime lastDate = AUtils.ToDateTimeByUTCUnix(noneBootPeers[cacheIndex].report_reach);
                DateTime nowDate = AUtils.NowUtcTime;
                if (nowDate.Subtract(lastDate).Days > 1)
                {
                    NodePeer updateNpde = noneBootPeers[cacheIndex];
                    remote_peer.stable_count = updateNpde.stable_count += 1;
                }

                SolveManager.BlockChain.NodePeer.UpdateCache(remote_peer);

                //设置这个新链接的Peer的Node信息，方便为其他Peer使用,不要去重复保存
                P2PManager.Instance.SetNodePeerState(peer, remote_peer,false);
            }
            else {

                //设置这个新链接的Peer的Node信息，方便为其他Peer使用
                P2PManager.Instance.SetNodePeerState(peer, remote_peer);

            }

            Dictionary<IPEndPoint, NodePeerNet> allLinkPeer = P2PManager.PeersConnected;//获取在线节点
            if (allLinkPeer.Count > 10)
            {  //确保中继服务的性能，超过10个Peer,则提醒性能
                status._Status = MESSatus.STATUS_WARN;
                status._Message = "中继近满载，当前链接数：" + allLinkPeer.Count;
                response._ReStatus = status;
            }
            else if (allLinkPeer.Count > 40) {

                //确保中继服务的性能，超过40个Peer,则不提供同步服务
                status._Status = MESSatus.STATUS_ERROR;
                status._Message = "中继满载，请链接其他稳定中继，当前链接数：" + allLinkPeer.Count;
                response._ReStatus = status;

                peer.Send(response);
                peer.Disconnect();
                return;
            }

            var stabilizePeers = SolveManager.BlockChain.NodePeer.StabilizePeersExcludeBootInCache();//获取稳定节点
           

            allLinkPeer.Remove(peer.Remote);//排除自己的

            //创建我方最新的Peer，包含：本地记录的稳定Peer 和 正在链接的Peer 以及我自己的最新的Peer

            stabilizePeers.ForEach(x => x.is_online = false);//缓存的地址默认应该为不在线节点

            var all_NodePeers = new List<NodePeer>();
            foreach (var item in allLinkPeer.Keys)
            {
                NodePeer linkPeer = allLinkPeer[item].NodePeer;
                linkPeer.is_online = true;
                all_NodePeers.Add(linkPeer);
            }

            if (stabilizePeers.Count > 0) {
                all_NodePeers.AddRange(stabilizePeers);
                all_NodePeers = NetNodePeer.NodePeerDeduplication(all_NodePeers);
            } //去重之后，发送我方记录的稳定节点


            //同时发送我的Node状态，看是否需要同步块
            NodePeer m_Peer = SolveManager.BlockChain.NodePeer.m_NodePeer;
            m_Peer.is_online = true;
            all_NodePeers.Add(m_Peer);


            //去重复
            all_NodePeers = NodePeerDeduplication(all_NodePeers);

            status._Status = (all_NodePeers!=null&& all_NodePeers.Count > 0) ?MESSatus.STATUS_SUCCESS: MESSatus.STATUS_SUC_NONE;
            status._Message = "当前可以同步的Peer数量：" + allLinkPeer.Count;

            response._ReStatus = status;
            response.NodePeers = all_NodePeers;

            //发送当前中继在线的所有的Peer，以方便客户端去同步这些Peer数据
            peer.Send(response);
            
        }



        /// <summary>
        /// 当接受到了其他Host返回的NodePeerLis数据
        /// 下一步：缓存这些Peer状态，看是否有新的块需要去同步的
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        /// <param name="info"></param>
        /// <exception cref="NotImplementedException"></exception>
        //public override void OnReceiveNodePeerLis(Peer peer, Reader reader)
        //{
        //    // We received a list of servers from the relay, notify the menu
        //    NodePeerListResponse message = new NodePeerListResponse();
        //    message.Read(reader);

        //    var allknowNodePeers = message.NodePeers;
        //    var m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;

        //    //确保是返回给自己的数据
        //    if (!AUtils.IsSameNetwork(peer.Remote, m_NodePeer))
        //    {
        //        //请求Peer 自己的 NodePeer数据，需要进行保存，并放入已知Peer库中
        //        SolveManager.BlockChain.NodePeer.AddToCache(allknowNodePeers.ToArray());

        //        //找寻比自己块高的Peer
        //        foreach (var otherPeer in allknowNodePeers)
        //        {
        //            if(m_NodePeer.node_state.height < otherPeer.node_state.height)
        //            {
        //                P2PManager._P2PSync.AddNeedSyncBlock(peer, otherPeer);
        //            }
        //        }
        //        //再次检查看是否需要再次探索P2P网络
        //        P2PManager._P2PSync.SyncState();

        //        ALog.Log("Receive [" + peer.Remote + "] allknowNodePeers list = " + allknowNodePeers.Count);
        //    }
        //    else
        //    {
        //        ALog.Error("[" + peer.Remote + "] ??? 我是客户端，不需要反复接受自己的消息！！！ = ");
        //    }
        //}

    }

}