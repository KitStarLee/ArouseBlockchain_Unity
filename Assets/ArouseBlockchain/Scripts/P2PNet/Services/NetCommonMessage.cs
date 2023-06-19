using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.Solve;
using Cysharp.Threading.Tasks;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using static ArouseBlockchain.Common.AUtils;
using static ArouseBlockchain.P2PNet.NetTransaction;

//request //请求
//response // 响应

namespace ArouseBlockchain.P2PNet
{
    ///// <summary>
    ///// 无链接的自定义消息
    ///// </summary>
    //public interface ICNetWritable : IWritable
    //{
    //  //  public byte UNCChannel { get; }
    //    public void Read(Reader reader);

    //}

    ///// <summary>
    ///// 无链接的自定义消息
    ///// </summary>
    //public interface ISNetWritable : IWritable
    //{
    //   // public byte UNCChannel { get;}
    //    public ResponseStatus _ReStatus { get; set; }
    //    public void Read(Reader reader);
    //}



    ///// <summary>
    ///// 继承了IMessage，有更多的属性字段
    ///// </summary>
    //public interface ICRequestMessage : SuperNet.Netcode.Transport.IMessage //, ICNetWritable
    //{
    //    public void Read(Reader reader);
    //}

    /// <summary>
    /// 无链接的消息体，无链接的消息需要手动设置消息类型，所以这里强制提醒
    /// </summary>
    public interface IUncMessage : IMessage
    {
        public void FristWrite(Writer writer);
    }

    /// <summary>
    /// 继承了IMessage，有更多的属性字段
    /// </summary>
    public interface ISResponseMessage : IMessage //, ISNetWritable
    {
        public ResponseStatus _ReStatus { get; set; }
        public void Read(Reader reader);
    }


    /// <summary>
    /// 公共Message，响应并返回状态
    /// </summary>
    public struct ResponseStatus
    {
        public MESSatus _Status;
        public string _Message;

        public void Read(Reader reader)
        {
            _Status = (MESSatus)reader.ReadByte();
            _Message = reader.ReadString();
        }

        public void Write(Writer writer)
        {
            writer.Write((byte)_Status);
            writer.Write(_Message);
        }

        public override string ToString()
        {
            var str = string.Format("Status: {0} ｜ Message：{1}", _Status, _Message);
            ALog.Log(str);
            return str;
        }
    }


    /// <summary>
    /// 请求组件
    /// </summary>
    public class CResponseAsyncComponent
    {
       
        public CResponseAsyncComponent()
        {
            IsWaitResponse_Connect = false;
        }

        Peer _Peer;
        public bool IsWaitResponse_Connect { get; private set; }

        public async UniTask<Ts> CRequest<Tc, Ts>(Peer peer, Tc cRequestMes, CancellationToken cancel_token) where Tc : IMessage where Ts : ISResponseMessage, new()
        {
            if(cancel_token == default)
            {
                CancellationTokenSource _loaclDCTS = new CancellationTokenSource();
                _loaclDCTS.CancelAfterSlim(TimeSpan.FromSeconds(AConstants.CLIREQUEST_TIMEOUT));
                cancel_token = _loaclDCTS.Token;

                ALog.Warning("{0}: 请求没有给到线程的cancel_token,将自动生成，会有风险...请自己尽量托管, 超时时间 = {1}", cRequestMes.GetType(), AConstants.CLIREQUEST_TIMEOUT);
            }

            var requestType = cRequestMes.GetType().ToString();

            if (IsWaitResponse_Connect)
            {
                ALog.Warning("{0}: 正在请求和Peer请求中 {0}, 请勿重复操作 {1}", requestType, _Peer.Remote, peer.Remote);
            }

            _Peer = peer;

           

            Ts _ResponseMessage = default;
            //if (!(_ResponseMessage is ISResponseMessage))
            //{
            //    ALog.Error("长链接的消息体需要继承自 ISResponseMessage接口....");
            //    return default;
            //}

            var messageSent = P2PManager._P2PService.SendAndAwait(_Peer,
               cRequestMes, ((_ResponseMessage).Channel, OnRecive)
            );

            if (messageSent == null)
            {
                ALog.Error("无法和Peer建立链接 {0}....", requestType);
                return default;
            }

            IsWaitResponse_Connect = true;
            void OnRecive(Peer peer, Reader reader)
            {
                if (cancel_token.IsCancellationRequested)
                {
                    //ALog.Error("进程取消");
                    IsWaitResponse_Connect = false;
                    return;
                }

                if (reader == null)
                {
                    ALog.Error("{0}: 错误返回，Reader 为空", requestType);
                    IsWaitResponse_Connect = false;
                    return;
                }
                _ResponseMessage = new Ts();
                _ResponseMessage.Read(reader);
                ALog.Log("{0}: 接收到响应 _Status {1} ,_Message {2} ", requestType, _ResponseMessage._ReStatus._Status, _ResponseMessage._ReStatus._Message);
                IsWaitResponse_Connect = false;
            }

            var IsCanceled = await UniTask.WaitUntil(() => IsWaitResponse_Connect == false, PlayerLoopTiming.FixedUpdate, cancel_token).SuppressCancellationThrow();

            //防止超时，下次无法进行，需要再次确定为False
            IsWaitResponse_Connect = false;

            if (IsCanceled)
            {
                ALog.Error("{0}: 访问等待 {1}， 超时！！", requestType, _Peer.Remote);
                return default;
            }

            return _ResponseMessage;
        }


