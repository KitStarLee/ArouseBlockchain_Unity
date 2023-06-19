// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using LiteDB;
using ArouseBlockchain.Common;
using System.Linq;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain.DB
{
    /// <summary>
    /// Block Database to keep block persistant
    /// </summary>
    public class BlockDb: IDbFun
    {
        private readonly LiteDatabase _db;

        public BlockDb(LiteDatabase db)
        {
            _db = db;
        }
        public void Close()
        {

            _db?.Dispose();
        }

        /// <summary>
        /// Add block
        /// </summary>
        public bool Add(Block block)
        {
            var blocks = GetAll();
            try
            {
                blocks.Insert(block);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get First Block or Genesis block, ordered by block Height
        /// </summary>
        public Block GetFirst()
        {
            return GetAll().FindAll().FirstOrDefault();
        }

        /// <summary>
        /// Get Last block ordered by block weight
        /// </summary>
        public Block GetLast()
        {
            return GetAll().FindOne(Query.All(Query.Descending));
        }

        /// <summary>
        /// Get Block by Block weight
        /// </summary>
        public Block GetByHeight(long weight)
        {
            var blockCollection = GetAll();
            var blocks = blockCollection.Query().Where(x => x.height == weight).ToList();

            if (blocks.Any())
            {
                return blocks.FirstOrDefault();
            }

            return default;
        }

        /// <summary>
        /// Get Block by block Hash
        /// </summary>
        public Block GetByHash(string hash)
        {
            var blockCollection = GetAll();
            var blocks = blockCollection.Query().Where(x => x.hash == hash).ToList();

            if (blocks.Any())
            {
                return blocks.FirstOrDefault();
            }

            return default;
        }

        /// <summary>
        /// Get blocks with paging, page number and number of row per page
        /// </summary>
        public List<Block> GetRange(int pageNumber, int resultPerPage)
        {
            var blockCollection = GetAll();

            blockCollection.EnsureIndex(x => x.height);

            var query = blockCollection.Query()
                .OrderByDescending(x => x.height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();

            return query;
        }

        /// <summary>
        /// 获取给定的Height之后的100个区块
        /// </summary>
        /// <param name="startHeight">开始区块</param>
        /// <returns></returns>
        public List<Block> GetRemaining(long startHeight, int count = 50)
        {
            var blockCollection = GetAll();

            blockCollection.EnsureIndex(x => x.height);

            var query = blockCollection.Query()
                .OrderByDescending(x => x.height)
                .Where(x => x.height >= startHeight && x.height <= startHeight + count)
                .ToList();

            return query;
        }

        /// <summary>
        /// Get last blocks 
        /// </summary>
        public List<Block> GetLast(int num)
        {
            var blockCollection = GetAll();

            blockCollection.EnsureIndex(x => x.height);

            var query = blockCollection.Query()
                .OrderByDescending(x => x.height)
                .Limit(num).ToList();

            return query;
        }

        /// <summary>
        /// Get blocks that validate by address / validator
        /// </summary>
        public IEnumerable<Block> GetByValidator(string address, int pageNumber, int resultPerPage)
        {
            var blockCollection = GetAll();

            blockCollection.EnsureIndex(x => x.validator);

            var query = blockCollection.Query()
                .OrderByDescending(x => x.height)
                .Where(x => x.validator == address)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();

            return query;
        }

        /// <summary>
        /// Get all blocks
        /// </summary>
        public ILiteCollection<Block> GetAll()
        {
            var blockCollection = _db.GetCollection<Block>(AConstants.TBL_BLOCKS);

            blockCollection.EnsureIndex(x => x.height);

            return blockCollection;
        }

        /// <summary>
        /// Get all hash of all blocks
        /// </summary>
        public IList<string> GetHashList()
        {
            var blockCollection = GetAll();

            IList<string> hashList = new List<string>();

            foreach (var block in blockCollection.FindAll())
            {
                hashList.Add(block.hash);
            }

            return hashList;
        }
    }
}