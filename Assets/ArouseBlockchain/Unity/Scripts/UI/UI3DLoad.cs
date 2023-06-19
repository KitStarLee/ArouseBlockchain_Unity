using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

public class UI3DLoad : MonoBehaviour
{
    bool display = false;
    public bool Display
    {
        get { return display; }
        set
        {
            display = value;
            ToDisabel(display);
            gameObject.SetActive(display);
        }
    }

    public Transform _HolographicCard;
    public Vector3 _InitRotation;

    public List<Transform> childs = new List<Transform>();

    private void Awake()
    {
        for (int i = transform.childCount - 1 ; i > 0; i--)
        {
            var obj = transform.GetChild(i);
            if (obj.gameObject.activeSelf) childs.Add(obj);
        }
    }

    void ToDisabel(bool enable)
    {
        if (_HolographicCard) _HolographicCard.localRotation = Quaternion.Euler(_InitRotation);
        if (childs!=null && childs.Count > 0)
        {
            childs.ForEach(x => x.gameObject.SetActive(enable));
        }
    }

    [Button]
    public void Test()
    {
        Display = !Display;
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
