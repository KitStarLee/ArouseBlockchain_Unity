using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.NetService;
using ArouseBlockchain.Solve;
using UnityEngine;
using static ArouseBlockchain.P2PNet.NetTransaction;
using ArouseBlockchain.P2PNet;

namespace ArouseBlockchain
{
    public class AWalletAccount : IBaseFun
    {
        public enum AccountType
        {
            main = 0,
            consume = 1, // 消费
            mining = 2, //挖矿
            contribute = 3, // 捐赠
        }
        public enum ChangeType
        {
            outside = 0, //外部接受
            interior = 1 //内部找零
        }


        public AWalletAccount()
        {
        }

        public async Task<bool> Init()
        {
            return true;
        }
        public void Close()
        {


        }

        /// <summary>
        /// 通过地址获取一个交易账户
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public Account GetByAddress(string address)
        {
            return DBSolve.DB.Account.GetByAddress(address);
        }
        public IEnumerable<Account> GetRange(int pageNumber, int resultPerPage)
        {
            return DBSolve.DB.Account.GetRange(pageNumber, resultPerPage);
        }
        public Account GetByPubKey(string pubkey)
        {
            return DBSolve.DB.Account.GetByPubKey(pubkey);
        }



        /// <summary>
        /// 通过给定的地址获取账号，并且扣除账号权益
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public (double tokens, Account) CutTokensByAddress(string address, double tokens)
        {
            var account = DBSolve.DB.Account.GetByAddress(address);
            if((account.tokens - tokens) >=0)
            {
                account.tokens = account.tokens - tokens;
                DBSolve.DB.Account.AddOrUpdate(account);
                return (tokens, account);
            }
            else
            {
                ALog.Error("非法操作，账号{0}持有的权益{1} 不足{2}", address, account.tokens, tokens);
                return (-1, default);
            }
        }

        public void AddAccount(Account acc)
        {
            DBSolve.DB.Account.Add(acc);
        }
        public void AddAccount(WalletCard card)
        {
            var accountAddress = card.GetAccountAddress();
            var acc = DBSolve.DB.Account.GetByAddress(accountAddress);

            long utcTime = AUtils.NowUtcTime_Unix;
            if (string.IsNullOrEmpty(acc.address))
            {
                acc = new Account
                {
                    address = accountAddress,
                    pub_key = card.GetPublicKeyHex(),
                    //新建账号给予10积分初始余额[后期要去掉，不然频繁的创建账户]
                    // item_type = ((byte)StorageItemType.Point),
                    // item_content = 10,
                    txn_count = 0,
                    created_time = utcTime,
                    updated_time = utcTime

                };

                // ALog.Log("01 账户添加：" + accountAddress);
                DBSolve.DB.Account.Add(acc);
            }
            else
            {
                DBSolve.DB.Account.AddOrUpdate(acc);
                ALog.Log("已经存在此账户，请勿重复添加：" + accountAddress);
            }
        }


        public Account UpdateAccount(Account acc)
        {
            return DBSolve.DB.Account.AddOrUpdate(acc);
        }

        /// <summary>
        /// 账户添加一个置换物去储仓
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toAddress"></param>
        /// <param name="item"></param>
        public void AddToStorage(string add_Address, StorageItem item)
        {
            OperatorTye operatorTye = OperatorTye.Add;
            var acc = DBSolve.DB.Account.GetByAddress(add_Address);
            //  ALog.Log("01!!! 添加交易查看OBJ type:{0} ; {1}" , item.item_type, (item is StorageObjItem));

            if (string.IsNullOrEmpty(acc.address)) acc = NewCreat(add_Address, "-", item, operatorTye);
            else
            {
                UpdateStorage(acc, item, operatorTye);
            }


        }

        /// <summary>
        /// 账户从储仓减去一个置换物
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <param name="item"></param>
        /// <param name="publicKey"></param>
        public void CutFromStorage(string cut_Address, StorageItem item, string publicKey)
        {
            OperatorTye operatorTye = OperatorTye.Reduce;
            var acc = DBSolve.DB.Account.GetByAddress(cut_Address);

            if (string.IsNullOrEmpty(acc.address)) acc = NewCreat(cut_Address, publicKey, item, operatorTye);
            else
            {
                acc.pub_key = publicKey;
                UpdateStorage(acc, item, operatorTye);
            }
        }

