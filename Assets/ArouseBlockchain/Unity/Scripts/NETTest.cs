using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ArouseBlockchain;
using ArouseBlockchain.Common;
using ArouseBlockchain.Solve;
using EasyButtons;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using UnityEngine;
using static ArouseBlockchain.Common.AUtils;
using ArouseBlockchain.P2PNet;
using static ArouseBlockchain.P2PNet.NetTransaction;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Net.Sockets;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Net.NetworkInformation;
using System.Collections;
using Ping = System.Net.NetworkInformation.Ping;
using UnityEngine.LowLevel;
using System.Reflection;
using Unity.VisualScripting;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;

[ExecuteInEditMode]
public class NETTest : MonoBehaviour
{
    public void Start()
    {

    }

    [GradientUsage(true)] //添加HDR类型的颜色条
    public Gradient HDRColor;


    [Button("删除缓存助记词")]
    public void DelectMnemonic() {

        //string prefkey = "&Mnemonic&";
        //PlayerPrefs.DeleteKey(prefkey);

        //IPEndPoint remote1 = IPResolver.TryParse("www.baidu.com");
        //IPEndPoint remote2 = IPResolver.Resolve("127.0.0.1:4488");

        //IPEndPoint remote3 = IPResolver.TryParse("10.217.80.84:44824");
        //IPEndPoint remote4 = IPResolver.Resolve("10.217.80.84:44824");

        //IPEndPoint remote5 = IPResolver.TryParse("54.255.130.228:44824");
        //IPEndPoint remote6 = IPResolver.Resolve("54.255.130.228:44824");


        //ALog.Log("IP {0} ; {1} ; {2}; {3}; {4}; {5}",
        //     remote1, remote2, remote3, remote4, remote5, remote6);


        //var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

        //foreach (var header in playerLoop.subSystemList)
        //{
        //    Debug.LogFormat("------{0}------", header.type.Name);
        //    foreach (var subSystem in header.subSystemList)
        //    {
        //        Debug.LogFormat("{0}.{1}", header.type.Name, subSystem.type.Name);
        //    }
        //}


        // Get and display the full name of the EXE assembly.

        // Construct and initialize settings for a second AppDomain.

        //23/12

        //var curr_hour = AUtils.NowLocalTime.Hour;

        //for (int i = 0; i <= 24; i++)
        //{
        //    var hour12 = i % 12;
        //    ALog.Log("当前时间 {0}， 12小时制 = {1} , 占比 {2}", i, hour12 , hour12/12f);
        //}

        var prefix = '0' * 1;
        var guess = "000001dadadadadad";

        //ALog.Log("AppDomain1 {0} ", (AUtils.NowLocalTime > NowUtcTime));
        //ALog.Log("AppDomain2 {0} ; {1} ", AUtils.NowLocalTime, NowUtcTime);
        //ALog.Log("AppDomain3 {0} ", (NowLocalTime_Unix > NowUtcTime_Unix));
        //ALog.Log("AppDomain4 {0} ; {1} ", NowLocalTime_Unix, NowUtcTime_Unix);

        //ALog.Log("AppDomain5 {0} ; {1} ", AUtils.ToDateTimeByLocalUnix(NowLocalTime_Unix), AUtils.ToDateTimeByUTCUnix(NowUtcTime_Unix));
        //ALog.Log("AppDomain5 {0} ; {1} ", AUtils.UnixLoaclTimeToString(NowLocalTime_Unix), AUtils.UnixUTCTimeToString(NowUtcTime_Unix,true));

       // Block ddd = new Block();
        //int size = SizeOf(new Block());
        //ALog.Log("AppDomain1 {0} ", sizeof(ddd));


        MESSatus mesSatus = MESSatus.STATUS_ERROR_BAD | MESSatus.STATUS_ERROR_NOT_FOUND;
        ALog.Log("错误状态 {0} ", mesSatus);
       

        MESSatus mesSatusSucc = MESSatus.STATUS_ERROR;
        ALog.Log("是否成功 {0} , 成功内容 {1} ", ( (mesSatusSucc & MESSatus.STATUS_ERROR)) , mesSatusSucc);
      
    }


