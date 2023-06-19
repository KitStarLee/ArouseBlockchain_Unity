using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Net;
using Cysharp.Threading.Tasks;
using LiteDB;
using NBitcoin;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using UnityEditor.PackageManager.Requests;
using UnityEditor.ShaderGraph.Internal;

namespace ArouseBlockchain.P2PNet
{

    #region 网络传输中的数据结构
    //-----------------------------------data------------------------------------//
    //byte :0～255
    public enum NetBlockMessageType : byte
    {
        BuildCRequest = 238, // 创建一个块时，通知
        ValidProofSResponse = 237, //收到成块的广播进行的验证确认回复,可能来自广播也可能是长链接的消息

        //下载块的事件只有两个：1. 有Peer构建了新块 2.Peer上线的时候去同步块
        //有的区块链是收到新块的消息后，直接就进行下载，但块的数据太大，只能长链接的时候下载，所以得收到新块通知后，请求下载并建立长链接
        DownCRequest = 236,  // 请求下载服务Peer的块
        //下载的时候就先不去嗅探Peer了，下载完了再继续
        DownSResponse = 235, // 返回块数据


        // 还有一些其他数据类型，比如获取块信息，高度，困难度等等，但那个都是网页开发需要的，需要进行HTTP访问
        // 而Arouse区块链中，客户端就是APP，直接检索数据库即可，所以主要的接口就上面两种类型
    }

    public struct Block
    {
        public ObjectId Id { get; set; }
        public int version { get; set; }
        //块的序列
        public long height { get; set; }
        public long time_stamp { get; set; }
        //前一个块的哈希
        public string prev_hash { get; set; }
        //自己的哈希
        public string hash { get; set; }
        //交易数据
        public string transactions { get; set; }
        /// <summary>
        /// 块的创建人，即使其他人验证的验证码
        /// </summary>
        public string validator { get; set; }
        public string validator_balance { get; set; }
        public string merkle_root { get; set; }

        public int num_of_tx { get; set; }
        //总的交易梳理
        public string total_amount { get; set; }
        //总的报酬
        public double total_reward { get; set; }
        //困难度
        public int difficulty { get; set; }
        //增值
        public long proof { get; set; }
        //挖矿的随机数据
        public int nonce { get; set; }
        //块的大小
        public int size { get; set; }
        //块的创建总时间
        public int build_time { get; set; }
        //签名
        public string signature { get; set; }

        public void Read(Reader reader)
        {
            version = reader.ReadInt32();
            height = reader.ReadInt64();
            time_stamp = reader.ReadInt64();

            prev_hash = reader.ReadString();
            hash = reader.ReadString();
            transactions = reader.ReadString();

            validator = reader.ReadString();
            validator_balance = reader.ReadString();
            merkle_root = reader.ReadString();

            num_of_tx = reader.ReadInt32();
            total_amount = reader.ReadString();
            total_reward = reader.ReadDouble();

            difficulty = reader.ReadInt32();
            proof = reader.ReadInt64();
            nonce = reader.ReadInt32();
            size = reader.ReadInt32();
            build_time = reader.ReadInt32();

            signature = reader.ReadString();
        }

        public void Write(Writer writer)
        {
            writer.Write(version);
            writer.Write(height);
            writer.Write(time_stamp);

            writer.Write(prev_hash);
            writer.Write(hash);
            writer.Write(transactions);

            writer.Write(validator);
            writer.Write(validator_balance);
            writer.Write(merkle_root);

            writer.Write(num_of_tx);
            writer.Write(total_amount);
            writer.Write(total_reward);

            writer.Write(difficulty);
            writer.Write(proof);
            writer.Write(nonce);
            writer.Write(size);
            writer.Write(build_time);

            writer.Write(signature);
        }

