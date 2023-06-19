using System;
using SuperNet.Netcode.Transport;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ArouseBlockchain.NetService;
using ArouseBlockchain.Solve;
using ArouseBlockchain.Common;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using System.Linq;
using ArouseBlockchain.DB;

namespace ArouseBlockchain.P2PNet
{
	public class PeerClientImpl : NodePeerClient
    {

        //需要同步的块的网络节点
        Dictionary<Peer, NodePeer> _NeedSyncBlockPeer = new Dictionary<Peer, NodePeer>();

        //需要访问的节点列表
        List<NodePeer> TemporarySyncPeer = null;

       
        Dictionary<IPEndPoint, Peer> PeerConnecting = new Dictionary<IPEndPoint, Peer>();


        public PeerClientImpl(Peer peer) : base(peer)
        {

        }

        public  void OnReviceP2PConnectResponse(P2PConnectResponse response)
        {
            
        }

        /// <summary>
        /// 同步的Peer 需要避免全是中继，需要全部是客户端。网络广播的时候需要确保都有中继服务
        /// </summary>
        /// <param name="response"></param>
        public  void OnReviceSyncStateResponse(NodeSyncStateResponse response)
        {
            TemporarySyncPeer = SolveManager.BlockChain.NodePeer.StabilizePeersExcludeBootInCache();

            //其他Peer同步过来的NodePeer进行分析，获取最新的稳定Peer
            if (response.NodePeers == null && response.NodePeers.Count >0)
            {
                IEnumerable<NodePeer> availablePeer = response.NodePeers.Except(TemporarySyncPeer);
                ALog.Log("接收到 Peer：{0}个，新的Peer有 {1}个 ", response.NodePeers.Count, availablePeer.Count());

                if (availablePeer.Count() > 0)
                {
                    TemporarySyncPeer.AddRange(availablePeer);
                }
            }
            else
            {
              
            }

            //去重，优先在线的IP
            TemporarySyncPeer = NodePeerDeduplication(TemporarySyncPeer);
        }



        public void AddNeedSyncBlock(Peer Source, NodePeer height_NodePeer)
        {
            _NeedSyncBlockPeer[Source] = height_NodePeer;
        }


        int counts = 0;

       

    }

}