using System.Net;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using UnityEngine;
using ArouseBlockchain.UI;
using ArouseBlockchain.Solve;
using System.Threading.Tasks;
using System;
using EasyButtons;
using static ArouseBlockchain.P2PNet.NetTransaction;
using Cysharp.Threading.Tasks;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace ArouseBlockchain
{
    public class ABlockchain : MonoBehaviour
    {
        P2PManager HostServer;
        public static ABlockchain Init;

        private void Awake()
        {
           // ALog.Enable = false;
            ALog.IsCacheLog = false;

            ALog.Log("01 Start game inti------");
            Init = this;
            //1. Update tool file
            //2. 配置运行环境
            //3. 同步NodePeer状态
            UpdateConfigFiles();

        }



        public long GetTime()
        {
            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long nowTicks = DateTime.UtcNow.Ticks;
            long tmStamp = ((nowTicks - epochTicks) / TimeSpan.TicksPerSecond);
            return tmStamp;
        }

        async void UpdateConfigFiles()
        {
            ALog.Log("02Start game inti------");

            //update config file
            AConfig.NetConfig config = await AConfig.UpdateNetConfig();
            if (config == null)
            {
                ALog.Error("Failed to update config file.");
                ALog.Error("Failed to get public IP. No internet connection! or server error!");
                UIManager.INST.ShowUI_NetworkError("Failed to get public IP. No internet connection! or server error!");
                return;
            }

            //check network
            IPAddress address = await P2PManager.GetPublicIP(config.PublicIPServices.ToArray());
            if (address == null)
            {
                ALog.Error("Failed to get public IP. No internet connection! or server error!");
                UINetError.Switch = true;
                UIManager.INST.ShowUI_NetworkError("Failed to get public IP. No internet connection! or server error!");
                return;
            }
            ALog.Log("PublicIP address : " + address );
         

            //Init Server
            MountArouseEnvironment(config);

            ALog.Log("End inti------" + " address : " + address + " config : "+ config.BootSrtapPeers);
        }


        CancellationTokenSource waitDCTS = new CancellationTokenSource();
        public async UniTaskVoid MountArouseEnvironment(AConfig.NetConfig netConfig)
        {
            if (waitDCTS.IsCancellationRequested)
            {
                waitDCTS?.Cancel();
                waitDCTS?.Dispose();
                waitDCTS = new CancellationTokenSource();
            }

            gameObject.name = "[ArouseBlockchain]";

            HostServer = P2PManager.Init(gameObject);
            bool isSuccess = HostServer.InitNetworkEnv(netConfig);
            SolveManager.Init.Add(
                new DBSolve(),  //数据库事务
                new BlockChainSolve()  //区块链事务
               
                );

            if (!AUtils.IRS)
            {
                SolveManager.Init.Add(
                    new WalletsSolve(),  //钱包事务
                    new MintingSolve() // 挖矿事务
                    );
            }

            if (waitDCTS.IsCancellationRequested) return;
            var result1 =  await SolveManager.Start(waitDCTS).SuppressCancellationThrow();//等数据库等等基础组建初始户完成
            if (result1.IsCanceled) return;
            await UniTask.Yield();

            if (!isSuccess)
            {
                ALog.Error("网络配置错误");
            }

            //装载解析模块
            HostServer.FitParseModule();
            // 运行Host服务
            HostServer.StartServer();
            gameObject.SetActive(true);
            
            var isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(5), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, waitDCTS.Token).SuppressCancellationThrow();
            if (isCanceled) return;

            var host = P2PManager.GetNetHost;

            IPEndPoint bingAddress = host.GetBindAddress();
            IPEndPoint localAddress = host.GetLocalAddress();

            ALog.Log("Host {0} To IP4 启动 BindAdd: {1} ; Local: {2} .", bingAddress.AddressFamily, AUtils.Net.IPToIP4(bingAddress),
                    AUtils.Net.IPToIP4(localAddress));
            ALog.Log("Host {0} 启动 BindAdd: {1} ; Local: {2} .", bingAddress.AddressFamily, bingAddress.ToString(), localAddress.ToString());

            if (!host.Listening)
            {
                UIManager.INST.ShowUI_NetworkError("网络服务错误，请重新链接!");
               // return;
            }
           

            AUtils.BC.PrintNodePeers(SolveManager.BlockChain.NodePeer.m_NodePeer);

            if(!AUtils.IRS) UIManager.INST.UIInit();
            var isCanceled2 = await UniTask.Delay(TimeSpan.FromSeconds(2), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, waitDCTS.Token).SuppressCancellationThrow();
            if (isCanceled2) return;

            if (!AUtils.IRS) GameManager.INST._3DScenes.SetActive(true);

        }


        

        private void OnApplicationQuit()
        {
            SolveManager.Stop();
        }

        private void OnDestroy()
        {
            ALog.Log("OnDestroy!  OnDestroy!  OnDestroy!");
        }


        //~ABlockchain() {
        //    ALog.Log("ABlockchain  ：：： None!  None!  None!");
        //    Console.WriteLine("ABlockchain  ：：： None!  None!  None!");
        //}


        /// <summary>
        /// 去发送一笔交易
        /// </summary>
        /// <param name="recipient">收件人</param>
        /// <param name="strAmount">发送数量</param>
        /// <param name="strFee">手续费</param>
        [Button]
        public void DoSendCoin(string recipient,int amount, int fee)
        {

            string sender = SolveManager.Wallets.GetUserWalletCard("被看古佛躺念谈争搞各型凝").GetAccountAddress();
            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(sender);

            var senderBalance = account.number_item.amount;

            if ((amount + fee) > senderBalance)
            {
                ALog.Log("Error! Sender ({"+ sender + "}) does not have enough balance!");
                ALog.Log("Sender balance is :"+ senderBalance);
                return;
            }

            var NewTxn = new Transaction
            {
                //创世账户
                sender = sender,
                time_stamp = AUtils.NowUtcTime_Unix,
                recipient = recipient,
                storage_item = new StorageNumberItem() { amount = amount },
                fee = fee,
                height = 0,
                tx_type = "Transfer",
                pub_key = SolveManager.Wallets.GetCurrWalletCard.GetKeyPair().PublicKeyHex,
            };

            var TxnHash = AUtils.BC.GetTransactionHash(NewTxn);
            var signature = SolveManager.Wallets.Sign(TxnHash);

            NewTxn.hash = TxnHash;
            NewTxn.signature = AUtils.BC.BytesToHexString(signature.Signature).ToLower();

            //发送到链上

            SolveManager.BlockChain.WalletAccount.UpdateStorage(NewTxn);
        }

        [Button]
        public void DoSendOBJItem(string recipient, string obj_hash, int fee)
        {
            string sender = SolveManager.Wallets.GetUserWalletCard("被看古佛躺念谈争搞各型凝").GetAccountAddress();
            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(sender);

            var senderBalance = account.number_item.amount;

            var sendObj = account.obj_items.Find(x => x.hash == obj_hash);

            if (sendObj== null)
            {
                ALog.Error("此账号没有这个OBJ :" + obj_hash);
                return;
            }
            if (fee > senderBalance)
            {
                ALog.Error("账号剩余余额无法支付手续费 :" + senderBalance);
                return;
            }

            var NewTxn = new Transaction
            {
                //创世账户
                sender = sender,
                time_stamp = AUtils.NowUtcTime_Unix,
                recipient = recipient,
                storage_item = sendObj,
                fee = fee,
                height = 0,
                tx_type = "Transfer",
                pub_key = SolveManager.Wallets.GetCurrWalletCard.GetKeyPair().PublicKeyHex,
            };

            var TxnHash = AUtils.BC.GetTransactionHash(NewTxn);
            var signature = SolveManager.Wallets.Sign(TxnHash);

            NewTxn.hash = TxnHash;
            NewTxn.signature = AUtils.BC.BytesToHexString(signature.Signature).ToLower();

            //发送到链上

            SolveManager.BlockChain.WalletAccount.UpdateStorage(NewTxn);
        }

        /// <summary>
        /// 获取账号储仓中所有物品
        /// </summary>
        [Button]
        public void DoGetBalance(int index)
        {
            var walletCard = SolveManager.Wallets.SelectUserWalletCard(index);
            var address = SolveManager.Wallets.GetCurrAddress;

            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(address);

            ALog.Log("当前用户 {0} 的余额是 :{1}", address, account.number_item.amount);
            AUtils.BC.PrintStorageItem(account.number_item);
            if (account.obj_items != null) AUtils.BC.PrintStorageItem(account.obj_items.ToArray());
            //物品分为不同类型，积分是数量，模型等等是物品ID
        }

        /// <summary>
        /// 获取所有交易历史
        /// </summary>
        [Button]
        public void DoGetTransactionHistory()
        {
            string address = SolveManager.Wallets.GetCurrAddress;
            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(address);

            if (string.IsNullOrWhiteSpace(address))
            {
                ALog.Error("Error, the address is empty, please create an account first!\n");
                return;
            }

            ALog.Log("Transaction History for "+ address);

            try
            {
                var transactions = SolveManager.BlockChain.Transaction.GetRangeByAddress(address, 1, 50);
               
                if (transactions != null)
                {
                    foreach (var Txn in transactions)
                    {
                        AUtils.BC.PrintTransaction(Txn);
                    }
                }
                else
                {
                    ALog.Log("\n---- No records found! ---");
                }

                ALog.Log("\n---- 交易池---");

                var transactionPool = SolveManager.BlockChain.TransactionPool.GetRangeByAddress(address, 1, 50);

                if (transactionPool != null)
                {
                    foreach (var Txn in transactionPool)
                    {
                        AUtils.BC.PrintTransaction(Txn);
                    }
                }
            }
            catch
            {
                ALog.Log("\nError! Please check your UbudKusCoin Node, it needs to be running!");
            }
        }

        

        [Button]
        public void DoShowWalletInfo()
        {
            var walletCard = SolveManager.Wallets.GetCurrWalletCard;

            AUtils.BC.PrintWallet(walletCard);
        }


        [Button]
        public void DoShowHeadAndTailBlock()
        {
            var tail_block = SolveManager.BlockChain.Block.GetLast();
            AUtils.BC.PrintBlock(tail_block);

            var head_block = SolveManager.BlockChain.Block.GetFirst();

            AUtils.BC.PrintBlock(head_block);

        }

       

        public void StopServer()
        {
            SolveManager.Stop();
            HostServer.Shutdown();
        }



        void Update()
        {

        }

    }
}




/*
1. P2P网络：节点发现、节点维护、持久化保存、区块同步。
2. 公私钥对：命令行，创建公私钥对并生成地址，提供私钥存储，公私钥验证。
3. 发送交易：命令行，发送成功验证，输入是交易哈希。
4. 交易查询：命令行，JSON格式的交易查询返回，输入是某个地址。
5. 余额查询：命令行，JSON格式的余额查询返回，输入是某个地址。
6. 挖矿：命令行、JSON格式挖矿信息返回，输入是某个地址。
7. 区块共识：编织区块链的算法，包含创世区块以及调整全网挖矿难度。
8. 交易共识：验证单个交易的算法，包含签名验证和UTXO验证。
9. 基础日志：用于监控网络，区块验证等操作。
10. 区块持久化存储：分叉与合并时的一致性，并为查询提供接口。
11. 提供格式化输出交易的功能，这里的格式化主要指JSON格式。
12. 有效防止双花交易。
*/


/*
 工具选择：
1. WebSocket
2. JSON格式 + Protobuf + gRPC
3. OpenSSL库中的RSA
4. UTXO模型
5. SQLite 3
6. PoW作为共识算法 ： SHA-256
 */
