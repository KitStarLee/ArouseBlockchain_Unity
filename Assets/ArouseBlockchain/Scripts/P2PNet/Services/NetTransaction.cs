using System;
using ArouseBlockchain.P2PNet;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Net.Mail;
using SuperNet.Unity.Core;
using LiteDB;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Cms;
using ArouseBlockchain;
using ArouseBlockchain.Common;
using static ArouseBlockchain.Common.AUtils;
using System.Threading;
using Cysharp.Threading.Tasks;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
using static ArouseBlockchain.P2PNet.NetTransaction;
using Unity.VisualScripting;
using Mono.Cecil.Cil;

namespace ArouseBlockchain.P2PNet
{

    public class NetTransaction : INetParse
    {
        public abstract class TransactionServer : INetServerSendComponent, INetServer
        {
            public abstract void TransferResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken);

            public abstract void TransactionListResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken);

            public virtual bool OnReceive(Peer peer, Reader message, MessageReceived info)
            {
                try
                {

                    if (!Enum.IsDefined(typeof(TransactionMessageType), info.Channel)) return false;

                    TransactionMessageType type = (TransactionMessageType)info.Channel;
                    //  ALog.Log("Reader : " + (message == null));
                    switch (type)
                    {
                        case TransactionMessageType.TransferRequest:
                            TransferResponse(peer, message, NewCancelToken);
                            return true;
                        case TransactionMessageType.TransactionListRequest:
                            TransactionListResponse(peer, message, NewCancelToken);
                            return true;
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

        public class TransactionClient : INetClient
        {
            public TransactionClient(Peer peer) : base(peer)
            {
            }


            CResponseAsyncComponent _TransferRequest_Num;
            public async Task<TransferResponse> TransferRequest_Num(string sender, string recipient, double amount, int fee, CancellationTokenSource cancellationToken)
            {
                if (_TransferRequest_Num != null && _TransferRequest_Num.IsWaitResponse_Connect)
                {
                    ALog.Error("请勿重复访问！！");
                    return default;
                }

                if (_TransferRequest_Num == null) _TransferRequest_Num = new CResponseAsyncComponent();

                TransferResponse _ResponseMessage = await _TransferRequest_Num.CRequest<TransferRequest, TransferResponse>(Peer,
                      new TransferRequest()
                      {
                          Sender = sender,
                          Recipient = recipient,
                          item_type = StorageItemType.NumberBase,
                          num_Amount = amount,
                          obj_Hash = "",
                          Fee = fee
                      },
                      cancellationToken.Token);

                return _ResponseMessage;

            }

            CResponseAsyncComponent _TransferRequest_Obj;
            public async Task<TransferResponse> TransferRequest_Obj(string sender, string recipient, string obj_Hash, int fee, CancellationTokenSource cancellationToken)
            {
                if (_TransferRequest_Obj != null && _TransferRequest_Obj.IsWaitResponse_Connect)
                {
                    ALog.Error("请勿重复访问！！");
                    return default;
                }

                if (_TransferRequest_Obj == null) _TransferRequest_Obj = new CResponseAsyncComponent();

                TransferResponse _ResponseMessage = await _TransferRequest_Obj.CRequest<TransferRequest, TransferResponse>(Peer,
                      new TransferRequest()
                      {
                          Sender = sender,
                          Recipient = recipient,
                          item_type = StorageItemType.NumberBase,
                          num_Amount = 0,
                          obj_Hash = obj_Hash,
                          Fee = fee
                      },
                      cancellationToken.Token);
                return _ResponseMessage;

            }


            CResponseAsyncComponent _TransactionListRequest;
            public async UniTask<TransactionListResponse> TransactionListRequest(int PageNumber, int ResultPerPage, CancellationTokenSource cancellationToken)
            {
                if (_TransactionListRequest != null && _TransactionListRequest.IsWaitResponse_Connect)
                {
                    ALog.Error("请勿重复访问！！");
                    return default;
                }

                if (_TransactionListRequest == null) _TransactionListRequest = new CResponseAsyncComponent();

                TransactionListResponse _ResponseMessage = await _TransactionListRequest.CRequest<TransactionListRequest, TransactionListResponse>(Peer,
                     new TransactionListRequest()
                     {
                         PageNumber = PageNumber,
                         ResultPerPage = ResultPerPage
                     },
                     cancellationToken.Token);


                return _ResponseMessage;
            }
        }



        #region 网络传输中的数据结构
        //-----------------------------------data------------------------------------//



        /// <summary>
        /// 业务逻辑：
        /// 1. A向B发送一笔交易。交易一旦建立，数据同步至区块链，用户余额等等就会变化，A 这时候需要更新余额。
        /// 2. 这笔交易会告知全Peers,并保存在交易池中
        /// 3. 等待Peers中有人构建了块，并放入这笔交易，并同步块去全Peers。
        /// 4. B上线了，或者存在Peer中，则直接确认交易行为一次 交易池并保存。
        /// </summary>
        //byte :0～255
        public enum TransactionMessageType : byte
        {

            // 1.发送一笔交易，Host接受到之后，保存到交易池子中，并广播出去
            // 2.所有的Peer进行对交易的Pool保存
            // 3.当其中一个Peer构建块的时候，把这个交易放入块中，并同步块。
            TransferRequest = 246,
            TransferResponse = 245,

            TransactionListRequest = 244,
            TransactionListResponse = 243,
        }

        public struct TransferRequest : SuperNet.Netcode.Transport.IMessage
        {
            HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
            byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)TransactionMessageType.TransferRequest;
            bool SuperNet.Netcode.Transport.IMessage.Timed => true;
            bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
            bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
            bool SuperNet.Netcode.Transport.IMessage.Unique => true;

            public string Sender;
            public string Recipient;
            public StorageItemType item_type;
            public double num_Amount;
            public string obj_Hash;
            public int Fee;

            public void Read(Reader reader)
            {
                Sender = reader.ReadString();
                Recipient = reader.ReadString();
                item_type = (StorageItemType)reader.ReadByte();
                num_Amount = reader.ReadDouble();
                obj_Hash = reader.ReadString();
                Fee = reader.ReadInt32();

            }

            public void Write(Writer writer)
            {
                writer.Write(Sender);
                writer.Write(Recipient);
                writer.Write((byte)item_type);
                writer.Write(num_Amount);
                writer.Write(obj_Hash);
                writer.Write(Fee);
            }
        }
        public struct TransferResponse : ISResponseMessage
        {
            HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
            byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)TransactionMessageType.TransferResponse;
            bool SuperNet.Netcode.Transport.IMessage.Timed => true;
            bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
            bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
            bool SuperNet.Netcode.Transport.IMessage.Unique => true;

           // public byte UNCChannel => 0;
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


        public struct TransactionListRequest : SuperNet.Netcode.Transport.IMessage
        {
            HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
            byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)TransactionMessageType.TransactionListRequest;
            bool SuperNet.Netcode.Transport.IMessage.Timed => false;
            bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
            bool SuperNet.Netcode.Transport.IMessage.Ordered => true;
            bool SuperNet.Netcode.Transport.IMessage.Unique => true;

            public int PageNumber;
            public int ResultPerPage;

            public void Read(Reader reader)
            {
                PageNumber = reader.ReadInt32();
                ResultPerPage = reader.ReadInt32();
            }

            public void Write(Writer writer)
            {
                writer.Write(PageNumber);
                writer.Write(ResultPerPage);
            }
        }

        public struct TransactionListResponse : ISResponseMessage
        {
            HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => (byte)TransactionMessageType.TransactionListResponse;
            bool IMessage.Timed => true;
            bool IMessage.Reliable => true;
            bool IMessage.Ordered => false;
            bool IMessage.Unique => true;

            //public byte UNCChannel => 0;
            public ResponseStatus _ReStatus { get; set; }
            public List<Transaction> Transactions;

            public void Read(Reader reader)
            {
                _ReStatus = new ResponseStatus();
                _ReStatus.Read(reader);

                //错误和空的成功不进行下一步读取
                if (this.IsError() || this.IsSuccNone())
                {
                    return;
                }

                Transactions = new List<Transaction>();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    Transaction transaction = new Transaction();
                    transaction.Read(reader);
                    Transactions.Add(transaction);
                }
            }

            public void Write(Writer writer)
            {
                _ReStatus.Write(writer);
                writer.Write(Transactions.Count);
                foreach (Transaction transaction in Transactions)
                {
                    transaction.Write(writer);
                }
            }
        }



