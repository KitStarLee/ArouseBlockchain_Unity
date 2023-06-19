using SuperNet.Unity.Core;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperNet.Examples.BPP {

	public class BPP_Menu : MonoBehaviour {

		// NetworkHost that persists across scenes
		public static NetworkHost Host { get; private set; } = null;

		[Header("Canvas")]
		public Button CanvasLaunch;
		public InputField CanvasPort;
		public Text CanvasError;

		[Header("Scene")]
		public NetworkHost SceneHost;

		private void Reset() {
			// Reset the inspector fields to default values (editor only)
			CanvasLaunch = transform.Find("Launch/Start").GetComponent<Button>();
			CanvasPort = transform.Find("Launch/Port/Input").GetComponent<InputField>();
			CanvasError = transform.Find("Launch/Error").GetComponent<Text>();
			SceneHost = FindObjectOfType<NetworkHost>();
		}

		private void Awake() {

			// If we go from BPP_Menu to BPP_Game and then back to BCS_Game
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

			// Initialize network manager
			NetworkManager.Initialize();

			// Destroy any old connections
			Host.Dispose();

			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = 60;

			// Register button events and clear error text field
			CanvasLaunch.onClick.AddListener(OnClickLaunch);
			CanvasError.text = "";

		}

		private void OnClickLaunch() {
			try {

				// Convert port input field to a number
				int.TryParse(CanvasPort.text, out int port);
				if (port < 0) throw new Exception("Port must be positive");
				if (port > 65536) throw new Exception("Port must be less than 65536");

				// Start the host
				SceneHost.Dispose();
				SceneHost.HostConfiguration.Port = port;
				SceneHost.Startup(true);

				// Load the game scene
				SceneManager.LoadScene("BPP_Game");
				CanvasError.text = "Add BPP_Game scene to build settings";

			} catch (Exception exception) {
				Debug.LogException(exception);
				CanvasError.text = exception.Message;
			}
		}

	}

}