    [Button]
    public  void ScreenXXX()
    {
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 screen2 = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        float widthRadio = (screen.x / 1440f);
        float hightRadio = (screen.y / 2960f);
       
        //match = 0; widthRadio 5进行缩放
        //match = 1；hightRadio 8进行缩放

        // (widthRadio)
        Debug.Log("screen Resolution  : " + screen2 + "  ; screen: " + screen);
        //X :[0-1]  Y :(4, 10) ;  0:4 , 1:10,  0.5:7
        // y = (10-4)x+4
        // scale = (widthRadio - hightRadio) * match + hightRadio;

        float target = ((widthRadio - hightRadio) * 0.5f + hightRadio);

        float match =  (target - hightRadio) / (widthRadio - hightRadio);

        Debug.Log("widthRadio  : " + widthRadio + " : hightRadio: " + hightRadio + " : target : " + target + " ; match : " + match);
    }


    [Button]
    public void dd()
    {
        //CryptoRSA rsa = new CryptoRSA();
        //Debug.Log("PrivateKey = " + rsa.ExportPrivateKey());
        //Debug.Log("PublicKey = " + rsa.ExportPublicKey());

        var my_bing_adrs = P2PManager.GetNetHost.GetBindAddress();
        var my_local_adrs = P2PManager.GetNetHost.GetLocalAddress();
        var my_loop_adrs = P2PManager.GetNetHost.GetLoopbackAddress();

        ALog.Log("IP协议 BindAddress:{0} ; LocalAddress:{1} ; LoopbackAddress{2}",
              my_bing_adrs.AddressFamily,
              my_local_adrs.AddressFamily, my_loop_adrs.AddressFamily);

        if (my_bing_adrs.AddressFamily == AddressFamily.InterNetworkV6)
        {
            my_bing_adrs = new IPEndPoint(my_bing_adrs.Address.MapToIPv4(), my_bing_adrs.Port);
            my_bing_adrs = IPResolver.Resolve(my_bing_adrs.Address.ToString(), my_bing_adrs.Port);

            my_local_adrs = new IPEndPoint(my_local_adrs.Address.MapToIPv4(), my_local_adrs.Port);
            my_local_adrs = IPResolver.Resolve(my_local_adrs.Address.ToString(), my_local_adrs.Port);

            my_loop_adrs = new IPEndPoint(my_loop_adrs.Address.MapToIPv4(), my_loop_adrs.Port);
        }
       // ALog.Log("本地Host:" + my_bing_adrs + " ; " + my_local_adrs);

         ALog.Log("Host启动 BindAdd:{0} ; Local:{1} ; Loopback{2}",
                my_bing_adrs.ToString(),
                my_local_adrs.ToString(), my_loop_adrs.ToString() );
    }

    //[Button]
    //public void QuickPiNetTrans(int count = 10)
    //{
    //    for (int i = 0; i < count; i++)
    //    {
    //        TestNetTrans();
    //    }
    //}

    CancellationTokenSource _DCTS = new CancellationTokenSource();

    private void OnDisable()
    {
        _DCTS?.Cancel();
        _DCTS?.Dispose();
    }


