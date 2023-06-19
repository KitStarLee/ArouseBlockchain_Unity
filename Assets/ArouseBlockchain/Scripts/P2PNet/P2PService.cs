using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Net;
using Cysharp.Threading.Tasks;
using SuperNet.Examples.Broadcast;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using UnityEngine;
using static SuperNet.Netcode.Transport.PeerEvents;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

namespace ArouseBlockchain.P2PNet
{
    /// <summary>
    /// P2P服务基础，仅仅用来和其他节点进行通信（不对信息进行处理）
    /// 基础逻辑：
    /// 1. A 发送请求去中继服务，获取户群(peers)数据 [每个peer可以有自己区块Node数据]
    /// 2. 获取后进行区块数据同步
    /// 3. B接受到数据同步请求，进行数据比对，已有的数据将主动断开A的链接，否则更新自己数据
    /// </summary>

    //#region//CallBack
    //public delegate void CallBack();
    //public delegate void CallBack<in T>(T arg);
    //public delegate void CallBack<in T, in X>(T arg1, X arg2);
    //public delegate void CallBack<in T, in X, in Y>(T arg1, X arg2, Y arg3);
    //public delegate void CallBack<in T, in X, in Y, in Z>(T arg1, X arg2, Y arg3, Z arg4);
    //#endregion



    public class P2PService
    {
        private ANetworkHost _Host;

        Dictionary<Peer, List<ValueTuple<byte, Action<Peer, Reader>>>> TransactionWaitePool = new Dictionary<Peer, List<ValueTuple<byte, Action<Peer, Reader>>>>();

        //Unconnecte
        Dictionary<IPEndPoint, List<ValueTuple<int, byte, Action<IPEndPoint, Reader>>>> UnconnecteMesWaitePool = new Dictionary<IPEndPoint, List<ValueTuple<int, byte, Action<IPEndPoint, Reader>>>>();
        // Dictionary<IPEndPoint, (byte mesType, int timer)> UnconnecteMesWaiteTimer = new Dictionary<IPEndPoint, (byte mesType, int timer)>();

        PeerEvents peerEvents = null;

        public event OnConnectHandler OnConnect;
        public event OnDisconnectHandler OnDisconnect;
        public event SuperNet.Netcode.Transport.PeerEvents.OnExceptionHandler OnException;



        private Process parentProcess;
        public P2PService()
        {
            if (AUtils.IRS)
            {
                string parentProcessName = System.IO.Path.GetFileName(System.Environment.CommandLine);

                // 获取父进程的句柄
                parentProcess = Process.GetProcessesByName(parentProcessName).FirstOrDefault();

                ALog.Log("!!! Parent process is null={0} ； ID= {1} ", (parentProcess == null), parentProcess?.Id);


                if (parentProcess != null)
                {
                    WaitKill();
                }
                else
                {

                    ALog.Error("！！！应该使用父进程来控制服务，不然无法彻底杀死进程！！！");

                }
            }

        }

        public void Init(ANetworkHost host)
        {
            _Host = host;


            peerEvents = _Host.PeerEvents;
            peerEvents.OnConnect += OnPeerConnect;
            peerEvents.OnReceive += OnPeerReceive;
            peerEvents.OnUpdateRTT += OnPeerUpdateRTT;
            peerEvents.OnDisconnect += OnPeerDisconnect;
            peerEvents.OnException += OnPeerException;


            //  _Host.HostEvents.OnReceiveSocket += OnHostReceiveSocket;
            _Host.HostEvents.OnReceiveUnconnected += OnHostReceiveUnconnected;
            _Host.HostEvents.OnReceiveBroadcast += OnHostReceiveBroadcast;
            _Host.HostEvents.OnReceiveRequest += OnHostReceiveRequest;
            _Host.HostEvents.OnException += OnHostException;
            _Host.HostEvents.OnShutdown += OnHostShutdown;




        }

        Action _write_close_action;
        async void WaitKill()
        {
            while (true)
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                foreach (var p in processes)
                {
                    //  Console.WriteLine("Unity 全部进程--- Process : HasExited {0} ; ID {1} ; Same : {2}!!!", p.HasExited, p.Id, (p.Id == currentProcess.Id));

                    if (currentProcess != null && currentProcess.HasExited)
                    {
                        if (p != null && !p.HasExited)
                        {
                            // ALog.Log("!!! Unity 本身进程 {0} Parent 关闭了 整个服务开始退出 ", p.Id);
                            p.Kill();

                        }
                    }
                }

                if (parentProcess != null && parentProcess.HasExited)
                {
                    // ALog.Log("!!! Parent 关闭了 整个服务开始退出 ");

                    Application.Quit();
                    return;
                }

                await Task.Delay(200);

            }
        }




