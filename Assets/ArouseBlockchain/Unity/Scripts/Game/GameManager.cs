using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager INST;

    public GameObject _3DScenes;
    public Color _ThemeColors;

    Skybox _MainSkybox;
    RoleControl _RoleControl;

    private void Awake()
    {
        INST = this;
        _3DScenes.SetActive(false);
    }


    [Button]
    void InitSceneColor()
    {
        if(_MainSkybox == null) _MainSkybox = FindObjectOfType<Skybox>();
        Color offColor = _ThemeColors * 0.4f;
        Debug.Log(string.Format("原色= {0} ; 现色= {1}", _ThemeColors, offColor));
       //  var newSkyColor = (_ThemeColors.g >= 0.5) ? _ThemeColors - offColor  : _ThemeColors +  offColor;
        var newSkyColor = _ThemeColors - offColor;
        _MainSkybox.material.SetColor("_Color1", newSkyColor);
        if(_RoleControl == null) _RoleControl = FindObjectOfType<RoleControl>();
        _ThemeColors.a = 1;
        _RoleControl.SetRoleColor(_ThemeColors);
    }


    //Color ColorLimit(Color color,float max = 0.8f)
    //{
    //    color = new Color(Limit(color.r), Limit(color.g), Limit(color.b));

    //    float Limit(float cannle)
    //    {
    //        return Mathf.Max(cannle, max);
    //    }
    //    color.a = 1;

    //    return color;
    //}
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
