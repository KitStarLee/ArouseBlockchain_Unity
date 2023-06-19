using System;
using System.Threading;
using ArouseBlockchain.Common;
using Cysharp.Threading.Tasks;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace ArouseBlockchain.P2PNet
{
    public interface INetServer
    {
        void Start();
        bool OnReceive(Peer peer, Reader message, MessageReceived info);
        bool OnUnconnectedReceive(byte mes_Channel, IPEndPoint remote, Reader message);
        void Shutdown();
    }

    public abstract class INetServerSendComponent
    {
        protected List<CancellationTokenSource> _DCTSList;
        public virtual void Start()
        {
            if (_DCTSList == null) _DCTSList = new List<CancellationTokenSource>();
        }

        public virtual void Shutdown()
        {
            if(_DCTSList != null && _DCTSList.Count>0)
            {
                _DCTSList.ForEach(x => { x.Cancel(); x.Dispose(); }) ;
                _DCTSList.Clear();
                _DCTSList = null;
            }
        }

        protected CancellationTokenSource NewCancelToken
        {
            get
            {
                if (_DCTSList != null && _DCTSList.Count > 0)
                {
                    int total = _DCTSList.Count;
                    int clearNum = 0;
                    for (int i = _DCTSList.Count-1; i >= 0; i--)
                    {
                        var temp = _DCTSList[i];
                        if (temp!=null && temp.IsCancellationRequested)
                        {
                            temp.Cancel();
                            temp.Dispose();
                            _DCTSList.RemoveAt(i);
                            clearNum += 1;
                        }
                    }

                    //关注服务的处理压力，如果过大，可能造成性能问题
                    if(_DCTSList.Count > 3)
                        ALog.Log("{0}: 总请求处理数 {1} ,清除 {2} , 剩余 {3}", this.GetType(), total, clearNum, _DCTSList.Count);
                }
                var cancellationToken = new CancellationTokenSource();
                //设置超时时间
                cancellationToken.CancelAfterSlim(TimeSpan.FromSeconds(AConstants.SERRESPONSE_TIMEOUT));
                _DCTSList.Add(cancellationToken);

                return cancellationToken;
            }
        }


        protected virtual void SendSucc(Peer peer, ISResponseMessage response, MESSatus satus = MESSatus.STATUS_SUCCESS, string message ="")
        {
            NewResponseStatus(ref response, peer.Remote, satus, message);
            Send(peer, response);
        }

        protected virtual void SendError(Peer peer, ISResponseMessage response, MESSatus satus, string message = "")
        {
            NewResponseStatus(ref response, peer.Remote, satus, message);
            Send(peer, response);
        }
        protected virtual void SendWarn(Peer peer, ISResponseMessage response, string message, MESSatus satus = MESSatus.STATUS_WARN)
        {
            NewResponseStatus(ref response, peer.Remote, satus, message);
            Send(peer, response);
        }

        protected virtual void SendUNCSucc(IPEndPoint Remote, ISResponseMessage response, MESSatus satus = MESSatus.STATUS_SUCCESS, string message = "")
        {
            NewResponseStatus(ref response, Remote, satus, message);
            SendUnconnected(Remote, response);
        }

        protected virtual void SendUNCError(IPEndPoint Remote, ISResponseMessage response, MESSatus satus, string message = "")
        {
            NewResponseStatus(ref response, Remote, satus, message);
            SendUnconnected(Remote, response);
        }
        protected virtual void SendUNCWarn(IPEndPoint Remote, ISResponseMessage response, string message, MESSatus satus = MESSatus.STATUS_WARN)
        {
            NewResponseStatus(ref response, Remote, satus, message);

            SendUnconnected(Remote, response);
        }

        void NewResponseStatus(ref ISResponseMessage response, IPEndPoint Remote, MESSatus satus = MESSatus.STATUS_WARN, string message ="") 
        {
            ResponseStatus status = new ResponseStatus()
            {
                _Status = satus,
                _Message = message
            };

            if((satus & MESSatus.STATUS_WARN) != MESSatus.NONE)
                ALog.Warning("{0}->{1}: Peer{2} 请求警告: {3}->{4}", this.GetType(), response.GetType(), Remote, satus, message);

            if ((satus & MESSatus.STATUS_ERROR) != MESSatus.NONE)
                ALog.Error("{0}->{1}: Peer{2} 请求警告: {3}->{4}", this.GetType(), response.GetType(), Remote, satus, message);

            if ((satus & MESSatus.STATUS_SUCCESS) != MESSatus.NONE)
                ALog.Log("{0}->{1}: Peer{2} 请求警告: {3}->{4}", this.GetType(), response.GetType(), Remote, satus, message);

            response._ReStatus = status;
        }

        void SendUnconnected(IPEndPoint remote, IMessage message)
        {
            ANetworkHost host = P2PManager.GetNetHost;
            host.SendUnconnected(remote, message);
        }

        void Send(Peer peer, ISResponseMessage response)
        {
            peer.Send(response);
        }

    }
}