        public void Shutdown()
        {
            if (TransactionWaitePool != null && TransactionWaitePool.Count > 0)
            {
                foreach (var trans in TransactionWaitePool.Values)
                {
                    if (trans != null && trans.Count > 0)
                    {
                        foreach (var act in trans)
                        {
                            act.Item2.Invoke(null, null);
                        }
                    }
                }
            }

            TransactionWaitePool.Clear();

            if (UnconnecteMesWaitePool != null && UnconnecteMesWaitePool.Count > 0)
            {
                foreach (var item in UnconnecteMesWaitePool.Values)
                {
                    if (item != null && item.Count > 0)
                    {
                        foreach (var act in item)
                        {
                            act.Item3.Invoke(null, null);
                        }
                    }
                }
            }
            UnconnecteMesWaitePool.Clear();
            // UnconnecteMesWaiteTimer.Clear();


            _Host.Shutdown();

            peerEvents.OnConnect -= OnPeerConnect;
            peerEvents.OnReceive -= OnPeerReceive;
            peerEvents.OnUpdateRTT -= OnPeerUpdateRTT;
            peerEvents.OnDisconnect -= OnPeerDisconnect;
            peerEvents.OnException -= OnPeerException;


            // _Host.HostEvents.OnReceiveSocket -= OnHostReceiveSocket;
            _Host.HostEvents.OnReceiveUnconnected -= OnHostReceiveUnconnected;
            _Host.HostEvents.OnReceiveBroadcast -= OnHostReceiveBroadcast;
            _Host.HostEvents.OnReceiveRequest -= OnHostReceiveRequest;
            _Host.HostEvents.OnException -= OnHostException;
            _Host.HostEvents.OnShutdown -= OnHostShutdown;



            ////通知所有的Peer断开链接
            //foreach (Peer peer in _Host.GetPeers())
            //{
            //    if (peer != P2PManager._P2PRelay.Peer)
            //    {
            //        peer.Disconnect();
            //    }
            //}


            //_Host.Dispose();


            ALog.Log("--- Host Server Stop ----");
        }


        /// <summary>
        /// 用于本地局域网找玩家
        /// </summary>
        public void SendBroadcast()
        {
            // If host not yet started, create one
            if (_Host == null || !_Host.Listening)
                ALog.Log("[P2PService :] _Host is Null "!);

            _Host.SendBroadcast(_Host.HostConfiguration.Port, new BroadcastMessage());
        }


        List<Peer> _PeerEventsList = new List<Peer>();
        public MessageSent SendAndAwait(Peer to_Peer, SuperNet.Netcode.Transport.IMessage message, (byte mesType, Action<Peer, Reader> OnReciveAndResponse) ListenerTransaction)
        {
            for (int i = _PeerEventsList.Count - 1; i >= 0; i--)
            {
                if (!_PeerEventsList[i].Connected && !_PeerEventsList[i].Connecting)
                {
                    _PeerEventsList[i].SetListener(null);
                    _PeerEventsList.RemoveAt(i);
                }
            }

            if (!_PeerEventsList.Contains(to_Peer)) to_Peer.SetListener(peerEvents);

            bool isSened = TransactionWaitePool.ContainsKey(to_Peer);
            bool isHaveWait = false;
            if (isSened)
            {
                // foreach (var item in TransactionWaitePool.Keys)
                // {
                var waiteList = TransactionWaitePool[to_Peer];
                if (waiteList.Count > 0)
                {
                    for (int i = 0; i < waiteList.Count; i++)
                    {
                        isHaveWait = waiteList[i].Item1 == ListenerTransaction.mesType;
                        break;
                    }
                }
                //  break;
                //}
            }

            if (isSened && isHaveWait) ALog.Log("消息等待回复中..,请勿重复发送.");
            else
            {
                if (to_Peer.Connected)
                {
                    if (!TransactionWaitePool.ContainsKey(to_Peer)) TransactionWaitePool[to_Peer] = new List<(byte, Action<Peer, Reader>)>();
                    TransactionWaitePool[to_Peer].Add(ListenerTransaction);

                    return to_Peer.Send(message);
                }
                else if (to_Peer.Connecting)
                {
                    ALog.Log("Peer正在链接中还无法发送消息..,请勿重复发送.");
                }
                else
                {
                    ALog.Log("Peer并未链接,请先链接成功后再重试.");
                }
            }

            return null;

        }



