using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class autoRotate : MonoBehaviour
{
    public Vector3 rotation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        transform.Rotate(rotation * Time.maximumParticleDeltaTime * 10f);
    }

    private void FixedUpdate()
    {
       // transform.Rotate(rotation * Time.fixedDeltaTime * 10f);
    }
}
