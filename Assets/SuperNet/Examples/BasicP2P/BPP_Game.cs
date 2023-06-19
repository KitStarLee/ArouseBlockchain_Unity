using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Components;
using SuperNet.Unity.Core;
using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.BPP {

	public class BPP_Game : MonoBehaviour {

		[Header("Canvas Components")]
		public Text LeftConnectStatus;
		public InputField LeftConnectInput;
		public Button LeftConnectSubmit;
		public Text MiddleTitle;
		public Text MiddlePublic;
		public Text MiddleBind;
		public Text MiddleError;
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
		public BPP_PlayerSpawner PlayerSpawner;

		// Resources
		private BPP_Player LocalPlayer = null;
		private Peer ConnectingPeer = null;

		private void Reset() {
			// Resets the inspector fields to default values (editor only)
			LeftConnectStatus = transform.Find("Menu/Left/Status").GetComponent<Text>();
			LeftConnectInput = transform.Find("Menu/Left/Connect/Input").GetComponent<InputField>();
			LeftConnectSubmit = transform.Find("Menu/Left/Connect/Submit").GetComponent<Button>();
			MiddleTitle = transform.Find("Menu/Middle/Title").GetComponent<Text>();
			MiddlePublic = transform.Find("Menu/Middle/Public").GetComponent<Text>();
			MiddleBind = transform.Find("Menu/Middle/Bind").GetComponent<Text>();
			MiddleError = transform.Find("Menu/Middle/Error").GetComponent<Text>();
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
			PlayerSpawner = FindObjectOfType<BPP_PlayerSpawner>();
		}

		private void Awake() {

			// Initialize network manager
			NetworkManager.Initialize();

			// Register button listeners
			LeftConnectSubmit.onClick.AddListener(OnClickConnect);
			RightExit.onClick.AddListener(OnClickExit);
			RightNameSubmit.onClick.AddListener(OnClickName);
			RightColorSubmit.onClick.AddListener(OnClickColor);
			ChatSubmit.onClick.AddListener(OnClickChat);

			// Register peer events so we can detect if connections are succesful
			if (BPP_Menu.Host != null) BPP_Menu.Host.PeerEvents.OnConnect += OnPeerConnect;
			if (BPP_Menu.Host != null) BPP_Menu.Host.PeerEvents.OnDisconnect += OnPeerDisconnect;

		}

		private void Start() {

			// Clear chat and players
			UpdatePlayers();
			ClearChat();
			
			// Initialize UI
			if (BPP_Menu.Host == null) {
				// There is no NetworkHost, we launched this scene without going through BPP_Menu
				MiddlePublic.text = "Public IP: ...";
				MiddleBind.text = "Bind IP: None";
				MiddleError.text = "No Network";
				LeftConnectStatus.text = "";
			} else {
				// We are a client
				MiddlePublic.text = "Public IP: ...";
				MiddleBind.text = "Bind IP: " + BPP_Menu.Host.GetBindAddress();
				MiddleError.text = "";
				LeftConnectStatus.text = "";
			}

			// Start obtaining Public IP
			StartCoroutine(GetPublicIP());

		}

		private void OnDestroy() {

			// Shutdown the network
			if (BPP_Menu.Host != null) BPP_Menu.Host.Shutdown();

			// Unregister peer events, this is important because BPP_Game is destroyed here
			// After BPP_Game is destroyed it shouldn't be receiving any events anymore
			if (BPP_Menu.Host != null) BPP_Menu.Host.PeerEvents.OnConnect -= OnPeerConnect;
			if (BPP_Menu.Host != null) BPP_Menu.Host.PeerEvents.OnDisconnect -= OnPeerDisconnect;

		}

		private void OnIPObtainComplete(IPAddress ip) {
			// Called when Public IP is obtained
			if (ip == null) {
				MiddlePublic.text = "Public IP: Unknown";
			} else if (BPP_Menu.Host == null) {
				MiddlePublic.text = "Public IP: " + ip;
			} else {
				MiddlePublic.text = "Public IP: " + ip + ":" + BPP_Menu.Host.GetBindAddress().Port;
			}
		}

		public void OnPlayerUpdate(BPP_Player player) {
			// Called by BPP_Player when a player state changes
			if (this == null) return;
			if (player.Authority) {
				LocalPlayer = player;
				RightNameInput.text = LocalPlayer.Name;
				RightColorRed.text = LocalPlayer.Color.r.ToString();
				RightColorGreen.text = LocalPlayer.Color.g.ToString();
				RightColorBlue.text = LocalPlayer.Color.b.ToString();
			}
			UpdatePlayers();
		}

		public void OnPlayerChat(BPP_Player player, string message) {
			// Called by BPP_Player when a player sends a chat message
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
				BPP_Player player = prefab.GetComponent<BPP_Player>();
				if (player == null) continue;

				// Clone players template and activate it
				Transform sibling = Instantiate(PlayersTemplate, PlayersTemplate.parent);
				sibling.gameObject.SetActive(true);

				// Set player name
				Text name = sibling.Find("Name").GetComponent<Text>();
				name.text = player.Name;
				name.color = player.Color;
				if (player.NetworkPrefab.Spawnee != null) {
					name.text += " (" + player.NetworkPrefab.Spawnee.Remote + ")";
				}

				// Add kick button listener
				Button kick = sibling.Find("Kick").GetComponent<Button>();
				kick.onClick.AddListener(() => OnClickKick(player));

			}

		}

		private void OnClickExit() {
			// Shutdown the network and switch to the menu scene
			if (BPP_Menu.Host != null) BPP_Menu.Host.Shutdown();
			SceneManager.LoadScene("BPP_Menu");
			MiddleError.text = "Add BPP_Menu scene to build settings";
		}

		private void OnClickName() {
			if (LocalPlayer == null) {
				MiddleError.text = "No local player to change name";
			} else if (string.IsNullOrWhiteSpace(RightNameInput.text)) {
				MiddleError.text = "Name must not be empty";
			} else {
				LocalPlayer.SetName(RightNameInput.text);
			}
		}

		private void OnClickColor() {
			bool validRed = byte.TryParse(RightColorRed.text, out byte red);
			bool validGreen = byte.TryParse(RightColorGreen.text, out byte green);
			bool validBlue = byte.TryParse(RightColorBlue.text, out byte blue);
			if (LocalPlayer == null) {
				MiddleError.text = "No local player to change color";
			} else if (!validRed) {
				MiddleError.text = "Invalid red value";
			} else if (!validGreen) {
				MiddleError.text = "Invalid green value";
			} else if (!validBlue) {
				MiddleError.text = "Invalid blue value";
			} else {
				Color32 color = new Color32(red, green, blue, 0xFF);
				LocalPlayer.SetColor(color);
			}
		}

		private void OnClickConnect() {
			try {
				if (BPP_Menu.Host == null) {
					LeftConnectStatus.color = Color.red;
					LeftConnectStatus.text = "No network";
				} else if (string.IsNullOrWhiteSpace(LeftConnectInput.text)) {
					LeftConnectStatus.color = Color.red;
					LeftConnectStatus.text = "No address";
				} else {
					IPEndPoint ip = IPResolver.Resolve(LeftConnectInput.text);
					ConnectingPeer = BPP_Menu.Host.Connect(ip, null, null, true);
					LeftConnectStatus.color = Color.white;
					LeftConnectStatus.text = "Connecting to " + ip;
					LeftConnectSubmit.interactable = false;
					LeftConnectInput.interactable = false; 
				}
			} catch (Exception exception) {
				Debug.LogException(exception);
				LeftConnectStatus.color = Color.red;
				LeftConnectStatus.text = exception.Message;
			}
		}

		private void OnClickChat() {
			if (LocalPlayer != null) {
				OnPlayerChat(LocalPlayer, ChatInput.text);
				LocalPlayer.SendChat(ChatInput.text);
				ChatInput.text = "";
			}
		}

		private void OnClickKick(BPP_Player player) {
			// Disconnect the peer that spawned this player
			player.NetworkPrefab.Spawnee?.Disconnect();
		}

		private void OnPeerConnect(Peer peer) {
			NetworkManager.Run(() => {
				if (ConnectingPeer == peer) {
					LeftConnectInput.text = "";
					LeftConnectStatus.color = Color.green;
					LeftConnectStatus.text = "Connected to " + peer.Remote;
					LeftConnectSubmit.interactable = true;
					LeftConnectInput.interactable = true;
					ConnectingPeer = null;
				}
			});
		}

		private void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {
			NetworkManager.Run(() => {
				if (ConnectingPeer == peer) {
					LeftConnectStatus.color = Color.red;
					LeftConnectStatus.text = "Failed to connect: " + reason;
					LeftConnectSubmit.interactable = true;
					LeftConnectInput.interactable = true;
					ConnectingPeer = null;
				}
			});
		}

		private static readonly string[] PublicIPServices = new string[] {
			"https://icanhazip.com",
			"https://ipinfo.io/ip",
			"https://bot.whatismyipaddress.com/",
			"https://api.ipify.org/",
			"https://checkip.amazonaws.com/",
			"https://wtfismyip.com/text",
		};

		private IEnumerator GetPublicIP() {
			foreach (string url in PublicIPServices) {
				Debug.Log("Obtaining public IP from " + url);
				using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url)) {
					UnityEngine.Networking.UnityWebRequestAsyncOperation request = www.SendWebRequest();
					yield return request;
					if (www.isDone && string.IsNullOrWhiteSpace(www.error)) {
						string ip = www.downloadHandler.text.Trim();
						if (IPAddress.TryParse(ip, out IPAddress parsed)) {
							OnIPObtainComplete(parsed);
							yield break;
						} else {
							Debug.LogWarning("Public IP '" + ip + "' from " + url + " is invalid.");
						}
					} else {
						Debug.LogWarning("Failed to get public IP from " + url + ": " + www.error);
					}
				}
			}
			OnIPObtainComplete(null);
			yield break;
		}

	}

}
