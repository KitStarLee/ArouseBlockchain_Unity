using System;
using System.Linq;
using System.Collections.Generic;
using ArouseBlockchain.P2PNet;
using System.Threading.Tasks;
using System.Net;
using SuperNet.Netcode.Transport;
using LiteDB;
using System.Threading;
using Cysharp.Threading.Tasks;
using ArouseBlockchain.Common;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using System.Security.Principal;
using UnityEngine;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.Solve;
using static ArouseBlockchain.Common.AUtils;
using SuperNet.Netcode.Util;

namespace ArouseBlockchain.P2PNet
{


    #region 网络传输中的数据结构
    //-----------------------------------data------------------------------------//

    //byte :0～255
    public enum NetStakeMessageType : byte
    {
        // 1. A客户端上线后，质押权益（+账号信息），然后和其他Peer同步P2P网络的当下时间的全部质押
        // 2. 当A在一个区块链周期内被选中作为创建块的幸运儿时候，找到一个验证人（见证人），让他进行验证
        // 3. 无论A是否是幸运儿，所有的质押重新开始，新一轮的质押抽签。（质押的权益会在周期结束时销毁）
        // 4. 如果A经过验证是幸运儿，则构建区块，获取收益，并且同步广播区块
        // 周期分为区块链大周期，过了这个周期所有质押清空。以及抽签小周期，用来选择幸运儿来进行创建块。目前程序是自动质押，所以的两个周期是一样的，

        
        Add_BDA_CRequest = 242,
        AddSResponse = 241,

        
        ValidatorCRequest = 240,
        ValidatorSResponse = 239,
    }

    public struct Stake
    {
        public ObjectId Id { get; set; }
        public string address { get; set; }
        public double amount { get; set; }

        public long time_stamp { get; set; }

        public void Read(Reader reader)
        {
            address = reader.ReadString();
            amount = reader.ReadDouble();
            time_stamp = reader.ReadInt64();
        }

        public void Write(Writer writer)
        {
            writer.Write(address);
            writer.Write(amount);
            writer.Write(time_stamp);
        }

        public override string ToString()
        {
            return string.Format("Id: {0} ; \nAddress: {1} ; \nAmount: {2} ; \nAmount: {3} ;",
               Id, address, amount, AUtils.UnixUTCTimeToString(time_stamp, true));//base.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                if (string.IsNullOrEmpty(address) && amount == 0 && time_stamp == 0) return true;
                else return false;
            }
            else
            {
                if (obj is Stake)
                {
                    return base.Equals(obj);
                }
                else
                {
                    return false;
                }
            }

