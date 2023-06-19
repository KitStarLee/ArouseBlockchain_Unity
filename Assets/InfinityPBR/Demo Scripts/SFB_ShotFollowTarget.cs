using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_ShotFollowTarget : MonoBehaviour {

	public GameObject target;
	public float turnSpeed = 4.0f;
	public float speed = 10.0f;

	// Use this for initialization
	void Start () {
		if (!target) {
			target = GameObject.Find ("Enemy");
		}
	}
	
	// Update is called once per frame
	void Update () {
		// Creates a Vector with the X/Z of the target, but the Y of the source
		// Otherwise, when the playerObject gets close to the characer doing the looking,
		// it will lean back or forward in order to Look At the player.
		// Get rotation between forward of two objects
		Quaternion targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
		// Turn towards playerObject over time.
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
		transform.Translate(Vector3.forward * Time.deltaTime * speed);
	}
}
