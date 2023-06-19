using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using ArouseBlockchain.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

using static ArouseBlockchain.UI.UI_P2PContact;


public class UIPage_Interim : MonoBehaviour, IUIPage
{
    // Start is called before the first frame update

    Image image;
    public void Init(RectTransform rect)
    {
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 过渡颜色
    /// </summary>
    /// <param name="basecolor">初始颜色</param>
    /// <param name="toAlpha">目标透明度，0-1直接</param>
    /// <param name="duration">持续时间</param>
    public void InterimClor(Color basecolor, float toAlpha, float duration = 1f)
    {
        image.enabled = true;
        basecolor.a = 1 - toAlpha;
        image.color = basecolor;
        image.DOFade(toAlpha, duration).onComplete = () => {
            image.enabled = false;
        };

    }

}
