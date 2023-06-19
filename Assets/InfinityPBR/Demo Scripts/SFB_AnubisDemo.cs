using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_AnubisDemo : MonoBehaviour {

	public GameObject anubis;
	private Animator animator;
	public Transform spawnPos;
	public GameObject spawnObj;

	// Use this for initialization
	void Start () {
		animator = anubis.GetComponent<Animator> ();
	}
	
	public void Locomotion(float newValue){
		animator.SetFloat ("locomotion", newValue);
	}

	public void Cast(){
		GameObject newObject = Instantiate (spawnObj, spawnPos.position, Quaternion.identity);
		Destroy (newObject, 10.0f);
	}
}

