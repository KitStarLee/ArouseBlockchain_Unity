using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using Newtonsoft.Json;
using ArouseBlockchain.NetService;
using ArouseBlockchain.P2PNet;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using ArouseBlockchain.Solve;
using System.Linq;
using static ArouseBlockchain.P2PNet.NetTransaction;
using SuperNet.Unity.Core;
using System.Security.Principal;
using System.Threading;

namespace ArouseBlockchain.P2PNet
{
    public class TransactionServiceImpl : TransactionServer
    {
        public override void TransactionListResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken)
        {
            NetTransaction.TransactionListRequest message = new NetTransaction.TransactionListRequest();
            // ALog.Log("Reader : " + reader.Buffer.Length + " : " + reader.Position);
            message.Read(reader);
            int PageNumber = message.PageNumber;
            int ResultPerPage = message.ResultPerPage;

            NetTransaction.TransactionListResponse response = new NetTransaction.TransactionListResponse();
            response.Transactions = new List<NetTransaction.Transaction>();

            string address = SolveManager.Wallets.GetCurrAddress;

            if (string.IsNullOrWhiteSpace(address))
            {
                ALog.Log("Error, the address is empty, please create an account first!\n");
                return;
            }

            try
            {
                if (_CancelToken.IsCancellationRequested)
                {
                    SendError(peer, response,MESSatus.STATUS_ERROR_TIMEOUT);
                    return;
                }
                List<Transaction> transactions = transactions = SolveManager.BlockChain.Transaction.GetRangeByAddress(address, PageNumber, ResultPerPage)?.ToList();

                if (transactions != null)
                {
                    response.Transactions = transactions;
                    peer.Send(response);
                    ALog.Log("发送回去 : " + response.Transactions.Count);
                    return;
                }
                else
                {
                    ALog.Log("\n---- No records found! ---");
                }
            }
            catch (Exception ex)
            {
                ALog.Error("Error! !" + ex);
                return;
            }

        }

        public override void TransferResponse(Peer peer, Reader reader, CancellationTokenSource _CancelToken)
        {
            NetTransaction.TransferRequest message = new NetTransaction.TransferRequest();
            message.Read(reader);

            string sender = message.Sender;
            double amount = message.num_Amount;
            double fee = message.Fee;
            string recipient = message.Recipient;
            StorageItemType itemType = message.item_type;
            string obj_Hash = message.obj_Hash;

            NetTransaction.TransferResponse response = new NetTransaction.TransferResponse();
            var statu = response._ReStatus;


            string currAdderss = SolveManager.Wallets.GetCurrAddress;

            if (currAdderss != sender)
            {
                statu = response._ReStatus;
                statu._Status = MESSatus.STATUS_ERROR_BAD;
                statu._Message = "非法交易";
                response._ReStatus = statu;

                peer.Send(response);
                return;
            }

            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(sender);
            if (string.IsNullOrEmpty(account.address))
            {
                statu = response._ReStatus;
                statu._Status = MESSatus.STATUS_ERROR_NOT_FOUND;
                statu._Message = "系统错误，无您的账号信息";
                response._ReStatus = statu;

                peer.Send(response);
                return;
            }
            var senderBalance = account.number_item.amount;

            StorageObjItem sendObj = null;
            if (itemType == StorageItemType.ObjBase)
            {
                sendObj = account.obj_items.Find(x => x.hash == obj_Hash);

                if (sendObj == null)
                {
                    statu = response._ReStatus;
                    statu._Status = MESSatus.STATUS_ERROR_BAD;
                    statu._Message = "非法交易，你没有此Obj物品";
                    response._ReStatus = statu;

                    peer.Send(response);
                    return;
                }
            }
            else
            {
                if (amount <= 0)
                {
                    statu = response._ReStatus;
                    statu._Status = MESSatus.STATUS_ERROR_BAD;
                    statu._Message = "不能进行0积分的交易";
                    response._ReStatus = statu;

                    peer.Send(response);
                    return;
                }
            }


            if (((itemType == StorageItemType.NumberBase) ? (amount + fee) : fee) > senderBalance)
            {
                ALog.Log("Error! Sender ({" + sender + "}) does not have enough balance!");
                ALog.Log("Sender balance is :" + senderBalance);

                statu = response._ReStatus;
                statu._Status = MESSatus.STATUS_ERROR_BAD;
                statu._Message = "你的积分余额不足，无法完成交易";
                response._ReStatus = statu;

                peer.Send(response);
                return;
            }

            var NewTxn = new Transaction
            {
                sender = sender,
                time_stamp = AUtils.NowUtcTime_Unix,
                recipient = recipient,
                storage_item = (itemType == StorageItemType.NumberBase) ? new StorageNumberItem() { amount = amount } : sendObj,
                fee = fee,
                height = 0,
                tx_type = "Transfer",
                pub_key = SolveManager.Wallets.GetCurrWalletCard.GetKeyPair().PublicKeyHex,
            };

            var TxnHash = AUtils.BC.GetTransactionHash(NewTxn);
            var signature = SolveManager.Wallets.Sign(TxnHash);

            NewTxn.hash = TxnHash;
            NewTxn.signature = AUtils.BC.BytesToHexString(signature.Signature).ToLower();

            SolveManager.BlockChain.WalletAccount.UpdateStorage(NewTxn);

            statu = response._ReStatus;
            statu._Status = MESSatus.STATUS_SUC_NONE;
            statu._Message = "完成交易，载入区块(并未进行区块化)";
            response._ReStatus = statu;

            peer.Send(response);

            //发送到链上
        }


        //{
        //    public override Task<Transaction> GetByHash(Transaction transaction, ServerCallContext callContext)
        //    {
        //        throw new NotImplementedException();
        //    }

        //public override Task<TransactionList> GetPendingTxns(TransactionPaging paging, ServerCallContext callContext)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<TransactionList> GetPoolRange(TransactionPaging paging, ServerCallContext callContext)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<TransactionList> GetRange(TransactionPaging paging, ServerCallContext callContext)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<TransactionList> GetRangeByAddress(TransactionPaging paging, ServerCallContext callContext)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<TransactionStatus> Receive(TransactionPost transactionPost, ServerCallContext callContext)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public override Task<TransactionStatus> Transfer(TransactionPost transactionPost, ServerCallContext callContext)
        //    {
        //        throw new NotImplementedException();
        //    }
    }

}