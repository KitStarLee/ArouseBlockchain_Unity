using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace SuperNet.Examples.Chat {

	public class ChatClient : MonoBehaviour {

		// UI Elements
		public Text MenuChatTemplate;
		public Text MenuStatus;
		public Text MenuPing;
		public InputField MenuAddress;
		public InputField MenuChatInput;
		public Button MenuConnect;
		public Button MenuDisconnect;
		public Button MenuChatSend;

		// Netcode
		public NetworkHost Host;
		private Peer Peer;

		public void Start() {

			// Register button listeners
			MenuConnect.onClick.AddListener(OnConnectClick);
			MenuDisconnect.onClick.AddListener(OnDisconnectClick);
			MenuChatSend.onClick.AddListener(OnChatSendClick);

			// Hide chat template
			MenuChatTemplate.gameObject.SetActive(false);

			// Initialize network manager
			NetworkManager.Initialize();

			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = 60;

			// Register peer events
			Host.PeerEvents.OnConnect += OnPeerConnect;
			Host.PeerEvents.OnDisconnect += OnPeerDisconnect;
			Host.PeerEvents.OnException += OnPeerException;
			Host.PeerEvents.OnReceive += OnPeerReceive;
			Host.PeerEvents.OnUpdateRTT += OnPeerUpdateRTT;

			// Register host events
			Host.HostEvents.OnException += OnHostException;
			Host.HostEvents.OnShutdown += OnHostShutdown;

		}

		private void AddChat(string message, Color color) {
			// Instantiate a new template, enable it and assign text and color
			Text text = Instantiate(MenuChatTemplate, MenuChatTemplate.transform.parent);
			text.gameObject.SetActive(true);
			text.text = message;
			text.color = color;
		}

		private void OnConnectClick() {

			// If already connected, do nothing
			if (Host.Listening) {
				AddChat("Cannot connect while already connected.", Color.red);
				return;
			}

			// Parse address
			IPEndPoint address = IPResolver.TryParse(MenuAddress.text);
			if (address == null) {
				AddChat("Bad server address '" + MenuAddress.text + "'.", Color.red);
				return;
			}

			// Start connecting
			try {
				Host.Startup(true);
				Peer = Host.Connect(address, null, null, true);
			} catch (Exception exception) {
				AddChat("Connect exception: " + exception.Message, Color.red);
				Debug.LogException(exception);
				return;
			}

			// Notify console and update status
			AddChat("Host created with bind address " + Host.GetBindAddress(), Color.white);
			AddChat("Connecting to " + address, Color.white);
			MenuStatus.text = "Connecting to " + address;

		}

		private void OnDisconnectClick() {

			// If not connected, do nothing
			if (!Host.Listening) {
				AddChat("Cannot disconnect: Not connected.", Color.red);
				return;
			}

			// Disconnect
			Peer?.Disconnect();
			Host.Shutdown();
			Peer = null;

			// Notify console and update status
			AddChat("Disconnecting.", Color.white);
			MenuStatus.text = "Disconnecting.";

		}

		private void OnChatSendClick() {

			// If not connected, do nothing
			if (Peer == null || !Peer.Connected) {
				AddChat("Cannot send: Not connected.", Color.red);
				return;
			}

			// Send chat message to server
			ClientMessageChat message = new ClientMessageChat();
			message.Message = MenuChatInput.text;
			Peer.Send(message);

			// Clear the UI input
			MenuChatInput.text = "";

		}

		private void OnPeerConnect(Peer peer) {
			NetworkManager.Run(() => {
				// Notify console and update status
				AddChat("Connected.", Color.white);
				MenuStatus.text = "Connected to " + peer.Remote;
			});
		}

		private void OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {
			NetworkManager.Run(() => {

				// Dispose resources
				Peer.Dispose();
				Host.Dispose();
				Peer = null;

				// Notify console and update status
				AddChat("Disconnected: " + reason, Color.white);
				MenuStatus.text = "Disconnected: " + reason;
				MenuPing.text = "";

				// Display any exceptions
				if (exception != null) {
					Debug.LogException(exception);
					AddChat("Disconnect Exception:" + exception.ToString(), Color.red);
				}

			});
		}

		private void OnPeerUpdateRTT(Peer peer, ushort rtt) {
			NetworkManager.Run(() => {
				MenuPing.text = "Ping: " + rtt;
			});
		}

		private void OnPeerReceive(Peer peer, Reader message, MessageReceived info) {

			// Convert channel to message type
			ServerMessageType type = (ServerMessageType)info.Channel;

			// Process message based on type
			switch (type) {
				case ServerMessageType.Join:
					OnPeerReceiveJoin(peer, message, info);
					break;
				case ServerMessageType.Leave:
					OnPeerReceiveLeave(peer, message, info);
					break;
				case ServerMessageType.Chat:
					OnPeerReceiveChat(peer, message, info);
					break;
				default:
					NetworkManager.Run(() => {
						AddChat("Server sent an unknown message type " + info.Channel + ".", Color.red);
					});
					break;
			}

		}

		private void OnPeerReceiveJoin(Peer peer, Reader reader, MessageReceived info) {

			// Read the message
			ServerMessageJoin message = new ServerMessageJoin();
			message.Read(reader);

			// Notify console
			NetworkManager.Run(() => {
				AddChat(message.Name + " joined.", Color.white);
			});

		}

		private void OnPeerReceiveLeave(Peer peer, Reader reader, MessageReceived info) {

			// Read the message
			ServerMessageLeave message = new ServerMessageLeave();
			message.Read(reader);

			// Notify console
			NetworkManager.Run(() => {
				AddChat(message.Name + " disconnected.", Color.white);
			});

		}

		private void OnPeerReceiveChat(Peer peer, Reader reader, MessageReceived info) {

			// Read the message
			ServerMessageChat message = new ServerMessageChat();
			message.Read(reader);

			// Notify console
			NetworkManager.Run(() => {
				AddChat(message.Name + ": " + message.Message, Color.white);
			});

		}

		private void OnPeerException(Peer peer, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Debug.LogException(exception);
			NetworkManager.Run(() => AddChat("Peer Exception:" + exception.ToString(), Color.red));
		}

		private void OnHostException(IPEndPoint remote, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Debug.LogException(exception);
			NetworkManager.Run(() => AddChat("Host Exception:" + exception.ToString(), Color.red));
		}

		private void OnHostShutdown() {
			NetworkManager.Run(() => AddChat("Host was shut down.", Color.red));
		}

	}

}
