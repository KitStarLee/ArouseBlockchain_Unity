using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.NetService;
using ArouseBlockchain.P2PNet;
using BestHTTP;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using static ArouseBlockchain.Common.AUtils;
using static NBitcoin.RPC.SignRawTransactionRequest;
using Newtonsoft.Json;
using static ArouseBlockchain.P2PNet.NetStake;

namespace ArouseBlockchain.Solve
{
    /// <summary>
    /// 挖矿服务，用于创建区块[实施权益证明共识并进行块状态的创建和广播]
    /// 区块链上的交易只有被验证才能确定和记录在区块中，而挖矿就是一种以验证区块链上的交易为目的，最终创建区块，并且记录在区块链
    ///     上，这样会给予交易确定一定的缓冲时间，避免一些非法交易进行，并且最终给予矿工一定奖励，激励更多人参与区块链的生态和P2P网络。
    ///
    /// A: Proof of work (PoW)工作量证明
    ///     比特币的 PoW 算法中，挖矿的出块频率大约为每 10 分钟一次
    ///     可以理解为就是给你一个账户和密码，你通过反复用不同的数来解锁这个账号。最终就是谁算的块，算的多谁权利大。
    /// B: Proof of stake (PoS)权益证明
    ///     在以太坊的 PoS 算法 Casper 中，每个验证节点的出块频率大约为每 6 秒钟一次
    ///     通过为权益抵押的方式，就是你用一些社区币去用来进行抵押，就和贷款一样，你资产抵押的越多，你回报的就越多，优势是抛弃了那种PoW需要昂贵的成本的方式，且更具方便谁去运营。
    ///     挖矿规则：
    ///     1. 用户抵押社区币从而获得相关价值收益（本质目的是希望更积极参与的用户更进一步的提高积极性，和往常的运营目的一样，所以社区币不只是局限与币这一个概念，
    ///          也可以是工作量、绩效、最终成绩等等最终用于衡量结果的数值）
    ///     2. 抵押的社区币在一个周期后会被销毁，不可领取
    ///     3. 当社区币到达一定数量，可以用来兑换其他对等物品。
    ///     
    /// C: Proof of Authority(POA)权威证明
    ///     通过某种方式获得授权或认证来参与生成新的数据块，并获得相应的奖励。这种方式可以提高区块链网络的效率和稳定性，但也可能损害区块链网络的去中心化和透明性。
    ///
    /// 由于此项目只是教育性质项目，所以，不做挖矿/抵押/权威的相关工作，介于产品不完善，用户量不会大，目前采取的方式所有用户都有创建块的权利，并且条件不像三个这么复杂，
    ///     条件只是谁的交易池新增10条，并且上线次数多，就有机会构建区块。
    ///     
    /// 区块链周期分为区块链大周期，过了这个周期所有质押清空。以及抽签小周期，用来选择幸运儿来进行创建块。目前程序是自动质押，所以的两个周期是一样的，
    /// </summary>
    public class MintingSolve: ISolve
    {
        private CancellationTokenSource cancelTask;
        //是否已经抵押权益
        private bool isAlreadyStaking;
        //是否正在创建块
        private bool isMakingBlock;
        //随机值
        private readonly Random rnd;


        public MintingSolve ()
        {
            rnd = new Random();
            isAlreadyStaking = true;
            isMakingBlock = true;
        }

        public async UniTask<bool> Start(CancellationTokenSource _DCTS)
        {
            if (cancelTask != null) Stop();
            cancelTask = new CancellationTokenSource();
           // cancelTask.CancelAfterSlim(TimeSpan.FromSeconds(10)); //不设置超时

            // 这里运行的是默认的权益证明，并且是自动生产和质押权益，用来展示质押工作的过程
            // 在实际区块链中，用户需要通过网站或者APP进行实际的权益抵押
            // 在这个自动权益过程中,暂时还没有去做用户自主的质押
            // 也没有对等端的验证工作

            //Pow挖矿
            //WokrMintingBlock(); POW 挖矿，太费了，这里不运行，制作教学

            SolveManager.Wallets.OnSelectAccount += OnSelectAccount;

           

            return true;
        }

