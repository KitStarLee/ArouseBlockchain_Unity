using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_RedDotReminderPart : MonoBehaviour
{
    public Image reddot;
    public Animator icon_ani;

    public Animation OnRemind;

    // Start is called before the first frame update
    void Start()
    {
        reddot = transform.Find("reddot").GetComponent<Image>();
        icon_ani = transform.Find("Icon").GetComponent<Animator>();
        reddot.gameObject.SetActive(false);
       // icon_ani.
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
