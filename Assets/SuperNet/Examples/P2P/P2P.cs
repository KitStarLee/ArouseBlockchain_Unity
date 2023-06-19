using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace SuperNet.Examples.P2P {

	public class P2P : MonoBehaviour {

		// UI Elements
		public InputField HostPort;
		public Button HostCreate;
		public Button HostShutdown;
		public Text HostStatus;
		public Button IPObtain;
		public Text IPStatus;
		public InputField PunchAddress;
		public Button PunchConnect;
		public Text PunchStatus;
		public Text PunchPing;

		// Netcode
		public NetworkHost Host;
		private Peer Peer;

		public void Start() {
			
			// Register button click events
			HostCreate.onClick.AddListener(OnHostCreateClick);
			HostShutdown.onClick.AddListener(OnHostShutdownClick);
			IPObtain.onClick.AddListener(OnIPObtainClick);
			PunchConnect.onClick.AddListener(OnPunchConnectClick);

			// Reset everything
			OnHostShutdownClick();

			// Initialize network manager
			NetworkManager.Initialize();

			// Reduce the frame rate to use less CPU
			Application.targetFrameRate = 60;

		}

		private void OnHostCreateClick() {

			// Parse port
			bool portParsed = int.TryParse(HostPort.text, out int port);
			if (HostPort.text != "" && !portParsed || port < 0 || port >= 65536) {
				HostStatus.text = "Bad port: " + HostPort.text;
				HostStatus.color = Color.red;
				return;
			}

			// Create host
			try {
				Host.Dispose();
				Host.HostConfiguration.Port = port;
				Host.Startup(true);
				HostStatus.text = "Listening on port: " + Host.GetBindAddress().Port;
				HostStatus.color = Color.green;

				Debug.Log(Host.GetBindAddress().Address + " : " + Host.GetBindAddress().Port );

			} catch (Exception exception) {
				Debug.LogException(exception);
				HostStatus.text = "Exception: " + exception.Message;
				HostStatus.color = Color.red;
			}

			// Enable next stage
			HostCreate.interactable = false;
			IPObtain.interactable = true;

		}

		private void OnHostShutdownClick() {

			// Dispose of the host
			Host.Dispose();
			Peer = null;

			// Disable all stages
			HostCreate.interactable = true;
			IPObtain.interactable = false;
			PunchConnect.interactable = false;
			PunchAddress.interactable = false;

			// Reset all statuses
			HostStatus.text = "Status: Not listening";
			HostStatus.color = Color.white;
			IPStatus.text = "IP: Unknown";
			IPStatus.color = Color.white;
			PunchStatus.text = "Status: Not connected";
			PunchStatus.color = Color.white;
			PunchPing.text = "Ping: Unknown";

		}

		private void OnIPObtainClick() {

			// If host not running, reset
			if (!Host.Listening) {
				OnHostShutdownClick();
				return;
			}

			// Update status
			IPStatus.text = "Obtaining IP...";
			IPStatus.color = Color.white;

			// Make sure we can only click obtain once
			IPObtain.interactable = false;

			// Start obtaining IP
			StartCoroutine(GetPublicIP());

		}

		private void OnIPObtainComplete(IPAddress ip) {

			// If host not running, do nothing
			if (!Host.Listening) return;

			// If no ip obtained, display error
			if (ip == null) {
				IPStatus.text = "Failed to obtain IP";
				IPStatus.color = Color.red;
				IPObtain.interactable = true;
				return;
			}

			// Display obtained IP
			IPStatus.text = ip.ToString() + ":" + Host.GetBindAddress().Port.ToString();
			IPStatus.color = Color.green;

			// Enable next stage
			IPObtain.interactable = false;
			PunchConnect.interactable = true;
			PunchAddress.interactable = true;

		}

		private void OnPunchConnectClick() {

			// If host not running or already connecting, reset
			if (!Host.Listening || Peer != null) {
				OnHostShutdownClick();
				return;
			}

			// Parse address
			IPEndPoint address = IPResolver.TryParse(PunchAddress.text);
			if (address == null) {
				PunchStatus.text = "Bad address: " + PunchAddress.text;
				PunchStatus.color = Color.red;
				return;
			}

			// Register peer events
			PeerEvents events = new PeerEvents();
			events.OnConnect += OnPeerConnect;
			events.OnDisconnect += OnPeerDisconnect;
			events.OnUpdateRTT += OnPeerUpdateRTT;

			// Start connecting
			Host.PeerConfiguration.ConnectAttempts = 40;
			Host.PeerConfiguration.ConnectDelay = 200;
			Peer = Host.Connect(address, events, null, true);

			// Update status
			PunchStatus.text = "Connecting to: " + address;
			PunchStatus.color = Color.white;

			// Make sure we can't connect again while connecting
			PunchConnect.interactable = false;
			PunchAddress.interactable = false;

		}

		private void OnPeerConnect(Peer peer) {
			NetworkManager.Run(() => {

				// Succesfully connected, update status
				PunchStatus.text = "Connected to: " + peer.Remote;
				PunchStatus.color = Color.green;

			});
		}

		private void OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {
			NetworkManager.Run(() => {

				// Update status
				PunchStatus.text = "Disconnected: " + reason;
				PunchStatus.color = Color.red;
				Peer = null;

				// Allow connecting again
				PunchConnect.interactable = true;
				PunchAddress.interactable = true;

			});
		}

		private void OnPeerUpdateRTT(Peer peer, ushort rtt) {
			NetworkManager.Run(() => {

				// Update ping display text in the Unity thread
				PunchPing.text = "Ping: " + rtt;

			});
		}

		private static readonly string[] PublicIPServices = new string[] {
			"https://ipinfo.io/ip",
            "http://120.26.66.97:43023/Pip",

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