        public void Stop()
        {
            cancelTask?.Cancel();
            cancelTask?.Dispose();
            SolveManager.Wallets.OnSelectAccount -= OnSelectAccount;
            // Console.WriteLine("Minter has been stopped");
        }

        //当用户确定账号，则进行出块权益抽签
        async void OnSelectAccount(Account account)
        {
            //Pos质押
            //自动质押
            UniTask.Void(() => AutoStakeBalance());

            await UniTask.DelayFrame(10);

            //进行区块权益抽签
            UniTask.Void(() => StakeMintingBlock());
        }

        /// <summary>
        /// 工作量证明的挖矿
        /// 原理是：给予一串字符(题目)，通过计算字符的Hash(答案)，当Hash的前N位都是0(答案正确)，则挖矿成功，成功后下一个人会沿用你的随机数(题目的困难度)值再次计算。当其他人拿到你的答案后，同样的方式验证一下即可。
        /// 其中困难度需要根据全网算力调整，挖矿的算力多速度越快，难度增减，否则减少。目的是不需要时间控制的情况下固定时间生成一个块。
        /// 难度的计算公示：
        ///     1. 假设一个区块计算从挖矿到成块我们希望是10分钟。
        ///     2. 那100个区块就是 1000分钟。
        ///     3. 那我们就通过计算 100个区块的平均成块时间是否是10分钟，如果低则增加难度，如果高则减少难度。
        ///     4. 这样确保难度能让全网成块的时间平均在10分钟左右。
        ///     
        /// 注：由于POW不实用与其他业务和产品，这里仅仅做演示，没有实际运行过
        /// 其中，困难度difficulty、nonce 随机数、这些都是需要成块的时候放入块数据的，别人也需要通过这些内容再次hash来验证你的hash是否对的，从而判断是否是争取的块。
        /// </summary>
        async UniTaskVoid WokrMintingBlock()
        {
            var accoutAddress = SolveManager.Wallets.GetCurrAddress;

            var minterAddress = accoutAddress;
            var minterBalance = AUtils.BC.GetTotalAmountByAccount(SolveManager.BlockChain.WalletAccount.GetByAddress(accoutAddress));
            var balance = JsonConvert.SerializeObject(minterBalance);

            var startTimer = AUtils.NowUtcTime.Millisecond;

            var lastBlock = DBSolve.DB.Block.GetLast();
            var poolTransactions = SolveManager.BlockChain.TransactionPool.GetAllRecord();

            int remainCount = 100;
            int HopeCycle = 10; // 10分钟 仅演示，实际这里需要转化为10分钟的时间戳
            var lastHeight = Math.Max(0, lastBlock.height- remainCount); 
            var blocks = SolveManager.BlockChain.Block.GetRemaining(lastHeight, remainCount); //取最后的100个块
            long totleTime = 0;
            blocks.ForEach(x => totleTime = x.build_time + totleTime);
            float actual_averageTime = totleTime / (blocks.Count * HopeCycle);  //HopeCycle; // 计算得到的成块的平均时间，其中blocks.Count 最大不过 remainCount = 100

            //假设期望是10分钟产生一次块
            int difficulty = Math.Min(lastBlock.difficulty, 1) * Math.Min((int)Math.Round(actual_averageTime) ,1); //保证苦难度最小是1
            int nonce = rnd.Next();

            var last_block_hash = lastBlock.hash;
            var curr_transaction_list = JsonConvert.SerializeObject(poolTransactions);

            //难题计算规则：将前一区块hash和当前交易列表和难题拼接，进行sha256哈希计算,（我自己添加了困难度、随机值在里面，可以增加一定的破解难度）
            var guess = ('0' * difficulty) + nonce + last_block_hash + curr_transaction_list;
               
            var proof = lastBlock.proof; //一个不断增进的数值，作用是每次循环，不断尝试不同的哈希

            while (true)
            {
                proof += 1;
                if (AUtils.BC.Valid_Proof(last_block_hash, curr_transaction_list, difficulty, nonce, proof))
                {
                    break;
                }

                var isCanale = await UniTask.DelayFrame(500, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow(); //让CPU缓一下
                if (isCanale) return;
            }

            // 当得出结果后，创建区块

            await SolveManager.BlockChain.Block.Build(startTimer, minterAddress, balance, difficulty, nonce, proof, cancelTask.Token);
           
        }



        /// <summary>
        /// 抵押方式的挖矿：（通常POS机制各有不同，没有很统一的标准）
        /// 由于PoS质押和PoW挖矿不同：
        /// 1. PoS是谁投入的多就有权利，所以理论上有一个机制去评判谁投入的多，并在这个机制需要去记录投入
        /// 2. 而挖矿是就和考试一样，谁先做完题了就可以离场，若以不需要记录，主要最后验证答案，对了则离场，其他人在基础上重新答新卷。
        /// 3. 实际的 PoS 系统中，还需要考虑到节点加入和退出的情况，以及可能存在的欺诈行为等问题，比如需要 锁定期（bonding period）、罚款（slashing）等等。这里不做赘述。
        /// 最终选择逻辑：
        /// 1. 根据Peer节点的权益值进行随机选择
        /// 2. 根据Peer节点的权益值大小选择对大的节点
        /// 
        /// 1.用户的每次上线，会累计他的在线时长
        /// 2.在和其他Peer同步完状态后。
        /// 3.由于是自动质押和挖矿，所以在本地交易池数量超过10，再触发挖矿机制
        /// 4.当全链统一时间在45 - 60秒的时候进行一次检测，确定谁质押的多，则它进行区块的创建。
        /// 5.创建块成功之后，所有的质押和挖矿，从新开始
        /// </summary>
        async UniTaskVoid StakeMintingBlock()
        {
            if (AUtils.IRS) return;//非客户端不去同步


            var accoutAddress = SolveManager.Wallets.GetCurrAddress;
            if (string.IsNullOrEmpty(accoutAddress))
            {
                ALog.Error("不能抽签，地址不能为空 {0},请选择一个账号", accoutAddress);
                return;
            }

            isMakingBlock = true; //是否进行出块权益抽签

            ALog.Log("= = = = = = = = = = = = 开启出块权益抽签 = = = = = = = = = = = =");
            var isCanceled = false;
            while (true)
            {
                if (string.IsNullOrEmpty(accoutAddress)) continue;

                var timeMinting = AUtils.NowUtcTime;

                var _Minute = timeMinting.Minute;
                if(_Minute%2 >0)
                {
                    //让质押和抽签不在同一分钟运行
                    //抽签在偶数分钟内
                    //可以优化网络，防止一时间突然间的网络数据请求爆发
                    //确保了区块链的成块周期至少在一分钟到两分钟内。
                    ALog.Log("抽签 将 等待 {0} ",(61 - timeMinting.Second));
                    isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(61 - timeMinting.Second), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow();
                    if (isCanceled) return;
                    continue;
                }

                if (timeMinting.Second < 3)
                {
                    isMakingBlock = false;
                }

                //Second(1-60): 0-3: 停止； 3-45: 等待自动质押并广播数据； 45-60:进行一次出块权益抽签（挖矿）

                if (!isMakingBlock && timeMinting.Second >= 45)
                {
                    isMakingBlock = true;

                    ALog.Log("- 开始抽签 Time: {0}", timeMinting.Second);

                    var poolTransactions = SolveManager.BlockChain.TransactionPool.GetAllRecord();
                    var stakeList = DBSolve.DB.Stake.GetALL();

                    //目前条件比较宽松
                    if(poolTransactions != null && poolTransactions.Count > 2 && stakeList != null && stakeList.Count >= 2)
                    {
                        foreach (var stake in stakeList)
                        {
                            ALog.Log("用户：{0} ； 质押权益= {1}", stake.address, stake.amount);
                        }

                        //选择可以构建区块的幸运Peer
                        int index = SelectCreater(stakeList);

                        //给这个验证人发送验证信息，经过验证通过后再构建区块，假设验证通过
                        bool isSuccess = false;

                        if (index > 0 && accoutAddress == stakeList[index].address)
                        {
                            //选择一个验证的人
                            int index2 = SelectValidator(stakeList);
                            //给这个验证人发送验证信息，经过验证通过后再构建区块，假设验证通过


                            ///发送给验证人，征求验证，这里只写出来仅做示意，没有测试过
                            ///(抱歉，没太多时间，还得找工作苟且，已经一个月时间过去了，得尽快完成这个项目的初版，未来看热度，好的话再完善)
                            //NetStakeClient client = new NetStakeClient();
                            //client.ValidatorCRequest(xxx, xxx);
                            isSuccess = true;//直接验证成功


                            if (index2 > 0 && isSuccess)
                            {
                                ALog.Log("-- Good, 我来构建下一个区块");
                                if (cancelTask.IsCancellationRequested) return;

                                await SolveManager.BlockChain.Block.Build(timeMinting.Second, stakeList[index], cancelTask.Token);
                            }
                            else
                            {
                                ALog.Log("-- ???, 你的构建错误，原因....");
                                //执行惩戒措施
                            }
                        }
                        else
                        {
                            ALog.Log("-- 可惜了, 这个时间我不是幸运儿，不能构建区块. \n");
                        }
                        //清空所有质押，等待用户质押，并等待下次抽签
                        //可以按需，只清楚成块得奖励的用户
                        DBSolve.DB.Stake.DeleteAll();
                        ALog.Log("= = = = 抽签结束，幸运儿是 {0} = = =", (isSuccess) ? stakeList[index].address : "没人");

                    }
                    else
                    {

                        ALog.Log("没有足够的 质押数据= {0} 或者 交易数据= {1},等待....", stakeList?.Count, poolTransactions?.Count);
                    }

                   
                }

                isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow();
                if (isCanceled) return;
            }

            //选择谁来出块
            //根据节点质押的出块权益越多，约有几率获取出块的权利
            //这个过程叫出块权益抽签（Block Production Lottery）
            int SelectCreater(List<Stake> stakeWeights)
            {
                // 计算所有节点权益总和
                double totalStakeWeight = 0;
                stakeWeights.ForEach(
                    x => totalStakeWeight = totalStakeWeight + x.amount);

                // 生成随机数
                int randomNumber = rnd.Next((int)totalStakeWeight);

                // 选择验证节点
                double threshold = 0;
                for (int i = 0; i < stakeWeights.Count; i++)
                {
                    threshold += stakeWeights[i].amount;
                    if (randomNumber < threshold)
                    {
                        ALog.Log($"Node {i + 1} produced the block.");
                        return i;
                    }
                }

              
                return -1;
            }


            //选择谁来验证块的创建
            //尽可能随机的去选择一个节点去验证，而不是让过半数的节点去验证
            int SelectValidator(List<Stake> stakeWeights)
            {
                // 计算所有节点权益总和
                double totalStakeWeight = 0;
                stakeWeights.ForEach(
                    x => totalStakeWeight = totalStakeWeight + x.amount);

                // 生成随机数
                double randomNumber = rnd.NextDouble();

                // 选择验证节点
                double threshold = 0;
                for (int i = 0; i < stakeWeights.Count; i++)
                {
                    threshold += stakeWeights[i].amount / totalStakeWeight;
                    if (randomNumber < threshold)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// 每个节点在网络中拥有一定数量的代币或权益，这些代币或权益可以被视为该节点的出块权益。
        /// 通常会有代币的加入，它相当于APP的积分，增减由社区运营人员运维，然后用户去进行活动来获取代币
        /// (本质目的是希望更积极参与的用户更进一步的提高积极性，和往常的运营目的一样，所以社区币不只是局限与币这一个概念，也可以是工作量、绩效、最终成绩等等最终用于衡量结果的数值)
        /// 以运营为目的，所以这里抵押的就是用户的在线时长
        /// 1. 去自动抵押社区币（社区币：一种激励代币，无限提供，用于鼓励用户积极参与进而提升社区活跃度，通过可以通过玩游戏，聊天、完成任务、创作内容、参与活动等等获取）
        /// 2. 抵押币在获取实际奖励之后，会进行销毁。
        /// </summary>
        async UniTaskVoid AutoStakeBalance()
        {
            if (AUtils.IRS) return;//非客户端不去同步

            DBSolve.DB.Stake.DeleteAll();//清楚缓存的质押记录

            var accoutAddress = SolveManager.Wallets.GetCurrAddress;

            if (string.IsNullOrEmpty(accoutAddress))
            {
                ALog.Error("不能质押，地址不能为空 {0},请选择一个账号", accoutAddress);
                return;
            }

            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(accoutAddress);
            if (account.Equals(null))
            {
                ALog.Error("没有找到这个地址{0}的对应账号 {1},请确定创建了", accoutAddress, account);
                return;
            }

            //目前这种方式验证不公平，正式项目切记不可。不然每次用户启动都会有一大权益
            account.tokens = rnd.Next(10, 400);// 默认给予随机的质押股权

            account = SolveManager.BlockChain.WalletAccount.UpdateAccount(account);
            //没有任何 代币的不能质押,实际中还需要加入在线时长，在线年龄或者其他贡献度等等
            if (account.tokens == 0)
            {
                ALog.Log("账号的权益为 {0} ，无法质押，多努力参与社区活动。");
                return;
            }

            ALog.Log("= = = = = = = = = = = = 开启自动质押权益 = = = = = = = = = = = =");

            bool isCanceled = false;
            while (true)
            {
                var timeStaking = AUtils.NowUtcTime;

                var _Minute = timeStaking.Minute;
                if (_Minute % 2 != 0)
                {
                    //让质押和抽签不在同一分钟运行
                    //质押在奇数分钟内
                    ALog.Log("质押将 等待 {0} ", (61 - timeStaking.Second));
                    isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(61-timeStaking.Second), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow();
                    if (isCanceled) return;
                    continue;
                }

                if (timeStaking.Second < 3)
                {
                    isAlreadyStaking = false;

                    ALog.Log("01 准备自动随机质押：{0}", timeStaking.Second);
                    isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(4), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow();
                    if (isCanceled) return;

                    timeStaking = AUtils.NowUtcTime;
                }

                //质押股权将暂时设置在固定的时间内，即每分钟的4到30秒内。实际项目中，应该由用户决定时间和数量
                if (!isAlreadyStaking && timeStaking.Second < 30)
                {
                    account = SolveManager.BlockChain.WalletAccount.GetByAddress(accoutAddress);

                    ALog.Log("02 开始自动随机质押：{0}", timeStaking);
                    PledgedStake(account);

                    isAlreadyStaking = true;
                }

                isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate, cancelTask.Token).SuppressCancellationThrow();
                if (isCanceled) return;
            }
        }

        /// <summary>
        /// 用户通过手动操作来质押权益
        /// 这里会自动进行,并且质押后不能取出
        /// </summary>
        public void PledgedStake(Account account, double stake_token = -1)
        {
            var curr_hour = AUtils.NowUtcTime.Hour;
            var hour12 = Math.Max(curr_hour % 12, 1) ;//让可以持久些
            var auto_stake_tokens = rnd.NextDouble() * account.tokens * (hour12 / 12f);

            var amount = (stake_token < 0) ? auto_stake_tokens : stake_token;
            ALog.Log("想质押 {0} 权益，总权益 {1} ,如成功剩余 {2} , 时间平滑 {3}", amount, account.tokens, (account.tokens - amount), (hour12 / 12f));
            var relust = SolveManager.BlockChain.WalletAccount.CutTokensByAddress(account.address, amount);
            var fristBlock = DBSolve.DB.Block.GetFirst();

            if (relust.tokens < 0)
            {
                ALog.Log("想质押 {0} 没有足够的权益 {1}，请多在社区中积极活跃", amount, relust.Item2.tokens);
                return;
            }
            if (string.IsNullOrEmpty( relust.Item2.address))
            {
                ALog.Error("没有这个账号，请先同步账号信息");
                return;
            }

            // 计算节点的币龄系数 , 这里先去掉，实际项目需要复杂的计算，目的是激励社区活跃度
            // double coinAgeWeight = (account.updated - account.created) / fristBlock.time_stamp;

            //计算最终的节点出块权益
            double stakeWeight = relust.tokens;// * coinAgeWeight;

            var stake = new Stake
            {
                address = account.address,// SolveManager.Wallet.GetAddress(),
                amount = stakeWeight,
                time_stamp = AUtils.NowUtcTime_Unix
            };

            if (cancelTask.IsCancellationRequested) return;

            stake = DBSolve.DB.Stake.AddOrUpdate(stake);
            ALog.Log("--- 我质押的权益明细： {0} ", stake.ToString());

            ///广播质押信息
            NetStakeClient client = new NetStakeClient();
            client.AddCBroadcast(account, stake);
        }



    }
}


