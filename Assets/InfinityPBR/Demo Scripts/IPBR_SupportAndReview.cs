using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IPBR_SupportAndReview : MonoBehaviour
{
    public string url;
    public GameObject panel;
    
#if UNITY_EDITOR

    void Awake()
    {
        panel.SetActive(true);
    }
    public void CloseWindow()
    {
        this.gameObject.SetActive(false);
    }

    public void GoToAssetStore()
    {
        Application.OpenURL(url);
    }
    
    public void GoToInfinity()
    {
        Application.OpenURL("https://www.infinitypbr.com");
    }
    
    #endif
}
