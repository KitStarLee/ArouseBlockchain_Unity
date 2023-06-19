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
using System;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain.DB
{
    /// <summary>
    /// Transaction DB, for add, update transaction
    /// </summary>
    public class TransactionDb : IDbFun
    {
        protected readonly LiteDatabase _db;
        protected readonly BsonMapper _mapper;
        protected string _db_name = AConstants.TBL_TRANSACTIONS;

        public TransactionDb(LiteDatabase db, BsonMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _db_name = AConstants.TBL_TRANSACTIONS;
        }
        public void Close()
        {

            _db?.Dispose();
        }

        /// <summary>
        /// Add some transaction in smae time
        /// </summary>
        public bool AddBulk(List<Transaction> transactions)
        {
            try
            {
                var collection = GetAll()
                    .Include(x => x.storage_item); 
                var item = GetAll_Item();

                // _mapper.ToDocument<Transaction>(transaction);

                IList<StorageItem> itmeList = new List<StorageItem>();

                foreach (var trans in transactions)
                {
                    itmeList.Add(trans.storage_item);
                }

                item.InsertBulk(itmeList);
                collection.InsertBulk(transactions);

                return true;
            }
            catch (Exception ex)
            {
                ALog.Error("【oo】添加交易错误: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Add a transaction
        /// </summary>
        public bool Add(Transaction trans)
        {
            try
            {
                var transactions = GetAll()
                    .Include(x => x.storage_item);
                var item = GetAll_Item();

                item.Insert(trans.storage_item);
                // ALog.Log("【oo】添加一笔交易{0} ：{1}" , trans.storage_item.get_content,(trans.storage_item as StorageObjItem));


                var dOrder = _mapper.ToDocument<Transaction>(trans);

                transactions.Insert(trans);


                return true;
            }
            catch (Exception ex)
            {
                ALog.Error("【oo】添加交易错误: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Get All Transactions by Address and with paging
        /// </summary>
        public IEnumerable<Transaction> GetRangeByAddress(string address, int pageNumber, int resultsPerPage)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                ALog.Log("没有交易任何记录...");
                return null;
            }

            transactions.EnsureIndex(x => x.sender);
            transactions.EnsureIndex(x => x.recipient);

            var query = transactions.Query()
                .OrderByDescending(x => x.time_stamp)
                .Where(x => x.sender == address || x.recipient == address)
                .Offset((pageNumber - 1) * resultsPerPage)
                .Limit(resultsPerPage).ToList();

            return query;
        }

        /// <summary>
        /// Get Transaction by Hash
        /// </summary>
        public Transaction GetByHash(string hash)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.hash);

            return transactions.FindOne(x => x.hash == hash);
        }

        /// <summary>
        /// Get transactions
        /// </summary>
        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.time_stamp);

            var query = transactions.Query()
                .OrderByDescending(x => x.time_stamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();

            return query;
        }

        public IEnumerable<Transaction> GetLast(int num)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.time_stamp);

            var query = transactions.Query()
                .OrderByDescending(x => x.time_stamp)
                .Limit(num).ToList();

            return query;
        }

        /// <summary>
        /// get one transaction by address
        /// </summary>
        public Transaction GetByAddress(string address)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.time_stamp);
            var transaction = transactions.FindOne(x => x.sender == address || x.recipient == address);
            return transaction;
        }

        public StorageObjItem GetStorageObjItem(string obj_item_hash)
        {
            var items = GetAll_Item();
            if (items is null || items.Count() < 1)
            {
                return null;
            }
            items.EnsureIndex(x => x.item_type);

            IList<StorageObjItem> objItemList = new List<StorageObjItem>();

            foreach (var item in items.Find(it => it.item_type == StorageItemType.ObjBase))
            {
                if ((item is StorageObjItem) && (item as StorageObjItem).hash == obj_item_hash)
                {
                    objItemList.Add((item as StorageObjItem));
                }
                else
                {
                    ALog.Error("错误的Item,它的item_type是OBJ，但无法进行正确拆箱{0}", obj_item_hash);
                }

            }
            if (objItemList == null || objItemList.Count <= 0) return null;
            if (objItemList.Count > 1) ALog.Error("有{0}个相同的ObjItem ：{1}", objItemList.Count, obj_item_hash);

            return objItemList[0];
        }

        protected ILiteCollection<Transaction> GetAll()
        {
            var tran = _db.GetCollection<Transaction>(_db_name);
            var query = tran
             .Include(x => x.hash)
             .Include(x => x.time_stamp)
             .Include(x => x.sender)
             .Include(x => x.recipient)
             .Include(x => x.storage_item)
             .Include(x => x.fee)
             .Include(x => x.height)
             .Include(x => x.signature)
             .Include(x => x.pub_key)
             .Include(x => x.tx_type);
            //BsonMapper.Global.Entity<Transaction>()
            //    .DbRef(x => x.storage_item, "storage_item");  // 1 对 1/0 引用
            return query;
        }


        protected ILiteCollection<StorageItem> GetAll_Item()
        {
            var storage_item = _db.GetCollection<StorageItem>("storage_item");
            return storage_item;
        }
        //private ILiteCollection<StorageObjItem> GetAll_OI()
        //{
        //    var obj_item = _db.GetCollection<StorageObjItem>("obj_items");
        //    return obj_item;
        //}
    }
}