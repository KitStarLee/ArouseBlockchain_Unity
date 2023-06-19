using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.DB;
using ArouseBlockchain.NetService;
using ArouseBlockchain.Solve;
using static NBitcoin.Scripting.OutputDescriptor;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;
using BestHTTP;
using UnityEngine.AdaptivePerformance;

namespace ArouseBlockchain
{

    /// <summary>
    ///1. 发起交易：某个用户通过客户端软件发起一笔交易，指定交易的金额、接收方、手续费等信息，并使用私钥对交易进行签名。
    ///2. 广播交易：客户端软件将交易发送到区块链网络中的其他节点，这些节点称为全节点，它们负责验证和转发交易。
    ///3. 验证交易：全节点收到交易后，会检查交易的格式、签名、余额等是否有效，如果有效，就将交易放入一个临时存储区，称为内存池或未确认交易池。
    ///4. 打包交易：一些特殊的全节点，称为矿工节点，会从内存池中选择一些未确认的交易，并按照一定的规则将它们组合成一个数据块，称为候选块。
    ///5. 确认块：矿工节点会对候选块进行一个复杂的计算，称为工作量证明（Proof of Work），目的是找到一个满足条件的随机数，称为难题答案或区块头哈希。这个计算过程需要大量的时间和能源，但验证过程却很简单。当某个矿工节点找到难题答案后，就会将候选块和难题答案广播给其他全节点。其他全节点收到后，会验证难题答案是否正确，以及候选块中的交易是否有效。如果都正确和有效，就将候选块添加到自己维护的区块链上，并继续寻找下一个候选块。
    ///6. 完成交易：当一个数据块被添加到区块链上后，它就成为了一个已确认的数据块，并且包含在其中的所有交易也都被确认了。通常情况下，一个数据块被确认后就很难被修改或撤销了。但有时也会出现分叉的情况，即不同的全节点维护了不同版本的区块链。这时候，需要通过共识协议来解决分叉问题，并选择最长或最重的链作为有效链。因此，在进行区块链交易时，通常需要等待多个数据块被确认后才能确保交易不会被回滚。
    /// </summary>
    public class ATransaction : IBaseFun
    {
        public ATransaction()
        {
        }

        public async Task<bool> Init()
        {
            return true;
        }
        public void Close()
        {


        }

        protected virtual TransactionDb GetDB { get => DBSolve.DB.Transaction; }


        public bool AddBulk(List<Transaction> transactions)
        {
            return GetDB.AddBulk(transactions);
        }

        public bool Add(Transaction transaction)
        {
            return GetDB.Add(transaction);
        }
        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            return GetDB.GetRange(pageNumber, resultPerPage);
        }
        public IEnumerable<Transaction> GetRangeByAddress(string address, int pageNumber, int resultPerPage)
        {
            return GetDB.GetRangeByAddress(address, pageNumber, resultPerPage);
        }
        public Transaction GetByHash(string hash)
        {
            return GetDB.GetByHash(hash);
        }
        public IEnumerable<Transaction> GetLast(int num)
        {
            return GetDB.GetLast(num);
        }
        public Transaction GetByAddress(string address)
        {
            return GetDB.GetByAddress(address);
        }
        public StorageObjItem GetStorageObjItem(string obj_item_hash)
        {
            return GetDB.GetStorageObjItem(obj_item_hash);
        }

        public bool TransactionExists(Transaction txn)
        {
            var transaction = GetDB.GetByHash(txn.hash);
            if (transaction is null)
            {
                return false;
            }

            return false;
        }

        public bool TransactionStorageItemExists(Transaction txn)
        {
            if (txn.storage_item == null || !(txn.storage_item is StorageObjItem)) return false;

            var objItem = GetDB.GetStorageObjItem((txn.storage_item as StorageObjItem).hash);
            if (objItem is null)
            {
                return false;
            }

            return false;
        }


        /// <summary>
        /// Create genesis transaction for each genesis account
        /// Sender and recipeint is same
        /// </summary>
        public  List<Transaction> CreateGenesis(long timeStamp, Dictionary<Solve.KeyPair, Account> g_account)
        {
            var genesisTransactions = new List<Transaction>();
            //var timeStamp = UkcUtils.GetTime();

            foreach (var card in g_account.Keys)
            {
                if (g_account[card].number_item != null)
                {
                    Transaction numberTransaction = NewTransaction(g_account[card]);
                    numberTransaction.storage_item = g_account[card].number_item;
                    AddGenesisTransactions(numberTransaction, card);
                }

                if (g_account[card].obj_items != null && g_account[card].obj_items.Count > 0)
                {
                    foreach (var obj_item in g_account[card].obj_items)
                    {
                        Transaction objTransaction = NewTransaction(g_account[card]);
                        objTransaction.storage_item = obj_item;
                        AddGenesisTransactions(objTransaction, card);
                       // AUtils.BC.PrintStorageItem(obj_item);
                        ALog.Log("Index {0}: 添加一个OBJ交易 {1} \n hash {2}：" , objTransaction.Id , (objTransaction.storage_item as StorageObjItem).hash, objTransaction.hash);
                    }
                }
            }
            return genesisTransactions;

            Transaction NewTransaction(Account account)
            {
                return new Transaction() {
                    time_stamp = timeStamp,
                    sender = account.address,
                    recipient = account.address,
                    fee = 0.0f,
                    height = 1,
                    pub_key = account.pub_key,
                    storage_item = null
                };
            }
           
            void AddGenesisTransactions(Transaction transaction, Solve.KeyPair card)
            {
                var _hash = AUtils.BC.GetTransactionHash(transaction);
                //新交易的哈希
                transaction.hash = _hash;
                //新交易签名

                transaction.signature = AUtils.BC.BytesToHexString(WalletsSolve.Sign(card.PrivateKey.PrivateKey, _hash).Signature);
                Add(transaction);
                DBSolve.DB.PoolTransactions.Add(transaction);
                genesisTransactions.Add(transaction);
            }

            
        }

    }
}
