using System.Collections;
using System.Collections.Generic;
using UltimateClean;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace ArouseBlockchain.UI
{
    [RequireComponent(typeof(Button))]
    public class UI_PopupOpener : MonoBehaviour
    {
        /// <summary>
        /// This class is responsible for creating and opening a popup of the
        /// given prefab and adding it to the UI canvas of the current scene.
        /// </summary>
        public GameObject popupPrefab;

        protected Canvas m_canvas;
        protected GameObject m_popup;


        protected void Start()
        {
            Button button = transform.GetComponent<Button>();
            button.onClick.AddListener(() =>{
                OpenPopup();
            });
            m_canvas = transform.GetComponentInParent<Canvas>();
        }

        public virtual void OpenPopup()
        {
            UIPopupPool.OpenPopup(m_canvas, popupPrefab);
            //m_popup = Instantiate(popupPrefab, m_canvas.transform, false);
            //m_popup.SetActive(true);
            //m_popup.transform.localScale = Vector3.zero;
            //m_popup.GetComponent<UI_Popup>().Open(m_canvas);
        }
    }
}