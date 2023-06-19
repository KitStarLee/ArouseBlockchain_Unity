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
	public class AStake : IBaseFun
    {
        // public List<NetService.NodePeer> InitialPeers { get; set; }

        public AStake()
        {
            // Init();
        }

        /// <summary>
        /// Node节点进行初始化，确保数据库保存初始的主网节点，用于网络的初始工作
        /// </summary>
        public async Task<bool> Init()
        {
           
            return true;
        }
        public void Close()
        {


        }


        public Stake GetMaxStake()
        {
            return DBSolve.DB.Stake.GetMax();
        }

        public void AddOrUpdate(Stake stake)
        {
            DBSolve.DB.Stake.AddOrUpdate(stake);
        }
    }
}
