using UnityEngine;
using UnityEngine.UI;

public class SFB_DemoControl_v2 : MonoBehaviour {

	public GameObject cameraObject;							// The camera object
	public float mouseSensitivityY = -0.005f;
	public float mouseSensitivityX = 0.01f;

	private Vector2 lastMousePosition;

	public Slider heightBar;
	
	/// <summary>
	/// Simply sets the .y value of the camera transform
	/// </summary>
	/// <param name="newValue">New value.</param>
	public void SetCameraHeight(float newValue)
	{
		//Debug.Log("Old:" + cameraObject.transform.localPosition);
		Vector3 newPosition = new Vector3(cameraObject.transform.localPosition.x, newValue, cameraObject.transform.localPosition.z);
		cameraObject.transform.localPosition = newPosition;			// Set the position
		//Debug.Log("New: " + cameraObject.transform.localPosition);
	}

	/// <summary>
	/// Simply sets the timescale
	/// </summary>
	/// <param name="newValue">New value.</param>
	public void SetTimescale(float newValue){
		Time.timeScale = newValue;							// Set the value
	}

	private void FixedUpdate()
	{
		if (Input.GetMouseButtonDown(1))
		{
			lastMousePosition = Input.mousePosition;
		}
		if (Input.GetMouseButton(1))
		{
			//Debug.Log("Value 1: " + heightBar.value);
			heightBar.value += (Input.mousePosition.y - lastMousePosition.y) * mouseSensitivityY;
			lastMousePosition = Input.mousePosition;
			//Debug.Log("Value 2: " + heightBar.value);
		}
	}
}