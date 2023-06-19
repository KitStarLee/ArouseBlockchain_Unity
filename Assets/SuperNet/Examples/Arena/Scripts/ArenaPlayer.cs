using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System;
using UnityEngine;

namespace SuperNet.Examples.Arena {

	public class ArenaPlayer : NetworkComponent {

		// Movement settings
		private const float AccelerationStandingForward = 45f;
		private const float AccelerationStandingBackward = 25f;
		private const float AccelerationStandingSideways = 40f;
		private const float AccelerationCrouchingForward = 16f;
		private const float AccelerationCrouchingBackward = 14f;
		private const float AccelerationCrouchingSideways = 14f;
		private const float AccelerationFallingForward = 4f;
		private const float AccelerationFallingBackward = 2f;
		private const float AccelerationFallingSideways = 2f;
		private const float MaxSpeedStandingForward = 4.5f;
		private const float MaxSpeedStandingBackward = 2.5f;
		private const float MaxSpeedStandingSideways = 4f;
		private const float MaxSpeedCrouchingForward = 1.5f;
		private const float MaxSpeedCrouchingBackward = 1.4f;
		private const float MaxSpeedCrouchingSideways = 1.4f;
		private const float BodyJumpVelocity = 6f;
		private const float BodyDragRemote = 0f;
		private const float BodyDragStanding = 8f;
		private const float BodyDragCrouching = 8f;
		private const float BodyDragFalling = 0.5f;
		private const float BodyGravity = 10f;

		// Scene objects
		public Rigidbody Body;
		public NetworkTransform NetworkTransform;
		public NetworkAnimator NetworkAnimator;
		public Transform FloorRaycastSource;
		public Transform FloorRaycastTarget;
		public Transform ViewTarget;
		public bool Authority;

		// Resources
		private ArenaCamera View;
		private ArenaGame Menu;
		private PlayerAnimation Animation;
		private Vector3 Force;
		private ArenaCar Car;

		// Animation states
		private enum PlayerAnimation : byte {
			Invalid = 0,
			Standing = 1,
			Crouching = 2,
			Falling = 3,
			Riding = 4,
		}

		private void Reset() {
			ResetNetworkID();
			Body = GetComponentInChildren<Rigidbody>();
			NetworkTransform = GetComponentInChildren<NetworkTransform>();
			NetworkAnimator = GetComponentInChildren<NetworkAnimator>();
			FloorRaycastSource = transform.Find("RaycastSource").transform;
			FloorRaycastTarget = transform.Find("RaycastTarget").transform;
			ViewTarget = transform.Find("ViewTarget").transform;
			Authority = false;
		}

		private void Awake() {

			// Initialize resources
			View = FindObjectOfType<ArenaCamera>();
			Menu = FindObjectOfType<ArenaGame>();
			NetworkAnimator.OnNetworkTrigger += OnAnimatorTrigger;
			Animation = PlayerAnimation.Standing;
			Force = Vector3.zero;
			Car = null;

			// Set initial animation
			if (Authority) UpdateAnimation(PlayerAnimation.Standing);

		}

		private void Update() {
			if (Authority) {
				// This is a local player, perform input based movement
				UpdateLocal();
			} else {
				// This is a remote player, just apply animator values
				SetAnimationValues(transform.rotation);
			}
		}

		private void FixedUpdate() {

			// If we are riding a car, do nothing
			if (Animation == PlayerAnimation.Riding && Car != null) {
				return;
			}

			// Apply gravity
			Body.AddForce(Vector3.down * BodyGravity, ForceMode.Acceleration);

			// Apply input force
			if (Authority) Body.AddForce(Force, ForceMode.Acceleration);

		}

