using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using UnityEngine;
using UnityEngine.UI;

namespace ArouseBlockchain.UI
{

    public class UI_WalletsSolve : UI_SolveParent
    {
        public CP_Creat _CreatPage;
        public CP_Explain _ExplainPage;
        public CP_SaveMne _SavePage;
        public CP_Impore _ImporePage;
        public CP_SelectAccount _SelectAccPage;


        public override void InitSolvePage()
        {
            SetUIPageByIndex = (index) => {

                switch (index)
                {
                    case 0:
                        _CreatPage.PageIndex = index;
                        return _CreatPage;
                    case 1:
                        _ExplainPage.PageIndex = index;
                        return _ExplainPage;
                    case 2:
                        _SavePage.PageIndex = index;
                        return _SavePage;
                    case 3:
                        _ImporePage.PageIndex = index;
                        return _ImporePage;
                    case 4:
                        _SelectAccPage.PageIndex = index;
                        return _SelectAccPage;
                }

                return null;
            };

            base.InitSolvePage();
        }

        public override void OpenSolve(UI_SolveParent prevSolve, UI_Page prevPage = null)
        {
            //var currAccount =  SolveManager.Wallets.GetCurrWalletCard;
            //if(currAccount == null) {
            //    var account =  SolveManager.Wallets.GetWalletCardCount;
            //    if(account == 1) {
            //        SolveManager.Wallets.SelectUserWalletCard(0);
            //    }
            //    else if(account >= 2) {
            //        SkipPage("SelectAccount",null);
            //    }
            //    else {
            //        SkipPage(0,null);
            //    }
            //}

            base.OpenSolve(prevSolve, prevPage);
        }



        #region UI Child Page

        /// <summary>
        /// 钱包首页，选择新建还是导入钱包
        /// </summary>
        [Serializable]
        public class CP_Creat : UI_Page
        {
            public Button butt_Creat;
            public Button impore_Creat;

            public override void Restart()
            {
                butt_Creat.onClick.RemoveAllListeners();
                impore_Creat.onClick.RemoveAllListeners();

                butt_Creat.onClick.AddListener(() => {
                    UIParent.NextPage(this);
                });

                impore_Creat.onClick.AddListener(() => {
                    UIParent.SkipPage("Impore", this);
                });
            }
        }

        /// <summary>
        /// 解释说明页面，白皮书
        /// </summary>
        [Serializable]
        public class CP_Explain : UI_Page
        {
            public Button butt_Next;

            public override void Restart()
            {
                butt_Next.onClick.RemoveAllListeners();

                butt_Next.onClick.AddListener(() => {
                    UIParent.NextPage(this);
                });
            }
        }


        /// <summary>
        /// 生成助记词界面，并确定是否保存
        /// </summary>
        [Serializable]
        public class CP_SaveMne : UI_Page
        {
            public InputField output_Mne;
            public Text text_Tip;
            public Toggle togl_IsCache;
            public Button butt_Copy;
            public Button butt_Next;

            (string, KeyPair) MneReply;


            public void OutputMnemonic(string mnemonic)
            {
                text_Tip.text = "这是您的12个中文助记词(根密码)";
                output_Mne.text = mnemonic;
                butt_Copy.interactable = true;
                butt_Next.interactable = true;
            }
            public override void Restart()
            {
                output_Mne.interactable = false;
                output_Mne.text = "";
                butt_Copy.interactable = false;
                butt_Next.interactable = false;

                text_Tip.text = "";
                text_Tip.text = "正在生成您的助记词";

                output_Mne.text = "--正在生成您的助记词--";

                butt_Copy.onClick.RemoveAllListeners();
                butt_Next.onClick.RemoveAllListeners();
                togl_IsCache.isOn = false;

                butt_Copy.onClick.AddListener(() => {

                    GUIUtility.systemCopyBuffer = output_Mne.text;
                });

                butt_Next.onClick.AddListener(() => {

                    //本地线验证助记词
                    if (!AUtils.IsRightMnemonics(output_Mne.text))
                    {
                        //需要输入合法的助记词
                        TipLog("助记词未知错误，请重试", LogType.Error);
                        return;
                    }

                    SolveManager.Wallets.IsAutoSave = togl_IsCache.isOn;
                    if (SolveManager.Wallets.IsAutoSave)
                    {
                        SolveManager.Wallets.SavaUserWalletMnemonic(output_Mne.text);
                    }

                    SolveManager.Wallets.SelectUserWalletCard(MneReply.Item2.PublicKey);

                });

                OnPageShow = async () => {

                    //检查是否需要生成助记词
                    bool needNewMnemonic = true;
                    if (needNewMnemonic)
                    {
                        MneReply = await SolveManager.Wallets.CreatUserWalletCard();
                        OutputMnemonic(MneReply.Item1);
                    }
                };
            }
        }


