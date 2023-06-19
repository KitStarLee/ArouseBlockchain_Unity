using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_StayWithTarget : MonoBehaviour {

	public GameObject target;								// Target to stay with

	void LateUpdate(){
		transform.position = target.transform.position;
	}
}