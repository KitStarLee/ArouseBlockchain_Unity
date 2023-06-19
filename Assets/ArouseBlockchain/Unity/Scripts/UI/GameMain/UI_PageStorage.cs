using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.Solve;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI_PageStorage : UI_PagePopup
{
    public TextMeshProUGUI _ScoreText;
    public Transform _OrginStorageItem;


    List<UI_StorageItem> _UIStorageItems = new List<UI_StorageItem>();

    public override void Init(RectTransform rect)
    {
        _OrginStorageItem.gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        if (_UIStorageItems.Count <= 0) _UIStorageItems.Add(_OrginStorageItem.GetComponent<UI_StorageItem>());

        if (SolveManager.Wallets == null) return;
        var address = SolveManager.Wallets.GetCurrAddress;

        Account account = SolveManager.BlockChain.WalletAccount.GetByAddress(address);
        if (account.Equals(null))
        {
            ALog.Error("没有这个账户 ：{0} ", address);
            return;
        }

        UpdateBalance_Number(account.number_item.amount);
        UpdateBalance_Obj(account.obj_items);
    }

    public void UpdateBalance_Number(double score)
    {
        _ScoreText.text = score.ToString();
    }

    public void UpdateBalance_Obj(List<StorageObjItem> storageItems)
    {
        _UIStorageItems.ForEach(x => x.gameObject.SetActive(false));

        ALog.Log("!!!!!!!!!! 总共有储仓物品 ：{0} ", storageItems.Count);
        for (int i = 0; i < storageItems.Count; i++)
        {
            UI_StorageItem temp = null;
            if (i >= _UIStorageItems.Count)
            {
                GameObject newItemUI = Instantiate(_OrginStorageItem.gameObject, _OrginStorageItem.parent);
                temp = newItemUI.GetComponent<UI_StorageItem>();
                _UIStorageItems.Add(temp);
            }
            else
            {
                temp = _UIStorageItems[i];
            }

            temp.gameObject.SetActive(true);
           
            temp.InitUI(storageItems[i]);
        }
    }


}
