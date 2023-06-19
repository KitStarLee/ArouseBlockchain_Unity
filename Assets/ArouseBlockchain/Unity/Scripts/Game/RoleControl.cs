using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

[RequireComponent(typeof(SFB_BlendShapesManager))]
public class RoleControl : MonoBehaviour
{
    [SerializeField]
    Transform _BodyMesh;


    Animator _Animator;
    SFB_BlendShapesManager _BlendShapes;
    [SerializeField]
    SkinnedMeshRenderer bodyMeshRenderer;

    private void Awake()
    {
        _Animator = GetComponent<Animator>();
        _BlendShapes = GetComponent<SFB_BlendShapesManager>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   
    public void SetRoleColor(Color color)
    {
        if (_BodyMesh)
        {
             if(bodyMeshRenderer == null)
            {
                bodyMeshRenderer = _BodyMesh.GetComponent<SkinnedMeshRenderer>();
            
            }
            var bodyMaterial = bodyMeshRenderer.materials[0];
            var newColor = color;// - new Color(color.g * 0.2f, color.g * 0.2f, 0, 0);
            bodyMaterial.SetColor("_BaseColor", newColor);
            var emiss = newColor + (newColor * 0.1f);
            emiss.a += 1;
            bodyMaterial.SetColor("_EmissionColor", emiss);

            var bodyMaterial2 = bodyMeshRenderer.materials[1];
            var baseColor = newColor - (newColor * 0.7f);
            baseColor.a = 0;
            bodyMaterial2.SetColor("_BaseColor", baseColor);
        }
    }

    [Button]
    void RandomizeAll()
    {
        _BlendShapes.RandomizeAll();
    }

    /// <summary>
    /// idleBreak 默认状态;  attack1 攻击1;  attack2 攻击2;  cast;  dodge ; gotHit;  death
    /// </summary>
    /// <param name="triggerName"></param>
    [Button]
    void SetAnimatorTrigger(string triggerName)
    {
        _Animator.SetTrigger(triggerName);
    }

    //移动
    public void Locomotion(float newValue)
    {
        _Animator.SetFloat("locomotion", newValue);
    }

    public void SetWalk(float newValue)
    {
        _Animator.SetFloat("walk", newValue);
    }

}
