using System;
using System.Collections.Generic;
using System.Net;
using LiteDB;
using ArouseBlockchain.Common;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Threading;
using static ArouseBlockchain.Common.AUtils;
using Cysharp.Threading.Tasks;
using ArouseBlockchain.Solve;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Net;

namespace ArouseBlockchain.P2PNet
{

    public class NetNodePeer : INetParse
    {
        /// <summary>
        /// 网络通讯的服务端，用于处理客户端请求，并返回客户端数据
        /// </summary>
        public abstract class NodePeerServer : INetServerSendComponent, INetServer
        {
            public abstract UniTaskVoid P2PConnectResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken);

            public abstract void NodeSyncStateResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken);

            public virtual bool OnReceive(Peer peer, Reader message, MessageReceived info)
            {

                try
                {
                    if (!Enum.IsDefined(typeof(NodePeerMessageType), info.Channel)) return false;

                    NodePeerMessageType type = (NodePeerMessageType)info.Channel;

                    switch (type)
                    {
                        case NodePeerMessageType.P2PConnectRequest:
                            P2PConnectResponse(peer, message, NewCancelToken);
                            return true;
                        case NodePeerMessageType.SyncStateRequest:
                            NodeSyncStateResponse(peer, message, NewCancelToken);
                            return true;
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
                throw new NotImplementedException();
            }

        }

        /// <summary>
        /// 网络通讯的客户端，用于调用服务器的方法
        /// 中继和中继直接不能去进行链接和同步
        /// </summary>
        public class NodePeerClient : INetClient
        {
            //客户端需要通讯的channel
            // public Channel netChannel；
            public NodePeerClient(Peer peer) : base(peer)
            {
            }

            public NodePeerClient() : base()
            {
            }

            public void SetNetPeer(Peer peer)
            {
                if (Peer != null)
                {
                    Peer.Disconnect();
                }

                Peer = peer;
            }

            public bool IsFreePeer
            {
                get
                {
                    bool isFree = ((Peer == null) || (!Peer.Connected && !Peer.Connecting)) && (_P2PConnectRequest !=null && _NodeSyncStateRequest!=null &&!_P2PConnectRequest.IsWaitResponse_Connect && !_NodeSyncStateRequest.IsWaitResponse_Connect);
                    if(isFree) Peer?.Disconnect();
                    return isFree;
                }
            }

            public bool IsSamePeer(Peer peer)
            {
                return (Peer == peer);
            }

            //ISResponseMessage
            CResponseAsyncComponent _P2PConnectRequest;
            public async UniTask<ResponseStatus> P2PConnectRequest(IPEndPoint targerAddress, CancellationToken cancel_token)
            {
                //中继和中继直接不能去进行链接和同步
                if(Peer == null)
                {
                    var try_Peer = P2PManager.GetNetHost.Connect(targerAddress, null, null, true);
                    var IsCanceled = await UniTask.WaitUntil(() => try_Peer.Connected && !try_Peer.Connecting, PlayerLoopTiming.FixedUpdate, cancel_token).SuppressCancellationThrow();
                    if (IsCanceled) //如果取消超时
                    {
                        try_Peer?.Disconnect();
                        try_Peer?.Dispose();
                        ALog.Log("---------^^^^^链接超时或Task取消 {0} ,{1}^^^^^-------", cancel_token.IsCancellationRequested, targerAddress);
                        return default;
                    }

                    Peer = try_Peer;
                }

                if(_P2PConnectRequest == null) _P2PConnectRequest = new CResponseAsyncComponent();

                P2PConnectResponse _ResponseMessage = await _P2PConnectRequest.CRequest<P2PConnectRequest, P2PConnectResponse>(Peer,
                    new P2PConnectRequest() { ToAddress = targerAddress.ToString() },
                    cancel_token);

                return _ResponseMessage._ReStatus;
            }

            CResponseAsyncComponent _NodeSyncStateRequest;
           
            public async UniTask<NodeSyncStateResponse> NodeSyncStateRequest(NodePeer nodePeer, CancellationToken cancel_token)
            {
                //中继和中继直接不能去进行链接和同步

                if (_NodeSyncStateRequest == null) _NodeSyncStateRequest = new CResponseAsyncComponent();

                NodeSyncStateResponse _ResponseMessage = await _NodeSyncStateRequest.CRequest<NodeSyncStateRequest, NodeSyncStateResponse>(Peer,
                     new NodeSyncStateRequest() { NodePeer = nodePeer },
                     cancel_token);
                if (_ResponseMessage.NodePeers != null)
                {
                    ALog.Log("接收到 Peer 列表 数量： {0}  ", _ResponseMessage.NodePeers.Count);
                }
                return _ResponseMessage;

            }
        }
        /// <summary>
        /// NodePeer去除重复，
        /// 1.如重复先留下在线的节点，
        /// 2.如重复留下时间最新的 
        /// </summary>
        /// <param name="oldList"></param>
        /// <returns></returns>
        public static List<NodePeerNet> NodePeerDeduplication(List<NodePeerNet> oldList)
        {
            Dictionary<IPEndPoint, NodePeerNet> tempMap = new Dictionary<IPEndPoint, NodePeerNet>();

            NodePeer m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;
            IPEndPoint myPublic = IPResolver.TryParse(m_NodePeer.address_public);
            IPEndPoint myLocal = IPResolver.TryParse(m_NodePeer.address_local);

            for (int i = 0; i < oldList.Count; i++)
            {
                var item = oldList[i];

                IPEndPoint remote_pub = IPResolver.TryParse(item.NodePeer.address_public);
                IPEndPoint remote_loac = IPResolver.TryParse(item.NodePeer.address_local);

                if (remote_pub == null)
                {
                    ALog.Error("公网 ID 地址都是空的，是局域网的么 {0} ？", item.ToString());
                }

                if (remote_pub == null && remote_loac == null)
                {
                    ALog.Error("ID 地址都是空的！！");
                    continue;
                }

                //同一局域网
                if (remote_pub.Address == myPublic.Address && remote_loac != null)
                {
                    remote_pub = remote_loac;
                }

                // 同一台电脑
                if (remote_pub.Address.ToString() == "127.0.0.1")
                {
                    remote_pub = new IPEndPoint(myLocal.Address, remote_pub.Port);
                    item.NodePeer.address_public = remote_pub.ToString();

                }


                if (tempMap.ContainsKey(remote_pub))
                {
                    NodePeer temPeer = tempMap[remote_pub].NodePeer;
                    if ((item.NodePeer.report_reach > temPeer.report_reach) || (item.NodePeer.is_online && !temPeer.is_online))
                    {
                        tempMap[remote_pub] = item;
                    }

                }
                else
                {
                    tempMap.Add(remote_pub, item);
                }
            }

            List<NodePeerNet> newList = new List<NodePeerNet>();
            foreach (var item in tempMap.Keys)
            {
                newList.Add(tempMap[item]);
            }

            return newList;
        }


        /// <summary>
        /// NodePeer去除重复，
        /// 1.如重复先留下在线的节点，
        /// 2.如重复留下时间最新的 
        /// </summary>
        /// <param name="oldList"></param>
        /// <returns></returns>
        public static List<NodePeer> NodePeerDeduplication(List<NodePeer> oldList)
        {
            Dictionary<IPEndPoint, NodePeer> tempMap = new Dictionary<IPEndPoint, NodePeer>();

            NodePeer m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;
            IPEndPoint myPublic = IPResolver.TryParse(m_NodePeer.address_public);
            IPEndPoint myLocal = IPResolver.TryParse(m_NodePeer.address_local);

            for (int i = 0; i < oldList.Count; i++)
            {
                var item = oldList[i];

                IPEndPoint remote_pub = IPResolver.TryParse(item.address_public);
                IPEndPoint remote_loac = IPResolver.TryParse(item.address_local);

                if (remote_pub == null)
                {
                    ALog.Error("公网 ID 地址都是空的，是局域网的么 {0} ？" , item.ToString());
                }

                if (remote_pub == null && remote_loac == null)
                {
                    ALog.Error("ID 地址都是空的！！" );
                    continue;
                }

                //同一局域网
                if (remote_pub.Address == myPublic.Address && remote_loac != null)
                {
                    remote_pub = remote_loac;
                }

                // 同一台电脑
                if (remote_pub.Address.ToString() =="127.0.0.1" ) {
                    remote_pub = new IPEndPoint(myLocal.Address, remote_pub.Port);
                    item.address_public = remote_pub.ToString();
                    
                }


                if (tempMap.ContainsKey(remote_pub))
                {
                    NodePeer temPeer = tempMap[remote_pub];
                    if((item.report_reach > temPeer.report_reach) ||(item.is_online && !temPeer.is_online))
                    {
                        tempMap[remote_pub] = item;
                    }

                }
                else
                {
                    tempMap.Add(remote_pub, item);
                }
            }

            List<NodePeer> newList = new List<NodePeer>();
            foreach (var item in tempMap.Keys)
            {
                newList.Add(tempMap[item]);
            }
           
            return newList;
        }

        /// <summary>
        /// 1. Node节点由一个Peer和Peer所对应的块来组成
        /// 2. NodePeer保存了用户的网络讯息
        /// 3. 块保留了用户的区块数据
        /// 4. Peer用户网络讯息可能发生变化
        /// 5. Node使用用户唯一ID（手机号？设备ID）进行绑定，知道Node就知道了块
        /// </summary>
        ///

        #region 网络传输中的数据结构
        //-----------------------------------data------------------------------------//

        //byte :0～255
        public enum NodePeerMessageType : byte
        {
            // 1. A客户端访问中继获取 Peers [PeerListRequest] 并发送自己的 NodePeer
            // 2. 并且 A-Host 链接到 Host-B, 返回B的Peer
            // 3. 并且通知中继，A要去链接B。
            // 4. 中继收到之后，找到 Host-B，并发送A的消息
            // 5. Host-B 主动链接到 客户端 A
            // 6. 完成通讯

            //开机后同步状态的事物
            SyncStateRequest = 250,
            SyncStateResponse = 249,

            //链接事物
            P2PConnectRequest = 248,
            P2PConnectResponse = 247,



        }

        public struct NodePeerAddress
        {
            //NodePeer的公网IP地址
            public string address_public { get; set; }
            //NodePeer的本地局域网IP地址
            public string address_local { get; set; }
            //NodePeer的host IP 地址
            public string address_remote { get; set; }


            public NodePeerAddress(string public_add, string local_add, string remote_add) {
                address_public = public_add;
                address_local = local_add;
                address_remote = remote_add;
            }

            public void Read(Reader reader)
            {
                address_public = reader.ReadString();
                address_local = reader.ReadString();
                address_remote = reader.ReadString();
            }

            public void Write(Writer writer)
            {
                writer.Write(address_public);
                writer.Write(address_local);
                writer.Write(address_remote);
            }

        }

        /// <summary>
        /// 通过中继服务，和它方Peer进行链接
        /// </summary>
        public struct P2PConnectRequest : IMessage
        {
            HostTimestamp IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => (byte)NodePeerMessageType.P2PConnectRequest;
            bool IMessage.Timed => false;
            bool IMessage.Reliable => true;
            bool IMessage.Ordered => false;
            bool IMessage.Unique => true;

            //public byte NodePeerMsgeType;
            public string FormAddress; //这个地址是中继告诉我让我去链接的Peer
            public string ToAddress; //这个地址是中继返回给我的

            public void Read(Reader reader)
            {
                //FormAddress = new NodePeerAddress();
                //ToAddress = new NodePeerAddress();

                //FormAddress.Read(reader);
                //ToAddress.Read(reader);
                // NodePeerMsgeType = reader.ReadByte();
                FormAddress = reader.ReadString();
                ToAddress = reader.ReadString();
            }

            public void Write(Writer writer)
            {
                // NodePeerMsgeType = (byte)NodePeerMessageType.P2PConnectRequest;
                // writer.Write(NodePeerMsgeType);
                //FormAddress.Write(writer);
                //ToAddress.Write(writer);
                writer.Write(FormAddress);
                writer.Write(ToAddress);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct P2PConnectResponse : ISResponseMessage //, IMessageListener
        {
            HostTimestamp IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => (byte)NodePeerMessageType.P2PConnectResponse;
            bool IMessage.Timed => false;
            bool IMessage.Reliable => true;
            bool IMessage.Ordered => false;
            bool IMessage.Unique => true;

          //  public byte UNCChannel => 0;
            public ResponseStatus _ReStatus { get ; set; }

            public void Read(Reader reader)
            {
                _ReStatus = new ResponseStatus();

                //错误和空的成功不进行下一步读取
                if(this.IsError() || this.IsSuccNone())
                {
                    return;
                }
               
                _ReStatus.Read(reader);
            }

            public void Write(Writer writer)
            {
                _ReStatus.Write(writer);
            }

            //public void OnMessageSend(Peer peer, MessageSent message)
            //{
            //    ALog.Log("Mesage sent to " + peer);
            //}

            //public void OnMessageAcknowledge(Peer peer, MessageSent message)
            //{
            //    ALog.Log("Mesage acknowledged by " + peer);
            //}
        }


        /// <summary>
        /// 向另外一个Host请求对方的可以链接的NodePeerList列表
        /// </summary>
        public struct NodeSyncStateRequest : IMessage
        {
            HostTimestamp IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => (byte)NodePeerMessageType.SyncStateRequest;
            bool IMessage.Timed => false;
            bool IMessage.Reliable => true;
            bool IMessage.Ordered => false;
            bool IMessage.Unique => true;

            public byte NodePeerMsgeType;
            public NodePeer NodePeer;

            public void Read(Reader reader)
            {
                NodePeer = new NodePeer();
               // NodePeerMsgeType = reader.ReadByte();
                NodePeer.Read(reader);
            }

            public void Write(Writer writer)
            {
                //NodePeerMsgeType = (byte)NodePeerMessageType.P2PConnectRequest;
                NodePeer.Write(writer);
            }
        }

        /// <summary>
        /// Host接受到请求后，返回给客户端NodePeerList列表
        /// </summary>
        public struct NodeSyncStateResponse : ISResponseMessage
        {
            HostTimestamp IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => (byte)NodePeerMessageType.SyncStateResponse;
            bool IMessage.Timed => false;
            bool IMessage.Reliable => true;
            bool IMessage.Ordered => false;
            bool IMessage.Unique => true;

          //  public byte UNCChannel => 0;
            public ResponseStatus _ReStatus { get; set; }
            public List<NodePeer> NodePeers;

            public void Read(Reader reader)
            {
                _ReStatus = new ResponseStatus();
                _ReStatus.Read(reader);

                //错误和空的成功不进行下一步读取
                if (this.IsError() || this.IsSuccNone())
                {
                    return;
                }

                NodePeers = new List<NodePeer>();

                //排错
                if ((_ReStatus._Status & MESSatus.STATUS_ERROR) != MESSatus.NONE)
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        NodePeer entry = new NodePeer();
                        entry.Read(reader);
                        NodePeers.Add(entry);
                    }
                }
            }

            public void Write(Writer writer)
            {
                _ReStatus.Write(writer);

                writer.Write(NodePeers.Count);
                foreach (NodePeer server in NodePeers)
                {
                    server.Write(writer);
                }
            }

        }


       

        public struct NodePeer
        {
            public ObjectId Id { get; set; }
            //NodePeer的公网IP地址
            public string address_public { get; set; }
            //NodePeer的本地局域网IP地址
            public string address_local { get; set; }
            //NodePeer的host IP 地址
            public string address_remote { get; set; }
            //Node的设备Mac地址
            public string mac_address { get; set; }
            //最新登入时间
            public long report_reach { get; set; }
            //是否是启动中继节点
            public bool is_bootstrap { get; set; }
            //此Node稳定系数
            public UInt16 stable_count { get; set; }

            //是否在线
            public bool is_online { get; set; } 

            public NodeState node_state { get; set; }

            public void Read(Reader reader)
            {
                address_public = reader.ReadString();
                address_local = reader.ReadString();
                address_remote = reader.ReadString();
                mac_address = reader.ReadString();
                report_reach = reader.ReadInt64();
                is_bootstrap = reader.ReadBoolean();
                stable_count = reader.ReadUInt16();
                is_online = reader.ReadBoolean();

                node_state = new NodeState();
                node_state.Read(reader);
            }

            public void Write(Writer writer)
            {
                writer.Write(address_public);
                writer.Write(address_local);
                writer.Write(address_remote);
                writer.Write(mac_address);
                writer.Write(report_reach);
                writer.Write(is_bootstrap);
                writer.Write(stable_count);
                writer.Write(is_online);

                node_state.Write(writer);
            }

            /// <summary>
            /// 判断此Peer节点是否稳定的Client节点,不包含中继
            /// </summary>
            public bool IsStabilizeExcludeBoot
            {
                get
                {
                    DateTime lastDate = AUtils.ToDateTimeByUTCUnix(report_reach);
                    DateTime nowDate = AUtils.NowUtcTime;
                    return (!is_bootstrap && stable_count > 11 && nowDate.Subtract(lastDate).Days < (stable_count - 1) * 3);
                }
            }

            /// <summary>
            /// 是否稳定节点，包含中继
            /// 1.当2/3/4次不需要中继都能访问通过则稳定，否则不稳定，缓存中删除
            //  2.如果已经稳定，但是最后访问时间是1/2/3个月之前的，则不稳定，缓存中删除
            /// </summary>
            public bool IsStabilize
            {
                get
                {
                    DateTime lastDate = AUtils.ToDateTimeByUTCUnix(report_reach);
                    DateTime nowDate = AUtils.NowUtcTime;
                    return (is_bootstrap || (stable_count > 11 && nowDate.Subtract(lastDate).Days < (stable_count - 1) * 3));
                }
            }



            public class NodeState
            {
                public ObjectId Id { get; set; }
                //[BsonId]
                public string hash { get; set; }
                public long height { get; set; }
                public int version { get; set; }

                public void Read(Reader reader)
                {
                    hash = reader.ReadString();
                    height = reader.ReadInt64();
                    version = reader.ReadInt32();
                }

                public void Write(Writer writer)
                {
                    writer.Write(hash);
                    writer.Write(height);
                    writer.Write(version);
                }
            }

            //public class UserInfo
            //{
            //    public ObjectId Id { get; set; }
            //    //[BsonId]
            //    public string name { get; set; }
            //    public long avatar { get; set; }
            //    public int version { get; set; }

            //    public void Read(Reader reader)
            //    {
            //        hash = reader.ReadString();
            //        height = reader.ReadInt64();
            //        version = reader.ReadInt32();
            //    }

            //    public void Write(Writer writer)
            //    {
            //        writer.Write(hash);
            //        writer.Write(height);
            //        writer.Write(version);
            //    }
            //}


            public override string ToString()
            {
                var str = string.Format(" = address_public : {0}\n = address_local : {1}\n" +
                        "= address_remote : {2}\n = mac_address : {3}\n = is_bootstrap : {4}\n = report_reach : {5}\n" +
                        "= stable_count : {6}\n = is_online : {7}\n = node_hash : {8}\n = node_height : {9}\n" +
                "= node_version : {10}",
                        address_public,
                        address_local,
                        address_remote,
                        mac_address,
                        is_bootstrap,
                        DateTimeToString(ToDateTimeByUTCUnix(report_reach,true)),
                        stable_count,
                        is_online,
                        node_state.hash,
                        node_state.height,
                        node_state.version);
                return str;
            }
        }


        //public class NodeParam
        //{
        //    public string nodeIpAddress;
        //}

        //public class AddNodePeerReply
        //{
        //    public string status;
        //    public string message;
        //}

        //public class NodePeerPaging
        //{
        //    public Int32 page_number;
        //    public Int32 result_per_page;
        //}


        //-----------------------------------data------------------------------------//
        #endregion


    }
}