        // List<IPEndPoint> _HostEventsList = new List<IPEndPoint>();
        /// <summary>
        /// 发送并等待Host无链接的消息
        /// </summary> HostReceiveUnconnecte
        /// <param name="to_Peer"></param>
        /// <param name="message"></param>
        /// <param name="ListenerTransaction"></param>
        /// <returns></returns>
        public bool SendUnconnecteAndAwait(IPEndPoint address, IMessage message, (byte mesType, Action<IPEndPoint, Reader> OnReciveUnconnecteMes) ListenerTransaction)
        {
            var dateTime = AUtils.NowUtcTime;

            bool isSened = UnconnecteMesWaitePool.ContainsKey(address); // 确实是否给这个IP发送过消息
            bool isHaveWait = false; //确定是否还有等待中，没有回复的消息

            if (isSened)
            {
                foreach (var iPEndPoint in UnconnecteMesWaitePool.Keys)
                {
                    var waiteList = UnconnecteMesWaitePool[iPEndPoint];
                    if (waiteList.Count > 0)
                    {
                        for (int i = waiteList.Count-1; i >= 0; i--)
                        {
                            var timeGap = (dateTime.Second - waiteList[i].Item1);
                            var waitTime = (timeGap >= 0) ? timeGap : timeGap + 60;

                            if (waiteList[i].Item2 == ListenerTransaction.mesType && address == iPEndPoint)
                            {
                                //超时
                                if (waitTime > AConstants.SERRESPONSE_TIMEOUT)
                                {
                                    UnconnecteMesWaitePool[iPEndPoint].RemoveAt(i);
                                }
                                else
                                {
                                    isHaveWait = true; //有这个消息并且没有超时，则确定等待中
                                    break;
                                }
                            }
                        }
                    }
                }

            }

            if (isSened && isHaveWait) ALog.Log("消息等待回复中..,请勿重复发送.");
            else
            {

                if (!UnconnecteMesWaitePool.ContainsKey(address)) UnconnecteMesWaitePool[address] = new List<(int ,byte, Action<IPEndPoint, Reader>)>();
                UnconnecteMesWaitePool[address].Add(new (dateTime.Second, ListenerTransaction.mesType, ListenerTransaction.OnReciveUnconnecteMes));

                _Host.SendUnconnected(address, message);

                return true;

            }

            return false;

        }


        #region Peer Listener


        Peer peer1 = null;
        void OnPeerConnect(Peer peer)
        {
            ALog.Log("[P2PService :] New Peer Connected：Remote{0} ; Peer.Host.BindAddress{1}.  ", peer.Remote,
                peer.Host.BindAddress.ToString());

            if (peer1 == null) peer1 = peer;
            else ALog.Log("!!!! : " + (peer == peer1));

            OnConnect?.Invoke(peer);

        }

        public void OnPeerReceive(Peer peer, Reader reader, MessageReceived info)
        {
            ALog.Log("[P2PService :] Receive Message from {0} !! Reader is Disposed {1}", peer.Remote, reader.Disposed);

            var sure1 = MessageParse_Response(peer, reader, info);
            var sure2 = MessageParse_Request(peer, reader, info);
            if (sure1 && sure2) ALog.Error("严重错误！！！ 消息类型不能混淆");
        }

        /// <summary>
        /// 事务发出去后等待处理结果，专门处理各种Response 消息
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        /// <param name="info"></param>
        bool MessageParse_Response(Peer peer, Reader reader, MessageReceived info)
        {

            if (TransactionWaitePool.ContainsKey(peer) && TransactionWaitePool[peer].Count > 0)
            {
                for (int i = TransactionWaitePool[peer].Count - 1; i >= 0; i--)
                {
                    if (TransactionWaitePool[peer][i].Item1 == info.Channel)
                    {
                        TransactionWaitePool[peer][i].Item2.Invoke(peer, reader);
                        TransactionWaitePool[peer].RemoveAt(i);
                        ALog.Log("Clientlmpl {0} : {1} ", reader.Disposed, info.Channel);
                        return true;
                    }

                }
            }

            return false;
        }

        /// <summary>
        /// 专门处理Request 消息，Host接收到各种请求数据并处理之后返回给请求Peer
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        /// <param name="info"></param>
        bool MessageParse_Request(Peer peer, Reader reader, MessageReceived info)
        {

            List<INetServer> NetServerlmpl = P2PManager.Instance.NetServerlmpl;

            if (NetServerlmpl != null && NetServerlmpl.Count > 0)
            {
                int count = 0;
                for (int i = 0; i < NetServerlmpl.Count; i++)
                {
                    if (NetServerlmpl[i].OnReceive(peer, reader, info))
                    {
                        ALog.Log("Serverlmpl {0} : {1} ", reader.Disposed, info.Channel);
                        return true;
                    }
                    count = i;
                }
                if (count == NetServerlmpl.Count - 1) ALog.Error("Error :未知消息类型，无法处理！！{0} ; {1} ", info.Channel, count);
            }
            else ALog.Error("Error :没有装载服务解析模块？！！");

            return false;
        }

