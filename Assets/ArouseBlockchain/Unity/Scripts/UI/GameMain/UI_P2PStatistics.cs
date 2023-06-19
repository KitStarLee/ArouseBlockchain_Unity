using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace ArouseBlockchain.UI
{
    /// <summary>
    /// P2P整个区块的现状数据同步，比如置换交易内容
    /// </summary>
    public class UI_P2PStatistics : UI_PagePopup
    {
        public ToggleGroup _ToggleGroup;
        public TextMeshProUGUI _ContentText;

        List<(string funName, Action Callblak)> FunList = new List<(string fun, Action callblak)>();


        string allBolcks = "";
        string allBalance = "";
        string allNode = "";
        string allTrans = "";

        public override void Init(RectTransform rect)
        {

            FunList.Add(("区块", DoShowAllBlocks));
            FunList.Add(("储仓", DoShowBalance));
            FunList.Add(("节点", DoShowAllNodePeer));
           // FunList.Add(("交易", DoShowTransactionHistory));

            allBolcks = "";
            allBalance = "";
            allNode = "";
            allTrans = "";

            _ContentText.text = "";

            Toggle[] childToggle = _ToggleGroup.transform.GetComponentsInChildren<Toggle>();
          
            for (int i = 0; i < childToggle.Length; i++)
            {
                int index = i;
                Toggle toggle = childToggle[index];
                toggle.onValueChanged.RemoveAllListeners();
                var text = toggle.GetComponentInChildren<Text>();
                text.text = FunList[index].funName;

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        FunList[index].Callblak();
                    }
                });
            }

            FunList[0].Callblak();
        }


        private void OnEnable()
        {
            allBolcks = "";
            allBalance = "";
            allNode = "";
            allTrans = "";

            FunList[0].Callblak();
        }





        /// <summary>
        /// 展示所有的块信息
        /// </summary>
        void DoShowAllBlocks()
        {
            if (allBolcks != "")
            {
                _ContentText.text = allBolcks;
                return;
            }
            else
            {
                _ContentText.text = "....加载中....";
            }

           
            List<Block> tail_block = SolveManager.BlockChain.Block.GetRemaining(0);

            allBolcks += string.Format("总计{0}个块\n\n", tail_block.Count);

            for (int i = 0; i < tail_block.Count; i++)
            {
                allBolcks += string.Format("-------第 {0} 个块数据--------\n{1}\n\n", i, tail_block[i].ToString());
            }

            _ContentText.text = allBolcks;
        }


        void DoShowTransactionHistory()
        {
            if (allTrans != "")
            {
                _ContentText.text = allTrans;
                return;
            }
            else
            {
                _ContentText.text = "....加载中....";
            }

            string address = SolveManager.Wallets.GetCurrAddress;
            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(address);

            if (string.IsNullOrWhiteSpace(address) || account.Equals(null))
            {
                ALog.Error("Error, the address is empty, please create an account first!\n");
                return;
            }


            var transactions = SolveManager.BlockChain.Transaction.GetRangeByAddress(address, 1, 50).ToArray();

            allTrans += string.Format("确定交易的记录有 {0} 个\n\n", transactions.Length);

            if (transactions != null)
            {
                for (int i = 0; i < transactions.Length; i++)
                {
                    allTrans += string.Format("{0}: {1}\n\n", i, transactions[i].ToString());
                }
            }
            else
            {
                ALog.Log("\n---- 没有确定的交易记录! ---");
            }

            _ContentText.text = allTrans;

        }


        /// <summary>
        /// 展示所有的节点信息
        /// </summary>
        void DoShowAllNodePeer()
        {
            if (allNode != "")
            {
                _ContentText.text = allNode;
                return;
            }
            else
            {
                _ContentText.text = "....加载中....";
            }

            var allnodepeer = SolveManager.BlockChain.NodePeer.GetAll();
            if (allnodepeer == null) return;

           // AUtils.BC.PrintNodePeers(allnodepeer);

            allNode += string.Format("区块链总共记录了 {0} 个节点\n\n", allnodepeer.Length);

            for (int i = 0; i < allnodepeer.Length; i++)
            {
                allNode += string.Format("{0}: {1}\n\n", i, allnodepeer[i].ToString());
            }

            _ContentText.text = allNode;
        }

        /// <summary>
        /// 获取账号储仓中所有物品信息
        /// </summary>
        void DoShowBalance()
        {
            if (allBalance != "")
            {
                _ContentText.text = allBalance;
                return;
            }
            else
            {
                _ContentText.text = "....加载中....";
            }

            var address = SolveManager.Wallets.GetCurrAddress;

            Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(address);
            if (account.Equals(null))
            {
                _ContentText.text = "....Null....";
                return;
            }

          //  ALog.Log("当前用户 {0} 的余额是 :{1}", address, account.number_item.amount);
         //   AUtils.BC.PrintStorageItem(account.number_item);
          //  if (account.obj_items != null) AUtils.BC.PrintStorageItem(account.obj_items.ToArray());

            allBalance += string.Format("当前用户地址 {0} \n\n余额是 : {1}\n", address, account.number_item.amount);

            var objItems = account.obj_items;
            if (objItems != null && objItems.Count > 0)
            {
                for (int i = 0; i < objItems.Count; i++)
                {
                    allBalance += string.Format("--------物品 {0} ：--------\n{1}\n\n", i, objItems[i].ToString());
                }

            }
            else
            {
                allBalance += "没有任何物品\n";
            }

            _ContentText.text = allBalance;

        }



        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}