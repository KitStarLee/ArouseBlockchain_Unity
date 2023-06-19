using System;
using ArouseBlockchain.Common;
using ArouseBlockchain.Solve;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.DB;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ArouseBlockchain
{

    //1. 创世区块
    //2. 创建块，并添加在区块链中呢
    //3. 检查余额
    //4. 检查交易历史
    //5. 显示所有块
    //6. 进行交易
    public class ABlock : IBaseFun
    {
        private readonly Unity.Mathematics.Random rnd;
        public ABlock()
        {
            rnd = new Unity.Mathematics.Random();

        }

        /// <summary>
        /// Node节点进行初始化，确保数据库保存初始的主网节点，用于网络的初始工作
        /// </summary>
        public async Task<bool> Init()
        {
            var blocks = DBSolve.DB.Block.GetAll();
            if (blocks.Count() < 1)
            {
                await BuildGenesis();
            }

            return true;
        }

        public void Close()
        {


        }


        /// <summary>
        /// 创建创世区块
        /// </summary>
        async Task<Block> BuildGenesis()
        {
            var startTimer = AUtils.NowUtcTime.Millisecond;


            var genesisTicks = new DateTime(2023, 2, 28).Ticks;
            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long timeStamp = (genesisTicks - epochTicks) / TimeSpan.TicksPerSecond;
            // timeStamp = 02/28/2023 00:00:00.000; 确保是 2月28日正时

            //[ALog] Your Wallet ：被 看 古 佛 躺 念 谈 争 搞 各 型 凝
            //ADDRESS: 6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh
            //PUBLIC KEY Hex: 71f3937602245be20c738ba1af70e5491773476a
            //PUBLIC KEY :02c33711236eed92cae4857ec3450429a185beee5a19bfbedbc2a848a77cca3920

            //[ALog] Your Wallet ：吧 涤 鸿 旨 愿 啥 荒 语 匀 徙 珠 置
            //ADDRESS: Fpe8Qv4MZxavc7Zgr3fRbYHg9FkNR5opcRteST3XQ8Tz
            //PUBLIC KEY Hex: b762d00313389231d4d34de3c1571d142bdaed4e
            //PUBLIC KEY :035d3b1cd8fd53050a56ffc486578fa1216047dcbbe8e01264fffdbf50cb5f09f6

            Block block = default;
            await Task.Run(() =>
            {

                WalletCard wallet = new WalletCard();
                KeyPair keyPair1 = wallet.GetMasterKeyPair("被看古佛躺念谈争搞各型凝");
                //KeyPair keyPair2 = wallet2.GetMasterKeyPair("吧涤鸿旨愿啥荒语匀徙珠置");

                KeyPair keyPair2 = new Solve.KeyPair();
                keyPair2.PublicKey = new NBitcoin.PubKey("035d3b1cd8fd53050a56ffc486578fa1216047dcbbe8e01264fffdbf50cb5f09f6");
                keyPair2.PublicKeyHex = "b762d00313389231d4d34de3c1571d142bdaed4e";


                Dictionary<Solve.KeyPair, Account> genesisAccounts = SolveManager.BlockChain.WalletAccount.GetGenesis(keyPair1);
                List<Transaction> genesisTransactions = SolveManager.BlockChain.Transaction.CreateGenesis(timeStamp, genesisAccounts);


                string currAccountAddresss = WalletsSolve.GetWalletAddress(keyPair1.PublicKey); //.GetAccountAddress();

                var total_amount = AUtils.BC.GetTotalAmountByTransaction(genesisTransactions);

                block = new Block
                {
                    version = 1,
                    height = 1,
                    time_stamp = timeStamp,
                    prev_hash = "-",
                    transactions = JsonConvert.SerializeObject(genesisTransactions), //JsonSerializer.(genesisTransactions),
                    validator = currAccountAddresss,
                    validator_balance = "-",
                    merkle_root = CreateMerkleRoot(genesisTransactions),
                    num_of_tx = genesisTransactions.Count,
                    //计算总的交易
                    total_amount = JsonConvert.SerializeObject(total_amount),
                    //计算总的手续费
                    total_reward = 0,
                    difficulty = 1,
                    proof = 0,
                    nonce = 1
                };

                var blockHash = AUtils.BC.GetBlockHash(block);
                block.hash = blockHash;
                block.signature = AUtils.BC.BytesToHexString(WalletsSolve.Sign(keyPair1.PrivateKey.PrivateKey, blockHash).Signature);

                block.size = JsonConvert.SerializeObject(block).Length;

                var endTimer = AUtils.NowUtcTime.Millisecond;
                block.build_time = (endTimer - startTimer);

                SolveManager.BlockChain.WalletAccount.UpdateStorageGenesis(genesisTransactions);
                DBSolve.DB.Block.Add(block);

                return true;

            });
            ALog.Log("创建了创世区块 {0} :{1}", block.height, block.transactions);
            AUtils.BC.PrintBlock(block);
            return block;
        }


        /// <summary>
        /// 创建并添加新的块
        /// </summary>
        /// <param name="startTimer">挖矿或者质押的开始时间</param>
        /// <param name="validator">矿工的钱包地址</param>
        /// <param name="validator_balance">矿工的余额</param>
        /// <param name="difficulty">困难度</param>
        /// <param name="nonce">随机数值</param>
        /// <param name="proof">不断递增的变量数值</param>
        /// <returns></returns>
        public async UniTask<Block> Build(int startSecond, string validator, string validator_balance, int difficulty, int nonce, long proof, CancellationToken cancelTask)
        {
            if (cancelTask.IsCancellationRequested) return default;

            var poolTransactions = SolveManager.BlockChain.TransactionPool.GetAllRecord();

            Block block = default;
            // 获取最后一个块
            var lastBlock = DBSolve.DB.Block.GetLast();
            var nextHeight = lastBlock.height + 1;
            var prevHash = lastBlock.hash;

            var timestamp = AUtils.NowUtcTime_Unix;

            // var validator = ServicePool.FacadeService.Stake.GetValidator();

            //获取到收益，并且给予挖矿者
            Transaction trans_reward = SolveManager.BlockChain.TransactionPool.GetRewardForMinting(nextHeight, validator, timestamp);
            if (trans_reward != null) poolTransactions.Add(trans_reward);

            var total_amount = AUtils.BC.GetTotalAmountByTransaction(poolTransactions);
            block = new Block
            {
                version = 1,
                height = nextHeight,
                time_stamp = timestamp,
                prev_hash = prevHash,
                transactions = JsonConvert.SerializeObject(poolTransactions),
                validator = validator,
                validator_balance = validator_balance,// JsonConvert.SerializeObject(minterBalance),
                merkle_root = CreateMerkleRoot(poolTransactions),
                num_of_tx = poolTransactions.Count,
                total_amount = JsonConvert.SerializeObject(total_amount),
                total_reward = AUtils.BC.GetTotalFees(poolTransactions),
                difficulty = difficulty,
                proof = proof,
                nonce = nonce,

            };

            var blockHash = AUtils.BC.GetBlockHash(block);
            block.hash = blockHash;
            block.signature = AUtils.BC.BytesToHexString(SolveManager.Wallets.Sign(blockHash).Signature);

            block.size = JsonConvert.SerializeObject(block).Length;

            var endTimer = AUtils.NowUtcTime.Second;
            block.build_time = (endTimer - startSecond);

            AUtils.BC.PrintBlock(block);

            SolveManager.BlockChain.WalletAccount.UpdateStorage(poolTransactions);
            SolveManager.BlockChain.Transaction.AddBulk(poolTransactions);
            DBSolve.DB.PoolTransactions.DeleteAll();
            DBSolve.DB.Block.Add(block);

            NetBlock.BlockClient blockClient = new NetBlock.BlockClient();

            blockClient.BuildCBroadcast(block); //给没有链接的Peer广播块
            blockClient.BuildCRequest(block ,default); //给正在长链接的Peer同步块
          
            return block;
        }

        public async UniTask<Block> Build(int startSecond, Stake stake, CancellationToken cancelTask)
        {
            // var startTimer = AUtils.NowUtcTime.Millisecond;
            if (cancelTask.IsCancellationRequested) return default;

            var minterAddress = stake.address;

            var poolTransactions = SolveManager.BlockChain.TransactionPool.GetAllRecord();

            var minterBalance = AUtils.BC.GetTotalAmountByAccount(SolveManager.BlockChain.WalletAccount.GetByAddress(minterAddress));
            var balance = JsonConvert.SerializeObject(minterBalance);


            //质押和挖矿不同，困难度、nonce、proof这些都会随机
            return await Build(startSecond, minterAddress, balance, 1, rnd.NextInt(100000), 1, cancelTask);


        }

        public Block GetFirst()
        {
            return DBSolve.DB.Block.GetFirst();
        }


        /// <summary>
        /// 获取给定的Height之后的100个区块
        /// </summary>
        /// <param name="startHeight">开始区块</param>
        /// <param name="count">剩余的块的数量要求</param>
        /// <returns></returns>
        public List<Block> GetRemaining(long startHeight, int count = 50)
        {
            return DBSolve.DB.Block.GetRemaining(startHeight, count);
        }

        /// <summary>
        /// Get Last block ordered by block weight
        /// </summary>
        public Block GetLast()
        {
            return DBSolve.DB.Block.GetLast();
        }


       

        private string CreateMerkleRoot(List<Transaction> txns)
        {
            return AUtils.BC.CreateMerkleRoot(txns.Select(tx => tx.hash).ToArray());
        }


        async UniTask<bool> AddBlock(Block block)
        {
            return await Task.Run(() => {

                List<Transaction> newTransaction = new List<Transaction>();
                newTransaction = JsonConvert.DeserializeObject<List<Transaction>>(block.transactions);

                if (newTransaction != null && newTransaction.Count > 0)
                {
                    SolveManager.BlockChain.WalletAccount.UpdateStorage(newTransaction);
                    SolveManager.BlockChain.Transaction.AddBulk(newTransaction);

                    var localRecord = DBSolve.DB.PoolTransactions.GetAllRecord();
                    var intersect = localRecord.Intersect(newTransaction).ToList();

                    if (intersect != null && intersect.Count > 0)
                        DBSolve.DB.PoolTransactions.DeleteAll(intersect.ToList()); // 清楚本地的交易池

                    DBSolve.DB.Block.Add(block);

                    return true;
                }
                else
                {
                    ALog.Error("反序列化失败或者这个块没有交易内容 {0}", block.ToString());
                    return false;
                }

            }).AsUniTask();

        }

        /// <summary>
        /// 当从其他Peer接受到添加块的请求后，先验证块，再添加到DB中。
        /// </summary>
        public async UniTask<bool> ValidNextBlockAndSave(Block block, bool isValid = true)
        {
            if(!isValid) return await AddBlock(block);

            var lastBlock = GetLast();

            //是否是我需要的下一个块，并且取保是正确的块
            if (!lastBlock.Equals(null) && !AUtils.BC.Valid_Proof(block.prev_hash, block.transactions, block.difficulty, block.nonce, block.proof))
            {
                ALog.Error("非法块，验证失败: {0} ", block.ToString()) ;
                return false;
            }

            //compare block height with prev
            if (lastBlock.Equals(null) || AUtils.BC.IsNextBlock(lastBlock, block))
            {
                AUtils.BC.PrintBlock(block);

                try
                {
                    return await AddBlock(block);
                }
                catch (Exception ex)
                {
                    ALog.Error("块保存失败 {0}" ,ex);
                    return false;
                }

               // return true;
            }
            else
            {
                ALog.Log("不平衡块，不保存，平衡后后期保存: {0} ", block.ToString());
                return false;
            }

        }

        /// <summary>
        /// 当从其他Peer接受到添加块的请求后，先验证块，再添加到DB中。
        /// </summary>
        public async UniTask<bool> ValidNextBlockAndSave(List<Block> blocks)
        {
            if(blocks == null || blocks.Count <= 0)
            {
                ALog.Error("多个区块为空！！，验证失败 ");

                return false;
            }

            var lastBlock = GetLast();

            var newFirstBolck = blocks.First();

            if (!((lastBlock.height + 1) == newFirstBolck.height)) blocks.Reverse();

            var re_newFirstBolck = blocks.First();
            if (!((lastBlock.height + 1) == re_newFirstBolck.height))
            {
                ALog.Error("无法添加这个区块链！！头尾都不合适！我的高度 ：{0} ,这个链 头 {1} ; 链 尾 {2} ", lastBlock.height,
                    newFirstBolck.height, re_newFirstBolck.height);
                return false;
            }
               

            int errorCount = 0;
            foreach (var block in blocks)
            {
              if( await ValidNextBlockAndSave(block) == false)
                {
                    errorCount += 1;
                }
            }
            ALog.Log("{0} : 总共 {1} 个块，出错 {2} 个，成功保存 {3}个", this.GetType(), blocks.Count, errorCount, blocks.Count - errorCount);

            return (errorCount < blocks.Count);

        }


    }
}
