using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.DB;
using ArouseBlockchain.NetService;
using ArouseBlockchain.Solve;
using LiteDB;
using UnityEngine.AdaptivePerformance;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain
{

	/// <summary>
	/// 产生的交易会先存放在交易池中，在一定条件下才会进入区块，而不是直接储存在区块
	/// </summary>
	public class ATransactionPool: ATransaction
    {
        protected override TransactionDb GetDB => DBSolve.DB.PoolTransactions;

        public void DeleteAll()
        {
            (GetDB as PoolTransactionsDb).DeleteAll();
        }

        public List<Transaction> GetAllRecord()
        {
            return (GetDB as PoolTransactionsDb).GetAllRecord();
        }

        public List<StorageItem> GetAllItemRecord()
        {
            return (GetDB as PoolTransactionsDb).GetAllItemRecord();
        }

        /// <summary>
        /// 通过挖矿获取到的交易奖励
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="recipient"></param>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public Transaction GetRewardForMinting(long weight, string recipient, long timeStamp)
        {
            if(recipient == null)
            {
                return null;
            }
            // 获取需要构建块的全部交易
            var poolTransactions = GetAllRecord();

            // validator will get coin reward from genesis account
            // to keep total coin in Blockchain not changed
            var totalFees = AUtils.BC.GetTotalFees(poolTransactions);

            var conbaseTrx = new Transaction()
            {
                time_stamp = timeStamp,
                sender = "-",
                pub_key = "-",
                height = weight,
                recipient = recipient,
                fee = 0.0f,
                tx_type = AConstants.TXN_TYPE_VALIDATOR_FEE
            };

            if (poolTransactions.Any())
            {
                //sum all fees and give block creator as reward
                conbaseTrx.storage_item = new StorageNumberItem() { amount = totalFees };
                conbaseTrx.hash = AUtils.BC.GetTransactionHash(conbaseTrx);
                return conbaseTrx;

            }

            return null;
          
        }

    }
}
