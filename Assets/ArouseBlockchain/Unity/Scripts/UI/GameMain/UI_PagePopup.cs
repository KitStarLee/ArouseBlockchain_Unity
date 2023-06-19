using UnityEngine;
using System.Collections;
using ArouseBlockchain.UI;

public abstract class UI_PagePopup : MonoBehaviour, IUIPage
{
    public abstract void Init(RectTransform rect);
}

