using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using ArouseBlockchain.UI;
using UltimateClean;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupPool : MonoBehaviour
{
    GameObject PoolRootObj;
    static UIPopupPool instance;
    public static UIPopupPool INST => instance;

    Dictionary<string, GameObject> ObjPool = new Dictionary<string, GameObject>();
    public GameObject m_background;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        instance = this;
        transform.parent = null;

        if (INST.m_background != null) m_background.SetActive(false);

    }

    public static void AddAdvancePopup(Canvas parent_canvas, GameObject popupobj)
    {
        if (popupobj != null)
        {
            INST.ObjPool.Add(popupobj.name, popupobj);
            popupobj.gameObject.SetActive(false);
        }
    }
    public static void OpenPopup(Canvas parent_canvas, GameObject popupPrefab)
    {
        if(INST.PoolRootObj == null) {
            INST.PoolRootObj = INST.CreatBackground("PoolRoot");
            INST.PoolRootObj.GetComponent<Image>().enabled = false;
        }

        INST.PoolRootObj.transform.SetParent(parent_canvas.transform, false);
        INST.PoolRootObj.GetComponent<RectTransform>().sizeDelta = parent_canvas.GetComponent<RectTransform>().sizeDelta;



        GameObject m_popup = null;
        foreach (var objname in INST.ObjPool.Keys)
        {
            if(objname == popupPrefab.name)
            {
                m_popup = INST.ObjPool[objname];
                if (m_popup.gameObject.activeSelf) {

                    ALog.Log("this ui obj is active,please sure this success ?");
                    return;
                }
                break;
            }
        }

        if(m_popup == null) {
            m_popup = Instantiate(popupPrefab, parent_canvas.transform, false);
            INST.ObjPool.Add(popupPrefab.name, m_popup);
        }

        if (INST.m_background == null) INST.m_background = INST.CreatBackground("PopupBackground");

        m_popup.gameObject.SetActive(true);
        m_popup.transform.SetParent(INST.PoolRootObj.transform, false);
        m_popup.transform.SetSiblingIndex(INST.PoolRootObj.transform.childCount);
        m_popup.transform.localScale = Vector3.zero;
        m_popup.GetComponent<UI_Popup>().Open(parent_canvas);
     

        INST.m_background.gameObject.SetActive(true);
        INST.m_background.GetComponent<RectTransform>().sizeDelta = parent_canvas.GetComponent<RectTransform>().sizeDelta;
        INST.m_background.transform.SetParent(parent_canvas.transform, false);
        INST.m_background.transform.SetSiblingIndex(parent_canvas.transform.childCount);
        INST.PoolRootObj.transform.SetSiblingIndex(parent_canvas.transform.childCount);
        // INST.PoolRootObj.transform.position = Vector3.zero;
        var image = INST.m_background.GetComponent<Image>();
        if (image != null)
        {
            image.CrossFadeAlpha(1.0f, 0.4f, false);
        }

        
    }

    //public static void SetBackgroundColor(Color color)
    //{
    //    if (INST.m_background == null) return;

    //    var image = INST.m_background.GetComponent<Image>();
    //    if (image != null)
    //    {
    //        image.CrossFadeAlpha(0.0f, 0.2f, false);
    //    }
    //}

    public void RemoveBackground(float duration = 0.2f)
    {
        if (INST.m_background == null) return;

        var image = INST.m_background.GetComponent<Image>();
        if (image != null)
        {
            image.CrossFadeAlpha(0.0f, duration, false);
            StartCoroutine(INST.RunBackgroundColse(duration));
        }
    }

    private IEnumerator RunBackgroundColse(float duration)
    {
        yield return new WaitForSeconds(duration);
        //Destroy(m_background);
        // Destroy(gameObject);
        m_background.SetActive(false);
    }

    private GameObject CreatBackground(string objname)
    {
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f));
        bgTex.Apply();

        var obj  = new GameObject(objname);
        var image = obj.AddComponent<Image>();
        var rect = new Rect(0, 0, bgTex.width, bgTex.height);
        var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
        image.material.mainTexture = bgTex;
        image.sprite = sprite;
        var newColor = image.color;
        image.color = newColor;
        image.canvasRenderer.SetAlpha(0.0f);
        image.CrossFadeAlpha(1.0f, 0.4f, false);

        //  var canvas = GameObject.Find("Canvas");
        obj.transform.localScale = new Vector3(1, 1, 1);

        return obj;
    }
}
