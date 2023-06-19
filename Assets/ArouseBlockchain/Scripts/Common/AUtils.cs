using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net.NetworkInformation;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Text.RegularExpressions;
using ArouseBlockchain.P2PNet;
using SuperNet.Netcode.Util;
using System.Net;
using ArouseBlockchain.Solve;
using static ArouseBlockchain.P2PNet.NetTransaction;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using Cysharp.Threading.Tasks;
using System.Net.Sockets;
using UnityEngine.SocialPlatforms;
using ArouseBlockchain.DB;
using BestHTTP.JSON.LitJson;
using Newtonsoft.Json;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace ArouseBlockchain.Common
{
    public static class AUtils
    {
        public static bool IRS
        {

            get
            {

#if RelayServer && (UNITY_STANDALONE) && !UNITY_EDITOR

                return true;
#endif
                return false;

            }
        }

        /// <summary>
        /// 异步加载Http或者文件
        /// </summary>
        /// <param name="PathOrUrl">需要获取的文件路径或者Htpp地址</param>
        /// <param name="timeout">容许的最大超时时间</param>
        /// <returns></returns>
        public static async UniTask<string> AsyncLoadTextFile(string PathOrUrl, int timeout = 10)
        {

            bool isFile = false;
            var if_url = PathOrUrl;
            if (if_url.ToLower().Contains("http://") || if_url.ToLower().Contains("https://")) { }
            else isFile = true;

            if (isFile)
            {
                if (!File.Exists(PathOrUrl))
                {
                    ALog.Error("The file does not exist :" + PathOrUrl);
                    return null;
                }
            }

            try
            {
                var getRequest = UnityWebRequest.Get(PathOrUrl);
                getRequest.timeout = timeout;
                await getRequest.SendWebRequest();

               // var txt = (await UnityWebRequest.Get("https://...").SendWebRequest()).downloadHandler.text;

                bool connectionError = getRequest.result == UnityWebRequest.Result.ConnectionError;
                bool protocolError = getRequest.result == UnityWebRequest.Result.ProtocolError;

                if (getRequest.isDone && string.IsNullOrWhiteSpace(getRequest.error))
                {
                    return getRequest.downloadHandler.text;
                }
                else
                {
                    ALog.Error("File read error: {0} ; connectionError= {1} ; protocolError= {2}" , getRequest.error , connectionError , protocolError);
                    return null;
                }


            }
            catch (Exception ex)
            {
                ALog.Error("Load Resources " + PathOrUrl + " Assets :" + ex);
                return null;
            }

        }


        public static class Net
        {

            public static IPEndPoint IPToIP4(IPEndPoint adddress)
            {
                if (adddress == null)
                {
                    ALog.Error("地址是空的 {0}", (adddress == null));
                    return null;
                }
                

                if (adddress.AddressFamily == AddressFamily.InterNetworkV6) adddress = new IPEndPoint(adddress.Address.MapToIPv4(), adddress.Port);

                return adddress;
            }

            public static bool IsIPSame(string adddress1, IPEndPoint adddress2)
            {
                var add1 = IPResolver.TryParse(adddress1);
                return IsIPSame(add1, adddress2);

            }

            public static bool IsIPSame(string adddress1, string adddress2)
            {
                var add1 = IPResolver.TryParse(adddress1);
                var add2 = IPResolver.TryParse(adddress2);

                return IsIPSame(add1, add2);

            }

            public static bool IsIPSame(IPEndPoint adddress1, IPEndPoint adddress2)
            {
                if (adddress1 == null || adddress2 == null) {
                    if(adddress1 == adddress2) return true;
                    ALog.Error("地址是空的 1: {0} 2:  {1}", adddress1, adddress2 );
                    return false;
                }

                if( adddress1.AddressFamily == adddress2.AddressFamily) {
                    return adddress1 == adddress2;
                }
                return (IPToIP4(adddress1).ToString() == IPToIP4(adddress2).ToString());

            }

            /// <summary>
            /// 是否局域网地址
            /// A类地址：10.0.0.0 - 10.255.255.255 
            /// B类地址：172.16.0.0 - 172.31.255.255 
            /// C类地址：192.168.0.0 -192.168.255.255 
            /// </summary>
            /// <param name="address"></param>
            /// <returns></returns>
            public static bool IsInternal(IPAddress address)
            {
                if (IPAddress.IsLoopback(address)) return true;
                if (address.ToString() == "::1") return true;
                byte[] ip = address.GetAddressBytes();
                switch (ip[0])
                {
                    case 10: return true;
                    case 127: return true;
                    case 172: return ip[1] >= 16 && ip[1] < 32;
                    case 192: return ip[1] == 168;
                    default: return false;
                }
            }

            /// <summary>
            /// 判断是否是局域网，如果是的话返回局域网地址，否则给予公网
            /// </summary>
            /// <param name="nodePeer"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public static IPEndPoint FindRightNetAddress(NodePeer nodePeer)
            {
                IPEndPoint address = null;
                IPEndPoint address_local = string.IsNullOrEmpty(nodePeer.address_local) ? null : IPResolver.Resolve(nodePeer.address_local);
                IPEndPoint address_remote = string.IsNullOrEmpty(nodePeer.address_remote)  ? null : IPResolver.Resolve(nodePeer.address_remote);
                IPEndPoint address_public = string.IsNullOrEmpty(nodePeer.address_public) ? null : IPResolver.Resolve(nodePeer.address_public);

                if (address_public!=null && address_public.Address.ToString() == "127.0.0.1")
                {
                    NodePeer m_NodePeer = SolveManager.BlockChain.NodePeer.m_NodePeer;
                    IPEndPoint myPublic = IPResolver.TryParse(m_NodePeer.address_public);
                    IPEndPoint myLocal = IPResolver.TryParse(m_NodePeer.address_local);

                    address_public = new IPEndPoint(myLocal.Address, address_public.Port);
                }

                if (P2PManager.PublicIP != null && P2PManager.PublicIP.Equals(address_public?.Address))
                {
                    //如果和本地NodePeer公网地址相同，那肯定是本地局域网环境，则不需要公网IP链接
                    address = address_local ?? address_remote ?? throw new Exception("No local address");
                }
                else
                {
                    //否则需要通过public id 进行链接
                    address = address_public ?? address_remote ?? throw new Exception("No public address");
                }

                return address;
            }

            public static bool IsSameNodePeer(NodePeer nodePeer1, NodePeer nodePeer2)
            {
                IPEndPoint address1 = FindRightNetAddress(nodePeer1);
                IPEndPoint address2 = FindRightNetAddress(nodePeer2);

                bool isSame = IsIPSame(address1, address2);
                     
                if (isSame) ALog.Error(string.Format("that nodePeer1 :{0} and nodePeer2 {1} is same !"
                    , nodePeer1.ToString(),
                    nodePeer2.ToString()
                    ));

                return isSame;
            }

            //public static bool IsSameNodePeer(IPEndPoint peerAddress, NodePeer nodePeer)
            //{
            //    IPEndPoint address1 = FindRightNetAddress(nodePeer);

            //    bool isSame = (IsIPSame(address1, peerAddress));
              
            //    ALog.Error(string.Format("that peerAddress :{0} and nodePeer {1} is Same!"
            //        , peerAddress.ToString(),
            //        nodePeer.ToString()
            //        ));
            //    return isSame;
            //}
        }
        /// <summary>
        /// 获取设备的Mac地址
        /// </summary>
        /// <returns></returns>
        public static string DeviceMacAddress
        {
            get
            {
                string physicalAddress = "";

                //UNITY_EDITOR 编辑器 用于从您的游戏代码调用 Unity 编辑器脚本的脚本符号。
                //UNITY_EDITOR_WIN Windows 上编辑器代码的脚本符号。
                //UNITY_EDITOR_OSX Mac OS X 上编辑器代码的脚本符号。
                //UNITY_EDITOR_LINUX Linux 上编辑器代码的脚本符号。

#if UNITY_ANDROID
                physicalAddress = SystemInfo.deviceUniqueIdentifier;
                return physicalAddress;
#elif UNITY_IOS
            physicalAddress = Device.vendorIdentifier;
             return physicalAddress;
#elif UNITY_STANDALONE

#endif

                if (physicalAddress != "") return physicalAddress;

                NetworkInterface[] nice = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface adaper in nice)
                {
                    if (adaper.Description == "en0")
                    {
                        physicalAddress = adaper.GetPhysicalAddress().ToString();
                        break;
                    }
                    else
                    {
                        physicalAddress = adaper.GetPhysicalAddress().ToString();

                        if (physicalAddress != "")
                        {
                            break;
                        };
                    }
                }

                return physicalAddress;
            }

        }


        static List<CancellationTokenSource> CancellationList = new List<CancellationTokenSource>();
        /// <summary>
        /// 延时操作
        /// </summary>
        /// <param name="second">延时时间-秒</param>
        /// <param name="onComplete">当进度完成</param>
        /// <param name="nowCancellation">如果用户自己有管理CancellationToken，则为了避免同一个任务被多次出发可以把自己的记录放入进来</param>
        public static async void Delaye(int second, Action<CancellationTokenSource> onComplete, CancellationTokenSource nowCancellation = null)
        {
            CancellationTokenSource newCancellation = null;
            if (CancellationList.Exists((can) => { return can.Token == nowCancellation.Token; }))
            {
                nowCancellation?.Cancel();
                newCancellation = nowCancellation;
            }
            else
            {
                newCancellation = new CancellationTokenSource();
                CancellationList.Add(newCancellation);
            }


            if (CancellationList.Count > 10)
            {
                for (int i = 0; i < CancellationList.Count / 2; i++)
                {
                    CancellationList[i].Cancel();
                }
                CancellationList.RemoveRange(0, CancellationList.Count / 2);
            }

            if (second > 10)
            {
                ALog.Error("Delay time dont > 10 second");
                return;
            }
            await Task.Delay(second * 1000, newCancellation.Token);
            onComplete?.Invoke(newCancellation);
        }


        //public class AsyncWaitTrue
        //{
        //    int _MaxTime = 0;
        //    DateTime startTime = DateTime.Now;

        //    bool isUsed = false;
        //    public bool IsUseding => isUsed;

        //    public AsyncWaitTrue(int maxTime = 10)
        //    {
        //        _MaxTime = maxTime;
        //    }

        //    /// <summary>
        //    /// 开始计时等待，如果返回True证明等待完成，返回False证明超时返回
        //    /// </summary>
        //    /// <param name="predicate"></param>
        //    /// <param name="cancellationToken"></param>
        //    /// <returns></returns>
        //    public async UniTask<bool> Start(Func<bool> predicate, CancellationTokenSource cancellationToken)
        //    {
        //        startTime = DateTime.Now;
        //        isUsed = true;
        //        while (!predicate())
        //        {
        //            if ((DateTime.Now - startTime).Seconds > _MaxTime)
        //            {
        //                isUsed = false;
        //                return false;
        //            }
        //          //  await UniTask
        //           var isCanceled = await UniTask.Yield( PlayerLoopTiming.Update, cancellationToken.Token).SuppressCancellationThrow();
        //            if (isCanceled) {

        //                isUsed = false;
        //                return false;
        //            }

        //        }
        //        isUsed = false;
        //        return true;
        //    }

        //}


        /// <summary>
        /// 本地检查是否是合格的助记词
        /// </summary>
        /// <param name="text">助记词文本</param>
        /// <returns></returns>
        public static bool IsRightMnemonics(string text)
        {
            if (text.Length != 12) return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!Regex.IsMatch(text[i].ToString(), @"[\u4e00-\u9fbb]")) return false;
            }

            int temp = 0;
            for (int i = 0; i < text.Length; i++)
            {
                temp = i;
                for (int j = i + 1; j < text.Length; j++)
                {
                    if (text[temp] == text[j]) return false;
                }
            }

            return true;
        }



        static string[] sensitiveWords = new string[]
        {
            "妈","骚","逼","胸","道","毛","泽","东","进","平","习","死","杀","黄","毒","赌","色","嫖","娼",
            "骚","枪","骂","傻","垃","暴","恐","怖","党","中","国","美","最","娘","爷","孙","爸","丧","亡",
            "周","来","狗","屎","猪"
        };
        /// <summary>
        /// 获取随机的中文字符
        /// </summary>
        /// <param name="strlength"></param>
        /// <returns></returns>
        public static async Task<string> RandomChinese(int strlength = 32)
        {
            // 获取GB2312编码页（表）
            Encoding gb = Encoding.GetEncoding("gb2312");

            string conent = await Task.Run(() =>
            {
                object[] bytes = CreateRegionCode(strlength);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < strlength; i++)
                {
                    string temp = gb.GetString((byte[])Convert.ChangeType(bytes[i], typeof(byte[])));
                    foreach (var item in sensitiveWords)
                    {
                        if (temp == item) temp = "";
                    }
                    sb.Append(temp);
                }

                return sb.ToString();

            });

            return conent;

            object[] CreateRegionCode(int strlength)
            {
                string[] rBase = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

                System.Random rnd = new System.Random();

                object[] bytes = new object[strlength];

                for (int i = 0; i < strlength; i++)
                {
                    //区位码第1位
                    int r1 = rnd.Next(11, 14);
                    string str_r1 = rBase[r1].Trim();

                    //区位码第2位
                    rnd = new System.Random(r1 * unchecked((int)DateTime.Now.Ticks) + i);
                    int r2;
                    if (r1 == 13)
                    {
                        r2 = rnd.Next(0, 7);
                    }
                    else
                    {
                        r2 = rnd.Next(0, 16);
                    }
                    string str_r2 = rBase[r2].Trim();

                    //区位码第3位
                    rnd = new System.Random(r2 * unchecked((int)DateTime.Now.Ticks) + i);
                    int r3 = rnd.Next(10, 16);
                    string str_r3 = rBase[r3].Trim();

                    //区位码第4位
                    rnd = new System.Random(r3 * unchecked((int)DateTime.Now.Ticks) + i);
                    int r4;
                    if (r3 == 10)
                    {
                        r4 = rnd.Next(1, 16);
                    }
                    else if (r3 == 15)
                    {
                        r4 = rnd.Next(0, 15);
                    }
                    else
                    {
                        r4 = rnd.Next(0, 16);
                    }
                    string str_r4 = rBase[r4].Trim();

                    byte byte1 = Convert.ToByte(str_r1 + str_r2, 16);
                    byte byte2 = Convert.ToByte(str_r3 + str_r4, 16);

                    byte[] str_r = new byte[] { byte1, byte2 };

                    bytes.SetValue(str_r, i);
                }

                return bytes;
            }

        }


        ///时间尽量都用全球 Utc 统一时间，输出的时候可以转化为本地时间
        #region Time tools

        /// <summary>
        /// 获取世界UTC时间
        /// </summary>
        public static DateTime NowUtcTime => NowLocalTime.ToUniversalTime();
        /// <summary>
        /// 获取世界UTC时间的Unix格式
        /// </summary>
        /// <returns></returns>
        public static long NowUtcTime_Unix => ToUnixTimeByDateTime(NowUtcTime);// ToUnixTimeByDateTime(NowUtcTime);
        /// <summary>
        /// 获取本地时间
        /// </summary>
        public static DateTime NowLocalTime => DateTime.Now;

        public static long NowLocalTime_Unix => ToUnixTimeByDateTime(NowLocalTime);// ToUnixTimeByDateTime(NowUtcTime);

        ///// <summary>
        ///// 把本地时间转化为世界时间
        ///// </summary>
        //private static DateTime NowUtcTimeByLocal => TimeZoneInfo.ConvertTimeToUtc(NowLocalTime);
        ///// <summary>
        ///// 把Utc世界时间转化为本地时间
        ///// </summary>
        //public static DateTime NowLocalTimeByUtc => NowUtcTime.ToLocalTime();

        /// <summary>
        /// 把DateTime 转化为字符串
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string DateTimeToString(DateTime dateTime) { return string.Format("{0:MM/dd/yyy HH:mm:ss.fff}", dateTime); }

        /// <summary>
        /// 把UNIX DateTime 转化为字符串
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string UnixLoaclTimeToString(long unix_dateTime) { return string.Format("{0:MM/dd/yyy HH:mm:ss.fff}", ToDateTimeByLocalUnix(unix_dateTime)); }


        /// <summary>
        /// 把UNIX DateTime 转化为字符串
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string UnixUTCTimeToString(long unix_dateTime, bool isToLocal = true) { return string.Format("{0:MM/dd/yyy HH:mm:ss.fff}", ToDateTimeByUTCUnix(unix_dateTime, isToLocal)); }


        /// <summary>
        /// 把DateTime 转化为 long类型的UNIX Timestamps
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToUnixTimeByDateTime(DateTime dateTime) { return (long)(dateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; }


        /// <summary>
        /// 本地 UNIX Timestamps 转化为 DateTime
        /// </summary>
        /// <param name="unixTime">本地 UNIX Timestamps</param>
        /// <returns></returns>
        public static DateTime ToDateTimeByLocalUnix(long unixTime)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(unixTime);

            return dtDateTime;

        }

        /// <summary>
        /// UTC 时区  UNIX Timestamps 转化为 DateTime
        /// </summary>
        /// <param name="unixTime">UTC 时区 UNIX Timestamps</param>
        /// <param name="isToLocal">如果为True，请确保 参数unixTime为 UTC时间，否则输出的时间是错误的。转为为本地时间还是时间时间（相差8小时） </param>
        /// <returns></returns>
        public static DateTime ToDateTimeByUTCUnix(long unixTime, bool isToLocal = false)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTime);

            //不同的时区，直接这样转化会错误，所以这里弃用
            if (isToLocal) {

                // 获取目标时区信息
                //TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

                // 将 UTC 时间转换为目标时区时间
                dtDateTime = TimeZoneInfo.ConvertTimeFromUtc(dtDateTime, localTimeZone);

            }

            return dtDateTime;

        }

        #endregion


        #region 区块链相关工具
        /// <summary>
        /// BlockChain 区块链相关工具
        /// </summary>
        public static class BC
        {
            public static string GenHash(string data)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = SHA256.Create().ComputeHash(bytes);
                return BytesToHexString(hash);
            }

            public static byte[] GenHashBytes(string data)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = SHA256.Create().ComputeHash(bytes);
                return hash;
            }

            public static string GenHashHex(string hex)
            {
                byte[] bytes = HexStringToBytes(hex);
                byte[] hash = SHA256.Create().ComputeHash(bytes);
                return BytesToHexString(hash);
            }

            //public static string BytesToHex(byte[] bytes)
            //{
            //    return System.Convert.ToHexString(bytes).ToLower();
            //}


            /// <summary>
            /// 和BytesToHexString 相互反向
            /// </summary>
            /// <param name="hexString"></param>
            /// <returns></returns>
            public static byte[] HexStringToBytes(string hexString)
            {
                var bytes = new byte[hexString.Length / 2];
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                return bytes; 
            }
            /// <summary>
            /// 和HexStringToBytes 相互反向
            /// </summary>
            /// <param name="byteArray"></param>
            /// <returns></returns>
            public static string BytesToHexString(byte[] byteArray)
            {
                var str = new System.Text.StringBuilder();
                foreach (var t in byteArray)
                {
                    str.Append(t.ToString("X2"));
                }
                return str.ToString();
            }


            //public static byte[] HexToBytes(string hex)
            //{

            //    return Enumerable.Range(0, hex.Length)
            //        .Where(x => x % 2 == 0)
            //        .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            //        .ToArray();
            //}

            public static long GetTmStamp()
            {
                long epochTicks = new DateTime(1970, 1, 1).Ticks;
                long nowTicks = DateTime.UtcNow.Ticks;
                long tmStamp = ((nowTicks - epochTicks) / TimeSpan.TicksPerSecond);
                return tmStamp;
            }

            public static string CreateMerkleRoot(string[] txsHash)
            {
                while (true)
                {
                    if (txsHash.Length == 0)
                    {
                        return string.Empty;
                    }

                    if (txsHash.Length == 1)
                    {
                        return txsHash[0];
                    }

                    List<string> newHashList = new List<string>();

                    int len = (txsHash.Length % 2 != 0) ? txsHash.Length - 1 : txsHash.Length;

                    for (int i = 0; i < len; i += 2)
                    {
                        newHashList.Add(DoubleHash(txsHash[i], txsHash[i + 1]));
                    }

                    if (len < txsHash.Length)
                    {
                        newHashList.Add(DoubleHash(txsHash[^1], txsHash[^1]));
                    }

                    txsHash = newHashList.ToArray();
                }
            }

            static string DoubleHash(string leaf1, string leaf2)
            {
                byte[] leaf1Byte = HexStringToBytes(leaf1);
                byte[] leaf2Byte = HexStringToBytes(leaf2);

                var concatHash = leaf1Byte.Concat(leaf2Byte).ToArray();
                SHA256 sha256 = SHA256.Create();
                byte[] sendHash = sha256.ComputeHash(sha256.ComputeHash(concatHash));

                return BytesToHexString(sendHash).ToLower();
            }

            public static double GetTotalFees(List<Transaction> txns)
            {
                return txns.AsEnumerable().Sum(x => x.fee);
            }

            public static (double total, List<string> objlist) GetTotalAmountByTransaction(List<Transaction> txns)
            {
                double num_total = 0;
                List<string> objlist = new List<string>();

                txns.ForEach(item => {

                    StorageItem sto = item.storage_item;

                    switch (sto.item_type)
                    {
                        case StorageItemType.NumberBase:
                            int a = int.Parse(sto.get_content);
                            num_total += a;
                            ALog.Log("交易了 {0}积分, 总计{1}积分", a, num_total);
                            break;
                        case StorageItemType.ObjBase:
                            string itemHash = sto.get_content;
                            objlist.Add(itemHash);
                            ALog.Log("交易了 {0}物品, 累计{1}个物品", itemHash, objlist.Count);
                            break;
                        default:
                            break;
                    }
                });

                return (num_total, objlist);
            }

            public static (double total, List<string> objlist) GetTotalAmountByAccount(Account account)
            {
                List<string> objlist = new List<string>();

                foreach (var item in account.obj_items)
                {
                    string itemHash = item.get_content;
                    objlist.Add(itemHash);
                    ALog.Log("交易了 {0}物品, 累计{1}个物品", itemHash, objlist.Count);
                }
               
                return (account.number_item.amount, objlist);
            }

            /// <summary>
            /// 获取文件的Hash值，文件大小不要超过1MB
            /// </summary>
            /// <param name="file_path"></param>
            /// <returns></returns>
            public static string GetFileHash(string file_path)
            {
                if (!File.Exists(file_path))
                {
                    ALog.Error("没有找到这个文件 {0}", file_path);
                    return "";
                }

                using (FileStream stream = new FileStream(file_path, FileMode.Open))
                {
                    var hash = SHA1.Create();
                    byte[] hashByte = hash.ComputeHash(stream);
                    return GenHash(BytesToHexString(hashByte));
                }

            }

            /// <summary>
            /// 获取文件的Hash值，文件大小不要超过1MB
            /// </summary>
            /// <param name="file_path"></param>
            /// <returns></returns>
            public static string GetFileHash(byte[] file_stream)
            {
                if (file_stream == null || file_stream.Length < 10)
                {
                    ALog.Error("文件过小或为空");
                    return "";
                }
                var hash = SHA1.Create();
                byte[] hashByte = hash.ComputeHash(file_stream);
                return GenHash(BytesToHexString(hashByte));

            }

            public static string GetStorageObjHash(StorageObjItem objItem, string file_path)
            {
                if (objItem == null) return "";

                string filehash = GetFileHash(file_path);
                if (!string.IsNullOrEmpty(filehash))
                {
                    var data = $"{objItem.name}{objItem.created}{objItem.file_size}{filehash}";
                    return GenHash(GenHash(data));
                }

                return null;
            }

            public static string GetStorageObjHash(StorageObjItem objItem, byte[] file_stream)
            {
                if (objItem == null) return "";

                string filehash = GetFileHash(file_stream);
                if (!string.IsNullOrEmpty(filehash))
                {
                    var data = $"{objItem.name}{objItem.created}{objItem.file_size}{filehash}";
                    return GenHash(GenHash(data));
                }

                return null;
            }

            public static string GetTransactionHash(Transaction txn)
            {
                // Console.WriteLine(" get transaction hash {0}", txn);
               // if (txn == null) ALog.Error("交易损坏，Transaction为空！");
                if (txn.storage_item == null) ALog.Error("交易的内容损坏，Item为空！");
                //if (txn.storage_item) ALog.Error("交易的内容损坏，item_type为空！");

                var data = $"{txn.time_stamp}{txn.sender}{txn.storage_item.item_type}{txn.storage_item.get_content}{txn.fee}{txn.recipient}";
                 switch (txn.storage_item.item_type)
                 {
                     case StorageItemType.NumberBase:
                        break;
                     case StorageItemType.ObjBase:
                        StorageObjItem objItem = txn.storage_item as StorageObjItem;
                        data += $"{objItem.hash}";
                        break;
                 }
                
                return GenHash(GenHash(data));
                // Console.WriteLine(" get transaction hash {0}", TxnId);
            }

            public static string GetBlockHash(Block block)
            {
                if (block.Equals(null)) {
                    ALog.Error("块不能为空！");
                    return "";
                }

                var strSum = block.total_amount + block.height + block.prev_hash + block.merkle_root + block.time_stamp + block.difficulty + block.validator;
                return GenHash(GenHash(strSum));
            }


            /// <summary>
            /// 验证答案的正确性
            /// </summary>
            /// <param name="last_block_hash">上一个区块的哈希地址</param>
            /// <param name="transaction_list">先有或准备区块的交易列表</param>
            /// <param name="difficulty">困难度</param>
            /// <param name="nonce">随机值</param>
            /// <param name="proof">尝试的循环数值</param>
            /// <returns></returns>
            public static bool Valid_Proof(string last_block_hash, string transaction_list, int difficulty, int nonce, long proof)
            {
                var guess_message = ('0' * difficulty) + nonce + last_block_hash + transaction_list + proof;
                var guess = GenHash(guess_message);

                var isFound = true;
                foreach (var item in guess.Substring(0, difficulty).ToList())
                {
                    isFound = item == '0';
                    if (!isFound) break;
                }
                return isFound;
            }

            /// <summary>
            /// 检查是否是我当下区块链需要的下一个块
            /// </summary>
            /// <param name="_local_lastBlock">本地记录的最后一个块</param>
            /// <param name="_estimate_block">需要进行预计的块</param>
            /// <returns></returns>
            public static bool IsNextBlock(Block local_lastBlock, Block estimate_nextblock)
            {
                if (estimate_nextblock.height == (local_lastBlock.height + 1) && estimate_nextblock.prev_hash == local_lastBlock.hash
               && estimate_nextblock.hash == GetBlockHash(estimate_nextblock)
               && estimate_nextblock.time_stamp >= local_lastBlock.time_stamp)
                {
                    return true;
                }

                return false;

            }


            public static void PrintWallet(WalletCard wallet)
            {
                ALog.Log(string.Format("Your Wallet") +
                    string.Format("\nADDRESS: {0}", wallet.GetAccountAddress()) +
                    (string.Format("\nPUBLIC KEY Hex: {0}\nPUBLIC KEY :{1}", wallet.GetPublicKeyHex(), wallet.GetPublicKey())));
            }


            public static void PrintWallet(Account account)
            {
                ALog.Log(string.Format(
                    "Your Account: user_id= {0}\n address= {1}\n pub_key= {2}\n txn_count= {3}\n created= {4}\n updated= {5}\n "
                    //, account.user_id
                    , account.address
                    , account.pub_key
                    , account.txn_count
                    , account.created_time
                    , account.updated_time
                    ));

                PrintStorageItem(account.number_item);
                if(account.obj_items!=null) PrintStorageItem(account.obj_items.ToArray());
            }
            public static void PrintStorageItem(List<StorageItem> item)
            {
                if (item == null || item.Count <= 0) return;
                PrintStorageItem(item.ToArray());
            }
            public static void PrintStorageItem(params StorageItem[] item)
            {
                if (item == null || item.Length <= 0) return;

                for (int i = item.Length - 1; i >= 0; i--)
                {
                    switch (item[i].item_type)
                    {
                        case StorageItemType.NumberBase:
                            ALog.Log(string.Format(
                                "This Account StorageNumberItem: amount= {0}"
                                 , (item[i] as StorageNumberItem).amount
                                 )+"\n"+string.Format(
                                     "hash= {0}", item[i].item_type
                                     ));
                            break;
                        case StorageItemType.ObjBase:
                            StorageObjItem objItem = (item[i] as StorageObjItem);
                            ALog.Log(string.Format(
                             "This Account StorageObjItem {0}: hash= {1}\n name= {2}\n signature= {3}\n pub_key= {4}"
                                , i
                                , objItem.hash
                                , objItem.name
                                , objItem.signature
                                , objItem.pub_key
                              ) + "\n" + string.Format(
                                     "item_type= {0}\n " +
                                     "-----------------------", item[i].item_type));
                            
                            break;
                    }
                }
                   
            }


            public static string PrintFileSize(long size)
            {
                var num = 1024.00; //byte
                string size_str = "";

                if (size < num)
                    size_str = size + "B";
                else if(size < Math.Pow(num, 2))
                    size_str =(size / num).ToString("f2") + "K"; //kb
                else if(size < Math.Pow(num, 3))
                    size_str = (size / Math.Pow(num, 2)).ToString("f2") + "M"; //M
                else if(size < Math.Pow(num, 4))
                    size_str = (size / Math.Pow(num, 3)).ToString("f2") + "G"; //G
                else size_str = (size / Math.Pow(num, 4)).ToString("f2") + "T";

                ALog.Log("文件大小 ：" + size_str);

                return size_str;
            }


            public static void PrintNodePeers(List<NodePeer> nodePeers) {

                PrintNodePeers(nodePeers.ToArray());
            }
            public static void PrintNodePeers(params NodePeer[] nodePeers)
            {
                if (nodePeers == null || nodePeers.Length <=0) {
                    ALog.Error("node peer list is null");
                    return;
                }
                for (int i = 0; i < nodePeers.Length; i++)
                {
                    var item = nodePeers[i];
                    ALog.Log("NodePeer " + i + " : " + item.ToString());
                }
                
            }
            public static void PrintTransactions(List<Transaction> transactions)
            {
                if (transactions == null) return;
                foreach (var item in transactions)
                {
                    PrintTransaction(item);
                }
            }
            public static void PrintTransaction(Transaction transaction)
            {
                ALog.Log(string.Format(" = Hash      : {0}\n", transaction.hash)+
                    string.Format(" = Sender   : {0}\n", transaction.sender)+
                    string.Format(" = Recipient        : {0}\n", transaction.recipient)+
                    string.Format(" = Item_content : {0}\n", transaction.storage_item.get_content) +
                    string.Format(" = Item_type  : {0}\n", transaction.storage_item.item_type) +
                    string.Format(" = Fee   : {0}\n", transaction.fee)+
                    string.Format(" = Height       : {0}\n", transaction.height)+
                    string.Format(" = Signature: {0}\n", transaction.signature)+
                    string.Format(" = Pub Key       : {0}\n", transaction.pub_key)+
                    string.Format(" = Tx Type      : {0}\n", transaction.tx_type)+
                    string.Format(" = World  Timestamp   : {0}\n", ToDateTimeByUTCUnix(transaction.time_stamp))+
                    string.Format(" = Local  Timestamp   : {0}", ToDateTimeByUTCUnix(transaction.time_stamp, true)));
            }

            public static void PrintBlock(Block block)
            {
                ALog.Log(string.Format(" = Height      : {0}\n", block.height)+
                    string.Format(" = Version     : {0}\n", block.version)+
                    string.Format(" = Prev Hash   : {0}\n", block.prev_hash)+
                    string.Format(" = Hash        : {0}\n", block.hash)+
                    string.Format(" = Merkle Hash : {0}\n", block.merkle_root)+
                    string.Format(" = World  Timestamp   : {0}\n", ToDateTimeByUTCUnix(block.time_stamp)) +
                    string.Format(" = Local  Timestamp   : {0}\n", ToDateTimeByUTCUnix(block.time_stamp,true))+
                    string.Format(" = Difficulty  : {0}\n", block.difficulty)+
                    string.Format(" = Validator   : {0}\n", block.validator)+
                    string.Format(" = Nonce       : {0}\n", block.nonce)+
                    string.Format(" = Number Of Tx: {0}\n", block.num_of_tx)+
                    string.Format(" = Amout       : {0}\n", block.total_amount)+
                    string.Format(" = Reward      : {0}\n", block.total_reward)+
                    string.Format(" = Size        : {0}\n", block.size)+
                    string.Format(" = Build Time  : {0}\n", block.build_time)+
                    string.Format(" = Signature   : {0}", block.signature));
            }
        }

    }

    #endregion

    public static class ALog
    {

        public static bool Enable = true;

        public static void Log(object message)
        {
            if (!Enable) return;

            var str = "[ALog] " + message;
           
            if(AUtils.IRS) Console.WriteLine(str);
            else Debug.Log(str);

            CacheLog(str);
        }
        public static void Log(string message)
        {
            if (!Enable) return;
            var str = "[ALog] " + message;

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.Log(str);

            CacheLog(str);
        }
        public static void Log(string message, params object[] param)
        {
            if (!Enable) return;
            var str = string.Format("[ALog] " + message, param);

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.Log(str);

            CacheLog(str);
        }


        public static void Error(object message)
        {
            var str = "[ALogError] " + message;

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogError(str);

            CacheLog(str);
        }
        public static void Error(string message)
        {
            var str = "[ALogError] " + message;

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogError(str);

            CacheLog(str);
        }
        public static void Error(string message, params object[] param)
        {
            var str = string.Format("[ALogError] " + message, param);

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogError(str);

            CacheLog(str);
        }

        public static void Warning(object message)
        {
            if (!Enable) return;
            var str = "[ALogWarn] " + message;

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogWarning(str);

            CacheLog(str);
        }
        public static void Warning(string message)
        {
            if (!Enable) return;
            var str = "[ALogWarn] " + message;

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogWarning(str);

            CacheLog(str);
        }
        public static void Warning(string message, params object[] param)
        {
            if (!Enable) return;
            var str = string.Format("[ALogWarn] " + message, param);

            if (AUtils.IRS) Console.WriteLine(str);
            else Debug.LogWarning(str);

            CacheLog(str);
        }


        public static void LogException(Exception ex)
        {

            if (AUtils.IRS) Console.WriteLine("[ALogError] " + ex.ToString());
            else Debug.LogException(ex);

            CacheLog(ex.ToString());
        }

        public static bool IsCacheLog { get { return isCacheLog; } set {
                isCacheLog = value;
                if (value) {
                    if (string.IsNullOrEmpty(logPath)) logPath = Application.persistentDataPath + string.Format("/{0}log.txt", AUtils.IRS ? "IRS_" : "ClE_");

                    File.Delete(logPath);
                }
            } }


        static bool isCacheLog = false;

        static string logPath = "";
        static bool isAsyncing = false;
        static StringBuilder myStringBuilder;
        static bool isFirst = true;
        static string Date => DateTime.Now.ToString("H:mm:ss.fff");
        static async void CacheLog(string log, bool isAppend = true) {

            if (!IsCacheLog) return;
            if (myStringBuilder == null) {
                myStringBuilder = new StringBuilder(); }
            try
            {
                if (isAppend) {
                    myStringBuilder.AppendLine("\n[" + Date + "]: " + log);
                }

                if (isAsyncing) return;

                var newStr = "";
                if (isAppend) {
                    newStr = myStringBuilder.ToString();
                    myStringBuilder.Clear();
                }
                else {
                    newStr = log;
                }

                if (string.IsNullOrEmpty(newStr)) return;

                if (string.IsNullOrEmpty(logPath)) logPath = Application.persistentDataPath + string.Format("/{0}log.txt", AUtils.IRS ? "IRS_" : "ClE_");

                isAsyncing = true;
                if (!File.Exists(logPath))
                {
                    await File.WriteAllTextAsync(logPath, log);
                }
                else
                {
                    await File.AppendAllTextAsync(logPath, newStr);

                }
                isAsyncing = false;
                if (!string.IsNullOrEmpty(myStringBuilder.ToString()))
                {
                    CacheLog(myStringBuilder.ToString(), false);
                    myStringBuilder.Clear();
                }
            }catch(Exception ex) {

                Debug.LogException(ex);
            }
           }
    }

}