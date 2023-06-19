// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using LiteDB;
using ArouseBlockchain.Common;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using static ArouseBlockchain.P2PNet.NetNodePeer.NodePeer;
using System.Linq;

namespace ArouseBlockchain.DB
{
    /// <summary>
    /// Peer database, for add, update list of peers
    /// </summary>
    public class NodePeerDb: IDbFun
    {

        private readonly LiteDatabase _db;
        protected readonly BsonMapper _mapper;

        public NodePeerDb(LiteDatabase db ,BsonMapper mapper)
        {
            _db = db;
            _mapper = mapper;
            _mapper.SerializeNullValues = false;
            // var typedCustomerCollection = _db.GetCollection<NodePeer>("peer");
        }
        public void Close()
        {

            _db?.Dispose();
        }

        /// <summary>
        /// Add a peer
        /// </summary>
        public void Add(NodePeer peer)
        {
            var peerandpeers = GetAllAndFindByAddress(peer.address_public);

            if (string.IsNullOrEmpty(peerandpeers.fingNode.address_public))
            {
                var AllNodes = peerandpeers.allNode.Include(x => x.node_state);

                var states = GetAll_State();


                if (peer.node_state == null) peer.node_state = new NodeState();
                states.Insert(peer.node_state);

                _mapper.SerializeNullValues = false;
                var dOrder = _mapper.ToDocument<NodePeer>(peer);

                AllNodes.Insert(peer);
            }
            else {
                ALog.Error("已经存在{0}Node，不能重复添加{1}", peer.address_public, peer.node_state.height);
                return;
            }
        }

        public void Updaet(NodePeer peer)
        {
            var peerandpeers = GetAllAndFindByAddress(peer.address_public);

            if (!string.IsNullOrEmpty(peerandpeers.fingNode.address_public))
            {
                var AllNodes = peerandpeers.allNode.Include(x => x.node_state);

                //if (AllNodes.Count() > 1) {
                //    ALog.Error("理论上不应该有相同的节点，{}个！！！" , AllNodes.Count());
                //}

                peerandpeers.fingNode.node_state = peer.node_state;

                var states = GetAll_State();

                if(peerandpeers.fingNode.node_state == null) {
                    states.Insert(peer.node_state);
                }
                else {
                    peerandpeers.fingNode.node_state.hash = peer.node_state.hash;
                    peerandpeers.fingNode.node_state.height = peer.node_state.height;
                    peerandpeers.fingNode.node_state.version = peer.node_state.version;

                    states.Update(peerandpeers.fingNode.node_state);
                }
               
                AllNodes.Update(peer);
            }
        }

        public void DeleteAll()
        {
            var peers = GetAll();
            if (peers is null || peers.Count() < 1)
            {
                return;
            }
            peers.DeleteAll();
        }

        /// <summary>
        /// Add a peer
        /// </summary>
        public void AddBulk(NodePeer[] peers)
        {
            if (peers == null || peers.Length < 1)
            {
                ALog.Error("无法替换过少或空或无效的列表");
                return;
            }
           
            var allpeers = GetAll().Include(x => x.node_state);
            if (allpeers is null)
            {
                ALog.Error("是空的，删除完");
            }

            for (int i = 0; i < peers.Length; i++)
            {
                if (peers[i].node_state == null)
                {
                    ALog.Error("这个Node {0} 的 node_state 是空的", peers[i].address_public);
                    peers[i].node_state = new NodeState();
                }
            }
            allpeers.InsertBulk(peers);
            
        }
        public void AddBulk(List<NodePeer> peers)
        {
            if (peers == null || peers.Count < 1)
            {
                ALog.Error("无法替换过少或空或无效的列表");
                return;
            }
           
            AddBulk(peers.ToArray());

        }

        /// <summary>
        /// 自检，去除哪些无用无价值的数据
        /// </summary>
        public int CheckUp()
        {

            //获取Customers集合 varcol=db.GetCollection<Customer>("customers");
            //创建一个对象 varcustomer=newCustomer { Name="JohnDoe", Phones=newstring[]{"8000-0000","9000-0000"}, Age=39, IsActive=true };
            //在Name字段上创建唯一索引 col.EnsureIndex(x=>x.Name,true);
            //数据插入 col.Insert(customer);
            //数据查询 List<Customer>list=col.Find(x=>x.Age>20).ToList(); Customeruser=col.FindOne(x=>x.Age>20);
            //数据删除 col.Delete(user.Id);
            var peers = GetAll();
            int deleteCount = peers.DeleteMany(p => !p.IsStabilize) ;


            return deleteCount;

        }

        //public bool Exists()
        //{
        //    _db.CollectionExists();
        //}

        /// <summary>
        /// Get list of peer, page number and number of row per page
        /// </summary>
        public List<NodePeer> GetRange(int pageNumber, int resultPerPage)
        {
            var peers = GetAll();

            peers.EnsureIndex(x => x.report_reach);

            var query = peers.Query()
                .OrderByDescending(x => x.report_reach)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();

            return query;
        }


        /// <summary>
        /// Get all peer
        /// </summary>
        public ILiteCollection<NodePeer> GetAll()
        {
            var peers = _db.GetCollection<NodePeer>(AConstants.TBL_PEERS);
            var query = peers
            .Include(x => x.node_state);

            return query;
        }

        private ILiteCollection<NodeState> GetAll_State()
        {
            return _db.GetCollection<NodeState>("node_state");

           // return number_item;
        }


        (ILiteCollection<NodePeer> allNode,NodePeer fingNode) GetAllAndFindByAddress(string address)
        {
            var peers = GetAll();
            if (peers is null)
            {
                return (null,default(NodePeer));
            }

            peers.EnsureIndex(x => x.address_public);

            return (peers, peers.FindOne(x => x.address_public == address));
        }

        /// <summary>
        /// Get peer by network address/IP
        /// </summary>
        public NodePeer GetByAddress(string address)
        {
            var peers = GetAll();
            if (peers is null)
            {
                return default(NodePeer);
            }

            peers.EnsureIndex(x => x.address_public);

            return peers.FindOne(x => x.address_public == address);
        }
    }
}