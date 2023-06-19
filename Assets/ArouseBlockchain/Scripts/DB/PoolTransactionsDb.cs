// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using LiteDB;
using ArouseBlockchain.Common;
using ArouseBlockchain.NetService;
using System.Linq;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain.DB
{
    public class PoolTransactionsDb : TransactionDb
    {
        public PoolTransactionsDb(LiteDatabase db, BsonMapper mapper) : base(db, mapper)
        {
            _db_name = AConstants.TBL_TRANSACTIONS_POOL;
        }
        public void Close()
        {

            _db?.Dispose();
        }

        public void DeleteAll()
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return;
            }

            transactions.DeleteAll();
        }


        /// <summary>
        /// 删除只能是相同ID的交易记录，避免通过交易的内容和Hash进行删除
        /// </summary>
        /// <param name="trans"></param>
        public void DeleteAll(List<Transaction> trans)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1 || trans == null || trans.Count < 1)
            {
                return;
            }

            foreach (var tran in trans)
            {
                var local_tran = transactions.FindById(tran.Id);
                if(local_tran == null)
                {
                    ALog.Error("没有在本地找到这笔交易，无法删除 {0}", tran.ToString());
                    continue;
                }
                transactions.Delete(local_tran.Id);
            }
        }

        public List<Transaction> GetAllRecord()
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.time_stamp);

            var query = transactions.FindAll().ToList();

            return query;
        }

        public List<StorageItem> GetAllItemRecord()
        {
            var items = GetAll_Item();
            if (items is null || items.Count() < 1)
            {
                return null;
            }

            items.EnsureIndex(x => x.item_type);

            var query = items.FindAll().ToList();

            return query;
        }
    }
}