using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.BCS {

	public class BCS_Game : MonoBehaviour {

		[Header("Canvas Components")]
		public Text LeftTitle;
		public Text LeftRemote;
		public Text LeftBind;
		public Text LeftError;
		public InputField RightNameInput;
		public Button RightNameSubmit;
		public InputField RightColorRed;
		public InputField RightColorGreen;
		public InputField RightColorBlue;
		public Button RightColorSubmit;
		public Button RightExit;
		public GameObject ChatCanvas;
		public InputField ChatInput;
		public Button ChatSubmit;
		public Text ChatTemplate;
		public GameObject PlayersCanvas;
		public Transform PlayersTemplate;

		[Header("Scene Objects")]
		public BCS_PlayerSpawner PlayerSpawner;

		// Local player we own
		private BCS_Player LocalPlayer = null;

		private void Reset() {
			// Resets the inspector fields to default values (editor only)
			LeftTitle = transform.Find("Menu/Left/Title").GetComponent<Text>();
			LeftRemote = transform.Find("Menu/Left/Remote").GetComponent<Text>();
			LeftBind = transform.Find("Menu/Left/Bind").GetComponent<Text>();
			LeftError = transform.Find("Menu/Left/Error").GetComponent<Text>();
			RightNameInput = transform.Find("Menu/Right/Name/Input").GetComponent<InputField>();
			RightNameSubmit = transform.Find("Menu/Right/Name/Submit").GetComponent<Button>();
			RightColorRed = transform.Find("Menu/Right/Color/Red").GetComponent<InputField>();
			RightColorGreen = transform.Find("Menu/Right/Color/Green").GetComponent<InputField>();
			RightColorBlue = transform.Find("Menu/Right/Color/Blue").GetComponent<InputField>();
			RightColorSubmit = transform.Find("Menu/Right/Color/Submit").GetComponent<Button>();
			RightExit = transform.Find("Menu/Right/Exit").GetComponent<Button>();
			ChatCanvas = transform.Find("Chat").gameObject;
			ChatInput = transform.Find("Chat/Input").GetComponent<InputField>();
			ChatSubmit = transform.Find("Chat/Submit").GetComponent<Button>();
			ChatTemplate = transform.Find("Chat/View/Viewport/Content/Template").GetComponent<Text>();
			PlayersCanvas = transform.Find("Players").gameObject;
			PlayersTemplate = transform.Find("Players/View/Viewport/Content/Template");
			PlayerSpawner = FindObjectOfType<BCS_PlayerSpawner>();
		}

		private void Awake() {
			
			// Initialize network manager
			NetworkManager.Initialize();

			// Register button listeners
			RightExit.onClick.AddListener(OnClickExit);
			RightNameSubmit.onClick.AddListener(OnClickName);
			RightColorSubmit.onClick.AddListener(OnClickColor);
			ChatSubmit.onClick.AddListener(OnClickChat);

			// Register peer disconnect event so we can check if we disconnected from the server
			if (BCS_Menu.Host != null) BCS_Menu.Host.PeerEvents.OnDisconnect += OnPeerDisconnect;

			// Register console events 
			if (BCS_Menu.Console != null) BCS_Menu.Console.OnInputText += OnConsoleInput;

		}

		private void Start() {

			// Clear chat and players
			UpdatePlayers();
			ClearChat();
			
			// Initialize UI
			if (BCS_Menu.Host == null) {
				// There is no NetworkHost, we launched this scene without going through BCS_Menu
				LeftTitle.text = "Super Netcode Server (No Network)";
				LeftRemote.text = "Remote IP: None";
				LeftBind.text = "Bind IP: None";
				LeftError.text = "No Network";
			} else if (BCS_Menu.IsServer) {
				// We are a server
				LeftTitle.text = "Super Netcode Server";
				LeftRemote.text = "Remote IP: None";
				LeftBind.text = "Bind IP: " + BCS_Menu.Host.GetBindAddress();
				LeftError.text = "";
			} else {
				// We are a client
				LeftTitle.text = "Super Netcode Client";
				LeftRemote.text = "Remote IP: " + BCS_Menu.Peer.Remote;
				LeftBind.text = "Bind IP: " + BCS_Menu.Host.GetBindAddress();
				LeftError.text = "";
			}

		}

		private void OnDestroy() {

			// Unregister peer & console events, this is important because BCS_Game is destroyed here
			// After BCS_Game is destroyed it shouldn't be receiving any events anymore
			if (BCS_Menu.Host != null) BCS_Menu.Host.PeerEvents.OnDisconnect -= OnPeerDisconnect;
			if (BCS_Menu.Console != null) BCS_Menu.Console.OnInputText -= OnConsoleInput;

			// Shutdown the network
			if (BCS_Menu.Host != null) BCS_Menu.Host.Shutdown();

		}

		public void OnPlayerUpdate(BCS_Player player) {
			// Called by BCS_Player when a player state changes
			if (this == null) return;
			if (player.NetworkAuthority.IsOwner) {
				LocalPlayer = player;
				RightNameInput.text = LocalPlayer.Name;
				RightColorRed.text = LocalPlayer.Color.r.ToString();
				RightColorGreen.text = LocalPlayer.Color.g.ToString();
				RightColorBlue.text = LocalPlayer.Color.b.ToString();
			}
			UpdatePlayers();
		}

		public void OnPlayerChat(BCS_Player player, string message) {
			// Called by BCS_Player when a player sends a chat message
			if (this == null) return;
			AddChat(player.Name + ": " + message);
		}

		private void ClearChat() {

			// Enable chat view
			ChatCanvas.SetActive(true);

			// Remove all chat messages
			ChatTemplate.gameObject.SetActive(false);
			foreach (Transform sibling in ChatTemplate.transform.parent) {
				if (sibling != ChatTemplate.transform) {
					Destroy(sibling.gameObject);
				}
			}

		}

		private void AddChat(string message) {
			// Clone chat template, activate it and modify text
			Text sibling = Instantiate(ChatTemplate, ChatTemplate.transform.parent);
			sibling.gameObject.SetActive(true);
			sibling.text = message;
		}

		private void UpdatePlayers() {

			// Enable player view
			PlayersCanvas.SetActive(true);

			// Remove all players
			PlayersTemplate.gameObject.SetActive(false);
			foreach (Transform sibling in PlayersTemplate.transform.parent) {
				if (sibling != PlayersTemplate.transform) {
					Destroy(sibling.gameObject);
				}
			}

			// Add all players
			foreach (NetworkPrefab prefab in PlayerSpawner.GetSpawnedPrefabs()) {
				BCS_Player player = prefab.GetComponent<BCS_Player>();
				if (player == null) continue;
				
				// Clone players template and activate it
				Transform sibling = Instantiate(PlayersTemplate, PlayersTemplate.parent);
				sibling.gameObject.SetActive(true);

				// Set player name
				Text name = sibling.Find("Name").GetComponent<Text>();
				name.text = player.Name;
				name.color = player.Color;
				if (BCS_Menu.IsServer && player.NetworkAuthority.Owner != null) {
					name.text += " (" + player.NetworkAuthority.Owner.Remote + ")";
				}

				// Add kick button listener
				Button kick = sibling.Find("Kick").GetComponent<Button>();
				kick.interactable = BCS_Menu.IsServer;
				kick.onClick.AddListener(() => OnClickKick(player));
				
			}

		}

		private void OnConsoleInput(string message) {
			// Handle console commands here
			if (message == "shutdown") {
				Application.Quit();
			}
		}

		private void OnClickExit() {
			// Shutdown the network and switch to the menu scene
			if (BCS_Menu.Host != null) BCS_Menu.Host.Shutdown();
			SceneManager.LoadScene("BCS_Menu");
			LeftError.text = "Add BCS_Menu scene to build settings";
		}

		private void OnClickName() {
			if (LocalPlayer == null) {
				LeftError.text = "No local player to change name";
			} else if (string.IsNullOrWhiteSpace(RightNameInput.text)) {
				LeftError.text = "Name must not be empty";
			} else {
				LocalPlayer.SetName(RightNameInput.text);
			}
		}

		private void OnClickColor() {
			bool validRed = byte.TryParse(RightColorRed.text, out byte red);
			bool validGreen = byte.TryParse(RightColorGreen.text, out byte green);
			bool validBlue = byte.TryParse(RightColorBlue.text, out byte blue);
			if (LocalPlayer == null) {
				LeftError.text = "No local player to change color";
			} else if (!validRed) {
				LeftError.text = "Invalid red value";
			} else if (!validGreen) {
				LeftError.text = "Invalid green value";
			} else if (!validBlue) {
				LeftError.text = "Invalid blue value";
			} else {
				Color32 color = new Color32(red, green, blue, 0xFF);
				LocalPlayer.SetColor(color);
			}
		}

		private void OnClickChat() {
			if (LocalPlayer != null) {
				OnPlayerChat(LocalPlayer, ChatInput.text);
				LocalPlayer.SendChat(ChatInput.text);
				ChatInput.text = "";
			}
		}

		private void OnClickKick(BCS_Player player) {
			if (BCS_Menu.IsServer) {
				// Disconnect the client and destroy the player
				player.NetworkAuthority.Owner?.Disconnect();
				Destroy(player.gameObject);
			}
		}

		private void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {
			NetworkManager.Run(() => {
				if (peer == BCS_Menu.Peer) {

					// We are a client and we disconnected from the server
					Debug.LogError("Disconnected from server: " + reason);
					LeftError.text = "Disconnected from server: " + reason;

					// Shutdown the network
					if (BCS_Menu.Host != null) {
						BCS_Menu.Host.Shutdown();
					}

					// You can switch to the menu scene here
					//SceneManager.LoadScene("BCS_Menu");

				}
			});
		}

	}

}
