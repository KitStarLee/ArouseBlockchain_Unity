using LiteDB;
using ArouseBlockchain.DB;
using UnityEngine;
using ArouseBlockchain.Common;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;
using Cysharp.Threading.Tasks;
using System.Threading;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using System.Collections.Generic;

namespace ArouseBlockchain.Solve
{

    /// <summary>
    /// 负责数据管理（插入、更新、删除、查询）
    /// </summary>
    public class DBSolve : ISolve
    {
        public static DBSolve DB;

        private readonly LiteDatabase DB_BLOCK;
        private readonly LiteDatabase DB_ACCOUNT;
        private readonly LiteDatabase DB_TRANSACTION;
        private readonly LiteDatabase DB_TRANSACTION_POOL;
        private readonly LiteDatabase DB_PEER;
        private readonly LiteDatabase DB_STAKE;


        List<IDbFun> db_list = new List<IDbFun>();

        public BlockDb Block { get; set; }
        public TransactionDb Transaction { get; set; }
        public NodePeerDb Peer { get; set; }

        public AccountDb Account { get; set; }
        public PoolTransactionsDb PoolTransactions { get; set; }
        public StakeDb Stake { get; set; }


        BsonMapper mapper_account ;
        BsonMapper mapper_tran ;
        BsonMapper mapper_tran_pool ;
        BsonMapper mapper_node;

        public DBSolve()
        {
            DB = this;

            string dbPath = Application.persistentDataPath + ((AUtils.IRS) ? "/IRSDB" : "/PEERDB");
            if (!System.IO.Directory.Exists(dbPath))
            {
                System.IO.Directory.CreateDirectory(dbPath);
            }

            ALog.Log("DB Directory: " + dbPath);


            DB_BLOCK = InitializeDatabase(dbPath + "/block.db");

            mapper_account = new BsonMapper();
            mapper_account.Entity<Account>()
                .DbRef(x => x.number_item, "number_item")   // 1 对 1/0 引用
                .DbRef(x => x.obj_items, "obj_items");  // 1 对多引用
            DB_ACCOUNT = InitializeDatabase(dbPath + "/account.db", mapper_account);

            mapper_tran = new BsonMapper();
            mapper_tran.Entity<Transaction>()
                  .DbRef(x => x.storage_item, "storage_item");
            DB_TRANSACTION = InitializeDatabase(dbPath + "/transaction.db", mapper_tran);

            mapper_tran_pool = new BsonMapper();
            mapper_tran_pool.Entity<Transaction>()
                  .DbRef(x => x.storage_item, "storage_item");
            DB_TRANSACTION_POOL = InitializeDatabase(dbPath + "/transaction_pool.db", mapper_tran_pool);

            DB_STAKE = InitializeDatabase(dbPath + "/stake.db");

            mapper_tran = new BsonMapper();
            mapper_tran.Entity<NodePeer>()
                  .DbRef(x => x.node_state, "node_state");
            DB_PEER = InitializeDatabase(dbPath + "/peer.db", mapper_tran);

        }


        private LiteDatabase InitializeDatabase(string path, BsonMapper mapper = null)
        {
            string connectionString = string.Format("Filename={0};Password={1}", path, "李凯Arouse区块");

            return (mapper==null)? new LiteDatabase(path): new LiteDatabase(path, mapper);
        }

        public async UniTask<bool> Start(CancellationTokenSource _DCTS)
        {
            ALog.Log("... DB Service is starting");
            Block = new BlockDb(DB_BLOCK);
            Account = new AccountDb(DB_ACCOUNT, mapper_account);
            Transaction = new TransactionDb(DB_TRANSACTION, mapper_tran);
            PoolTransactions = new PoolTransactionsDb(DB_TRANSACTION_POOL, mapper_tran_pool);
            Stake = new StakeDb(DB_STAKE);
            Peer = new NodePeerDb(DB_PEER, mapper_tran);

            db_list.Add(Block);
            db_list.Add(Account);
            db_list.Add(Transaction);
            db_list.Add(PoolTransactions);
            db_list.Add(Stake);
            db_list.Add(Peer);
            ALog.Log("...... DB Service is ready");

            return true;
        }

        public void Stop()
        {
            foreach (var db in db_list)
            {
                db.Close();
            }

            ALog.Log("... DB Service is stopping...");
            DB_BLOCK?.Dispose();
            DB_STAKE?.Dispose();
            DB_TRANSACTION?.Dispose();
            DB_TRANSACTION_POOL?.Dispose();
            DB_PEER?.Dispose();
            DB_ACCOUNT?.Dispose();
            ALog.Log("... DB Service has been disposed");
        }
    }
}
