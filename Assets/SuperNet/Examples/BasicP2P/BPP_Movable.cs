using SuperNet.Unity.Components;
using UnityEngine;

namespace SuperNet.Examples.BPP {

	public class BPP_Movable : MonoBehaviour {

		// Network authority responsible for this object
		public NetworkAuthority Authority;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			Authority = GetComponentInChildren<NetworkAuthority>();
		}

		private void OnCollisionEnter(Collision collision) {

			// Get object we're colliding with
			GameObject obj = collision.gameObject;
			if (obj == null) {
				return;
			}

			// If object is local player, claim authority
			BPP_Player player = obj.GetComponent<BPP_Player>();
			if (player != null && player.Authority) {
				Authority.Claim();
				return;
			}

		}

	}

}