            // return false ;// base.Equals(obj);

        }
    }


    /// <summary>
    /// 添加质押，并且同步给其他Peer
    /// </summary>
    public class AddCRequest : IMessage
    {
        public HostTimestamp Timestamp => Host.Now;
        public byte Channel => (byte)NetStakeMessageType.Add_BDA_CRequest;
        public bool Timed => true;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;
        public Account _Account;
        public Stake _Stake;

        public void Read(Reader reader)
        {
            _Account = new Account();
            _Stake = new Stake();

            _Account.Read(reader);
            _Stake.Read(reader);

        }

        public virtual void  Write(Writer writer)
        {
            _Account.Write(writer);
            _Stake.Write(writer);
        }

    }


    /// <summary>
    /// 添加质押，并且广播给其他Peer
    /// </summary>
    public class AddCRequest_UNC : AddCRequest, IUncMessage
    {
        public void FristWrite(Writer writer)
        {
            writer.Write(Channel);
        }

        public override void Write(Writer writer)
        {
            FristWrite(writer);
            base.Write(writer);
        }

    }


    /// <summary>
    /// Peer记录访问者的质押，并且返回给它自己记录的所有质押，供给给进行补充
    /// </summary>
    public struct AddSResponse : ISResponseMessage
    {
        public HostTimestamp Timestamp => Host.Now;
        public byte Channel => (byte)NetStakeMessageType.AddSResponse;
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;

        public ResponseStatus _ReStatus { get; set; }
        public List<Stake> _Stakes;

        public void Read(Reader reader)
        {
            _ReStatus = new ResponseStatus();
            _ReStatus.Read(reader);

            //错误和空的成功不进行下一步读取
            if (_ReStatus.IsError(this.GetType().ToString()) || _ReStatus.IsSuccNone())
            {
                return;
            }


            _Stakes = new List<Stake>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Stake stake = new Stake();
                stake.Read(reader);
                _Stakes.Add(stake);
            }
        }

        public void Write(Writer writer)
        {
            writer.Write(Channel);

            _ReStatus.Write(writer);
            writer.Write(_Stakes.Count);
            foreach (Stake item in _Stakes)
            {
                item.Write(writer);
            }
        }

    }


    /// <summary>
    /// 验证
    /// </summary>
    public struct ValidatorCRequest : IMessage
    {
        HostTimestamp IMessage.Timestamp => Host.Now;
        byte IMessage.Channel => (byte)NetStakeMessageType.ValidatorCRequest;
        bool IMessage.Timed => false;
        bool IMessage.Reliable => true;
        bool IMessage.Ordered => false;
        bool IMessage.Unique => true;

        public Account _Account;
        public Stake _Stake;

        public void Read(Reader reader)
        {
            _Account.Read(reader);
            _Stake.Read(reader);

        }

        public void Write(Writer writer)
        {
            _Account.Write(writer);
            _Stake.Write(writer);
        }

    }

    /// <summary>
    /// 验证
    /// </summary>
    public struct ValidatorSResponse : ISResponseMessage
    {
        HostTimestamp IMessage.Timestamp => Host.Now;
        byte IMessage.Channel => (byte)NetStakeMessageType.ValidatorSResponse;
        bool IMessage.Timed => false;
        bool IMessage.Reliable => true;
        bool IMessage.Ordered => false;
        bool IMessage.Unique => true;

        public ResponseStatus _ReStatus { get; set; }

        public void Read(Reader reader)
        {
            _ReStatus = new ResponseStatus();
            _ReStatus.Read(reader);

            //错误和空的成功不进行下一步读取
            if (this.IsError() || this.IsSuccNone())
            {
                return;
            }

        }

        public void Write(Writer writer)
        {
            _ReStatus.Write(writer);
        }

    }


    //rpc Add(Stake) returns(AddStakeStatus);
    //rpc GetRange(StakeParams) returns(StakeList);

    //-----------------------------------data------------------------------------//
    #endregion

    public class NetStake : INetParse
    {
        /// <summary>
        /// 网络通讯的服务端，用于处理客户端请求，并返回客户端数据
        /// </summary>
        public abstract class NetStakeServer : INetServerSendComponent, INetServer
        {
            public abstract void AddSResponse(IPEndPoint address, AddCRequest message, CancellationTokenSource _CancelToken); //(Account account,Stake stake);

            public abstract void ValidatorSResponse(Peer peer, ValidatorCRequest message, CancellationTokenSource _CancelToken);//(Account account, Stake stake);


            public virtual bool OnReceive(Peer peer, Reader reader, MessageReceived info)
            {
                try
                {
                    if (!Enum.IsDefined(typeof(NetStakeMessageType), info.Channel)) return false;

                    NetStakeMessageType type = (NetStakeMessageType)info.Channel;

                    switch (type)
                    {
                        case NetStakeMessageType.ValidatorCRequest:
                            ValidatorCRequest message2 = new ValidatorCRequest();
                            message2.Read(reader);
                            ValidatorSResponse(peer, message2, NewCancelToken);
                            break;
                        default:
                            ALog.Log("[" + peer.Remote + "] Unknown message type " + type + " received.");
                            break;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    ALog.Error("{0}->{1}: Peer{2} 请求时，系统出错: {3}", this.GetType(), info.Channel, peer.Remote, ex);
                    return false;
                }

            }

            public bool OnUnconnectedReceive(byte mes_Channel, IPEndPoint remote, Reader message)
            {
                try
                {
                    if (!Enum.IsDefined(typeof(NetStakeMessageType), mes_Channel)) return false;

                    NetStakeMessageType type = (NetStakeMessageType)mes_Channel;

                    switch (type)
                    {
                        case NetStakeMessageType.Add_BDA_CRequest:
                            AddCRequest request = new AddCRequest();
                            request.Read(message);
                            AddSResponse(remote, request, NewCancelToken);
                            break;
                       
                        default:
                            ALog.Log("[" + remote + "] Unknown message type " + type + " received.");
                            break;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    ALog.Error("{0}->{1}: Peer{2} 请求时，系统出错: {3}", this.GetType(), mes_Channel, remote, ex);
                    return false;
                }
            }

        }

        /// <summary>
        /// 网络通讯的客户端，用于调用服务器的方法
        /// </summary>
        public class NetStakeClient : INetClient
        {
            public NetStakeClient(Peer peer) : base(peer)
            {
            }
            public NetStakeClient() 
            {
            }


            CResponseAsyncComponent _AddCRequest;
            public void AddCBroadcast(Account account, Stake stake)
            {
                if (_AddCRequest == null) _AddCRequest = new CResponseAsyncComponent();

                _AddCRequest.CUnconnectedBroadcast<AddCRequest_UNC, AddSResponse>(
                     new AddCRequest_UNC() { _Account = account, _Stake = stake },
                    OnReviceReply
                 );

                void OnReviceReply(IPEndPoint address , AddSResponse response)
                {
                    ALog.Log("{0} : 广播接受：来自 {1} 的回复 {2}", this.GetType(), address, response._Stakes == null);
                }
            }



            CResponseAsyncComponent _ValidatorCRequest;
            public async UniTask<ValidatorSResponse> ValidatorCRequest(Account account, Stake stake, CancellationToken cancel_token)
            {
                if (_ValidatorCRequest == null) _ValidatorCRequest = new CResponseAsyncComponent();

                ValidatorSResponse _ResponseMessage = await _ValidatorCRequest.CRequest<ValidatorCRequest, ValidatorSResponse>(Peer,
                     new ValidatorCRequest() { _Account = account, _Stake = stake },
                     cancel_token);

                return _ResponseMessage;
            }
        }
    }

}