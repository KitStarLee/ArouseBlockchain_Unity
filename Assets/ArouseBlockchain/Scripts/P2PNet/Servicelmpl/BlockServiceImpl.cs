using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using Newtonsoft.Json;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.NetService;
using static ArouseBlockchain.P2PNet.NetTransaction;
using System.Net;
using System.Threading;
using SuperNet.Netcode.Transport;
using ArouseBlockchain.Solve;
using static ArouseBlockchain.P2PNet.NetBlock;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using Cysharp.Threading.Tasks;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Net;

namespace ArouseBlockchain.P2PNet
{
	public class BlockServiceImpl : NetBlock.BlockServer
    {

        /// <summary>
        /// 由于用户在线的时间点和时长不同，不能保证每个用户的块都是同一时间都是持平的。
        /// 1. 所以用户的在验证的话，需要先验证区块的工作量或者权益证明首先是否正确，否则肯定是错的
        /// 2. 其次在验证是否正好是本地的下一个区块，如果是则确定是正确的话，并且是我需的
        /// 3. 如果用户没有同步第N个块的块就收到N+1的块，则它只能验证权益证明。由于不知道自己是否和全链高度持平，所以返回不确定的消息
        /// 4. 构建方再收到验证回复后，如果错误占回复总量的20%则构建识别并惩罚，否则证明正确，则本地构建，其他收取方需要的则也进行同步。
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="request"></param>
        /// <param name="_CancelToken"></param>
        public override void ValidProofSResponse(Peer peer, BuildCRequest request, CancellationTokenSource _CancelToken)
        {
            ValidProofSResponse sResponse = new ValidProofSResponse();

            var lastBlock = SolveManager.BlockChain.Block.GetLast();

            Block newBlock = request._Block;

            //验证矿工答案的正确性
            if (!AUtils.BC.Valid_Proof(newBlock.prev_hash, newBlock.transactions, newBlock.difficulty, newBlock.nonce, newBlock.proof))
            {
                SendError(peer, sResponse, MESSatus.STATUS_ERROR_BAD, "非法的新块");

                return;
            }

            var results = sResponse._Verification;
            results._Status = MESSatus.STATUS_SUC_NONE; //验证成功
            sResponse._Verification = results;

            SendSucc(peer, sResponse, MESSatus.STATUS_SUCCESS);

            if (AUtils.BC.IsNextBlock(lastBlock, newBlock))
            {
                //这里已经验证过了，不需要重复验证
                SolveManager.BlockChain.Block.ValidNextBlockAndSave(newBlock,false);
            }
            else
            {
                if ((lastBlock.height + 1) < newBlock.height)
                {
                    if (_DCTS == null)
                    {
                        BlockClient client = new BlockClient(peer);
                        //请求获取这个Peer的块
                        client.DownCRequest(lastBlock.height + 1, default);
                    }
                    else
                    {
                        ALog.Log("广播消息：不要频繁的获取区块，回复量太大，会造成性能宕机");
                    }
                   
                }
               
            }
        }

        /// <summary>
        /// 接受到无链接的广播消息
        /// </summary>
        /// <param name="address"></param>
        /// <param name="request"></param>
        /// <param name="_CancelToken"></param>
        public override void ValidProofSResponse_UNC(IPEndPoint address, BuildCRequest_UNC request, CancellationTokenSource _CancelToken)
        {
            ValidProofSResponse_UNC sResponse = new ValidProofSResponse_UNC();

            var lastBlock = SolveManager.BlockChain.Block.GetLast();

            Block newBlock = request._Block;

            //验证矿工答案的正确性
            if (!AUtils.BC.Valid_Proof(newBlock.prev_hash, newBlock.transactions, newBlock.difficulty, newBlock.nonce, newBlock.proof))
            {
                SendUNCError(address, sResponse, MESSatus.STATUS_ERROR_BAD, "非法的新块");

                return;
            }

            var results = sResponse._Verification;
            results._Status = MESSatus.STATUS_SUC_NONE; //验证成功
            sResponse._Verification = results;

            SendUNCSucc(address, sResponse, MESSatus.STATUS_SUCCESS);

            if (AUtils.BC.IsNextBlock(lastBlock, newBlock))
            {
                //这里已经验证过了，不需要重复验证
                SolveManager.BlockChain.Block.ValidNextBlockAndSave(newBlock, false);
            }
            else
            {
                if ((lastBlock.height + 1) < newBlock.height)
                {
                    if(_DCTS == null)
                    {
                       DownCResponse(address, (lastBlock.height + 1));
                    }
                    else
                    {
                        ALog.Log("不要频繁的获取区块，回复量太大，会造成性能宕机");
                    }
                  
                }
            }
        }



        CancellationTokenSource _DCTS = new CancellationTokenSource();
        /// <summary>
        /// 进行长链接，索取块
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        async UniTask<bool> DownCResponse(IPEndPoint address, long needHeight)
        {
            NewCancellationToken();

            NodePeerClient peerClient = new NodePeerClient();
            var connect_status = await peerClient.P2PConnectRequest(address, _DCTS.Token);

            if (!connect_status.IsError())
            {
                var netPeer = P2PManager.Instance.IsContainsInConnected(address);

                if(netPeer == null || netPeer.Peer == null || !netPeer.Peer.Connected)
                {
                    ALog.Error("显示链接成功，但是本地Host又没有这个Peer的链接实例！！ netPeer == {0} ; netPeer.Peer == {1} ", (netPeer==null ), (netPeer.Peer == null));
                    CLoseCancellationToken();
                    return false;
                }

                BlockClient client = new BlockClient(netPeer.Peer);

                //请求获取这个Peer的块
                await client.DownCRequest(needHeight, _DCTS.Token);

                CLoseCancellationToken();
                return true;

            }
            else
            {
                CLoseCancellationToken();
                return false;
            }

            void CLoseCancellationToken()
            {
                if (_DCTS != null)
                {
                    _DCTS.Cancel();
                    _DCTS.Dispose();
                    _DCTS.CancelAfterSlim(TimeSpan.FromSeconds(AConstants.CLIREQUEST_TIMEOUT));
                    _DCTS = null;
                }

            }

            void NewCancellationToken()
            {
                CLoseCancellationToken();

                _DCTS = new CancellationTokenSource();
                _DCTS.CancelAfterSlim(TimeSpan.FromSeconds(AConstants.CLIREQUEST_TIMEOUT));

            }

        }

        public override void DownSResponse(Peer peer, DownCRequest message, CancellationTokenSource _CancelToken)
        {
            DownSResponse sResponse = new DownSResponse();

            var lastBlock = SolveManager.BlockChain.Block.GetLast();

            var needHeight = message._need_height;

            if(needHeight > lastBlock.height)
            {
                SendError(peer, sResponse, MESSatus.STATUS_ERROR_NOT_FOUND, string.Format("我方没有{0}以上的块", needHeight));

                return;
            }

            //确保最多给予 50个块，信息体不能过大
            var newBlocks = SolveManager.BlockChain.Block.GetRemaining(needHeight,50);

            sResponse._Block = newBlocks;

            SendSucc(peer, sResponse, MESSatus.STATUS_SUCCESS);

        }
    }

}