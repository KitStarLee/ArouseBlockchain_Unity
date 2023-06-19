using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SceneEnvironment : MonoBehaviour
{

    private void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
    }

    private void EnvironmentReset()
    {
        Debug.Log("在这里重置场景Reset the scene here");
    }

    private void OnDestroy()
    {
        Academy.Instance.OnEnvironmentReset -= EnvironmentReset;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
