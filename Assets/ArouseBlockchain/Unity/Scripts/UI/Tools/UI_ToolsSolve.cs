using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.UI;
using UnityEngine;

public class UI_ToolsSolve : MonoBehaviour, IUISolveParent
{
    public UIPage_Interim pageInterim;
    static UIPage_Notification PageNotification;
    public bool IsUsed => gameObject.activeSelf;

    public void InitSolvePage()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (pageInterim == null)
        {
            pageInterim = GetComponentInChildren<UIPage_Interim>();
            pageInterim.Init(rect);
        }

        if (PageNotification == null)
        {
            PageNotification = GetComponentInChildren<UIPage_Notification>();
        }
    }

    public void OpenSolve(UI_SolveParent prevSolve, IUIPage prevPage = null)
    {
        gameObject.SetActive(true);
    }

    public void HideSolve()
    {
        gameObject.SetActive(false);
    }

    public static void LaunchNotification(string Title, string Message)
    {
        if(PageNotification != null)
        {
            PageNotification.Launch(Title, Message);
        }
    }


        // Update is called once per frame
        void Update()
    {
        
    }
}
