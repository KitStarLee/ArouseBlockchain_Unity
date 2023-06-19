using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_ToggleOpposite : MonoBehaviour {

	public GameObject objectToWatch;				// Object we will watch
	public GameObject objectToSwitch;				// Object that will be turned off

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		objectToSwitch.SetActive (!objectToWatch.activeSelf);
	}
}