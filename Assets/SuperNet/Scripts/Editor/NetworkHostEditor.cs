using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using SuperNet.Netcode.Transport;

#pragma warning disable IDE0066 // Convert switch statement to expression

namespace SuperNet.Unity.Editor {

	[CanEditMultipleObjects]
	[CustomEditor(typeof(NetworkHost))]
	public sealed class NetworkHostEditor : UnityEditor.Editor {

		public enum BandwithUnit {
			BitsPerSecond,
			BytesPerSecond,
			KilobitsPerSecond,
			KilobytesPerSecond,
			MegabitsPerSecond,
			MegabytesPerSecond,
		}

		private string ConnectAddress = "";
		private bool[] Foldouts = null;
		private long[] PeerLastBytesSent = null;
		private long[] PeerLastBytesReceived = null;
		private float[] PeerLastTimestamp = null;
		private float[] PeerBandwidthSend = null;
		private float[] PeerBandwidthReceive = null;
		private long LastBytesSent = 0;
		private long LastBytesReceived = 0;
		private float LastTimestamp = 0f;
		private float BandwidthSend = 0f;
		private float BandwidthReceive = 0f;
		private BandwithUnit Unit = BandwithUnit.KilobitsPerSecond;

		public override void OnInspectorGUI() {

			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			base.OnInspectorGUI();
			EditorGUI.EndDisabledGroup();

			if (serializedObject.isEditingMultipleObjects) {
				return;
			}

			NetworkHost target = (NetworkHost)serializedObject.targetObject;

			if (!Application.IsPlaying(target)) {
				return;
			}

			Host host = target.GetHost();
			if (host == null || host.Disposed) {
				EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Not listening.", MessageType.Info);
				if (GUILayout.Button("Startup")) target.Startup();
				EditorGUILayout.Space();
				EditorUtility.SetDirty(target);
				return;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			ConnectAddress = EditorGUILayout.TextField(ConnectAddress);
			EditorGUI.BeginDisabledGroup(IPResolver.TryParse(ConnectAddress) == null);
			if (GUILayout.Button("Connect")) target.Connect(ConnectAddress);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			if (target.HostConfiguration.SimulatorEnabled) {
				if (GUILayout.Button("Disable Lag Simulator")) {
					target.HostConfiguration.SimulatorEnabled = false;
				}
			} else {
				if (GUILayout.Button("Enable Lag Simulator")) {
					target.HostConfiguration.SimulatorEnabled = true;
				}
			}
			if (GUILayout.Button("Shutdown")) target.Shutdown();
			EditorGUILayout.EndHorizontal();
			if (target.HostConfiguration.SimulatorEnabled) {
				target.HostConfiguration.SimulatorOutgoingLoss = EditorGUILayout.Slider(
					"Outgoing Loss",
					target.HostConfiguration.SimulatorOutgoingLoss,
					0f, 0.8f
				);
				target.HostConfiguration.SimulatorOutgoingLatency = EditorGUILayout.Slider(
					"Outgoing Latency",
					target.HostConfiguration.SimulatorOutgoingLatency,
					0f, 800f
				);
				target.HostConfiguration.SimulatorOutgoingJitter = EditorGUILayout.Slider(
					"Outgoing Jitter",
					target.HostConfiguration.SimulatorOutgoingJitter,
					0f, 400f
				);
				target.HostConfiguration.SimulatorIncomingLoss = EditorGUILayout.Slider(
					"Incoming Loss",
					target.HostConfiguration.SimulatorIncomingLoss,
					0f, 0.8f
				);
				target.HostConfiguration.SimulatorIncomingLatency = EditorGUILayout.Slider(
					"Incoming Latency",
					target.HostConfiguration.SimulatorIncomingLatency,
					0f, 800f
				);
				target.HostConfiguration.SimulatorIncomingJitter = EditorGUILayout.Slider(
					"Incoming Jitter",
					target.HostConfiguration.SimulatorIncomingJitter,
					0f, 400f
				);
			}
			BandwithUnit previousUnit = Unit;
			Unit = (BandwithUnit)EditorGUILayout.EnumPopup("Bandwidth Unit:", Unit);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			{
				long bytesSent = host.Statistics.SocketSendBytes;
				long bytesReceived = host.Statistics.SocketReceiveBytes;
				float now = Time.realtimeSinceStartup;
				float timeDelta = now - LastTimestamp;
				if (timeDelta > 1f || previousUnit != Unit) {
					long bytesSentDelta = bytesSent - LastBytesSent;
					long bytesReceivedDelta = bytesReceived - LastBytesReceived;
					LastBytesSent = bytesSent;
					LastBytesReceived = bytesReceived;
					LastTimestamp = now;
					BandwidthSend = GetBandwidth(bytesSentDelta, timeDelta);
					BandwidthReceive = GetBandwidth(bytesReceivedDelta, timeDelta);
				}
				EditorGUILayout.IntField("Bind Port", host.BindAddress.Port);
				EditorGUILayout.TextField("Bind Address", host.BindAddress.Address.ToString());
				EditorBandwidth("Upload Bandwidth", BandwidthSend);
				EditorBandwidth("Download Bandwidth", BandwidthReceive);
				EditorGUILayout.LongField("Bytes Sent", bytesSent);
				EditorGUILayout.LongField("Bytes Received", bytesReceived);
				EditorGUILayout.LongField("Packets Sent", host.Statistics.SocketSendCount);
				EditorGUILayout.LongField("Packets Received", host.Statistics.SocketReceiveCount);
				EditorGUILayout.LongField("Ticks", Host.Ticks);
			}
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Peers", EditorStyles.boldLabel);
			{
				IReadOnlyList<Peer> peers = target.GetPeers();
				Array.Resize(ref Foldouts, peers?.Count ?? 0);
				Array.Resize(ref PeerLastBytesSent, peers?.Count ?? 0);
				Array.Resize(ref PeerLastBytesReceived, peers?.Count ?? 0);
				Array.Resize(ref PeerLastTimestamp, peers?.Count ?? 0);
				Array.Resize(ref PeerBandwidthSend, peers?.Count ?? 0);
				Array.Resize(ref PeerBandwidthReceive, peers?.Count ?? 0);

				if (peers == null || peers.Count <= 0) {
					EditorGUILayout.HelpBox("No peers.", MessageType.Info);
				}

				for (int i = 0; i < peers.Count; i++) {
					Peer peer = peers[i];

					long bytesSent = peer.Statistics.PacketSendBytes;
					long bytesReceived = peer.Statistics.PacketReceiveBytes;
					float now = Time.realtimeSinceStartup;
					float timeDelta = now - PeerLastTimestamp[i];
					if (timeDelta > 1f || previousUnit != Unit) {
						long bytesSentDelta = bytesSent - PeerLastBytesSent[i];
						long bytesReceivedDelta = bytesReceived - PeerLastBytesReceived[i];
						LastBytesSent = bytesSent;
						LastBytesReceived = bytesReceived;
						LastTimestamp = now;
						PeerBandwidthSend[i] = GetBandwidth(bytesSentDelta, timeDelta);
						PeerBandwidthReceive[i] = GetBandwidth(bytesReceivedDelta, timeDelta);
					}

					Foldouts[i] = EditorGUILayout.Foldout(Foldouts[i], peer.Remote.ToString());
					if (Foldouts[i]) {

						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.IntField("RTT", peer.RTT);
						EditorGUILayout.TextField("Connected", peer.Connected ? "true" : "false");
						EditorGUILayout.TextField("Connecting", peer.Connecting ? "true" : "false");
						EditorGUILayout.TextField("Remote Address", peer.Remote.ToString());
						EditorGUILayout.LongField("Receive Bytes", peer.Statistics.PacketReceiveBytes);
						EditorGUILayout.LongField("Receive Packets", peer.Statistics.PacketReceiveCount);
						EditorGUILayout.LongField("Receive Messages", peer.Statistics.MessageReceiveTotal);
						EditorGUILayout.LongField("Receive Duplicated", peer.Statistics.MessageReceiveDuplicated);
						EditorGUILayout.LongField("Receive Unreliables", peer.Statistics.MessageReceiveUnreliable);
						EditorGUILayout.LongField("Receive Acknowledgments", peer.Statistics.MessageReceiveAcknowledge);
						EditorBandwidth("Receive Bandwidth", PeerBandwidthReceive[i]);
						EditorGUILayout.LongField("Receive Pings", peer.Statistics.MessageReceivePing);
						EditorGUILayout.LongField("Receive Lost", peer.Statistics.MessageReceiveLost); 
						EditorGUILayout.LongField("Sent Bytes", peer.Statistics.PacketSendBytes);
						EditorGUILayout.LongField("Sent Packets", peer.Statistics.PacketSendCount);
						EditorGUILayout.LongField("Sent Messages", peer.Statistics.MessageSendTotal);
						EditorGUILayout.LongField("Sent Duplicated", peer.Statistics.MessageSendDuplicated);
						EditorGUILayout.LongField("Sent Unreliables", peer.Statistics.MessageSendUnreliable);
						EditorGUILayout.LongField("Sent Acknowledgments", peer.Statistics.MessageSendAcknowledge);
						EditorBandwidth("Send Bandwidth", PeerBandwidthSend[i]);
						EditorGUILayout.LongField("Sent Pings", peer.Statistics.MessageSendPing);
						EditorGUI.EndDisabledGroup();

						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Tools");
						if (GUILayout.Button("Disconnect")) {
							peer.Disconnect();
						}
						if (GUILayout.Button("Dispose")) {
							peer.Dispose();
						}
						EditorGUILayout.EndHorizontal();

					}
				}

			}

			EditorGUILayout.Space();
			EditorUtility.SetDirty(target);

		}

		private float GetBandwidth(long bytes, float timeDelta) {
			switch (Unit) {
				case BandwithUnit.BitsPerSecond:
					return 8f * bytes / timeDelta;
				case BandwithUnit.BytesPerSecond:
					return bytes / timeDelta;
				case BandwithUnit.KilobitsPerSecond:
					return bytes / (timeDelta * 125f);
				case BandwithUnit.KilobytesPerSecond:
					return bytes / (timeDelta * 1000f);
				default:
				case BandwithUnit.MegabitsPerSecond:
					return bytes / (timeDelta * 125000f);
				case BandwithUnit.MegabytesPerSecond:
					return bytes / (timeDelta * 1000000f);
			}
		}

		private void EditorBandwidth(string text, float bandwidth) {
			switch (Unit) {
				case BandwithUnit.BitsPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " bps");
					break;
				case BandwithUnit.BytesPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " Byte/s");
					break;
				case BandwithUnit.KilobitsPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " kbps");
					break;
				case BandwithUnit.KilobytesPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " KB/s");
					break;
				default:
				case BandwithUnit.MegabitsPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " mbps");
					break;
				case BandwithUnit.MegabytesPerSecond:
					EditorGUILayout.TextField(text, bandwidth + " MB/s");
					break;
			}
		}

	}

}
