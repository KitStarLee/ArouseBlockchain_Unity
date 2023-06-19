using System;
using System.Security.Cryptography;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Crypto;
using ArouseBlockchain.Common;
using static ArouseBlockchain.Common.AConfig;
using ArouseBlockchain.NetService;
using System.Net;
using ArouseBlockchain.P2PNet;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ArouseBlockchain.Solve
{

    /// <summary>
    /// 设计模式-外观模式，执行区块链的相关操作（计算余额、创建创世区块、新区块、处理交易）
    /// </summary>
    public class BlockChainSolve: ISolve
    {
        public ANodePeer NodePeer { set; get; }
        public AWalletAccount WalletAccount { set; get; }
        public ABlock Block { set; get; }
        public ATransaction Transaction { set; get; }
        public ATransactionPool TransactionPool { set; get; }
        public AStake Stake { set; get; }
        public AStorageItem Storage { set; get; }

        List<IBaseFun> baseFuns = new List<IBaseFun>();

        public BlockChainSolve()
        {
            NodePeer = new ANodePeer();
            Stake = new AStake();
            WalletAccount = new AWalletAccount();
            TransactionPool = new ATransactionPool();
            Transaction = new ATransaction();
            Block = new ABlock();
            Storage = new AStorageItem();

            baseFuns.Add(Block);
            baseFuns.Add(Storage);
            baseFuns.Add(Transaction);
            baseFuns.Add(NodePeer);
            baseFuns.Add(Stake);
            baseFuns.Add(TransactionPool);
            baseFuns.Add(WalletAccount);
        }

        public async UniTask<bool> Start(CancellationTokenSource _DCTS)
        {
            //await Block.Init();
            //await Transaction.Init();
            //await NodePeer.Init();
            //await Stake.Init();
            //await TransactionPool.Init();
            //await WalletAccount.Init();

            foreach (var item in baseFuns)
            {
                if (item != null)
                {
                    bool isOK = await item.Init();
                }
                else ALog.Log(item + " is NUll");

            }

            return true;

        }

        public void Stop()
        {
            foreach (var item in baseFuns)
            {
                if (item != null)
                {
                    item.Close();
                }
            }
        }

       
    }
}