		private void UpdateLocal() {

			// Check if touching floor
			Vector3 rOrigin = FloorRaycastSource.position;
			Vector3 rDirection = FloorRaycastTarget.position - rOrigin;
			bool isOnFloor = Physics.Raycast(rOrigin, rDirection, rDirection.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
			
			// If travelling upwards, we're not on the floor
			float speedUpwards = Vector3.Dot(Body.velocity, transform.up);
			if (speedUpwards > BodyJumpVelocity * 0.5f) {
				isOnFloor = false;
			}

			// Get view rotation
			ArenaCamera view = View;
			Quaternion viewRotation = Quaternion.Euler(0, view.RotationY, 0);

			// Get input direction
			Vector3 inputDirection = Vector3.zero;
			if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDirection += viewRotation * Vector3.forward;
			if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDirection += viewRotation * Vector3.back;
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDirection += viewRotation * Vector3.left;
			if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDirection += viewRotation * Vector3.right;

			if (Animation == PlayerAnimation.Riding && Car != null && Car.GetAttachedPlayer() == this) {

				// Set car force
				Car.SetForce(inputDirection);

			} else {

				// Get acceleration values
				GetAcceleration(out float AccelerationForward, out float AccelerationBackward, out float AccelerationSideways);

				// Convert input direction to force
				if (inputDirection.magnitude > 0.5f) {
					Vector3 directionLocal = Quaternion.Inverse(transform.rotation) * inputDirection.normalized;
					float z = (directionLocal.z > 0f ? AccelerationForward : AccelerationBackward);
					Vector3 forceLocal = new Vector3(directionLocal.x * AccelerationSideways, 0f, directionLocal.z * z);
					Force = transform.rotation * forceLocal;
				} else {
					Force = Vector3.zero;
				}

				// Set animator values
				SetAnimationValues(viewRotation);

				// Rotate towards view direction
				transform.rotation = Quaternion.Slerp(transform.rotation, viewRotation, 1 - Mathf.Pow(0.02f, Time.deltaTime));

				// Jump
				if (isOnFloor && Input.GetKeyDown(KeyCode.Space) && Animation != PlayerAnimation.Falling) {
					SetAnimation(PlayerAnimation.Falling);
					Body.AddForce(transform.up * BodyJumpVelocity, ForceMode.VelocityChange);
					isOnFloor = false;
				}

			}

			// Change animation based on input
			switch (Animation) {
				default:
				case PlayerAnimation.Standing:
					if (Car != null && Car.GetAttachedPlayer() == null && Input.GetKeyDown(KeyCode.E)) {
						Menu.GameInfo.text = "Press E to exit the car.";
						SetAnimation(PlayerAnimation.Riding);
						Car.Attach(this);
					} else if (isOnFloor && Input.GetKeyDown(KeyCode.Space)) {
						SetAnimation(PlayerAnimation.Falling);
						Body.AddForce(transform.up * BodyJumpVelocity, ForceMode.VelocityChange);
					} else if (isOnFloor && Input.GetKey(KeyCode.LeftShift)) {
						SetAnimation(PlayerAnimation.Crouching);
					} else if (!isOnFloor) {
						SetAnimation(PlayerAnimation.Falling);
					}
					break;
				case PlayerAnimation.Crouching:
					if (Car != null && Car.GetAttachedPlayer() == null && Input.GetKeyDown(KeyCode.E)) {
						Menu.GameInfo.text = "Press E to exit the car.";
						SetAnimation(PlayerAnimation.Riding);
						Car.Attach(this);
					} else if (isOnFloor && Input.GetKeyDown(KeyCode.Space)) {
						SetAnimation(PlayerAnimation.Falling);
						Body.AddForce(transform.up * BodyJumpVelocity, ForceMode.VelocityChange);
					} else if (isOnFloor && !Input.GetKey(KeyCode.LeftShift)) {
						SetAnimation(PlayerAnimation.Standing);
					} else if (!isOnFloor) {
						SetAnimation(PlayerAnimation.Falling);
					}
					break;
				case PlayerAnimation.Falling:
					if (Car != null && Car.GetAttachedPlayer() == null && Input.GetKeyDown(KeyCode.E)) {
						Menu.GameInfo.text = "Press E to exit the car.";
						SetAnimation(PlayerAnimation.Riding);
						Car.Attach(this);
					} else if (isOnFloor && Input.GetKey(KeyCode.LeftShift)) {
						SetAnimation(PlayerAnimation.Crouching);
					} else if (isOnFloor) {
						SetAnimation(PlayerAnimation.Standing);
					}
					break;
				case PlayerAnimation.Riding:
					if (Car == null || Car.GetAttachedPlayer() != this) {
						Menu.GameInfo.text = "";
						SetAnimation(PlayerAnimation.Standing);
					} else if (Input.GetKeyDown(KeyCode.E)) {
						Menu.GameInfo.text = "Press E to enter the car.";
						SetAnimation(PlayerAnimation.Standing);
						Car.Detach();
					}
					break;
			}

		}

		public void OnCarTriggerEnter(ArenaCar car) {
			if (Authority) {
				Menu.GameInfo.text = "Press E to enter the car.";
				Car = car;
			}
		}

		public void OnCarTriggerExit(ArenaCar car) {
			if (Authority && Car == car) {
				Menu.GameInfo.text = "";
				Car = null;
			}
		}

		private void SetAnimationValues(Quaternion viewRotation) {

			// Get max speeds
			GetMaxSpeed(out float MaxSpeedForward, out float MaxSpeedBackward, out float MaxSpeedSideways);

			// Set animator values
			Vector3 velocity = Vector3.ProjectOnPlane(transform.InverseTransformDirection(Body.velocity), Vector3.up);
			float movingSpeedX = velocity.x / MaxSpeedSideways;
			float movingSpeedZ = velocity.z / (velocity.z > 0f ? MaxSpeedForward : MaxSpeedBackward);
			float movingSpeed = Mathf.Clamp(new Vector2(movingSpeedX, movingSpeedZ).magnitude, 0, 1);
			float rotateSpeed = (1f - movingSpeed) * Vector3.SignedAngle(transform.forward, viewRotation * Vector3.forward, transform.up) / 40f;
			float idleAmount = Mathf.Clamp(new Vector2(movingSpeed, rotateSpeed).magnitude, 0, 1);
			NetworkAnimator.Animator.SetFloat("RunIdle", Mathf.Clamp(1f - idleAmount, 0, 1));
			NetworkAnimator.Animator.SetFloat("RunForward", Mathf.Clamp(movingSpeedZ, 0, 1) * idleAmount);
			NetworkAnimator.Animator.SetFloat("RunBackward", Mathf.Clamp(-movingSpeedZ, 0, 1) * idleAmount);
			NetworkAnimator.Animator.SetFloat("RunLeft", Mathf.Clamp(-movingSpeedX, 0, 1) * idleAmount);
			NetworkAnimator.Animator.SetFloat("RunRight", Mathf.Clamp(movingSpeedX, 0, 1) * idleAmount);
			NetworkAnimator.Animator.SetFloat("RotatingLeft", Mathf.Clamp(-rotateSpeed, 0, 1) * idleAmount);
			NetworkAnimator.Animator.SetFloat("RotatingRight", Mathf.Clamp(rotateSpeed, 0, 1) * idleAmount);

		}

		private void OnAnimatorTrigger(int hash) {

			// Go through all animations and update the animation if found
			foreach (PlayerAnimation animation in Enum.GetValues(typeof(PlayerAnimation))) {
				int test = Animator.StringToHash(animation.ToString());
				if (test == hash) {
					UpdateAnimation(animation);
					break;
				}
			}

		}

		private void SetAnimation(PlayerAnimation animation) {

			// Change animation in the animator and broadcast it to the network
			int hash = Animator.StringToHash(animation.ToString());
			NetworkAnimator.SetTrigger(hash);

			// Update animatiom
			UpdateAnimation(animation);

		}

		private void UpdateAnimation(PlayerAnimation animation) {
			
			// Update animation variables only
			// Does not effect the animator

			// Update animation and change rigidbody drag
			Animation = animation;
			if (Authority) {
				switch (animation) {
					default:
					case PlayerAnimation.Standing:
						Body.drag = BodyDragStanding;
						break;
					case PlayerAnimation.Crouching:
						Body.drag = BodyDragCrouching;
						break;
					case PlayerAnimation.Falling:
						Body.drag = BodyDragFalling;
						break;
				}
			} else {
				Body.drag = BodyDragRemote;
			}

		}

		private void GetMaxSpeed(out float MaxSpeedForward, out float MaxSpeedBackward, out float MaxSpeedSideways) {
			switch (Animation) {
				default:
				case PlayerAnimation.Standing:
					MaxSpeedForward = MaxSpeedStandingForward;
					MaxSpeedBackward = MaxSpeedStandingBackward;
					MaxSpeedSideways = MaxSpeedStandingSideways;
					break;
				case PlayerAnimation.Crouching:
					MaxSpeedForward = MaxSpeedCrouchingForward;
					MaxSpeedBackward = MaxSpeedCrouchingBackward;
					MaxSpeedSideways = MaxSpeedCrouchingSideways;
					break;
				case PlayerAnimation.Falling:
					MaxSpeedForward = MaxSpeedStandingForward;
					MaxSpeedBackward = MaxSpeedStandingBackward;
					MaxSpeedSideways = MaxSpeedStandingSideways;
					break;
			}
		}

		private void GetAcceleration(out float AccelerationForward, out float AccelerationBackward, out float AccelerationSideways) {
			switch (Animation) {
				default:
				case PlayerAnimation.Standing:
					AccelerationForward = AccelerationStandingForward;
					AccelerationBackward = AccelerationStandingBackward;
					AccelerationSideways = AccelerationStandingSideways;
					break;
				case PlayerAnimation.Crouching:
					AccelerationForward = AccelerationCrouchingForward;
					AccelerationBackward = AccelerationCrouchingBackward;
					AccelerationSideways = AccelerationCrouchingSideways;
					break;
				case PlayerAnimation.Falling:
					AccelerationForward = AccelerationFallingForward;
					AccelerationBackward = AccelerationFallingBackward;
					AccelerationSideways = AccelerationFallingSideways;
					break;
			}
		}

	}

}
