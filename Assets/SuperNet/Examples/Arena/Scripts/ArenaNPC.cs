using SuperNet.Netcode.Transport;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SuperNet.Examples.Arena {

	public class ArenaNPC : NetworkComponent, INetworkAuthoritative {

		// Movement settings
		private const float BodyGravity = 10f;
		private const float BodyJumpVelocity = 6f;
		private const float BodyAcceleration = 45f;
		
		// Scene objects
		public Rigidbody Body;

		// Resources
		private Vector3 Force;
		private float Changed;
		private HashSet<GameObject> Overlaps;

		private void Reset() {
			Body = GetComponentInChildren<Rigidbody>();
		}

		private void Awake() {
			Force = Vector3.zero;
			Changed = Time.time;
			Overlaps = new HashSet<GameObject>();
			SetRandomDirection();
		}

		private void Update() {

			// Rotate in the direction of force
			Quaternion rotation = Quaternion.LookRotation(Force, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1 - Mathf.Pow(0.02f, Time.deltaTime));

		}

		private void FixedUpdate() {

			// If colliding with something and we haven't moved yet, jump and change direction
			if (Overlaps.Count > 0 && Time.time - Changed > 0.5f) {
				Body.AddForce(Vector3.up * BodyJumpVelocity, ForceMode.VelocityChange);
				SetRandomDirection();
			}

			// Apply gravity
			Body.AddForce(Vector3.down * BodyGravity, ForceMode.Acceleration);

			// Apply input force
			Body.AddForce(Force * BodyAcceleration, ForceMode.Acceleration);

		}

		private void OnCollisionEnter(Collision collision) {

			// Ignore collisions with the floor
			if (new Vector2(collision.impulse.x, collision.impulse.z).magnitude <= 0.1f) {
				return;
			}

			// Collision detected, change direction
			SetRandomDirection();
			Overlaps.Add(collision.gameObject);
			
		}

		private void OnCollisionExit(Collision collision) {

			// Stopped colliding
			Overlaps.Remove(collision.gameObject);

		}

		private void SetRandomDirection() {
			Force = Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up).normalized;
			Changed = Time.time;
		}

		void INetworkAuthoritative.OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp) {
			
			// Enable if we have authority, disable if we dont
			Run(() => enabled = authority);

		}

	}

}
