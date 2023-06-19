using ArouseBlockchain.Common;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using SuperNet.Unity.Core;
using System;
using System.Collections;
using System.Net;
using UnityEngine;

namespace SuperNet.Examples.Arena {

	[RequireComponent(typeof(NetworkHost))]
	public class ArenaNetwork : MonoBehaviour {

		// Relay running on "superversus.com:44015" should not be used in production
		// It is meant to be for testing only
		private const string RelayIP = "superversus.com:44015";

		// Singleton instance
		private static ArenaNetwork Instance = null;

		// Relay
		public event Action OnRelayConnect;
		public event Action<Exception> OnRelayDisconnect;
		public event Action<RelayListResponse> OnRelayRefresh;
		public bool ConnectedToRelay => PeerRelay?.Connected ?? false;

		// Server
		public event Action OnServerConnect;
		public event Action<Exception> OnServerDisconnect;
		public bool IsServer => ServerAdded;
		public Peer Server => PeerClient;
		
		// Resources
		private Peer PeerRelay;
		private Peer PeerClient;
		private NetworkHost Host;
		private IPAddress PublicIP;
		private RelayListResponseEntry Entry;
		private bool ServerAdded;

		private void Awake() {

			// Initialize
			PeerRelay = null;
			PeerClient = null;
			Host = GetComponent<NetworkHost>();
			Entry = new RelayListResponseEntry();
			ServerAdded = false;
			PublicIP = null;

			// Destroy this instance if another instance has already been initialized
			if (Instance == null) {
				Instance = this;
			} else {
				Destroy(gameObject);
				return;
			}

			// Get public IP in a coroutine
			StartCoroutine(GetPublicIP());

		}

		private void Start() {
			// Register network events so we can detect connect and disconnect events
			Host.PeerEvents.OnConnect += OnPeerConnect;
			Host.PeerEvents.OnDisconnect += OnPeerDisconnect;
		}

		private void OnDestroy() {
			// Unregister events, this is important because ArenaNetwork is destroyed here
			// After ArenaNetwork is destroyed it shouldn't be receiving any events anymore
			Host.PeerEvents.OnConnect -= OnPeerConnect;
			Host.PeerEvents.OnDisconnect -= OnPeerDisconnect;
		}

		public bool ConnectToRelay() {

			// Make sure host is running
			Host.HostConfiguration.Port = 0;
			Host.Startup(true);

			// If already connected to relay, return true
			if (PeerRelay != null && !PeerRelay.Disposed) {
				return true;
			}

			// Start connecting to relay
			IPEndPoint address = IPResolver.Resolve(RelayIP);
			PeerEvents events = new PeerEvents();
			events.OnReceive += OnPeerRelayReceive;
			events.OnDisconnect += OnPeerDisconnect;
			events.OnConnect += OnPeerConnect;
			PeerRelay = Host.Connect(address, events, null, true);
			return false;

		}

		public void Shutdown() {
			// Shutdown all connections
			ServerAdded = false;
			PeerRelay?.Send(new RelayServerRemove());
			PeerClient?.Disconnect();
			PeerClient = null;
			foreach (Peer peer in Host.GetPeers()) {
				if (peer != PeerRelay) {
					peer.Disconnect();
				}
			}
		}

		public void Refresh() {
			// Request server list
			if (PeerRelay == null || !PeerRelay.Connected) {
				throw new Exception("Not connected to relay");
			} else {
				PeerRelay.Send(new RelayListRequest());
			}
		}

		public void CreateServer(string serverName) {
			// Create or update server
			if (PeerRelay == null || !PeerRelay.Connected) {
				throw new Exception("Not connected to relay");
			} else if (PeerClient != null) {
				throw new Exception("Already connected to a server");
			} else {
				string port = Host.GetBindAddress().Port.ToString();
				IPEndPoint local = Host.GetLocalAddress();
				Entry.AddressLocal = local.ToString();
				Entry.AddressRemote = PeerRelay.Remote.ToString();
				Entry.AddressPublic = PublicIP == null ? null : (PublicIP.ToString() + ":" + port);
				Entry.Name = serverName;
				Entry.Players = Host.GetPeers().Count;
				PeerRelay.Send(new RelayServerUpdate() { Entry = Entry });
				ServerAdded = true;

                Debug.Log("AAA-1!!!! Connected to " + Entry.Name + "\r\n" + Entry.AddressLocal + "\r\n" + Entry.AddressRemote + "\r\n" + Entry.AddressPublic);
            }
		}

        /// <summary
		///链接去一个Peer
        /// </summary>
        /// <param name="server"></param>
        /// <exception cref="Exception"></exception>