        /// <summary>
        /// 导入助记词界面
        /// </summary>
        [Serializable]
        public class CP_Impore : UI_Page
        {
            public Dropdown dpd_InputType;
            public InputField input_Mne;
            public Toggle togl_IsCache;
            public Button butt_Impore;
            public Button butt_Cancel;

            public override void Restart()
            {
                input_Mne.interactable = true;
                butt_Impore.interactable = false;

                Dropdown.OptionData option1 = new Dropdown.OptionData("助记词");
                Dropdown.OptionData option2 = new Dropdown.OptionData("秘钥(开发中....)");
               
                dpd_InputType.options = new List<Dropdown.OptionData>() {option1, option2};
                togl_IsCache.isOn = false;

                dpd_InputType.onValueChanged.RemoveAllListeners();
                butt_Impore.onClick.RemoveAllListeners();
                butt_Cancel.onClick.RemoveAllListeners();


                dpd_InputType.onValueChanged.AddListener((index) => {
                    if (index == 1) dpd_InputType.value = 0;
                });

                input_Mne.onValueChanged.AddListener((conent) =>
                {
                    if(conent.Length == 12)
                    {
                        if (AUtils.IsRightMnemonics(conent))
                        {
                            butt_Impore.interactable = true;
                            TipLog("");
                        }
                        else
                        {
                            butt_Impore.interactable = false;
                            TipLog("无效助记词", LogType.Error);
                        }
                    }
                    else
                    {
                        if(conent.Length == 0) TipLog("");
                        else
                        {
                            TipLog("请输入12个中文助记词", LogType.Warning);
                        }
                        butt_Impore.interactable = false;
                    }
                    
                });

                butt_Impore.onClick.AddListener(async () => {

                    //本地线验证助记词
                    if (!AUtils.IsRightMnemonics(input_Mne.text))
                    {
                        //需要输入合法的助记词
                        TipLog("无效助记词", LogType.Error);
                        return;
                    }

                    WalletCard card = SolveManager.Wallets.GetUserWalletCard(input_Mne.text);

                    if(card != null)
                    {
                        TipLog("检查中", LogType.Log);
                        butt_Impore.interactable = false;
                        // 线上验证助记词
                        bool isSure = await card.GetKeyPair().CheckInChain();

                        butt_Impore.interactable = true;
                        if (isSure)
                        {
                            SolveManager.Wallets.IsAutoSave = togl_IsCache.isOn;
                            if (SolveManager.Wallets.IsAutoSave)
                            {
                                SolveManager.Wallets.SavaUserWalletMnemonic(input_Mne.text);
                            }

                            SolveManager.Wallets.SelectUserWalletCard(card.GetPublicKey());

                        }
                        else
                        {
                            //请重新输入正确的助记词
                            TipLog("无效助记词", LogType.Error);
                        }
                    }
                    else
                    {
                        TipLog("错误助记词", LogType.Error);
                    }
                   
                });

                butt_Cancel.onClick.AddListener(() => {
                    UIParent.PrevPage();
                });
            }
        }

        /// <summary>
        /// 本地缓存了助记词，选择账号
        /// </summary>
        [Serializable]
        public class CP_SelectAccount : UI_Page
        {
            public Button orgin_butt;

            [NonSerialized]
            public List<Button> select_butt;

           
            public override void Restart()
            {
                var walletAccount = SolveManager.Wallets.GetUserAllWalletAccount();
                if (walletAccount == null || walletAccount.Length == 0) return;

                var cardCount = walletAccount.Length;

                if (select_butt == null) {
                    select_butt = new List<Button>();
                    select_butt.Add(orgin_butt);
                }

                for (int i = 0; i < cardCount; i++)
                {
                    Button newButt = null;
                    if (i >= select_butt.Count) {
                        GameObject newObj = Instantiate(orgin_butt.gameObject);
                        newObj.transform.parent = orgin_butt.transform.parent;
                        newButt = newObj.GetComponent<Button>();
                        select_butt.Add(newButt);
                    }
                    else {
                        select_butt[i].gameObject.SetActive(true);
                        newButt = select_butt[i];
                    }
                    Text text = newButt.GetComponentInChildren<Text>();
                    string address = walletAccount[i].address;
                    text.text = address.Insert(address.Length/2,"\n");
                    int buttIndex = i;
                    newButt.onClick.RemoveAllListeners();
                    newButt.onClick.AddListener(() => {
                        SolveManager.Wallets.SelectUserWalletCard(buttIndex);
                    });
                }

                if (cardCount < select_butt.Count)
                {
                    for (int i = cardCount; i < select_butt.Count; i++)
                    {
                        select_butt[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        #endregion
    }
}