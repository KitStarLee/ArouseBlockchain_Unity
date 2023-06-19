// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Linq;
using LiteDB;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using System.Security.Principal;

namespace ArouseBlockchain.DB
{
    /// <summary>
    /// 账号数据库（添加、更新、检索）
    /// </summary>
    public class AccountDb: IDbFun
    {
        private readonly LiteDatabase _db;
        private readonly BsonMapper _mapper;

        public AccountDb(LiteDatabase db, BsonMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public void Close() {

            _db?.Dispose();
        }

        /// <summary>
        /// 添加新的账号
        /// </summary>
        public void Add(Account acc)
        {
            var accounts = GetAll()
                .Include(x => x.obj_items)
                .Include(x => x.number_item); 
           
            var ni = GetAll_NI();
            var oi = GetAll_OI();

            if(string.IsNullOrEmpty(acc.address))
            {
                ALog.Error("{0} 账号 是空的地址，不能添加", acc.address);
                return;
            }

           var accs = accounts.Find(x => x.address == acc.address);
            int count = accs.Count();
            if (accs != null && count > 0)
            {
                ALog.Error("已经存在{0}账号，不能重复添加{1}", count, acc.address);
                return;
            }

            if (acc.number_item == null) acc.number_item = new StorageNumberItem()
            {
                amount = 0,
                item_type = StorageItemType.ObjBase
            };

            if (acc.obj_items == null) acc.obj_items = new List<StorageObjItem>() {
            //    new StorageObjItem() {
            //hash ="", name="",signature="",pub_key="",created=0,file_size=0,item_type= StorageItemType.ObjBase
            //    }
            };


            ni.Insert(acc.number_item);
            oi.Insert(acc.obj_items);

            _mapper.SerializeNullValues = false;
            var dOrder = _mapper.ToDocument<Account>(acc);

            //accounts.EnsureIndex(x=>x.it);

            accounts.Insert(acc);
        }

        

        public Account AddOrUpdate(Account acc)
        {
            if (acc.Equals(null))
            {
                ALog.Error("质押 是空 {0} 的地址", acc.address);
                return default;
            }

            var accounts = GetAll();

            var locAccount = GetByAddress(acc.address);
            //if (locStake is null)
            if (locAccount.Equals(null))
            {
                locAccount = acc;
                locAccount.Id = accounts.Insert(acc);
            }
            else
            {
                acc.Id = locAccount.Id; //重置为同一个ID
                if (locAccount.updated_time < acc.updated_time)
                {
                    accounts.Update(acc);
                }
            }

            return accounts.FindById(locAccount.Id);
        }


        /// <summary>
        /// 更新账号信息
        /// </summary>
        public Account Update(Account acc)
        {
            if (acc.Equals(null))
            {
                ALog.Error("账号 是空 {0} 的地址,不能更新", acc.ToString());
                return default;
            }

            var accounts = GetAll()
                .Include(x => x.obj_items)
                .Include(x => x.number_item)
                .Include(x => x.address);

            var accs = accounts.FindOne(x =>  x.address == acc.address);
            if (! accs.Equals(null))
            {
                var ni = GetAll_NI();
                ni.Update(acc.number_item);

                var obji = GetAll_OI();

                //Account account = accounts.FindById(acc.address);

               // if (count > 1) ALog.Error("不应该有多个相同的账号");

                List<StorageObjItem> objItem = acc.obj_items;
                obji.EnsureIndex(ob => ob.hash);
                foreach (var item in objItem)
                {
                    var obj = obji.FindOne(x=>x.hash == item.hash);
                    if (obj == null)
                    {
                        BsonValue bsonValue = obji.Insert(item);
                       // ALog.Log("01 OBJ 表中插入数据 {0} ; {1}", item.hash, bsonValue);
                        //account.obj_items.Add(item);
                    }
                }
                //_mapper.SerializeNullValues = false;
                //  var dOrder = _mapper.ToDocument<Account>(acc);


                accounts.Update(acc);

                var newacc = accounts.FindById(acc.Id);

                ALog.Log("更新了 用户{0}账号信息 {1}", newacc.address, newacc.ToString());
            }
            else
            {
                ALog.Error("没有这个账号{0}，不能进行更新", acc.address);
            }
          //  var newacc = 
            return accounts.FindById(acc.Id);


        }

        /// <summary>
        /// Get accounts with paging, page number and result per page
        /// </summary>
        public IEnumerable<Account> GetRange(int pageNumber, int resultPerPage)
        {
            var accounts = GetAll();

            accounts.EnsureIndex(x => x.updated_time);

            var query = accounts.Query()
                .OrderByDescending(x => x.updated_time)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();

            return query;
        }

        /// <summary>
        /// 通过address获取账号
        /// </summary>
        public Account GetByAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return default;

            var accounts = GetAll();
            if (accounts is null)
            {
                return default;
            }

            accounts.EnsureIndex(x => x.address);

            Account[] all_address = accounts.Find(x => x.address == address).ToArray();

            if(all_address != null && all_address.Length > 1) ALog.Error("!!!!账户有多个："+ address +" ; " + ((all_address != null) ? all_address.Length : "空的"));
            return (all_address!=null && all_address.Length >0) ?all_address[0]: default;
        }

        /// <summary>
        /// 通过Public Key获取账号
        /// </summary>
        public Account GetByPubKey(string pubkey)
        {
            var accounts = GetAll();

            accounts.EnsureIndex(x => x.pub_key);

            return accounts.FindOne(x => x.pub_key == pubkey);
        }

        private ILiteCollection<Account> GetAll()
        {
            var accounts = _db.GetCollection<Account>(AConstants.TBL_ACCOUNTS);
            //var number_item = _db.GetCollection<StorageNumberItem>("number_item");
            //var obj_item = _db.GetCollection<StorageObjItem>("obj_item");

            _mapper.Entity<Account>()
               .DbRef(x => x.number_item, "number_item")   // 1 对 1/0 引用
               .DbRef(x => x.obj_items, "obj_items");  // 1 对多引用

            var query = accounts
               .Include(x => x.number_item)
               .Include(x => x.obj_items)
               .Include(x => x.nickname)
               .Include(x => x.avatar)
               .Include(x => x.tokens)
               .Include(x => x.address)
               .Include(x => x.pub_key)
               .Include(x => x.txn_count)
               .Include(x => x.created_time)
               .Include(x => x.Id)
               .Include(x => x.updated_time);

          
            // .FindAll();
            return query;
        }

        private ILiteCollection<StorageNumberItem> GetAll_NI()
        {
            var number_item = _db.GetCollection<StorageNumberItem>("number_item");

            return number_item;
        }
        private ILiteCollection<StorageObjItem> GetAll_OI()
        {
            var obj_item = _db.GetCollection<StorageObjItem>("obj_items");//.EnsureIndex(ob => ob.hash);

            return obj_item;
        }

    }
}