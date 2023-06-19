using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ArouseBlockchain.Common;

namespace ArouseBlockchain.UI
{
    public class UI_Popup : MonoBehaviour
    {
        public Color backgroundColor = new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f);

        public float destroyTime = 0.5f;

        public Button close_Butt;
       
        protected Canvas m_canvas;

        private void Start()
        {
            if (close_Butt == null) {
                var butts  = transform.GetComponentsInChildren<Button>();
                close_Butt = butts.First(x=>x.gameObject.name == "Close Button");
                if(close_Butt == null) {
                    ALog.Error("None find that Close Button,please sure you have it!!");
                }
                else {
                    close_Butt.onClick.RemoveAllListeners();
                    close_Butt.onClick.AddListener(() => {
                        Close();
                    });
                }
            }

            
        }

        public void Open(Canvas canvas)
        {
            m_canvas = canvas;
           // AddBackground();
        }

        public void Close()
        {
            var animator = GetComponent<Animator>();
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Open"))
            {
                animator.Play("Close");
            }
           
          //  RemoveBackground();
            StartCoroutine(RunPopupColse());
            UIPopupPool.INST.RemoveBackground();
        }

        // We destroy the popup automatically 0.5 seconds after closing it.
        // The destruction is performed asynchronously via a coroutine. If you
        // want to destroy the popup at the exact time its closing animation is
        // finished, you can use an animation event instead.
        private IEnumerator RunPopupColse()
        {
            yield return new WaitForSeconds(destroyTime);
            //Destroy(m_background);
           // Destroy(gameObject);
            gameObject.SetActive(false);
          
        }

        //private void AddBackground()
        //{
        //    var bgTex = new Texture2D(1, 1);
        //    bgTex.SetPixel(0, 0, backgroundColor);
        //    bgTex.Apply();

        //    m_background = new GameObject("PopupBackground");
        //    var image = m_background.AddComponent<Image>();
        //    var rect = new Rect(0, 0, bgTex.width, bgTex.height);
        //    var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
        //    image.material.mainTexture = bgTex;
        //    image.sprite = sprite;
        //    var newColor = image.color;
        //    image.color = newColor;
        //    image.canvasRenderer.SetAlpha(0.0f);
        //    image.CrossFadeAlpha(1.0f, 0.4f, false);

        //  //  var canvas = GameObject.Find("Canvas");
        //    m_background.transform.localScale = new Vector3(1, 1, 1);
        //    m_background.GetComponent<RectTransform>().sizeDelta = m_canvas.GetComponent<RectTransform>().sizeDelta;
        //    m_background.transform.SetParent(m_canvas.transform, false);
        //    m_background.transform.SetSiblingIndex(transform.GetSiblingIndex());
        //}

        //private void RemoveBackground()
        //{
        //    var image = m_background.GetComponent<Image>();
        //    if (image != null)
        //    {
        //        image.CrossFadeAlpha(0.0f, 0.2f, false);
        //    }
        //}
    }

}