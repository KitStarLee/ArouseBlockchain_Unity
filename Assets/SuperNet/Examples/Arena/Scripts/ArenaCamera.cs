using UnityEngine;

namespace SuperNet.Examples.Arena {

	public class ArenaCamera : MonoBehaviour {

		public float Distance;
		public float RotationX;
		public float RotationY;
		public Transform Target;

		public void Start() {
			RotationX = transform.rotation.eulerAngles.x;
			RotationY = transform.rotation.eulerAngles.y;
		}
		
		public void Update() {

			if (Target == null) return;

			if (Cursor.lockState == CursorLockMode.Locked) {
				float inputX = Input.GetAxisRaw("Mouse X");
				float inputY = Input.GetAxisRaw("Mouse Y");
				RotationX -= inputY;
				RotationY += inputX;
			}

			transform.rotation = Quaternion.Euler(RotationX, RotationY, 0f);
			transform.position = Target.position + transform.rotation * Vector3.back * Distance;

		}
		
	}

}
