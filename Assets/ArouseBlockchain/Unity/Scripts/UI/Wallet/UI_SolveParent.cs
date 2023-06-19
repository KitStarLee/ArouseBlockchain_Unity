using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using ArouseBlockchain.Common;
using TMPro;
using System.Threading.Tasks;
using System.Threading;

namespace ArouseBlockchain.UI
{
    public class UI_SolveParent : MonoBehaviour, IUISolveParent
    {
        public RectTransform PageRoot;

        protected List<UI_Page> _ChildPage = new List<UI_Page>();

        protected UI_Page _CurrPage;
       // protected int _CurrIndex = 0;

        protected Func<int,UI_Page> SetUIPageByIndex;

        public virtual void InitSolvePage()
        {
           // if(PageRoot == null) PageRoot = GetComponent<RectTransform>();

            if (PageRoot.childCount > 0)
            {
                for (int i = 0; i < PageRoot.childCount; i++)
                {
                    RectTransform rect_page = PageRoot.GetChild(i).GetComponent<RectTransform>();
                    UI_Page page = SetUIPageByIndex(i);
                    page?.Init(this, rect_page);
                    _ChildPage.Add(page);

                }
            }
            SkipPage(0,null);
        }

        public bool IsUsed { get { return gameObject.activeSelf; } }

        public void OpenSolve(UI_SolveParent prevSolve, IUIPage prevPage = null)
        {
            gameObject.SetActive(true);
        }

        public virtual void OpenSolve(UI_SolveParent prevSolve, UI_Page prevPage = null)
        {
            gameObject.SetActive(true);
        }

        public virtual void HideSolve()
        {
            gameObject.SetActive(false);
        }


        public virtual bool SkipPage(string pageName, UI_Page currOrPrevPage)
        {
            int pageIndex = -1;

            foreach (var item in _ChildPage)
            {
                if (item.PageName == pageName){
                    pageIndex = item.PageIndex;
                    break;
                }
               
            }

            if (pageIndex >= 0)
            {
                return SkipPage(pageIndex, currOrPrevPage);
            }
            else {
                ALog.Log("None find this page : " + pageName);
            }

            return false;
        }

        public virtual bool SkipPage(int pageIndex, UI_Page currOrPrevPage)
        {
            UI_Page oldPage = _CurrPage;

            foreach (var item in _ChildPage)
            {
                if (item.PageIndex == pageIndex)
                {
                    _CurrPage = item;
                }
                item.SetActive(item.PageIndex == pageIndex);

            }
            if (oldPage == _CurrPage)
            {
                currOrPrevPage?.SetActive(true);
                ALog.Log("None find this page : " + pageIndex);
                return false;
            }
            else
            {
                _CurrPage.PrevPage = currOrPrevPage ?? _CurrPage.PrevPage;
                return true;
            }
        }

        public virtual bool NextPage(UI_Page currPage)
        {
            if (_CurrPage == null || _CurrPage.PageIndex == _ChildPage.Count - 1)
            {
                ALog.Log("This page is last page : " + (_ChildPage.Count - 1));
                return false;
            }

            return SkipPage(_CurrPage.PageIndex + 1, currPage);
        }

        public virtual bool PrevPage()
        {
            if (_CurrPage == null || _CurrPage.PageIndex == 0)
            {
                ALog.Log("Prev page is None : " );
                return false;
            }
            if (_CurrPage.PrevPage != null) return SkipPage(_CurrPage.PrevPage.PageIndex,null);

            return SkipPage(_CurrPage.PageIndex - 1, null);
        }

       
    }
}