        public void ConnectToServer(RelayListResponseEntry server) {
			// Connect to the chosen server
			if (PeerRelay == null || !PeerRelay.Connected) {
				throw new Exception("Not connected to relay");
			} else if (PeerClient != null) {
				throw new Exception("Already connected to a server");
			} else {
				IPEndPoint address;
				IPEndPoint address_local = server.AddressLocal == null ? null : IPResolver.Resolve(server.AddressLocal);
				IPEndPoint address_remote = server.AddressRemote == null ? null : IPResolver.Resolve(server.AddressRemote);
				IPEndPoint address_public = server.AddressPublic == null ? null : IPResolver.Resolve(server.AddressPublic);
                Debug.Log("AAA0!!!! Connected to " + address_local.Address + "\r\n" + address_remote.Address+ "\r\n" + address_public.Address);
                if (PublicIP != null && PublicIP.Equals(address_public?.Address)) {
					address = address_local ?? address_remote ?? throw new Exception("No local address");

                    Debug.Log("AAA1!!!! Connected to " + address);

				} else {
					address = address_public ?? address_remote ?? throw new Exception("No public address");

                    Debug.Log("AAA2!!!! Connected to " + address);
                }
				PeerClient = Host.Connect(address, null, null, true);
				PeerRelay.Send(new RelayServerRemove());
				PeerRelay.Send(new RelayConnect() { Address = address.ToString() });
				ServerAdded = false;
			}
		}

		private void OnPeerConnect(Peer peer) {
            Debug.Log("[P2PNetWork :][Arena] Connected to " + peer.Remote + ".");
			NetworkManager.Run(() => {

				if (peer == PeerRelay) {
					// We just finished connecting to the relay
					// Unregister the peer, refresh the list of servers, and notify the menu
					// We unregister the peer because it isn't used in the component system
					NetworkManager.Unregister(peer);
					peer.Send(new RelayListRequest());
					OnRelayConnect?.Invoke();
					return;
				}

				// Register the peer because it is used to syncronize components for the game
				// This is done automatically if AutoRegister is true in NetworkHost
				NetworkManager.Register(peer);

				if (peer == PeerClient) {
					// We are a client and we just finished connecting to a server, notify the menu
					OnServerConnect?.Invoke();
					return;
				}

				if (ServerAdded) {
					// We are a server and a new client just connected
					// Update the server entry on the relay
					Entry.Players = Host.GetPeers().Count;
					PeerRelay?.Send(new RelayServerUpdate() { Entry = Entry });
				}

			});
		}

		private void OnPeerDisconnect(Peer peer, Reader reader, DisconnectReason reason, Exception exception) {
			Debug.Log("[Arena] Disconnected from " + peer.Remote + ": " + reason);
			NetworkManager.Run(() => {

				// Unregister the peer because it is no longer active
				NetworkManager.Unregister(peer);

				if (peer == PeerRelay) {
					// We just disconnected from the relay, notify the menu
					PeerRelay = null;
					if (exception == null) {
						OnRelayDisconnect?.Invoke(new Exception(reason.ToString()));
					} else {
						OnRelayDisconnect?.Invoke(exception);
					}
					return;
				}

				if (peer == PeerClient) {
					// We are a client and we just disconnected from the server, notify the menu
					PeerClient = null;
					if (exception == null) {
						OnServerDisconnect?.Invoke(new Exception(reason.ToString()));
					} else {
						OnServerDisconnect?.Invoke(exception);
					}
				}

				if (ServerAdded) {
					// We are a server and a client just disconnected
					// Update the server entry on the relay
					Entry.Players = Host.GetPeers().Count;
					PeerRelay?.Send(new RelayServerUpdate() { Entry = Entry });
				}

			});
		}

		private void OnPeerRelayReceive(Peer peer, Reader message, MessageReceived info) {
			// A new message received from the relay
			RelayMessageType type = (RelayMessageType)info.Channel;
			switch (type) {
				case RelayMessageType.ListResponse:
					OnRelayReceiveList(peer, message);
					break;
				case RelayMessageType.Connect:
					OnRelayReceiveConnect(message);
					break;
			}
		}

		private void OnRelayReceiveList(Peer peer, Reader reader) {
			// We received a list of servers from the relay, notify the menu
			RelayListResponse message = new RelayListResponse();
			message.Read(reader);
			NetworkManager.Run(() => {
				OnRelayRefresh?.Invoke(message);
			});
		}

		private void OnRelayReceiveConnect(Reader reader) {
			// We received a P2P connection request from the relay
			// Start connecting to the received address
			RelayConnect message = new RelayConnect();
			message.Read(reader);
			IPEndPoint address = IPResolver.TryParse(message.Address);
			if (address == null) {
				Debug.LogError("[Arena] Invalid address from relay: " + message.Address);
			} else {
				Host.Connect(address);
			}
		}

		private void OnIPObtainComplete(IPAddress ip) {
			// GetPublicIP Coroutine completed and an IP Address was found
			// Update server entry if needed
			if (ip == null) {
				Debug.LogError("[Arena] Failed to get public IP. No internet connection?");
			} else {
				Debug.Log("AAA-2[Arena] Public IP: " + ip.ToString());
				PublicIP = ip;
				if (ServerAdded) {
					CreateServer(Entry.Name);
				}
			}
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

                        Debug.Log("Request Public IP '" + ip + "' from " + url + ", Conent : " + ip);
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