    Peer GetNewHostPeerClient() {

        //ANetworkHost host1 = P2PManager.GetNetHost;

        ANetworkHost testHost = GetComponent<ANetworkHost>();

        ALog.Log("----isListening: {0}", testHost.Listening);

        HostEvents hostEvents = new HostEvents();
        PeerEvents peerEvents = new PeerEvents();

        hostEvents.OnReceiveRequest += (request, message) => {
            ALog.Log("----01 isListening: {0}", request?.Peer.Remote);
        };

        hostEvents.OnReceiveSocket += (remote, buffer, length) => {
            ALog.Log("----02 isListening: {0}", length);
        };

        peerEvents.OnConnect += (peer) =>
        {
            ALog.Log("----03 isListening: {0}", peer?.Remote);
        };
        peerEvents.OnReceive += (peer, reader, info) => {
            ALog.Log("----04 isListening: {0}", peer?.Remote);
        };

        var ress = new TransactionListRequest()
        {
            PageNumber = 1,
            ResultPerPage = 10
        };


        IPEndPoint hostaddress = IPResolver.TryParse("127.0.0.1:44824");
        testPeer = testHost.Connect(hostaddress, peerEvents, ress, true);

        return testPeer;
    }




    [Button("获取服务器Node状态")]
    public async UniTaskVoid GetServerNodeState() {

        if (testPeer == null) testPeer = GetNewHostPeerClient();

        var wait_result = await UniTask.WaitUntil(() => testPeer.Connected == true, PlayerLoopTiming.Update, _DCTS.Token).SuppressCancellationThrow();

        NodePeerClient freeNodeClient = new NodePeerClient(testPeer);
        NodeSyncStateResponse connect_resp = await freeNodeClient.NodeSyncStateRequest(SolveManager.BlockChain.NodePeer.m_NodePeer, _DCTS.Token);

        var isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.DeltaTime, PlayerLoopTiming.Update, _DCTS.Token).SuppressCancellationThrow();


