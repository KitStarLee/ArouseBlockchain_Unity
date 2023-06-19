using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using UnityEngine;

namespace SuperNet.Examples.Arena {

	public class ArenaCar : NetworkComponent {

		// Movement settings
		private const float BodyGravity = 10f;
		private const float BodyAccelerationForward = 80f;
		private const float BodyAccelerationBackward = 40f;
		private const float MaxSpeed = 8f;

		// Scene objects
		public Rigidbody Body;
		public Transform Parent;
		public NetworkAuthority Authority;

		// Resources
		private ArenaPlayer Player;
		private Transform PlayerParent;
		private Vector3 Force;

		private void Reset() {
			Body = GetComponent<Rigidbody>();
			Parent = transform.Find("Player");
			Authority = transform.GetComponentInChildren<NetworkAuthority>();
		}

		private void Awake() {
			Player = null;
			PlayerParent = null;
			Force = Vector3.zero;
		}

		private void Update() {

			// Rotate car
			if (Player != null && Player.Authority && Force.magnitude > 0.5f) {
				Quaternion rotation = Quaternion.LookRotation(Force, Vector3.up);
				float factor = Mathf.Lerp(1f, 0.02f, Body.velocity.magnitude / MaxSpeed);
				transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Pow(factor, Time.deltaTime));
			}

		}

		private void FixedUpdate() {

			// Apply gravity
			Body.AddForce(Vector3.down * BodyGravity, ForceMode.Acceleration);

			// Apply input force
			if (Player != null && Player.Authority && Force.magnitude > 0.5f) {
				Vector3 forceLocal = Quaternion.Inverse(transform.rotation) * Force.normalized;
				if (forceLocal.z > 0f) {
					Vector3 force = transform.rotation * new Vector3(0f, 0f, +BodyAccelerationForward);
					Body.AddForce(force, ForceMode.Acceleration);
				} else {
					Vector3 force = transform.rotation * new Vector3(0f, 0f, -BodyAccelerationBackward);
					Body.AddForce(force, ForceMode.Acceleration);
				}
			}

		}

		protected override void OnDestroy() {
			Detach();
			base.OnDestroy();
		}

		public override void OnNetworkConnect(Peer peer) {

			// Send state to new peer
			if (Player != null) {
				SendNetworkMessage(new AttachMessage() { PlayerID = Player.NetworkID }, peer);
			}

		}

		public ArenaPlayer GetAttachedPlayer() {
			return Player;
		}

		public override void OnNetworkRegister() {

			// Send state request to all peers
			SendNetworkMessage(new AttachMessage() { PlayerID = uint.MaxValue });

		}

		public void Attach(ArenaPlayer player) {

			// If already attached, ignore
			if (player == Player) {
				return;
			}

			// Detach any existing players
			Detach();

			// Update player
			Player = player;
			PlayerParent = player.transform.parent;
			Player.transform.parent = Parent;
			Player.transform.localPosition = Vector3.zero;
			Player.transform.localRotation = Quaternion.identity;
			Player.GetComponent<NetworkTransform>().enabled = false;

			// Send attach message
			if (Player.Authority) {
				SendNetworkMessage(new AttachMessage() { PlayerID = player.NetworkID });
			}

			// Claim authority over the car
			if (Player.Authority) {
				Authority.Claim();
			}

			// Disable player collision
			Player.Body.isKinematic = true;
			Collider[] colliders = Player.transform.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colliders) {
				collider.enabled = false;
			}

		}

		public void Detach() {

			// If no player attached, do nothing
			if (Player == null) {
				return;
			}

			// Send detach message
			if (Player.Authority) {
				SendNetworkMessage(new AttachMessage() { PlayerID = NetworkIdentity.VALUE_INVALID });
			}

			// Enable player collision
			Player.Body.isKinematic = false;
			Collider[] colliders = Player.transform.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colliders) {
				collider.enabled = true;
			}

			// Update player
			Player.GetComponent<NetworkTransform>().enabled = true;
			Player.transform.parent = PlayerParent;
			PlayerParent = null;
			Player = null;
			Force = Vector3.zero;

		}

		public void SetForce(Vector3 force) {

			// Called by the local player to move the car
			Force = force;

		}

		private void OnTriggerEnter(Collider other) {

			// Check if collider has a parent
			if (other == null || other.transform == null || other.transform.parent == null) {
				return;
			}

			// Notify player
			ArenaPlayer player = other.transform.parent.GetComponent<ArenaPlayer>();
			if (player != null) {
				player.OnCarTriggerEnter(this);
			}

		}

		private void OnTriggerExit(Collider other) {

			// Check if collider has a parent
			if (other == null || other.transform == null || other.transform.parent == null) {
				return;
			}

			// Notify player
			ArenaPlayer player = other.transform.parent.GetComponent<ArenaPlayer>();
			if (player != null ) {
				player.OnCarTriggerExit(this);
			}

		}

		public override void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) {

			// Read message
			AttachMessage message = new AttachMessage();
			message.Read(reader);

			// Find component to attach
			NetworkComponent component = NetworkManager.FindComponent(message.PlayerID);

			Run(() => {

				// Find component to attach
				component = NetworkManager.FindComponent(message.PlayerID);

				if (message.PlayerID.IsInvalid) {

					// This is a detach message, detach
					Detach();

				} else if (message.PlayerID.Value == uint.MaxValue) {

					// This is a state request, respond
					if (Player != null) {
						SendNetworkMessage(new AttachMessage() { PlayerID = Player.NetworkID }, peer);
					}

				} else if (component != null) {

					// This is an attach message, attach
					ArenaPlayer player = component.GetComponent<ArenaPlayer>();
					if (player != null) {
						Attach(player);
					}

				} else {

					// Error
					Debug.LogError("[Arena] Player " + message.PlayerID + " not found.", this);
					
				}

			}, component == null ? 1f : 0f);

		}

		private struct AttachMessage : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => 0;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public NetworkIdentity PlayerID;

			public void Write(Writer writer) {
				writer.Write(PlayerID);
			}

			public void Read(Reader reader) {
				PlayerID = reader.ReadNetworkID();
			}

		}

	}

}
