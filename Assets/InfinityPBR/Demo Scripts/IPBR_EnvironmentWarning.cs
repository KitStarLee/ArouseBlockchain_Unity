using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class IPBR_EnvironmentWarning : MonoBehaviour
{
    private string folderPath = "assets/BOTD_Standard Shader";
    public GameObject panel;
    #if UNITY_EDITOR

    // Start is called before the first frame update
    void Awake()
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            panel.SetActive(true);
        }
    }

    public void GoToAssetStore()
    {
        Application.OpenURL("https://infinitypbr.com/book-of-the-dead-converted");
    }
    
    #endif
}