        public override string ToString()
        {
            string str = string.Format(" = Height      : {0}\n", height) +
            string.Format(" = Version     : {0}\n", version) +
            string.Format(" = World  Timestamp   : {0}\n", AUtils.ToDateTimeByUTCUnix(time_stamp)) +
            string.Format(" = Local  Timestamp   : {0}\n", AUtils.ToDateTimeByUTCUnix(time_stamp, true)) +

            string.Format(" = Prev Hash   : {0}\n", prev_hash) +
             string.Format(" = Hash        : {0}\n", hash) +
            string.Format(" = Transactions : {0}\n", transactions) +

             string.Format(" = Validator   : {0}\n", validator) +
            string.Format(" = Validator_Balence   : {0}\n", validator_balance) +
            string.Format(" = Merkle Hash : {0}\n", merkle_root) +

              string.Format(" = Number Of Tx: {0}\n", num_of_tx) +
            string.Format(" = Amout       : {0}\n", total_amount) +
            string.Format(" = Reward      : {0}\n", total_reward) +

            string.Format(" = Difficulty  : {0}\n", difficulty) +
          string.Format(" = Proof  : {0}\n", proof) +
            string.Format(" = Nonce       : {0}\n", nonce) +

            string.Format(" = Size        : {0}\n", size) +
            string.Format(" = Build Time  : {0}\n", build_time) +
            string.Format(" = Signature   : {0}", signature);

            return str;//base.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                if (string.IsNullOrEmpty(transactions) &&
                      string.IsNullOrEmpty(hash) &&
                    height == 0 && time_stamp == 0) return true;
                else return false;
            }
            else
            {
                if (obj is Block)
                {
                    return base.Equals(obj);
                }
                else
                {
                    return false;
                }
            }

        }

      
    }



  /// <summary>
  /// 创建块之后，同步给其他Peer
  /// </summary>
    public class BuildCRequest : IMessage
    {
        public HostTimestamp Timestamp => Host.Now;
        public byte Channel => (byte)NetBlockMessageType.BuildCRequest;
        public bool Timed => true;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;

        public Block _Block;

        public virtual void Read(Reader reader)
        {
            _Block = new Block();
            _Block.Read(reader);

        }

        public virtual void Write(Writer writer)
        {
            _Block.Write(writer);
        }

    }

    /// <summary>
    /// 创建块之后，广播给其他Peer
    /// </summary>
    public class BuildCRequest_UNC : BuildCRequest, IUncMessage
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


    public class ValidProofSResponse : ISResponseMessage
    {
        public HostTimestamp Timestamp => Host.Now;
        public byte Channel => (byte)NetBlockMessageType.ValidProofSResponse;
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;

        public ResponseStatus _ReStatus { get; set; }
        public ResponseStatus _Verification { get; set; }

        public void Read(Reader reader)
        {
            _ReStatus = new ResponseStatus();
            _ReStatus.Read(reader);

            //错误和空的成功不进行下一步读取
            if (_ReStatus.IsError(this.GetType().ToString()) || _ReStatus.IsSuccNone())
            {
                return;
            }

            _Verification = new ResponseStatus();
            _Verification.Read(reader);
        }

        public virtual void Write(Writer writer)
        {
            _ReStatus.Write(writer);
            _Verification.Write(writer);
        }
    }

    /// <summary>
    /// Peer接收到新块，进行验证和同步
    /// </summary>
    public class ValidProofSResponse_UNC : ValidProofSResponse, IUncMessage
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

    public struct DownCRequest : IMessage
    {
        HostTimestamp IMessage.Timestamp => Host.Now;
        public byte Channel => (byte)NetBlockMessageType.DownCRequest;
        bool IMessage.Timed => true;
        bool IMessage.Reliable => true;
        bool IMessage.Ordered => false;
        bool IMessage.Unique => true;


        //public long _last_height { get; set; }
        //public string _last_prev_hash { get; set; }

        /// <summary>
        /// 我需要获取的块的高度
        /// </summary>
        public long _need_height { get; set; }

        public void Read(Reader reader)
        {
            //_last_height = reader.ReadInt64();
            //_last_prev_hash = reader.ReadString();
            _need_height = reader.ReadInt64();

        }

        public void Write(Writer writer)
        {
            //writer.Write(_last_height);
            //writer.Write(_last_prev_hash);
            writer.Write(_need_height);
        }
    }

    public class DownSResponse : ISResponseMessage
    {
        public HostTimestamp Timestamp => Host.Now;
        public byte Channel => (byte)NetBlockMessageType.DownSResponse;
        public bool Timed => true;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;

        public ResponseStatus _ReStatus { get; set; }
        public List<Block> _Block;

        public void Read(Reader reader)
        {
            _ReStatus = new ResponseStatus();
            _ReStatus.Read(reader);

            //错误和空的成功不进行下一步读取
            if (_ReStatus.IsError(this.GetType().ToString()) || _ReStatus.IsSuccNone())
            {
                return;
            }

            _Block = new List<Block>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Block block = new Block();
                block.Read(reader);
                _Block.Add(block);
            }
        }

        public virtual void Write(Writer writer)
        {
            _ReStatus.Write(writer);

            foreach (Block item in _Block)
            {
                item.Write(writer);
            }
        }
    }



    //-----------------------------------data------------------------------------//
    #endregion


    public class NetBlock: INetParse
    {
        /// <summary>
        /// 网络通讯的服务端，用于处理客户端请求，并返回客户端数据
        /// </summary>
        public abstract class BlockServer : INetServerSendComponent, INetServer
        {
            //添加新的块
            public abstract void ValidProofSResponse_UNC(IPEndPoint address, BuildCRequest_UNC request, CancellationTokenSource _CancelToken);

            //添加新的块
            public abstract void ValidProofSResponse(Peer peer, BuildCRequest request, CancellationTokenSource _CancelToken);

            //收到下载区块的请求，返回我方区块
            public abstract void DownSResponse(Peer peer, DownCRequest message, CancellationTokenSource _CancelToken);


            public virtual bool OnReceive(Peer peer, Reader reader, MessageReceived info)
            {
                try
                {
                    if (!Enum.IsDefined(typeof(NetBlockMessageType), info.Channel)) return false;

                    NetBlockMessageType type = (NetBlockMessageType)info.Channel;

                    switch (type)
                    {
                        case NetBlockMessageType.DownCRequest:
                            DownCRequest message2 = new DownCRequest();
                            message2.Read(reader);
                            DownSResponse(peer, message2, NewCancelToken);
                            break;
                        case NetBlockMessageType.BuildCRequest:
                            BuildCRequest message3 = new BuildCRequest();
                            message3.Read(reader);
                            ValidProofSResponse(peer, message3, NewCancelToken);
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
                    if (!Enum.IsDefined(typeof(NetBlockMessageType), mes_Channel)) return false;

                    NetBlockMessageType type = (NetBlockMessageType)mes_Channel;

                    switch (type)
                    {
                        case NetBlockMessageType.BuildCRequest:
                            BuildCRequest_UNC request = new BuildCRequest_UNC();
                            request.Read(message);
                            ValidProofSResponse_UNC(remote, request, NewCancelToken);
                            break;

                        default:
                            ALog.Log("{0}[" + remote + "] Unknown message type " + type + " received.",this.GetType());
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
        public class BlockClient : INetClient
        {
            //客户端需要通讯的channel
            // public Channel netChannel；
            public BlockClient(Peer peer) : base(peer)
            {
            }
            public BlockClient() : base()
            {
            }


            CResponseAsyncComponent _BuildCBroadcast;
            public void BuildCBroadcast(Block block)
            {

                if (_BuildCBroadcast == null) _BuildCBroadcast = new CResponseAsyncComponent();

                _BuildCBroadcast.CUnconnectedBroadcast<BuildCRequest_UNC, ValidProofSResponse_UNC>(
                     new BuildCRequest_UNC() { _Block = block },
                    OnReviceReply, true
                 );

                void OnReviceReply(IPEndPoint address, ValidProofSResponse_UNC response)
                {
                    ALog.Log("{0} : 广播接受：来自 {1} 的回复 {2}", this.GetType() ,address, response._Verification._Status);
                }
            }

            CResponseAsyncComponent _BuildCRequest;
            public async UniTask<ValidProofSResponse> BuildCRequest(Block block, CancellationToken cancel_token)
            {
                if (_BuildCRequest == null) _BuildCRequest = new CResponseAsyncComponent();

                ValidProofSResponse _ResponseMessage = await _DownCRequest.CRequest<BuildCRequest, ValidProofSResponse>(Peer,
                     new BuildCRequest() { _Block = block },
                     cancel_token);

                return _ResponseMessage;
            }


            CResponseAsyncComponent _DownCRequest;
            public async UniTask<DownSResponse> DownCRequest(long _need_height, CancellationToken cancel_token)
            {
           
                if (_DownCRequest == null) _DownCRequest = new CResponseAsyncComponent();

                DownSResponse _ResponseMessage = await _DownCRequest.CRequest<DownCRequest, DownSResponse>(Peer,
                     new DownCRequest() { _need_height = _need_height },
                     cancel_token);


                if(!_ResponseMessage.IsError() && !_ResponseMessage.IsSuccNone())
                {
                    var blocks = _ResponseMessage._Block;
                    await SolveManager.BlockChain.Block.ValidNextBlockAndSave(blocks);
                }

                return _ResponseMessage;
            }

        }
    }

}