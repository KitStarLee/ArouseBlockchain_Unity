using System;
using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using ArouseBlockchain.UI;
using EasyButtons;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArouseBlockchain.UI
{
    /// <summary>
    /// UIManager : UI 的总控
    ///     -IUISolveParent ：UI页面事务处理的类（上一页下一页，跳转页面）
    ///         -IUIPage ： UI单独页面的管理
    /// </summary>
    
   // [ExecuteInEditMode]
    public class UIManager : MonoBehaviour
    {
        static UIManager instance;
        public static UIManager INST => instance;

        public UI3DLoad uI3DLoad;
        public Transform uI2DRoot;

        UI_WalletsSolve _UIWalletsSolve;
        UI_MainMenuSolve _UIMainMenuSolve;
        UI_ToolsSolve _UIToolsSolve;


        public static NodePeerSync P2PSync => P2PManager._NodePeerSync;

        private void Awake()
        {
            instance = this;

            if (uI3DLoad == null) uI3DLoad = GetComponentInChildren<UI3DLoad>(true);
        }

        private void Start()
        {
            SwictTo2D(false,0);
        }

        public void UIInit()
        {
            var allsolve = transform.GetComponentsInChildren<IUISolveParent>(true);
            if (allsolve != null)
            {
                for (int i = 0; i < allsolve.Length; i++)
                {
                    allsolve[i].HideSolve();
                }
            }

            SolveManager.Wallets.OnSelectAccount -= OnWallectCanvaAckAccount;
            SolveManager.Wallets.OnSelectAccount += OnWallectCanvaAckAccount;

            _UIWalletsSolve = transform.GetComponentInChildren<UI_WalletsSolve>(true);
            _UIMainMenuSolve = transform.GetComponentInChildren<UI_MainMenuSolve>(true);
            _UIToolsSolve = transform.GetComponentInChildren<UI_ToolsSolve>(true);

            _UIWalletsSolve.InitSolvePage();
            _UIMainMenuSolve.InitSolvePage();
            _UIToolsSolve.InitSolvePage();

            _UIToolsSolve.OpenSolve(null, null);

            SwictTo2D(true, 2, () => {
                _UIToolsSolve.OpenSolve(null, null);
                AutoOpenUISlove();
            });
        }

        /// <summary>
        /// 编辑器模式下，更换所有字体为中文字体
        /// </summary>
        /// <param name="tMP_Font"></param>
        [Button]
        public void ChackUINullText(TMP_FontAsset tMP_Font)
        {
            var gameOBJs = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < gameOBJs.Length; i++)
            {
              var meshText =  gameOBJs[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                if(meshText.Length > 0)
                {
                    for (int j = 0; j <  meshText.Length; j++)
                    {
                        if (meshText[j].font == null)
                        {
                            meshText[j].font = tMP_Font;
                            meshText[j].UpdateFontAsset();
                        }
                    }
                }
            }
        }

        [Button]
        public void ChackUINullLegacyText(Font fFont)
        {
            var gameOBJs = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < gameOBJs.Length; i++)
            {
                var meshText = gameOBJs[i].GetComponentsInChildren<Text>(true);
                if (meshText.Length > 0)
                {
                    for (int j = 0; j < meshText.Length; j++)
                    {
                        if (meshText[j].font == null)
                        {
                            meshText[j].font = fFont;
                        }
                    }
                }
            }
        }

        void AutoOpenUISlove()
        {
            var currAccount = SolveManager.Wallets.GetCurrWalletCard;
            if (currAccount == null)
            {
                var ac_count = SolveManager.Wallets.GetWalletCardCount;
                if (ac_count != 1) _UIWalletsSolve.OpenSolve(null, null);

                var log = "找到{0} 个账号";
                if (ac_count == 1)
                {
                    SolveManager.Wallets.SelectUserWalletCard(0);
                    log += ",直接默认账号";
                }
                else if (ac_count >= 2)
                {
                    _UIWalletsSolve.SkipPage("SelectAccount", null);
                    log += ",进入选择账号";
                }
                else
                {
                    _UIWalletsSolve.SkipPage(0, null);
                    log += ",创建账号";
                }
                ALog.Log(log, ac_count);
            }
        }
        public void SwictTo2D(bool isShow2D,float duration = 1f, Action onComplet = null)
        {
            uI2DRoot.gameObject.SetActive(isShow2D);
            _UIToolsSolve?.pageInterim.InterimClor(Color.white, 1, duration + 0.5f);
            StartCoroutine(Swict2DOr2D(isShow2D, duration));

            IEnumerator Swict2DOr2D(bool isShow2D, float duration)
            {
                yield return new WaitForSeconds(duration);
                uI3DLoad.Display = !isShow2D;
                onComplet?.Invoke();
            }
        }

        

        public void ShowUI_NetworkError(string error)
        {
            ALog.Log(error);
        }

        /// <summary>
        /// 当钱包Canva最终通过操作，选择了一个账户之后
        /// </summary>
        /// <param name="account"></param>
        void OnWallectCanvaAckAccount(Account account)
        {
            OpenGameMenuSolve(account);
        }

        public void OpenGameMenuSolve(Account account)
        {
            if (_UIWalletsSolve.IsUsed) _UIWalletsSolve.HideSolve();
            if (!_UIMainMenuSolve.IsUsed) _UIMainMenuSolve.OpenSolve(_UIWalletsSolve, null);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}