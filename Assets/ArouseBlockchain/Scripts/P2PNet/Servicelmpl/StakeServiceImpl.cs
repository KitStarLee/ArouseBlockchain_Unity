using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using Newtonsoft.Json;
using ArouseBlockchain.NetService;
using ArouseBlockchain.P2PNet;
using SuperNet.Netcode.Transport;
using ArouseBlockchain.Solve;
using System.Threading;
using UnityEditor.Build.Content;
using System.Net;

namespace ArouseBlockchain.P2PNet
{
    public class StakeServiceImpl : NetStake.NetStakeServer
    {
        public override void AddSResponse(IPEndPoint address, AddCRequest message, CancellationTokenSource _CancelToken)
        {
            AddSResponse response = new AddSResponse();

            if (message._Account.Equals(null) || message._Stake.Equals(null))
            {
                var error = string.Format("请求错误，不是合法的参数{0}, {1}", message._Account.ToString(),
                    message._Stake.ToString());

                SendUNCError(address, response, MESSatus.STATUS_ERROR_BAD, error);
               
                return;
            }

            if (_CancelToken.IsCancellationRequested)
            {
                SendUNCError(address, response, MESSatus.STATUS_ERROR_TIMEOUT);
                return;
            }
            DBSolve.DB.Account.AddOrUpdate(message._Account); //更新本地账号信息

            var allLoaclStake = DBSolve.DB.Stake.GetALL();

            DBSolve.DB.Stake.AddOrUpdate(message._Stake);  //添加质押

            response._Stakes = allLoaclStake;

            SendUNCSucc(address, response, (allLoaclStake.Count <= 0)? MESSatus.STATUS_SUC_NONE : MESSatus.STATUS_SUCCESS);
        }

        public override void ValidatorSResponse(Peer peer, ValidatorCRequest message, CancellationTokenSource _CancelToken)
        {
            ValidatorSResponse response = new ValidatorSResponse();

            //验证代码,理论上怎么被出块权益抽签 选中，则怎么严重，和POW逻辑一样。这里直接就同意，正式项目需要补充逻辑。

            SendSucc(peer, response);
        }



    }

}