        /// <summary>
        /// 广播消息，消息体大小受限制，不能超过PeerConfig.MTU, 否则拒绝发送
        /// </summary>
        /// <typeparam name="Tc"></typeparam>
        /// <typeparam name="Ts"></typeparam>
        /// <param name="cRequestMes">去发送的数据</param>
        /// <param name="OnReviceReply">当收到回复后的委托</param>
        /// <param name="isExcludeConnected">是否排除在线链接的Peer，如果有些消息需要把链接中和未链接的Peer分开，则为True</param>
        /// <returns></returns>
        public bool CUnconnectedBroadcast<Tc, Ts>(Tc cRequestMes, Action<IPEndPoint, Ts> OnReviceReply,bool isExcludeConnected = false) where Tc : IMessage where Ts : ISResponseMessage, new()
        {
            //获取缓存的稳定节点
            var _NeedBroadcastPeers = P2PManager.StabilizeNodePeers;

            if (_NeedBroadcastPeers == null || _NeedBroadcastPeers.Count <= 0)
            {
                ALog.Log("无法广播，没有有效的Peer节点");
                return false;
            }

            var _NetHost = P2PManager.GetNetHost;
            if (_NetHost == null || !_NetHost.Listening)
            {
                ALog.Error("本地服务没有打开或者没有启动");
                return false;
            }

            for (int i = _NeedBroadcastPeers.Count - 1; i >= 0; i--)
            {
                var nodePeer = _NeedBroadcastPeers[i];

                IPEndPoint _address = null;
                //有进行长链接的Peer
                if (nodePeer.Peer != null && !isExcludeConnected && !nodePeer.Peer.Disposed && nodePeer.Peer.Connected)
                {
                    _address = nodePeer.Peer.Remote;
                }
                else
                {
                    //解析IP
                    IPEndPoint peer_address = Net.FindRightNetAddress(nodePeer.NodePeer);
                    _address = peer_address;
                  
                }

                if (_address == null)
                {
                    if (isExcludeConnected) continue;

                    ALog.Log("无效IP {0} ", nodePeer.NodePeer.ToString());
                    continue;
                }

                CUnconnectedRequest(_address, cRequestMes, OnReviceReply);
            }


            return false;

        }



        /// <summary>
        /// 去发送不需要进行稳定长链接的请求，可能会没有回复
        /// </summary>
        /// <param name="ipAddress">请求的链接IP</param>
        /// <param name="onReviceReply">当收到对方的回复后，调用的委托，可能会不丢包不回复</param>
        public void CUnconnectedRequest<Tc, Ts>(IPEndPoint ipAddress, Tc cRequestMes, Action<IPEndPoint, Ts> OnReviceReply) where Tc : IMessage where Ts : ISResponseMessage, new()
        {
            var requestType = this.GetType().ToString();

            Ts _ResponseMessage = default;
            //if ((_ResponseMessage is ISResponseMessage))
            //{
            //    ALog.Error("Unconnected不求长链接的消息体需要“直接”继承自 INetWritable接口....");
            //    return;
            //}

            if (_ResponseMessage.Channel <= 0)
            {
                ALog.Error("Unconnected 请求的数据类型不能为 ：{0}", _ResponseMessage.Channel);
                return;
            }

            bool isSucc = P2PManager._P2PService.SendUnconnecteAndAwait(ipAddress,
               cRequestMes, (_ResponseMessage.Channel, OnRecive)
            );

            if (!isSucc)
            {
                ALog.Error("{0} UnconnectedRequest ：无法和此IP: {1} 进行通讯 {2}....", requestType, ipAddress, requestType);
                return;
            }

            void OnRecive(IPEndPoint ipAddress, Reader reader)
            {
                if (reader == null)
                {
                    ALog.Error("{0} UnconnectedRequest: 错误返回，Reader 为空", requestType);
                    return;
                }

                _ResponseMessage = new Ts();
                _ResponseMessage.Read(reader);
                OnReviceReply?.Invoke(ipAddress, _ResponseMessage);
                ALog.Log("{0}: 接收到响应 _Status {1} ,_Message {2} ", requestType, _ResponseMessage._ReStatus._Status, _ResponseMessage._ReStatus._Message);

            }
        }

    }



}