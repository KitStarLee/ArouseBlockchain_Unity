using System;
using System.Collections.Generic;
using ArouseBlockchain.P2PNet;
using System.Threading.Tasks;
using System.Net;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using LiteDB;
using System.Threading;
using Cysharp.Threading.Tasks;
using ArouseBlockchain.Common;

namespace ArouseBlockchain.P2PNet
{


    #region 网络传输中的数据结构
    //-----------------------------------data------------------------------------//

    public struct Account
    {
        public ObjectId Id { get; set; }
       // public int user_id { get; set; }
        public string nickname { get; set; } //用户昵称
        public string avatar { get; set; } // 头像
        public string address { get; set; }
        public string pub_key { get; set; }
        /// <summary>
        /// 交易次数
        /// </summary>
        public int txn_count { get; set; }
        public long created_time { get; set; }
        public long updated_time { get; set; }
        public double tokens { get; set; } // 用户的权益积分，通过社区活动指标(任务、交易、活跃度等等)来获取

        /// <summary>
        /// 流转的数字物品（积分、钻石）
        /// </summary>
        [BsonRef("number_item")]
        public StorageNumberItem number_item { get; set; }
        /// <summary>
        /// 流转的对象物品（模型、贴图、动画...）
        /// </summary>
        [BsonRef("obj_items")]
        public List<StorageObjItem> obj_items { get; set; }

      

        public void Read(Reader reader)
        {
            nickname = reader.ReadString();
            avatar = reader.ReadString();
            address = reader.ReadString();
            pub_key = reader.ReadString();

            txn_count = reader.ReadInt32();
            created_time = reader.ReadInt64();
            updated_time = reader.ReadInt64();
            tokens = reader.ReadDouble();
            number_item.Read(reader);

            obj_items = new List<StorageObjItem>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                StorageObjItem item = new StorageObjItem();
                item.Read(reader);
                obj_items.Add(item);
            }
        }

        public void Write(Writer writer)
        {
            writer.Write(nickname);
            writer.Write(avatar);
            writer.Write(address);
            writer.Write(pub_key);

            writer.Write(txn_count);
            writer.Write(created_time);
            writer.Write(updated_time);
            writer.Write(tokens);

            number_item.Write(writer);

            writer.Write(obj_items.Count);
            foreach (StorageObjItem item in obj_items)
            {
                item.Write(writer);
            }
        }

        public override bool Equals(object obj)
        {
            if(obj == null){
                if (string.IsNullOrEmpty(nickname) && string.IsNullOrEmpty(avatar)
                    && string.IsNullOrEmpty(address)
                    && string.IsNullOrEmpty(pub_key)
                    && string.IsNullOrEmpty(nickname)) return true;
                else return false;
            }
            else
            {
                if(obj is Account)
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
        public override string ToString()
        {
            string objStr = "";
            if (obj_items != null && obj_items.Count > 0) obj_items.ForEach(x=> objStr = objStr + "\n" + x.ToString());
            return string.Format("nickname: {0} ;\n avatar: {1}; \naddress: {2} ; \npub_key: {3} ; \ntxn_count: {4}" +
                " ; \ncreated: {5} ; \nupdated: {6} ; \ntokens: {7} ;  \nnumber_item: {8} ; \n obj_items: {9} ; ",
                nickname,avatar,address,pub_key,txn_count, AUtils.UnixUTCTimeToString(created_time,true), AUtils.UnixUTCTimeToString(updated_time,true),
                tokens, number_item?.get_content, objStr);
        }

    }

    public class AccountList
    {
        public List<Account> accounts;
    }

    public class AccountParams
    {
        public int page_number;
        public int result_per_page;
    }

    //-----------------------------------data------------------------------------//
    #endregion

    public class NetAccount: INetParse
    {
        /// <summary>
        /// 网络通讯的服务端，用于处理客户端请求，并返回客户端数据
        /// </summary>
        public abstract class AccountServer : INetServerSendComponent, INetServer
        {
            public abstract Task<Account> Add(Account account, ServerCallContext callContext);

            public abstract Task<Account> Update(Account account, ServerCallContext callContext);

            public abstract Task<Account> GetByPubkey(Account account, ServerCallContext callContext);

            public abstract Task<Account> GetByAddress(Account account, ServerCallContext callContext);

            public abstract Task<AccountList> GetRange(AccountParams accountParams, ServerCallContext callContext);

            public virtual bool OnReceive(Peer peer, Reader reader, MessageReceived info)
            {
                // RelayMessageType type = (RelayMessageType)info.Channel;

                //switch (type)
                //{
                //    case RelayMessageType.PeerListRequest:
                //        // Peer has requested current server list
                //        OnPeerReceiveListRequest(peer, message, info);
                //        break;
                //    case RelayMessageType.ServerUpdate:
                //        // Peer has request to add or update the server
                //        OnPeerReceiveServerUpdate(peer, message, info);
                //        break;
                //    case RelayMessageType.ServerRemove:
                //        // Peer has request to remove the server
                //        OnPeerReceiveServerRemove(peer, message, info);
                //        break;
                //    case RelayMessageType.Connect:
                //        // Peer has requested to connect to a server on this relay
                //        // Notify the server if it exists
                //        OnPeerReceiveConnect(peer, message, info);
                //        break;
                //    default:
                //        Console.WriteLine("[" + Date + "] [" + peer.Remote + "] Unknown message type " + info.Channel + " received.");
                //        break;
                //}

                try
                {

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
        /// </summary>
        public class AccountClient : INetClient
        {
            public AccountClient(Peer peer) : base(peer)
            {
            }
            public Account Add(Account account) { return default; }

            public Account Update(Account account) { return default; }

            public Account GetByPubkey(Account account) { return default; }

            public Account GetByAddress(Account account) { return default; }

            public AccountList GetRange(AccountParams accountParams) { return default; }
        }
    }

}