using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace SuperNet.Examples.Broadcast {

	// Empty message, no data is sent
	public struct BroadcastMessage : IWritable {
		public void Write(Writer writer) {}
	}

	public class Broadcast : MonoBehaviour {

		// UI Elements
		public Button SearchButton;
		public InputField SearchPort;
		public Text SearchTemplate;
		public Text SearchStatus;
		public InputField HostPort;
		public Button HostStart;
		public Button HostShutdown;
		public Text HostStatus;

		// Netcode
		public NetworkHost Host;

		public void Start() {

			// Register button listeners
			SearchButton.onClick.AddListener(OnSearchClick);
			HostStart.onClick.AddListener(OnHostStartClick);
			HostShutdown.onClick.AddListener(OnHostShutdownClick);

			// Hide server template
			SearchTemplate.gameObject.SetActive(false);

			// Initialize network manager
			NetworkManager.Initialize();

			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = 60;

			// Register host events
			Host.HostEvents.OnReceiveBroadcast += OnHostReceiveBroadcast;
			Host.HostEvents.OnReceiveUnconnected += OnHostReceiveUnconnected;
			Host.HostEvents.OnException += OnHostException;

		}

		private void AddServer(string server) {
			// Instantiate a new template, enable it and assign text and color
			Text text = Instantiate(SearchTemplate, SearchTemplate.transform.parent);
			text.gameObject.SetActive(true);
			text.text = server;
		}

		private void ClearServers() {
			foreach (Transform child in SearchTemplate.transform.parent) {
				if (child == SearchTemplate.transform) continue;
				Destroy(child.gameObject);
			}
		}

		private void OnSearchClick() {

			// Make sure search port exists
			if (SearchPort.text == "") {
				SearchStatus.text = "Search port is required.";
				SearchStatus.color = Color.red;
				return;
			}

			// Parse search port
			if (!int.TryParse(SearchPort.text, out int port) || port <= 0 || port >= 65536) {
				SearchStatus.text = "Search port is invalid.";
				SearchStatus.color = Color.red;
				return;
			}

			// Create a host if not created yet
			OnHostStartClick();

			// If host failed to start, update status and exit
			if (Host == null) {
				SearchStatus.text = "Host failed to start.";
				SearchStatus.color = Color.red;
				return;
			}

			// Send broadcast
			Host.SendBroadcast(port, new BroadcastMessage());

			// Clear servers
			ClearServers();

			// Update status
			SearchStatus.text = "Broadcast sent on port " + port + ".";
			SearchStatus.color = Color.green;

		}

		private void OnHostStartClick() {

			// Parse port
			bool portParsed = int.TryParse(HostPort.text, out int port);
			if (HostPort.text != "" && !portParsed || port < 0 || port >= 65536) {
				HostStatus.text = "Bad port: " + HostPort.text;
				HostStatus.color = Color.red;
				return;
			}

			// If host not yet started, create one
			if (!Host.Listening) {
				try {
					Host.HostConfiguration.Broadcast = true;
					Host.HostConfiguration.Port = port;
					Host.Startup(true);
					HostStatus.text = "Listening on port: " + Host.GetBindAddress().Port;
					HostStatus.color = Color.green;
				} catch (Exception exception) {
					Debug.LogException(exception);
					HostStatus.text = "Exception: " + exception.Message;
					HostStatus.color = Color.red;
				}
			}

		}

		private void OnHostShutdownClick() {

			// Dispose of the host
			Host.Dispose();

			// Update status
			HostStatus.text = "Status: Not listening";
			HostStatus.color = Color.white;

		}

		private void OnHostReceiveBroadcast(IPEndPoint remote, Reader message) {
			// Broadcast received, answer it with an unconnected message
			Host.SendUnconnected(remote, new BroadcastMessage());
		}

		private void OnHostReceiveUnconnected(IPEndPoint remote, Reader message) {
			NetworkManager.Run(() => {
				// Unconnected answer received, add to server list
				AddServer(remote.ToString());
			});
		}

		private void OnHostException(IPEndPoint remote, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Debug.LogException(exception);
		}

	}

}
