using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace SuperNet.Examples.Chat {

	public class ChatServer : MonoBehaviour {

		// UI Elements
		public Text MenuChatTemplate;
		public Text MenuStatus;
		public Text MenuPeerCount;
		public InputField MenuServerPort;
		public Button MenuStart;
		public Button MenuShutdown;

		// Netcode
		public NetworkHost Host;
		private int Clients;

		public void Start() {

			// Register button listeners
			MenuStart.onClick.AddListener(OnStartClick);
			MenuShutdown.onClick.AddListener(OnShutdownClick);

			// Hide chat template
			MenuChatTemplate.gameObject.SetActive(false);

			// Initialize network manager
			NetworkManager.Initialize();
			
			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = 60;

			// Register host events
			Host.HostEvents.OnException += OnHostException;

			// Register peer events
			Host.PeerEvents.OnConnect += OnPeerConnect;
			Host.PeerEvents.OnDisconnect += OnPeerDisconnect;
			Host.PeerEvents.OnException += OnPeerException;
			Host.PeerEvents.OnReceive += OnPeerReceive;

		}

		private void AddChat(string message, Color color) {
			// Instantiate a new template, enable it and assign text and color
			Text text = Instantiate(MenuChatTemplate, MenuChatTemplate.transform.parent);
			text.gameObject.SetActive(true);
			text.text = message;
			text.color = color;
		}

		private void OnStartClick() {

			// If already listening, do nothing
			if (Host.Listening) {
				AddChat("Cannot launch while already listening.", Color.red);
				return;
			}

			// Parse port
			bool portParsed = int.TryParse(MenuServerPort.text, out int port);
			if (MenuServerPort.text != "" && !portParsed || port < 0 || port >= 65536) {
				AddChat("Bad server port '" + MenuServerPort.text + "'.", Color.red);
				return;
			}

			// Start listening
			try {
				Host.HostConfiguration.Port = port;
				Host.Startup(true);
				Clients = 0;
			} catch (Exception exception) {
				AddChat("Launch exception: " + exception.Message, Color.red);
				Debug.LogException(exception);
				return;
			}

			// Notify console and update status
			AddChat("Server started listening on " + Host.GetBindAddress(), Color.white);
			MenuStatus.text = "Listening on " + Host.GetBindAddress();
			MenuPeerCount.text = "Clients: " + Clients;

		}

		private void OnShutdownClick() {

			// If not listening, do nothing
			if (!Host.Listening) {
				AddChat("Server is already not listening.", Color.red);
				return;
			}

			// Start shutting down
			Host.Shutdown();

			// Notify console and update status
			AddChat("Shutting down server.", Color.white);
			MenuStatus.text = "Shutting down.";

		}

		private void OnHostException(IPEndPoint remote, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Debug.LogException(exception);
			NetworkManager.Run(() => AddChat("Server exception: " + exception.ToString(), Color.red));
		}

		private void OnPeerConnect(Peer peer) {

			// Send join message to all clients
			ServerMessageJoin join = new ServerMessageJoin();
			join.Name = peer.Remote.ToString();
			Host.SendAll(join, peer);

			// Notify console and update status
			NetworkManager.Run(() => {
				Clients++;
				AddChat(peer.Remote + " connected.", Color.white);
				MenuPeerCount.text = "Clients: " + Clients;
			});

		}

		private void OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {

			// Send leave message to all clients
			ServerMessageLeave leave = new ServerMessageLeave();
			leave.Name = peer.Remote.ToString();
			Host.SendAll(leave, peer);

			NetworkManager.Run(() => {

				// Notify console and update status
				Clients--;
				AddChat(peer.Remote + " disconnected: " + reason, Color.white);
				MenuPeerCount.text = "Clients: " + Clients;

				// Display any exceptions
				if (exception != null) {
					Debug.LogException(exception);
					AddChat("Disconnect Exception:" + exception.ToString(), Color.red);
				}

			});

		}

		private void OnPeerReceive(Peer peer, Reader message, MessageReceived info) {

			// Convert channel to message type
			ClientMessageType type = (ClientMessageType)info.Channel;

			// Process message based on type
			switch (type) {
				case ClientMessageType.Chat:
					OnPeerReceiveChat(peer, message, info);
					break;
				default:
					NetworkManager.Run(() => {
						AddChat(peer.Remote + " sent an unknown message type " + info.Channel + ".", Color.red);
					});
					break;
			}

		}

		private void OnPeerReceiveChat(Peer peer, Reader reader, MessageReceived info) {

			// Read the message
			ClientMessageChat message = new ClientMessageChat();
			message.Read(reader);

			// Send message to all clients including the one who sent it
			ServerMessageChat chat = new ServerMessageChat();
			chat.Name = peer.Remote.ToString();
			chat.Message = message.Message;
			Host.SendAll(chat);

			// Add message to console
			NetworkManager.Run(() => {
				AddChat(peer.Remote + ": " + message.Message, Color.white);
			});

		}

		private void OnPeerException(Peer peer, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Debug.LogException(exception);
			NetworkManager.Run(() => AddChat("Client " + peer.Remote + " exception: " + exception.ToString(), Color.red));
		}

	}

}
