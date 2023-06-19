using System;
using System.Collections.Generic;
using ArouseBlockchain.Solve;
using ArouseBlockchain.Common;
using System.Linq;
using ArouseBlockchain.P2PNet;
using System.Net;
using System.Threading.Tasks;
using static ArouseBlockchain.P2PNet.NetNodePeer;

namespace ArouseBlockchain
{
    /// <summary>
    /// 节点需要区分全节点和SPV节点，全节点用于电脑端，SPV节点用于手机移动端
    /// </summary>
	public class ANodePeer : IBaseFun
    {
        // public List<NetService.NodePeer> InitialPeers { get; set; }

        public ANodePeer()
        {
            // Init();
        }

        NodePeer _NodePeer;
        public NodePeer m_NodePeer
        {
            get
            {
                string port = P2PManager.GetNetHost.GetBindAddress().Port.ToString();
                IPEndPoint local = P2PManager.GetNetHost.GetLocalAddress();

                if (string.IsNullOrEmpty(_NodePeer.address_public))
                {
                    
                    _NodePeer = new NodePeer()
                    {
                        //string port = Host.GetBindAddress().Port.ToString();
                        //IPEndPoint local = Host.GetLocalAddress();
                        //Entry.AddressLocal = local.ToString();
                        //Entry.AddressRemote = PeerRelay.Remote.ToString();
                        //Entry.AddressPublic = PublicIP == null ? null : (PublicIP.ToString() + ":" + port);

                        address_local = local.ToString(),
                        address_remote = "", //Peer.Remote
                        address_public = P2PManager.PublicIP == null ? null : (P2PManager.PublicIP.ToString() + ":" + port),
                        mac_address = AUtils.DeviceMacAddress ?? "",
                        is_bootstrap = AUtils.IRS, //false,
                        report_reach = AUtils.NowUtcTime_Unix,
                        node_state = GetNodeState()
                    };
                }
                else
                {
                    _NodePeer.node_state = GetNodeState();
                    _NodePeer.report_reach = AUtils.NowUtcTime_Unix;
                    _NodePeer.address_local = local.ToString();
                    _NodePeer.address_public = P2PManager.PublicIP == null ? null : (P2PManager.PublicIP.ToString() + ":" + port);
                }

                return _NodePeer;


            }
        }

        public bool m_IsBootstrap => m_NodePeer.is_bootstrap;
        public bool m_IsStabilize => m_NodePeer.IsStabilize;

        /// <summary>
        /// Node节点进行初始化，确保数据库保存初始的主网节点，用于网络的初始工作
        /// </summary>
        public async Task<bool> Init()
        {
            //如果本地数据库没有储存任何Noded地址，则储存公网上Node地址

            var bootstrapPeers = P2PManager.BootSrtapPeers;
            var nodes = StabilizePeersInCache();

            if ((bootstrapPeers == null || bootstrapPeers.Length <= 0) && (nodes == null || nodes.Count <= 0))
            {

                ALog.Error("严重错误，没有记录任何中继网络，可能无法正常游戏");
                return false;
            }

            //对于新用户，需要创建BootSrtap 节点到数据库中。并且随机去访问这些节点，更新他们的NodeState(如果需要)
            foreach (var tempPeer in bootstrapPeers)
            {

                var alreayNode = nodes.Find(x => x.address_public == tempPeer);
                if (!string.IsNullOrEmpty(alreayNode.address_public))
                {
                    alreayNode.is_bootstrap = true;
                    alreayNode.report_reach = AUtils.NowUtcTime_Unix;
                  
                    ALog.Log("----更新中继节点：{0}", tempPeer);
                    DBSolve.DB.Peer.Updaet(alreayNode);
                    continue;
                }

                ALog.Log("----保存中继节点：{0}", tempPeer);
                var newPeer = new NodePeer
                {
                    address_public = tempPeer,
                    // mac_address = AUtils.DeviceMacAddress,
                    is_bootstrap = true,
                    report_reach = AUtils.NowUtcTime_Unix,
                    node_state = new NodePeer.NodeState()
                };

                //把公网Node地址添加入数据库
                DBSolve.DB.Peer.Add(newPeer);

            }

            int count = DBSolve.DB.Peer.CheckUp();
            ALog.Log("清除无用节点：" + count + "个");

            return true;
        }
        public void Close()
        {


        }

        /// <summary>
        /// 获取全部相对稳定的节点不包括中继节点
        /// </summary>
        /// <param name="IsNeedStabilize"></param>
        /// <returns></returns>
        public List<NodePeer> StabilizePeersExcludeBootInCache()
        {
            return DBSolve.DB.Peer.GetAll().Find(p => (p.IsStabilizeExcludeBoot == true)).ToList();
        }

        /// <summary>
        /// 获取全部相对稳定的节点包括中继节点
        /// </summary>
        /// <param name="IsNeedStabilize"></param>
        /// <returns></returns>
        public List<NodePeer> StabilizePeersInCache()
        {
            var allNode = DBSolve.DB.Peer.GetAll();
            ALog.Log("全部节点：" + allNode.Count());
            return allNode.Find(p => (p.IsStabilize == true)).ToList();
        }

        /// <summary>
        /// 获取不是中继的所有缓存稳定不稳定的Peer
        /// </summary>
        /// <returns></returns>
        public List<NodePeer> NoneBootStrapPeersInCache()
        {
            return DBSolve.DB.Peer.GetAll().Find(p => (p.is_bootstrap == false)).ToList();
        }


        NodePeer.NodeState GetNodeState()
        {
            // 获取最后一个区块
            var lastBlock = DBSolve.DB.Block.GetLast();

            var nodeState = new NodePeer.NodeState
            {
                version = AConstants.VERSION,
                height = (!lastBlock.Equals(null) ) ? lastBlock.height : 0,
                hash = (!lastBlock.Equals(null)) ? lastBlock.hash : ""
            };

            // nodeState.known_peers.AddRange(GetKnownPeers());
            //返回这个Node当前的状态，包括版本、高度、地址、哈希以及保存的所有peer的信息
            return nodeState;
        }

        public void UpdateCache(NodePeer peer)
        {
            peer.is_online = false;
            DBSolve.DB.Peer.Updaet(peer);
        }

        public bool ReplaceAll(List<NodePeer> peers)
        {
            peers.ForEach(x => x.is_online = false); //缓存的node应该都重制为离线，是否在线只能中继去判断
            peers = NetNodePeer.NodePeerDeduplication(peers); //去重

            if (peers == null || peers.Count < 1)
            {
                ALog.Error("无法替换过少或空或无效的列表");
                //需要随机找多个，查看是否有效，如果无效，全部拒绝
                return false;
            }

            DBSolve.DB.Peer.DeleteAll();
            DBSolve.DB.Peer.AddBulk(peers);
            return true;
        }

        public NodePeer[] GetAll()
        {
            return DBSolve.DB.Peer.GetAll().FindAll().ToArray();
        }


        /// <summary>
        /// 添加一个新的Peer Node服务器
        /// 1. 
        /// </summary>
        /// <param name="peer"></param>
        public void AddToCache(NodePeer peer)
        {
            peer.is_online = false;
            DBSolve.DB.Peer.Add(peer);
        }


        public void AddToCache(NodePeer[] peer)
        {
            if (peer == null || peer.Length <= 0)
            {
                ALog.Error("是空的，不能添加");
                return;
            }
            for (int i = 0; i < peer.Length; i++)
            {
                peer[i].is_online = false;
            }
            DBSolve.DB.Peer.AddBulk(peer);

        }

    }
}