        public void OnPeerUpdateRTT(Peer peer, ushort rtt)
        {
            // ALog.Log("Host Time : " + Host.Now);
        }

        void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception)
        {
            ALog.Log("[P2PService :]Disconnected from " + peer.Remote + ": " + reason);

            OnDisconnect?.Invoke(peer, reader, reason, exception);

            if (TransactionWaitePool.ContainsKey(peer) && TransactionWaitePool[peer].Count > 0)
            {
                for (int i = 0; i < TransactionWaitePool[peer].Count; i++)
                {
                    TransactionWaitePool[peer][i].Item2.Invoke(null, null);
                }
                TransactionWaitePool[peer].Clear();
            }
            TransactionWaitePool.Remove(peer);
            // Unregister the peer because it is no longer active
            // NetworkManager.Unregister(peer);
        }

        public void OnPeerException(Peer peer, Exception exception)
        {
            ALog.Log("[P2PService :] Exception " + exception + "  ; Receive Message from " + peer.Remote);
            OnException?.Invoke(peer, exception);
        }



        #endregion



        #region Host Listener


        /// <summary>
        /// 接受和回复来的UnconnecteMes无链接消息进行解析
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool MessageParse_HostUnconnectedResponse(IPEndPoint remote, Reader message)
        {
            // Read the header
            byte mes_Channel = message.ReadByte();

            if (UnconnecteMesWaitePool.ContainsKey(remote) && UnconnecteMesWaitePool[remote].Count > 0)
            {
                for (int i = UnconnecteMesWaitePool[remote].Count - 1; i >= 0; i--)
                {
                    if (UnconnecteMesWaitePool[remote][i].Item2 == mes_Channel)
                    {
                        UnconnecteMesWaitePool[remote][i].Item3.Invoke(remote, message);
                        UnconnecteMesWaitePool[remote].RemoveAt(i);
                        ALog.Log("HostUnconnected Clientlmpl {0} : {1} ", message.Disposed, mes_Channel);
                        return true;
                    }

                }
            }
            ALog.Log("HostUnconnected Clientlmpl {0} : {1} ", "没有处理这个事务代码，是否超时了？", mes_Channel);
            return false;
        }

        /// <summary>
        /// 处理Host 无链接的 Request 消息，Host接收到各种请求数据并处理之后返回给请求另一个Host
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool MessageParse_HostUnconnectedRequest(IPEndPoint remote, Reader message)
        {

            List<INetServer> NetServerlmpl = P2PManager.Instance.NetServerlmpl;

            if (NetServerlmpl != null && NetServerlmpl.Count > 0)
            {
                int count = 0;
                for (int i = 0; i < NetServerlmpl.Count; i++)
                {
                    byte mes_Channel = message.ReadByte();

                    if (NetServerlmpl[i].OnUnconnectedReceive(mes_Channel, remote, message))
                    {
                        ALog.Log("HostUnconnected Serverlmpl {0} : {1} ", message.Disposed);
                        return true;
                    }
                    count = i;
                }
                if (count == NetServerlmpl.Count - 1) ALog.Error("Error :未知消息类型，无法处理！！{0} " , count);
            }
            else ALog.Error("Error :没有装载服务解析模块？！！");

            return false;
        }

        public void OnHostReceiveSocket(IPEndPoint remote, byte[] buffer, int length)
        {
            ALog.Log("[HostService :] ReceiveSocket from " + remote);
        }

        public void OnHostReceiveUnconnected(IPEndPoint remote, Reader message)
        {
            try
            {
                var sure1 = MessageParse_HostUnconnectedResponse(remote, message);
                var sure2 = MessageParse_HostUnconnectedRequest(remote, message);
                if (sure1 && sure2) ALog.Error("严重错误！！！ 消息类型不能混淆");

            }catch(Exception ex)
            {
                ALog.Error("[HostService :] 1 ReceiveUnconnected Error: " + ex);
            }

            ALog.Log("[HostService :] 1 ReceiveUnconnected from " + remote);
        }

        public void OnHostReceiveBroadcast(IPEndPoint remote, Reader message)
        {
           // _Host.SendUnconnected(remote, new BroadcastMessage());

            ALog.Log("[HostService :] Broadcast Receive from " + remote);
        }

        public void OnHostReceiveRequest(ConnectionRequest request, Reader message)
        {
            ALog.Log("[HostService :][New Peer] Connected form{0}, message{1} ", (request.Peer == null), request.Peer?.Remote);
        }

        public void OnHostException(IPEndPoint remote, Exception exception)
        {
            ALog.Log("[HostService :] Exception " + exception + "  ; Receive Message from " + remote);
        }

        public void OnHostShutdown()
        {
            ALog.Log("[HostService :] Shutdown!! Shutdown!! ");
        }

        #endregion
    }
}