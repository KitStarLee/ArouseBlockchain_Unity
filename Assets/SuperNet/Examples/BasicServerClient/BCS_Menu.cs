using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.BCS {

	public class BCS_Menu : MonoBehaviour {

		// Configuration loaded from the file
		public static BCS_Config Config { get; private set; } = new BCS_Config();

		// Console that persists across scenes
		public static BCS_Console Console { get; private set; } = null;

		// NetworkHost that persists across scenes
		public static NetworkHost Host { get; private set; } = null;

		// Connection to the server or null if we are the server
		public static Peer Peer { get; private set; } = null;

		// True if we are a server, false if we are a client
		public static bool IsServer => Peer == null;

		[Header("Server")]
		public Button ServerStart;
		public InputField ServerPort;
		public Text ServerError;

		[Header("Client")]
		public Button ClientStart;
		public InputField ClientAddress;
		public Text ClientError;

		[Header("Scene")]
		public NetworkHost SceneHost;
		public BCS_Console SceneConsole;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			ServerStart = transform.Find("Server/Start").GetComponent<Button>();
			ServerPort = transform.Find("Server/Port/Input").GetComponent<InputField>();
			ServerError = transform.Find("Server/Error").GetComponent<Text>();
			ClientStart = transform.Find("Client/Start").GetComponent<Button>();
			ClientAddress = transform.Find("Client/Address/Input").GetComponent<InputField>();
			ClientError = transform.Find("Client/Error").GetComponent<Text>();
			SceneHost = FindObjectOfType<NetworkHost>();
			SceneConsole = FindObjectOfType<BCS_Console>();
		}

		private void Awake() {

			// If we go from BCS_Menu to BCS_Game and then back to BCS_Menu
			// We will have two NetworkHosts because NetworkHost is set to persist across scenes
			// Here we detect this and use the old NetworkHost if available
			if (Host == null) {
				// No previous NetworkHost found, assign this one
				Host = SceneHost;
			} else {
				// Previous host found, destroy this one and reassign
				Destroy(SceneHost.gameObject);
				SceneHost = Host;
			}

			// If we go from BCS_Menu to BCS_Game and then back to BCS_Menu
			// We will have two BCS_Console objects because BCS_Console is set to persist across scenes
			// Here we detect this and use the old BCS_Console if available
			if (Console == null) {
				// No previous BCS_Console found, assign this one
				Console = SceneConsole;
			} else {
				// Previous BCS_Console found, destroy this one and reassign
				Destroy(SceneConsole.gameObject);
				SceneConsole = Console;
			}

			// Initialize network manager
			NetworkManager.Initialize();

			// Destroy any old connections
			Host.Dispose();
			Peer = null;

			// Read configuration from command line arguments
			Config = BCS_Config.Initialize();

			// Initialize console if requested
			if (Config.ConsoleEnabled) {
				Console.Initialize();
				if (Config.ConsoleTitle != "") Console.SetTitle(Config.ConsoleTitle);
			}

			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = Config.TargetFrameRate;

			// Register network events so we can switch to game scene when client connects
			Host.PeerEvents.OnConnect += OnPeerConnect;
			Host.PeerEvents.OnDisconnect += OnPeerDisconnect;

			// Register button events and clear all error text fields
			ServerStart.onClick.AddListener(OnServerStart);
			ClientStart.onClick.AddListener(OnClientStart);
			ClientError.text = "";
			ServerError.text = "";

			// Start server if requested
			if (Config.ServerDedicated) {
				ServerPort.text = Config.ServerPort.ToString();
				OnServerStart();
			}

		}

		private void OnDestroy() {
			// Unregister events, this is important because BCS_Menu is destroyed here
			// After BCS_Menu is destroyed it shouldn't be receiving any events anymore
			Host.PeerEvents.OnConnect -= OnPeerConnect;
			Host.PeerEvents.OnDisconnect -= OnPeerDisconnect;
		}

		private void OnServerStart() {
			try {

				// Convert port input field to a number
				int.TryParse(ServerPort.text, out int port);
				if (port < 0) throw new Exception("Port must be positive");
				if (port > 65536) throw new Exception("Port must be less than 65536");

				// Start the host
				SceneHost.Dispose();
				SceneHost.MaxConnections = Config.ServerMaxConnections;
				SceneHost.HostConfiguration.Port = port;
				SceneHost.Startup(true);

				// Load the game scene
				SceneManager.LoadScene("BCS_Game");
				ServerError.text = "Add BCS_Game scene to build settings";

			} catch (Exception exception) {
				Debug.LogException(exception);
				ServerError.text = exception.Message;
			}
		}

		private void OnClientStart() {
			try {

				// Convert address input field to IPEndPoint
				if (ClientAddress.text == "") throw new Exception("Address is empty");
				IPEndPoint address = IPResolver.Resolve(ClientAddress.text);

				// Start the host on a random port
				SceneHost.Dispose();
				SceneHost.MaxConnections = 1;
				SceneHost.HostConfiguration.Port = 0;
				SceneHost.Startup(true);

				// Start connecting
				Peer = SceneHost.Connect(address);
				ClientError.text = "Connecting...";

			} catch (Exception exception) {
				Debug.LogException(exception);
				ClientError.text = exception.Message;
			}
		}

		private void OnPeerConnect(Peer peer) {
			// OnPeerConnect is called in a separate thread
			// We use NetworkManager.Run to move it into the main unity thread
			NetworkManager.Run(() => {
				if (peer == Peer) {
					// We are a client and we successfully connected to the server
					// Load game scene
					SceneManager.LoadScene("BCS_Game");
					ClientError.text = "Add BCS_Game scene to build settings";
				}
			});
		}

		private void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {
			// OnPeerDisconnect is called in a separate thread
			// We use NetworkManager.Run to move it into the main unity thread
			NetworkManager.Run(() => {
				if (peer == Peer) {
					// We are a client and we failed to connect to the server
					// Display error and shutodown host
					ClientError.text = "Failed to connect: " + reason;
					SceneHost.Dispose();
					Peer = null;
				}
			});
		}

	}

}
