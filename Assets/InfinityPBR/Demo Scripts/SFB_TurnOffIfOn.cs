using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_TurnOffIfOn : MonoBehaviour {

	public GameObject objectToWatch;				// Object we will watch
	public GameObject objectToSwitch;				// Object that will be turned off

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (objectToWatch.activeSelf) {				// If the object we're watching is on
			objectToSwitch.SetActive (false);		// Turn object to switch off
		}
	}
}