        ALog.Log("4---------4 " + (connect_resp.NodePeers == null) + connect_resp.NodePeers?.Count);
        connect_resp._ReStatus.ToString();
        if (connect_resp.NodePeers != null)
        {
            AUtils.BC.PrintNodePeers(connect_resp.NodePeers);
        }
    }

    [Button("是否联网")]
    public bool UrlExistsUsingSockets()
    {
        string url = "http://120.26.66.97/";
        if (url.StartsWith("https://")) url = url.Remove(0, "https://".Length);
        try
        {
            System.Net.IPHostEntry ipHost = System.Net.Dns.GetHostEntry(url);// System.Net.Dns.Resolve(url);

           
           // ALog.Log("已联网; {0}" , NetworkInformation.);
            return true;
        }
        catch (System.Net.Sockets.SocketException se)
        {
            System.Diagnostics.Trace.Write(se.Message);
            Debug.Log("无联网");
            return false;
        }
    }

    //IEnumerator PingBaidu()
    //{
    //    Ping pingSender = new Ping();
    //    PingReply reply = pingSender.Send("www.baidu.com");
    //    while (reply.Status == IPStatus.)
    //    {
    //        yield return null;
    //    }
    //    if (reply.Status == IPStatus.Success)
    //    {
    //        Debug.Log("Address: " + reply.Address.ToString());
    //        Debug.Log("RoundTrip time: " + reply.RoundtripTime);
    //        Debug.Log("Time to live: " + reply.Options.Ttl);
    //        Debug.Log("Don't fragment: " + reply.Options.DontFragment);
    //        Debug.Log("Buffer size: " + reply.Buffer.Length);
    //    }
    //    else
    //    {
    //        Debug.Log("Ping failed: " + reply.Status);
    //    }
    //}



    Peer testPeer;
    [Button("测试异步获取服务器交易列表")]
    public async UniTaskVoid TestNetTrans()
    {
        if (testPeer == null) testPeer = GetNewHostPeerClient();
        BC.PrintNodePeers(SolveManager.BlockChain.NodePeer.m_NodePeer);

        var wait_result = await UniTask.WaitUntil(() => testPeer.Connected == true, PlayerLoopTiming.Update, _DCTS.Token).SuppressCancellationThrow();

        TransactionClient transactionClient = new TransactionClient(testPeer);
        TransactionListResponse request = await transactionClient.TransactionListRequest(1, 50,_DCTS);

        ALog.Log("4---------4 " + (request.Transactions == null) + request.Transactions?.Count);
        if (request.Transactions != null)
        {
            AUtils.BC.PrintTransactions(request.Transactions);
        }
    }

    [Button("全部NodePeer")]
    public void ShowAllNodePeer()
    {
        var allnodepeer = SolveManager.BlockChain.NodePeer.GetAll();
        AUtils.BC.PrintNodePeers(allnodepeer);

    }


    [Button]
    public void TTTime()
    {
        long ddd = Host.Now.GetStopwatchTicks();
        ALog.Log(" Host.Now : " + Host.Now.Elapsed + " : " + AUtils.ToDateTimeByUTCUnix(AUtils.NowUtcTime_Unix));

    }

    [Button]
    public void DoShowCacheWallet()
    {
        var walletCard = SolveManager.Wallets.GetCurrWalletCard;
        var cardCount = SolveManager.Wallets.GetWalletCardCount;

        AUtils.BC.PrintWallet(walletCard);
        ALog.Log("当前有 " + cardCount + " 个钱包");
    }

    [Button]
    public void DoShowCacheWallet(int index)
    {
        var walletCard = SolveManager.Wallets.SelectUserWalletCard(index);
        var cardCount = SolveManager.Wallets.GetWalletCardCount;

        AUtils.BC.PrintWallet(walletCard);
        ALog.Log("当前有 " + cardCount + " 个钱包");
    }

    [Button]
    public void GetALLAccount()
    {
        List<Account> ddd =  SolveManager.BlockChain.WalletAccount.GetRange(1,10).ToList();

        foreach (var item in ddd)
        {
            AUtils.BC.PrintWallet(item);
        }
    }

    [Button("测试Node DB 更新")]
    public void TestAddAccount()
    {
       var nodes = SolveManager.BlockChain.NodePeer.StabilizePeersInCache();

        ALog.Log("nodes : " + nodes[0].ToString());

        nodes[0].node_state.version += 10;

        SolveManager.BlockChain.NodePeer.UpdateCache(nodes[0]);

        ALog.Log("nodes : " + nodes[0].ToString());

        string tempPeer = "127.0.0.1:48832";
        var alreayNode = nodes.Find(x => x.address_public == tempPeer);

        ALog.Log("nodesxx : " + alreayNode.ToString());

        if (!string.IsNullOrEmpty(alreayNode.address_public))
        {
            alreayNode.is_bootstrap = true;
            alreayNode.report_reach = AUtils.NowUtcTime_Unix;
            alreayNode.node_state.version += 10;

            ALog.Log("----更新中继节点：{0}", tempPeer);
            DBSolve.DB.Peer.Updaet(alreayNode);
        }
        else {
            ALog.Log("----保存中继节点：{0}", tempPeer);
            var newPeer = new NodePeer
            {
                address_public = tempPeer,
                // mac_address = AUtils.DeviceMacAddress,
                is_bootstrap = true,
                report_reach = AUtils.NowUtcTime_Unix,
                node_state = new NodePeer.NodeState()
            };
            DBSolve.DB.Peer.Add(newPeer);
        }

        ///删除测试
        ALog.Log("1 nodes : " + nodes[0].ToString());

        nodes[0].node_state.version += 10;

        SolveManager.BlockChain.NodePeer.ReplaceAll(nodes);

        ALog.Log("2 nodes : " + nodes[0].ToString());

    }

    [Button("搜索并尝试建立P2P")]
    public void DoSearchValidNode()
    {
        //P2PManager.Instance.SearchValidNode();
    }

    [Button("停止搜索P2P")]
    public void DoStopSearchNode()
    {
       // P2PManager.Instance.StopSearchNode();
    }


    //测试钱包：

    //[ALog] Your Wallet ：被 看 古 佛 躺 念 谈 争 搞 各 型 凝
    //ADDRESS: 6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh
    //PUBLIC KEY Hex: 71f3937602245be20c738ba1af70e5491773476a
    //PUBLIC KEY :02c33711236eed92cae4857ec3450429a185beee5a19bfbedbc2a848a77cca3920

    //[ALog] Your Wallet ：吧 涤 鸿 旨 愿 啥 荒 语 匀 徙 珠 置
    //ADDRESS: Fpe8Qv4MZxavc7Zgr3fRbYHg9FkNR5opcRteST3XQ8Tz
    //PUBLIC KEY Hex: b762d00313389231d4d34de3c1571d142bdaed4e
    //PUBLIC KEY :035d3b1cd8fd53050a56ffc486578fa1216047dcbbe8e01264fffdbf50cb5f09f6


    /// <summary>
    /// 去发送一笔交易
    /// </summary>
    /// <param name="recipient">收件人</param>
    /// <param name="strAmount">发送数量</param>
    /// <param name="strFee">手续费</param>
    [Button]
    public void DoSendCoin(int amount, int fee)
    {
        ABlockchain.Init.DoSendCoin("6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh", amount, fee);
    }

    [Button]
    public void DoSendOBJItem(string obj_hash, int fee) {

        ABlockchain.Init.DoSendOBJItem("6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh", obj_hash, fee);
    }
    /// <summary>
    /// 获取账号储仓中所有物品
    /// </summary>
    [Button]
    public void DoGetBalance(int index)
    {
        ABlockchain.Init.DoGetBalance(index);
    }

    /// <summary>
    /// 获取所有交易历史
    /// </summary>
    [Button]
    public void DoGetTransactionHistory()
    {
        ABlockchain.Init.DoGetTransactionHistory();
    }

    [Button]
    public void DoDeleteTransPoolInfo()
    {
        SolveManager.BlockChain.TransactionPool.DeleteAll();
    }



    [Button]
    public void DoShowWalletInfo()
    {
        ABlockchain.Init.DoShowWalletInfo();
    }


    [Button]
    public void DoShowHeadAndTailBlock()
    {
        ABlockchain.Init.DoShowHeadAndTailBlock();

    }

    [Button]
    public void DoShowAllBlocks()
    {
        var tail_block = DBSolve.DB.Block.GetAll().Query().ToArray();

        foreach (var item in tail_block)
        {
            AUtils.BC.PrintBlock(item);
           // var size = sizeof(item);
        }
       
    }


    //add :6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh
    //pri :xprv9vJsAzQpzCc9fKbPZKhxWEmapZVWuGXQV6q9bzTRVH9wj3jZQUvLeYZWxH17Ps9THhY5nbuhWru6bpRiC1wRXJvwqrTnZeHhzp1UGbaLJkd
    //pri :L5jNrGRfA53e5dJ7qD5ByhBW5qpe44GzBk4trVP7L7jDvDkAaMe9

    //add :Fpe8Qv4MZxavc7Zgr3fRbYHg9FkNR5opcRteST3XQ8Tz
    //pri :xprv9vHASbzpk3Zg8vedD7Cj1gawLWQHBmx4dKXjoUi8dmRXqjXvacad5H5zKJ4eeaqoy5SRn4nuTk161wrK7nw3x6CkGtuXHtd6YLvi79LqmCM
    //pri :L1wdHCD5yrtEPAPvnpjs8aTuaE6LV3rTtRWTMAxugbd3yK1dpB15

    /// <summary>
    /// 测试，文件名、打开时间、文件格式均无影响
    /// </summary>
    /// <param name="path"></param>
    ///

    public CancellationTokenSource _DCTS2 = new CancellationTokenSource();
    TimeoutController timeoutController;
    DateTime startTime = DateTime.Now;
    bool isttt = true;
    [Button("测试Task")]
    public async UniTaskVoid FileHash(int timer = 5)
    {
        //var hash = SHA256.Create();
        //var hash = MD5.Create();
        // KeyPair keyPair1 = new ArouseBlockchain.Solve.KeyPair();
        // keyPair1.PublicKey = new NBitcoin.PubKey("02c33711236eed92cae4857ec3450429a185beee5a19bfbedbc2a848a77cca3920");
        // keyPair1.PublicKeyHex = "71f3937602245be20c738ba1af70e5491773476a";

        //private void OnEnable()
        //{
        //    if (disableCancellation != null)
        //    {
        //        disableCancellation.Dispose();
        //    }
        //    disableCancellation = new CancellationTokenSource();
        //}

        //private void OnDisable()
        //{
        //    disableCancellation.Cancel();
        //}

        //private void OnDestroy()
        //{
        //    destroyCancellation.Cancel();
        //    destroyCancellation.Dispose();
        //}


        //CancellationTokenSource 主要功能就是用来控制Task的中途取消。
        //且取消之后CancellationTokenSource 不能再次使用，需要从新New
        //取消直接需要对之前不会再次使用的 CancellationTokenSource 进行Dispose
        //1. 比如不愿意等了，取消
        //2. 比如超时了直接取消
        //3.

        startTime = DateTime.Now;
        if (_DCTS2.IsCancellationRequested) {
            ALog.Log("isCanceled 。NEW NEW ");
            _DCTS2 = new CancellationTokenSource();
        }
        if(timeoutController == null) timeoutController = new TimeoutController(_DCTS2);

        timeoutController.Reset();
        CancellationToken timeout_token = timeoutController.Timeout(TimeSpan.FromSeconds(timer));
       


        CancellationToken Token = _DCTS2.Token;
        Token.Register(() => ALog.Log("取消了？？？"));
        Token.Register(() => ALog.Log("取消了！！！"));
        Token.Register(() => ALog.Log("取消了。。。贼") );

        var isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(10), DelayType.DeltaTime, PlayerLoopTiming.Update, timeout_token).SuppressCancellationThrow();

        //isCanceled 和抛出异常一样，可以阻挡Task取消后，代码还继续运行。

        // _DCTS2.Token.ThrowIfCancellationRequested();
      //  var isCanceled2 = await AsyncSearcPeerWhile().SuppressCancellationThrow();
        //ALog.Log("isCanceled : " + isCanceled);

        ALog.Log("2 isCanceled {0} , IStimeou {1}, timer{2}。。。。。 " , isCanceled, timeoutController.IsTimeout() , (DateTime.Now - startTime).Seconds);
      
    }


    async UniTask<bool> AsyncSearcPeerWhile() {

        isttt = true;
        while (true)
        {
            ALog.Log("isCanceled 。NextFrame ");
            if (!isttt) break;



          //  await UniTask.Yield();
            await UniTask.DelayFrame(100);

        }

        return isttt;

    }

    [Button("取消TokenSource")]
    public void Test1()
    {
        // _DCTS2.Dispose(); //处理释放，它不会影响目前没有取消还在进行的task，会影响你使用同一个CancellationToken。

        _DCTS2.Cancel(); //会直接取消目前在await的task，让其不等待，直接往下执行

        //如果已经取消了，调用这个方法就会抛出一个异常，并且不执行下面的代码，用来防止代码往后执行，
        //否则什么都没有
        _DCTS2.Token.ThrowIfCancellationRequested();

        var Token = _DCTS2.Token;
        ALog.Log(string.Format("IsCancellationRequested {0} ; CanBeCanceled {1}",

            //是否已经取消了,取消则True,否则false。不代表Task是否执行完
            Token.IsCancellationRequested,
            //查看是否Task可以取消
            // 一直为 true,暂时不知道用途
            Token.CanBeCanceled
            ));
    }

    [Button("展示TokenSource")]
    public void Test2()
    {
        var Token = _DCTS2.Token;
        isttt = false;

        ALog.Log(string.Format("IsCancellationRequested {0} ; CanBeCanceled {1}",
            Token.IsCancellationRequested,
            Token.CanBeCanceled
            ));
    }
}