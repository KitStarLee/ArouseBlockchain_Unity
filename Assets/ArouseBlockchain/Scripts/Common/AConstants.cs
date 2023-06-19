using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArouseBlockchain.P2PNet;
using UnityEngine;

namespace ArouseBlockchain.Common {


    //byte :0～255
    //<< ：最大不能超过7，8的时候已经是256了，就不能用Byte了
    //如果要改成更大的数，需要把消息体中的Byte也得修改，不然消息发出去还全是Byte
    [Flags]
    public enum MESSatus : byte
    {

        NONE= 0,
        //---------成功类型----------//
        /// <summary>
        ///成功且有内容 
        /// </summary>
        STATUS_SUCCESS = 1 << 0 | STATUS_SUC_NONE, 
        /// <summary>
        /// 成功但只是点头，没有任何多余内容，不用进行数据的解析，避免造成消息太大影响传送
        /// </summary>
        STATUS_SUC_NONE = 1 << 1, //2



        //---------告警类型----------//
        /// <summary>
        /// 0b_0000_0100
        /// </summary>
        STATUS_WARN = 1 << 2, //4


        //---------错误类型----------//
        /// <summary>
        /// 未知错误
        /// </summary>
        STATUS_ERROR = 1 << 3| STATUS_ERROR_BAD | STATUS_ERROR_NOT_FOUND | STATUS_ERROR_TIMEOUT, //8|16|32|64
        /// <summary>
        /// 服务端无法或不会处理请求内容
        /// </summary>
        STATUS_ERROR_BAD = 1 << 4, // 16
        /// <summary>
        /// 请求的Peer由于离线或者错误，无法找到进行通讯
        /// </summary>
        STATUS_ERROR_NOT_FOUND = 1 << 5,  //32
        /// <summary>
        /// 请求超时
        /// </summary>
        STATUS_ERROR_TIMEOUT = 1 << 6,  //64
    }


    public static class AConstants
	{
        public const int VERSION = 0;
        public const double DEFAULT_TRANSACTION_FEE = 0.001;
        public const double COINT_REWARD = 0.001f;

        public const string TBL_BLOCKS = "tbl_blocks";
        public const string TBL_TRANSACTIONS = "tbl_txns";
        public const string TBL_TRANSACTIONS_POOL = "tbl_txns_pool";
        public const string TBL_STAKES = "tbl_stakes";
        public const string TBL_PEERS = "tbl_peers";
        public const string TBL_ACCOUNTS = "tbl_accounts";

        public const string TXN_TYPE_STAKE = "Staking";
        public const string TXN_TYPE_TRANSFER = "Transfer";
        public const string TXN_TYPE_VALIDATOR_FEE = "Validation_Fee";

        //响应服务(SResponse)在回复Peer的时间限制，超出则回复错误
        public const int SERRESPONSE_TIMEOUT = 7;
        //请求服务(CRequest)在请求Peer的时间限制，超出则请求停止并超时
        public const int CLIREQUEST_TIMEOUT = 8;
        //广播的时间限制，超时则不等待，广播是异步的，耗能不高加上无链接状态的信息传递会有丢失，可以时间长些
        public const int BROADCAST_TIMEOUT = 8;

    }

    public static class ExtensionMethods
    {
        public static bool IsError(this ISResponseMessage ResMes)
        {
            if ((ResMes._ReStatus._Status & MESSatus.STATUS_ERROR) != MESSatus.NONE)
            {
                ALog.Error("{0}: 收到错误: {1}->{2}", ResMes.GetType(), ResMes._ReStatus._Status, ResMes._ReStatus._Message);
                return true;
            }
            return false;

        }

        public static bool IsError(this ResponseStatus ResStatus, string logPrefix = "ResStatus")
        {
            if ((ResStatus._Status & MESSatus.STATUS_ERROR) != MESSatus.NONE)
            {
                ALog.Error("{0}: 收到错误: {1}->{2}", logPrefix, ResStatus._Status, ResStatus._Message);
                return true;
            }
            return false;

        }

        public static bool IsSuccNone(this ISResponseMessage ResMes)
        {
            if ((ResMes._ReStatus._Status & MESSatus.STATUS_SUCCESS) == MESSatus.STATUS_SUC_NONE)
            {
                ALog.Log("{0}: 成功,但没有返回的数据: {1}->{2}", ResMes.GetType(), ResMes._ReStatus._Status, ResMes._ReStatus._Message);
                return true;
            }
            return false;
        }
        public static bool IsSuccNone(this ResponseStatus ResStatus , string logPrefix = "ResStatus")
        {
            if ((ResStatus._Status & MESSatus.STATUS_SUCCESS) == MESSatus.STATUS_SUC_NONE)
            {
                ALog.Log("{0}: 成功,但没有返回的数据: {1}->{2}", logPrefix, ResStatus._Status, ResStatus._Message);
                return true;
            }
            return false;
        }

        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<object>();
            asyncOp.completed += obj => { tcs.SetResult(null); };
            return ((Task)tcs.Task).GetAwaiter();
        }
    }
}