        public class Transaction : IEquatable<Transaction>
        {
            public ObjectId Id { get; set; }

            public string hash { get; set; }
            //交易时间
            public Int64 time_stamp { get; set; }
            //发送方
            public string sender { get; set; }
            //接受方
            public string recipient { get; set; }

            [BsonRef("storage_item")]
            public StorageItem storage_item { get; set; } = new StorageItem();
            /// <summary>
            ///需要的手续费
            /// </summary>
            public double fee { get; set; }
            //交易的序列
            public Int64 height { get; set; }
            //签名
            public string signature { get; set; }
            //公钥
            public string pub_key { get; set; }
            //交易类型
            public string tx_type { get; set; }



            public void Read(Reader reader)
            {
                hash = reader.ReadString();
                time_stamp = reader.ReadInt64();
                sender = reader.ReadString();
                recipient = reader.ReadString();
                fee = reader.ReadDouble();
                height = reader.ReadInt64();
                signature = reader.ReadString();
                pub_key = reader.ReadString();
                tx_type = reader.ReadString();

                StorageItemType item_type = (StorageItemType)reader.ReadByte();
                switch (item_type)
                {
                    case StorageItemType.NumberBase:
                        storage_item = new StorageNumberItem();
                        storage_item.item_type = item_type;
                        (storage_item as StorageNumberItem).amount = reader.ReadDouble();
                        break;
                    case StorageItemType.ObjBase:
                        storage_item = new StorageObjItem();
                        storage_item.item_type = item_type;

                        StorageObjItem objItem = (storage_item as StorageObjItem);
                        objItem.hash = reader.ReadString();
                        objItem.signature = signature;
                        objItem.pub_key = pub_key;
                        objItem.name = reader.ReadString();
                        objItem.created = reader.ReadInt64();
                        objItem.file_size = reader.ReadInt32();

                        storage_item = objItem;
                        break;
                }
            }

