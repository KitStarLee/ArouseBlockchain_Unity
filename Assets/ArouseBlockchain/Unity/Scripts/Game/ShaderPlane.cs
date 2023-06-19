using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderPlane : MonoBehaviour
{
    [SerializeField]
    Transform _RoleTransform;
    [SerializeField]
    float dis;

    Material _Material;
    // Start is called before the first frame update
    void Start()
    {
        //  _RoleTransform = transform.parent;
        _Material = GetComponent<MeshRenderer>().material;
    }

   
    // Update is called once per frame
    void Update()
    {
        dis = Vector3.Distance(_RoleTransform.position, transform.position);
        Color color = _Material.GetColor("_BaseColor");
        //  0.16 <a < 0.4 
        color.a = Mathf.Max((1- (dis / 8)) * 0.5f, 0.1f); 
        _Material.SetColor("_BaseColor", color);
    }
}
