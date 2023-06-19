using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using UnityEngine;

namespace SuperNet.Examples.BCS {

	public class BCS_Player : NetworkComponent, INetworkAuthoritative {

		[Header("Player Components")]
		public NetworkAuthority NetworkAuthority;
		public MeshRenderer Capsule;
		public TextMesh Text;

		[Header("Scene Components")]
		public Camera Camera;
		public BCS_PlayerSpawner PlayerSpawner;
		public BCS_Game Game;

		[Header("Player Information")]
		public bool Authority;
		public string Name;
		public Color32 Color;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			NetworkAuthority = GetComponent<NetworkAuthority>();
			Capsule = GetComponentInChildren<MeshRenderer>();
			Text = GetComponentInChildren<TextMesh>();
			Authority = false;
			Name = "Player";
			Color = new Color32(0, 0xFF, 0, 0xFF);
		}

		private void Awake() {
			// Find scene components
			Camera = FindObjectOfType<Camera>();
			PlayerSpawner = FindObjectOfType<BCS_PlayerSpawner>();
			Game = FindObjectOfType<BCS_Game>();
		}

		private void Update() {

			// Rotate the player name to face the camera
			Text.transform.rotation = Quaternion.LookRotation(Text.transform.position - Camera.transform.position);

			// Only perform keyboard input if we have authority
			if (Authority) {

				// Get direction and rotation from keyboard input
				float rotation = 0f;
				Vector3 direction = Vector3.zero;
				if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
				if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
				if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
				if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
				if (Input.GetKey(KeyCode.E)) rotation += 1f;
				if (Input.GetKey(KeyCode.Q)) rotation -= 1f;

				// Move and rotate the player
				transform.position += 4f * Time.deltaTime * direction;
				transform.Rotate(Vector3.up, rotation * Time.deltaTime * 180f);

			}

		}

		public void OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp) {
			// NetworkAuthority has changed our authority
			Run(() => {

				// Update the manually managed authority
				Authority = authority;

				// Notify game ui
				Game.OnPlayerUpdate(this);

				// If we just gained authority, initialize name and color to something random
				if (authority) {
					SetColor(new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 0xFF));
					SetName("Player #" + Random.Range(111, 999));
				}

			});
		}

		public override void OnNetworkRegister() {
			// Called when the player spawns
			// In this method authority hasn't been assigned yet
			Run(() => {
				// Request player information and notify game ui
				SendNetworkMessage(new MessageRequest());
				Game.OnPlayerUpdate(this);
			});
		}

		public override void OnNetworkUnregister() {
			// Called when the player despawns
			Run(() => Game.OnPlayerUpdate(this));
		}

		public void SetName(string name) {
			// Called by BCS_Game when name is updated
			if (Authority) {
				// Update info, send network message and notify game ui
				Name = name;
				Text.text = name;
				SendNetworkMessage(new MessageSetName() { Name = name });
				Game.OnPlayerUpdate(this);
			}
		}

		public void SetColor(Color32 color) {
			// Called by BCS_Game when color is updated
			if (Authority) {
				// Update info, send network message and notify game ui
				Color = color;
				Capsule.material.color = color;
				SendNetworkMessage(new MessageSetColor() { Color = color });
				Game.OnPlayerUpdate(this);
			}
		}

		public void SendChat(string message) {
			// Called by BCS_Game when a chat message is sent
			if (Authority) {
				// Send to everybody
				SendNetworkMessage(new MessageChat() { Message = message });
			}
		}

		public override void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) {

			// Every message writes the header first, read the header here
			Header header = reader.ReadEnum<Header>();

			if (header == Header.Request) {

				// Read the message
				MessageRequest message = new MessageRequest();
				message.Read(reader);

				// Respond to the request by sending name and color if we have authority
				Run(() => {
					if (Authority) {
						SendNetworkMessage(new MessageSetColor() { Color = Color });
						SendNetworkMessage(new MessageSetName() { Name = Name });
					}
				});

			} else if (header == Header.SetName) {

				// Read the message
				MessageSetName message = new MessageSetName();
				message.Read(reader);

				// Update name if this is a remote player
				Run(() => {
					if (!Authority) {
						Name = message.Name;
						Text.text = message.Name;
						Game.OnPlayerUpdate(this);
					}
				});

			} else if (header == Header.SetColor) {

				// Read the message
				MessageSetColor message = new MessageSetColor();
				message.Read(reader);

				// Update color if this is a remote player
				Run(() => {
					if (!Authority) {
						Color = message.Color;
						Capsule.material.color = message.Color;
						Game.OnPlayerUpdate(this);
					}
				});

			} else if (header == Header.Chat) {

				// Read the message
				MessageChat message = new MessageChat();
				message.Read(reader);

				// Notify game ui
				Run(() => {
					Game.OnPlayerChat(this, message.Message);
				});

			} else {

				// We never send any other message headers, so this should never occur
				Debug.LogError("Invalid message header: " + header);

			}

		}

		private enum Header : byte {
			Request = 0,
			SetName = 1,
			SetColor = 2,
			Chat = 3,
		}

		private struct MessageRequest : IMessage {
			
			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => 0;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public void Write(Writer writer) {
				writer.WriteEnum(Header.Request);
			}

			public void Read(Reader reader) { }

		}

		private struct MessageSetName : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => 0;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public string Name;

			public void Write(Writer writer) {
				writer.WriteEnum(Header.SetName);
				writer.Write(Name);
			}

			public void Read(Reader reader) {
				Name = reader.ReadString();
			}

		}

		private struct MessageSetColor : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => 0;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public Color32 Color;

			public void Write(Writer writer) {
				writer.WriteEnum(Header.SetColor);
				writer.Write(Color);
			}

			public void Read(Reader reader) {
				Color = reader.ReadColor32();
			}

		}

		private struct MessageChat : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => 0;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;

			public string Message;

			public void Write(Writer writer) {
				writer.WriteEnum(Header.Chat);
				writer.Write(Message);
			}

			public void Read(Reader reader) {
				Message = reader.ReadString();
			}

		}

	}

}