        Account NewCreat(string address, string pub_key, StorageItem sto_item, OperatorTye operatorTye)
        {
            long utcTime = AUtils.NowUtcTime_Unix;

            Account acc = new Account
            {
                address = address,
                txn_count = 1,
                created_time = utcTime,
                updated_time = utcTime,
                pub_key = pub_key,
                obj_items = new List<StorageObjItem>()
            };

            switch (sto_item.item_type)
            {
                case StorageItemType.NumberBase:
                    if (operatorTye == OperatorTye.Add) acc.number_item = (sto_item as StorageNumberItem);
                    else acc.number_item.amount -= (sto_item as StorageNumberItem).amount;
                    break;
                case StorageItemType.ObjBase:
                    StorageObjItem objItem = (sto_item as StorageObjItem);
                    if (operatorTye == OperatorTye.Add) acc.obj_items.Add(objItem);
                    else ALog.Error("严重错误！账户没有这个 OBJ，请在转账前确定它有,或者更新的时候先更新添加的再更新删减的：" + acc + " \n" + objItem.name + " \n " + objItem.hash);
                    break;
            }
            DBSolve.DB.Account.Add(acc);

            // ALog.Log("02!!! 创建了这个账号 {0} ; {1} ; {2}", acc.address, (acc.number_item.get_content), (acc.obj_items == null));

            return acc;

        }

        void UpdateStorage(Account target_acc, StorageItem sto_item, OperatorTye operatorTye)
        {
            long utcTime = AUtils.NowUtcTime_Unix;

            target_acc.txn_count += 1;
            target_acc.updated_time = utcTime;

            switch (sto_item.item_type)
            {
                case StorageItemType.NumberBase:
                    if (operatorTye == OperatorTye.Add) target_acc.number_item.amount += (sto_item as StorageNumberItem).amount;
                    else target_acc.number_item.amount -= (sto_item as StorageNumberItem).amount;
                    break;
                case StorageItemType.ObjBase:

                    StorageObjItem objItem = (sto_item as StorageObjItem);
                    bool isHave = target_acc.obj_items.Exists(obj => obj.hash == objItem.hash);

                    if (operatorTye == OperatorTye.Add)
                    {
                        if (!isHave)
                        {
                            target_acc.obj_items.Add(objItem);
                        }
                        else ALog.Error("严重错误！不应该有相同的 OBJ");

                    }
                    else
                    {
                        if (isHave)
                        {
                            for (int i = target_acc.obj_items.Count - 1; i >= 0; i--)
                            {
                                if (target_acc.obj_items[i].hash == objItem.hash)
                                {
                                    target_acc.obj_items.Remove(target_acc.obj_items[i]);
                                    break;
                                }
                            }}
                        else ALog.Error("严重错误！不曾有的的OBJ不能移除");
                    }
                    break;
            }
            DBSolve.DB.Account.AddOrUpdate(target_acc);

        }

        /// <summary>
        /// 更新账户储仓内物品流转
        /// </summary>
        /// <param name="transactions"></param>
        public void UpdateStorage(List<Transaction> transactions)
        {
            UpdateStorage(transactions.ToArray()) ;
        }

        /// <summary>
        /// 更新账户储仓内物品流转
        /// </summary>
        /// <param name="transactions"></param>
        public void UpdateStorage(params Transaction[] transactions)
        {
            //需要对交易先进行验证
            foreach (var transaction in transactions)
            {
                CutFromStorage(transaction.sender, transaction.storage_item, transaction.pub_key);
                AddToStorage(transaction.recipient, transaction.storage_item);
            }
        }


        /// <summary>
        ///更新创世账户的储仓
        /// </summary>
        public void UpdateStorageGenesis(List<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                AddToStorage(transaction.recipient, transaction.storage_item);
            }
        }


        /// <summary>
        /// 创世账户 有初始资源
        /// </summary>
        public Dictionary<Solve.KeyPair, Account> GetGenesis(params Solve.KeyPair[] cards)
        {

            //保证创世区块已经存在时，不会重复创建
            //1. 存在则不创建
            //2. 没有网络则不创建
            //3. 有需要同步的块，且块的高度也大于目前自己的，则不创建
            //4.

            var blocks = DBSolve.DB.Block.GetAll();
            if (blocks.Count() > 1) return null;

            Dictionary<Solve.KeyPair, Account> list = new Dictionary<Solve.KeyPair, Account>();

            foreach (var card in cards)
            {
                var timestamp = AUtils.ToUnixTimeByDateTime(AUtils.NowUtcTime);
                var pub_key = card.PublicKey.ToString();
                var acc = (new Account()
                {
                    address = WalletsSolve.GetWalletAddress(card.PublicKey), // card.GetAccountAddress(),//"6mEWLaTGbmSsp7EKL8bdVLnyK7tLU3TDPGbC84p6bbUh",
                    pub_key = pub_key,
                    txn_count = 1,
                    created_time = timestamp,
                    updated_time = timestamp,
                    number_item = new StorageNumberItem() { amount = 2000000000 },
                    //安卓不支持IO来操作，只能未来获取到之后，直接网络加载资源，铸造可以通过服务器铸造
//#if UNITY_EDITOR
                    obj_items = SolveManager.BlockChain.Storage.GetGenesis(card)
//#endif
                });

                if (acc.obj_items == null || acc.obj_items.Count < 1) ALog.Error("Obj 没有创建成功：");
                list.Add(card, acc);// = acc;
                                    // ALog.Log("成功创建账户 {0} ; {1} ;", acc.address, acc.number_item.amount);
                                    //SolveManager.DB.Account.Add(acc);
            }

            return list;

        }
    }
}
