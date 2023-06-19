using System;
using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.Common;
using ArouseBlockchain.Solve;
using ArouseBlockchain.UI;
using EasyButtons;
using UnityEngine;
using UnityEngine.UI;
using static ArouseBlockchain.UI.UI_WalletsSolve;

[ExecuteInEditMode]
public class UI_MainMenuSolve : MonoBehaviour,IUISolveParent
{

    //private UI_PagePopup popup_P2PContact;

    public bool IsUsed => gameObject.activeSelf;



    public void InitSolvePage()
    {
        // throw new NotImplementedException();
        //popup_P2PContact = GetComponentInChildren<UI_P2PContact>(true);
        //popup_P2PContact.Init(GetComponent<RectTransform>());

        var pages = GetComponentsInChildren<UI_PagePopup>(true);

        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].Init(GetComponent<RectTransform>());
            UIPopupPool.AddAdvancePopup(GetComponent<Canvas>(), pages[i].gameObject);
        }

       // UIPopupPool.AddAdvancePopup(GetComponent<Canvas>(), popup_P2PContact.gameObject);
    }


    public void OpenSolve(UI_SolveParent prevSolve, IUIPage prevPage = null)
    {
        gameObject.SetActive(true);
    }

    public void HideSolve()
    {
        gameObject.SetActive(false);
    }

   
    [Button]
    void FindOldPopup() {

       var popup = transform.GetComponentsInChildren<UI_PopupOpener>();

        ALog.Log("{0} 个老的Popup", popup.Length);

        //string namamss = "";
        //for (int i = 0; i < popup.Length; i++)
        //{
        //    // ALog.Log("Popup {0} obj name {1}", i, popup[i].gameObject.name);
        //     namamss += popup[i].gameObject.name + " , " + popup[i].popupPrefab.name +" ; ";
        //    // popup[i].popupPrefab.
        //    // PrefabMode中的GameObject既不是Instance也不是Asset
        //    //var prefabStage = UnityEditor.HierarchyProperty.pr.GetPrefabStage(popup[i].popupPrefab);
        //    //if(prefabStage != null) {

        //    //    ALog.Log("1 Popup {0} ", prefabStage.assetPath);
        //    //}
        //    var isPrefab = PrefabUtility.IsPartOfPrefabInstance(popup[i].popupPrefab);
        //    if (isPrefab)
        //    {
        //        // 预制体资源：prefabAsset = prefabStage.prefabContentsRoot
        //        var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
        //       // return UnityEditor.AssetDatabase.GetAssetPath(prefabAsset);
        //        ALog.Log("2 Popup {0} ", UnityEditor.AssetDatabase.GetAssetPath(prefabAsset));
        //       // return prefabStage.prefabAssetPath;
        //    }
        //    //var butt = popup[i].gameObject.AddComponent<Button>();
        //    //butt.onClick.
        //    // newPopu.popupPrefab = oldPopup[i].popupPrefab;
        //    // newPopu
        //    //DestroyImmediate(oldPopup[i]);
        //}

       // ALog.Log("3 Popup {0}", namamss);
    }

   
    #region UI Child Page


    //[Serializable]
    //public class CP_Menu : UI_Page
    //{
    //    public Button butt_Creat;
    //    public Button impore_Creat;

    //    public override void Restart()
    //    {
    //        butt_Creat.onClick.RemoveAllListeners();
    //        impore_Creat.onClick.RemoveAllListeners();

    //        butt_Creat.onClick.AddListener(() => {
    //            UIParent.NextPage(this);
    //        });

    //        impore_Creat.onClick.AddListener(() => {
    //            UIParent.SkipPage("Impore", this);
    //        });
    //    }
    //}


    #endregion
}
