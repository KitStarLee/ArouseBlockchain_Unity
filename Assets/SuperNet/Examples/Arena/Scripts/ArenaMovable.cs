using SuperNet.Unity.Components;
using UnityEngine;

namespace SuperNet.Examples.Arena {

	public class ArenaMovable : MonoBehaviour {

		// Network authority responsible for this object
		public NetworkAuthority Authority;

		private void Reset() {
			Authority = GetComponentInChildren<NetworkAuthority>();
		}

		private void OnCollisionEnter(Collision collision) {

			// Get object we're colliding with
			GameObject obj = collision.gameObject;
			if (obj == null) {
				return;
			}

			// If object is local car, claim authority
			ArenaCar car = obj.GetComponent<ArenaCar>();
			if (car != null && car.Authority.IsOwner && car.GetAttachedPlayer() != null) {
				Authority.Claim();
				return;
			}

			// If object is local player, claim authority
			ArenaPlayer player = obj.GetComponent<ArenaPlayer>();
			if (player != null && player.Authority) {
				Authority.Claim();
				return;
			}

		}

	}

}
