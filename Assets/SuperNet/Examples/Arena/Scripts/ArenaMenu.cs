using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.Arena {

	public class ArenaMenu : MonoBehaviour {

		// UI objects
		public GameObject MenuCanvas;
		public Text MenuServersStatus;
		public GameObject MenuServersTemplate;
		public Button MenuBottomRefresh;
		public InputField MenuBottomName;
		public Button MenuBottomStart;
		public GameObject ErrorCanvas;
		public Button ErrorConfirm;
		public Text ErrorStatus;
		public GameObject LoadCanvas;
		public Text LoadStatus;

		// Resources
		private ArenaNetwork Network;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			MenuCanvas = transform.Find("Menu").gameObject;
			MenuServersStatus = transform.Find("Menu/Servers/Status").GetComponent<Text>();
			MenuServersTemplate = transform.Find("Menu/Servers/Table/Viewport/Content/Template").gameObject;
			MenuBottomRefresh = transform.Find("Menu/Bottom/Refresh").GetComponent<Button>();
			MenuBottomName = transform.Find("Menu/Bottom/Name").GetComponent<InputField>();
			MenuBottomStart = transform.Find("Menu/Bottom/Start").GetComponent<Button>();
			ErrorCanvas = transform.Find("Error").gameObject;
			ErrorConfirm = transform.Find("Error/Confirm").GetComponent<Button>();
			ErrorStatus = transform.Find("Error/Status").GetComponent<Text>();
			LoadCanvas = transform.Find("Load").gameObject;
			LoadStatus = transform.Find("Load/Status").GetComponent<Text>();
		}

		private void Awake() {
			
			// Register button listeners
			MenuBottomRefresh.onClick.AddListener(OnClickRefresh);
			MenuBottomStart.onClick.AddListener(OnClickStart);
			ErrorConfirm.onClick.AddListener(OnClickErrorConfirm);

			// Find network in the scene
			Network = FindObjectOfType<ArenaNetwork>();

			// Register network events
			Network.OnRelayRefresh += OnRelayRefresh;
			Network.OnRelayConnect += OnRelayConnect;
			Network.OnRelayDisconnect += OnRelayDisconnect;
			Network.OnServerConnect += OnServerConnect;
			Network.OnServerDisconnect += OnServerDisconnect;

		}

		private void Start() {
			try {

				// Clear server list and start connecting to relay
				ServerListDeleteAll("No connection");
				bool connected = Network.ConnectToRelay();
				if (connected) {
					Network.Refresh();
					OpenCanvasMenu();
				} else {
					OpenCanvasLoad("Connecting...");
				}

			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnDestroy() {
			// Unregister events, this is important because ArenaMenu is destroyed here
			// After ArenaMenu is destroyed it shouldn't be receiving any events anymore
			Network.OnRelayRefresh -= OnRelayRefresh;
			Network.OnRelayConnect -= OnRelayConnect;
			Network.OnRelayDisconnect -= OnRelayDisconnect;
			Network.OnServerConnect -= OnServerConnect;
			Network.OnServerDisconnect -= OnServerDisconnect;
		}

		private void OpenCanvasMenu() {
			// Open the main menu and close everything else
			MenuCanvas.SetActive(true);
			ErrorCanvas.SetActive(false);
			LoadCanvas.SetActive(false);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		public void OpenCanvasError(string error) {
			// Open error screen and close everything else
			MenuCanvas.SetActive(false);
			ErrorCanvas.SetActive(true);
			LoadCanvas.SetActive(false);
			ErrorStatus.text = error;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void OpenCanvasLoad(string status) {
			// Open loading screen and close everything else
			MenuCanvas.SetActive(false);
			ErrorCanvas.SetActive(false);
			LoadCanvas.SetActive(true);
			LoadStatus.text = status;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		public void ServerListDeleteAll(string status) {
			// Hide template, delete all copies and set status
			MenuServersTemplate.SetActive(false);
			foreach (Transform child in MenuServersTemplate.transform.parent) {
				if (child == MenuServersTemplate.transform) continue;
				Destroy(child.gameObject);
			}
			MenuServersStatus.text = status;
		}

		public void ServerListCreate(RelayListResponse message) {
			if (message.Servers.Count <= 0) {
				// No servers
				ServerListDeleteAll("No servers");
			} else {
				// Create server rows from template
				ServerListDeleteAll("");
				foreach (RelayListResponseEntry server in message.Servers) {
					GameObject instance = Instantiate(MenuServersTemplate, MenuServersTemplate.transform.parent);
					Button connect = instance.transform.Find("Connect").GetComponent<Button>();
					Text name = instance.transform.Find("Name").GetComponent<Text>();
					string serverName = string.IsNullOrWhiteSpace(server.Name) ? (server.AddressPublic ?? server.AddressRemote) : server.Name;
					name.text = serverName + " (Players: " + server.Players + ")";
					connect.onClick.AddListener(() => OnClickConnect(server));
					instance.SetActive(true);
				}
			}
		}

		private void OnClickRefresh() {
			// Request server list
			try {
				Network.Refresh();
			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnClickStart() {
			// Launch a new server
			try {
				Network.CreateServer(MenuBottomName.text);
				SceneManager.LoadScene("ArenaGame");
				OpenCanvasError("Add ArenaGame to build settings");
			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnClickConnect(RelayListResponseEntry server) {
			// Connect to a server
			try {
				Network.ConnectToServer(server);
				OpenCanvasLoad("Connecting...");
			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnClickErrorConfirm() {
			// Reinitialize the menu
			Start();
		}

		private void OnRelayRefresh(RelayListResponse response) {
			// Called by ArenaNetwork when we get a list of servers
			ServerListCreate(response);
		}

		private void OnRelayConnect() {
			// Called by ArenaNetwork when we finish connecting to the relay
			OpenCanvasMenu();
		}

		private void OnServerConnect() {
			// Called by ArenaNetwork when we finish connecting to the server
			try {
				SceneManager.LoadScene("ArenaGame");
				OpenCanvasError("Add ArenaGame to build settings");
			} catch (Exception exception) {
				Debug.LogException(exception);
				OpenCanvasError(exception.Message);
			}
		}

		private void OnRelayDisconnect(Exception exception) {
			// Called by ArenaNetwork when we disconnect from the relay
			Debug.LogException(exception);
			OpenCanvasError(exception.Message);
		}

		private void OnServerDisconnect(Exception exception) {
			// Called by ArenaNetwork when we disconnect from the server
			Debug.LogException(exception);
			OpenCanvasError(exception.Message);
		}

	}

}
