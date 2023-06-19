using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_CameraRotate_v2 : MonoBehaviour {

	public Transform target;									// Target for the rotation
	public float targetOffset = 1.0f;							// Vertical offset for the target, often 1.0 for the middle of a 2m tall character
	public float speed = 10.0f;									// Speed of the default rotation

	public float mouseSpeed = -3f;
	private Vector2 lastMousePosition;
	
	// Update is called once per frame
	void Update () {
		if (!Input.GetMouseButton(1))
		{
			transform.RotateAround(target.position + new Vector3(0, 1, 0), Vector3.up,
				speed * Time.deltaTime); // Rotate around the point
		}

		transform.LookAt (target.position + new Vector3 (0, targetOffset, 0));									// Look at the position we want
	}

	private void FixedUpdate()
	{
		if (Input.GetMouseButtonDown(1))
		{
			lastMousePosition = Input.mousePosition;
		}

		if (Input.GetMouseButton(1))
		{
			transform.RotateAround(target.position + new Vector3(0, 1, 0), Vector3.up,
				mouseSpeed * (lastMousePosition.x - Input.mousePosition.x) * Time.deltaTime);
			lastMousePosition = Input.mousePosition;
		}
	}

	/// <summary>
	/// Sets the speed value
	/// </summary>
	/// <param name="newValue">New value.</param>
	public void SetSpeed(float newValue){
		speed = newValue;
	}

}