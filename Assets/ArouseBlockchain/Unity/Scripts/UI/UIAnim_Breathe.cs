using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

public class UIAnim_Breathe : MonoBehaviour
{
    public float posAmplify = 3;
    public float scaleAmplify = 0.5f;
    public float runMinTime = 1;
    public float runMaxTime = 2;
    public bool posMotion = true;
    public bool scaleMotion = true;
    public RectTransform[] _RectObj;

    Vector2[] orginPos;
    Vector2[] currPos;
    Vector2[] toPos;
    Vector3[] toScale;
    Vector3[] currScale;
    float[] timer;
    float[] max_timer;

    // Start is called before the first frame update
    void Start()
    {
        if(_RectObj!=null && _RectObj.Length > 0)
        {
            orginPos = new Vector2[_RectObj.Length];
            currPos = new Vector2[_RectObj.Length];
            toPos = new Vector2[_RectObj.Length];
            toScale = new Vector3[_RectObj.Length];
            timer = new float[_RectObj.Length];
            max_timer = new float[_RectObj.Length];
            currScale = new Vector3[_RectObj.Length]; 

            for (int i = 0; i < orginPos.Length; i++)
            {
                orginPos[i] = _RectObj[i].anchoredPosition;
                SetRandomValue(i);
            }
        }
       
    }

    // Update is called once per frame
    void Update()
    {
        //for (int i = 0; i < orginPos.Length; i++)
        //{
        //    if (_RectObj[i] != null && _RectObj[i].gameObject.activeInHierarchy)
        //    {
        //        timer[i] += Time.deltaTime;

        //        if (timer[i] > max_timer[i]) SetRandomValue(i);
        //        else
        //        {
        //            float porr = timer[i] / max_timer[i];
        //            _RectObj[i].anchoredPosition = Vector2.Lerp(currPos[i], toPos[i], porr);
        //            _RectObj[i].localScale = Vector3.Lerp(currScale[i], toScale[i], porr);
        //        }
        //    }
        //}
    }

    private void FixedUpdate()
    {
        if (!posMotion && !scaleMotion) return;

        for (int i = 0; i < orginPos.Length; i++)
        {
            if (_RectObj[i] != null && _RectObj[i].gameObject.activeInHierarchy)
            {
                timer[i] += Time.fixedDeltaTime;

                if (timer[i] > max_timer[i]) SetRandomValue(i);
                else
                {
                    float porr = timer[i] / max_timer[i];
                    if(posMotion)_RectObj[i].anchoredPosition = Vector2.Lerp(currPos[i], toPos[i], porr);
                    if(scaleMotion)_RectObj[i].localScale = Vector3.Lerp(currScale[i], toScale[i], porr);
                }
            }
        }
    }

    void SetRandomValue(int index)
    {
        if (_RectObj[index] == null || !_RectObj[index].gameObject.activeInHierarchy) return;

        if (posMotion)
        {
            toPos[index] = orginPos[index] + Random.insideUnitCircle * posAmplify*10f;
            currPos[index] = _RectObj[index].anchoredPosition;
        }

        if (scaleMotion)
        {
            if (toScale[index] != Vector3.one) toScale[index] = Vector3.one;
            else toScale[index] = Vector3.one + (Vector3.one * Random.Range(scaleAmplify/2f, scaleAmplify));

            toScale[index].z = 1;
            currScale[index] = _RectObj[index].localScale;
            currScale[index].z = 1;
        }

        timer[index] = 0;
        max_timer[index] = Random.Range(runMinTime, runMaxTime);
    }
}
