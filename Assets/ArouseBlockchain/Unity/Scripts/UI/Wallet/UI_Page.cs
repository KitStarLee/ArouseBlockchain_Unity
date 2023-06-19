using System;
using System.Threading;
using ArouseBlockchain.Common;
using UnityEngine;
using UnityEngine.UI;

namespace ArouseBlockchain.UI
{
    public abstract class UI_Page: IUIPage
    {
        [HideInInspector]
        public UI_SolveParent UIParent;
        [HideInInspector]
        public RectTransform PageRect;
        [HideInInspector]
        public UI_Page PrevPage;
        [HideInInspector]
        public int PageIndex;
        [HideInInspector]
        public string PageName;
        [HideInInspector]
        public Text TipText;


        public Action OnPageShow;
        public Action OnPageHide;

        public void Init(UI_SolveParent uiParent, RectTransform rect)
        {
            PageRect = rect;
            UIParent = uiParent;
            PageName = PageRect.gameObject.name;
            TipText = rect.Find("TipText")?.GetComponent<Text>();
            if (TipText != null) TipText.text = "";
            Restart();
        }

        public void SetActive(bool value)
        {
            if (value) OnPageShow?.Invoke();
            else OnPageHide?.Invoke();

            PageRect?.gameObject.SetActive(value);
        }

        CancellationTokenSource newCancellation;
        public void TipLog(string log, LogType logType = LogType.Log, bool isDelaye = false)
        {
            TipText.text = "";
            if (TipText == null)
            {
                ALog.Error("None add TipText");
                return;
            }
            if (string.IsNullOrEmpty(log)) return;

            int second = 3;
            switch (logType)
            {
                case LogType.Log:
                    TipText.color = Color.white;
                    break;
                case LogType.Error:
                    TipText.color = Color.red;
                    second = 5;
                    break;
                case LogType.Warning:
                    TipText.color = Color.yellow;
                    second = 4;
                    break;
            }

            TipText.text = log;


            if (!TipText.gameObject.activeSelf) newCancellation?.Cancel();

            if (isDelaye)
            {

                AUtils.Delaye(second, (cancellation) =>
                {
                    newCancellation = cancellation;
                    TipText.gameObject.SetActive(false);
                }, newCancellation);
            }



            TipText.gameObject.SetActive(true);

        }


        public abstract void Restart();
    }


}