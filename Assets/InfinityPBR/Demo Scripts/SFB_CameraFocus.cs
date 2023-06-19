using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_CameraFocus : MonoBehaviour
{
    public GameObject[] targets;
    public SFB_CameraRotate_v2 cameraRotate;
    public SFB_StayWithTarget stayWithTarget;

    public void SelectTarget(int newTarget)
    {
        cameraRotate.target = targets[newTarget].transform;
        stayWithTarget.target = targets[newTarget];
    }
}
