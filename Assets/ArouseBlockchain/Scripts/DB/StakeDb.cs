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
    public class StakeDb : IDbFun
    {
        protected readonly LiteDatabase _db;
       // protected readonly BsonMapper _mapper;

        public StakeDb(LiteDatabase db)
        {
            _db = db;
          //  _mapper = mapper;
        }
        public void Close()
        {

            _db?.Dispose();
        }
        /// <summary>
        /// add or update stake
        /// </summary>
        public Stake AddOrUpdate(Stake stake)
        {
            if (string.IsNullOrEmpty(stake.address))
            {
                ALog.Error("质押 是空 {0} 的地址", stake.address);
                return default;
            }

            var stakers = GetAll();

            var locStake = GetByAddress(stake.address);
            //if (locStake is null)
            if (string.IsNullOrEmpty(locStake.address))
            {
                locStake = stake;
                locStake.Id = stakers.Insert(stake);
            }
            else
            {
                stake.Id = locStake.Id; //重置为同一个ID
                stakers.Update(stake);
            }

            return stakers.FindById(locStake.Id);

        }

        /// <summary>
        /// Delete all stake
        /// </summary>
        public void DeleteAll()
        {
            var stakers = GetAll();
            if (stakers is null || stakers.Count() < 1)
            {
                return;
            }

            stakers.DeleteAll();
        }

        /// <summary>
        /// Get maximum stake, base on amount
        /// </summary>
        public Stake GetMax()
        {
            var stakes = GetAll();
            if (stakes is null || stakes.Count() < 1)
            {
                return default;
            }

            stakes.EnsureIndex(x => x.amount);

            var query = stakes.Query()
                .OrderByDescending(x => x.amount);

            return query.FirstOrDefault();
        }


        /// <summary>
        /// Get maximum stake, base on amount
        /// </summary>
        public List<Stake> GetALL()
        {
            var stakes = GetAll();
            if (stakes is null || stakes.Count() < 1)
            {
                return null;
            }

            stakes.EnsureIndex(x => x.amount);

            var query = stakes.Query()
                .OrderByDescending(x => x.amount);

            return query.ToList();
        }

        /// <summary>
        /// Get stake by address
        /// </summary>
        public Stake GetByAddress(string address)
        {
            var stakes = GetAll();
            if (stakes is null)
            {
                return default;
            }

            stakes.EnsureIndex(x => x.address);

            var stake = stakes.FindOne(x => x.address == address);

            return stake;
        }

        /// <summary>
        /// Get all stake
        /// </summary>
        ILiteCollection<Stake> GetAll()
        {
            var stakes = _db.GetCollection<Stake>(AConstants.TBL_STAKES);

            stakes.EnsureIndex(x => x.address);

            return stakes;
        }
    }
}