            public void Write(Writer writer)
            {
                writer.Write(hash);
                writer.Write(time_stamp);
                writer.Write(sender);
                writer.Write(recipient);
                writer.Write(fee);
                writer.Write(height);
                writer.Write(signature);
                writer.Write(pub_key);
                writer.Write(tx_type);

                writer.Write((byte)storage_item.item_type);
                switch (storage_item.item_type)
                {
                    case StorageItemType.NumberBase:
                        writer.Write((storage_item as StorageNumberItem).amount);
                        break;
                    case StorageItemType.ObjBase:
                        StorageObjItem objItem = (storage_item as StorageObjItem);
                        writer.Write(objItem.hash);
                        writer.Write(objItem.name);
                        writer.Write(objItem.created);
                        writer.Write(objItem.file_size);
                        break;
                }
            }

            public bool Equals(Transaction other)
            {
                if (other is null)
                    return false;

                return this.hash == other.hash && this.sender == other.sender
                    && this.recipient == other.recipient
                    && this.signature == other.signature
                    && this.pub_key == other.pub_key
                    && this.time_stamp == other.time_stamp
                    && this.storage_item == other.storage_item
                    && this.sender == other.sender
                    && this.sender == other.sender;
            }

            public override bool Equals(object obj) => Equals(obj as Transaction);
            public override int GetHashCode() => (hash, sender, recipient, signature, pub_key, time_stamp, storage_item).GetHashCode();

            //public override string ToString()
            //{
            //    return string.Format("hash: {0} ; \nname: {1} ; \nsignature: {2} ; \npub_key: {3} ; \ncreated: {4} ; \nfile_size: {5} ;",
            //        hash, name, signature, pub_key, created, file_size);//base.ToString();
            //}
        
        }



        public class TransactionStatus
        {
            public string status;
            public string message;
        }


        public class TransactionGet
        {
            public string address;
            public string hash;
        }

        public class TransactionPost
        {
            public Transaction Transaction;
            public string sending_from;
        }


        public class TransactionPaging
        {
            public string address;
            public Int64 height;
            public Int32 page_number;
            public Int32 result_per_page;
        }

        //-----------------------------------data------------------------------------//
        #endregion

    }
}

