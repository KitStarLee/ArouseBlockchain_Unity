using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_DemoGelatinous : MonoBehaviour {

	public GameObject prefab;
	public Transform spawnPos;

	public void Cast(){
		GameObject newParticle = Instantiate(prefab, spawnPos.position, Quaternion.identity);
		Destroy (newParticle, 5.0f);
	}
}
