using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Synchronizes legacy animation over the network. 
	/// </summary>
	[AddComponentMenu("SuperNet/NetworkAnimation")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkAnimation.html")]
	public sealed class NetworkAnimation : NetworkComponent, INetworkAuthoritative {

		/// <summary>
		/// Animation component to synchronize. Required.
		/// </summary>
		[FormerlySerializedAs("Animation")]
		[Tooltip("Animation component to synchronize. Required.")]
		public Animation Animation;

		/// <summary>
		/// Which method to synchronize in.
		/// </summary>
		[FormerlySerializedAs("SyncMethod")]
		[Tooltip("Which method to synchronize in.")]
		public NetworkSyncModeMethod SyncMethod;

		/// <summary>
		/// Network channel to use.
		/// </summary>
		[FormerlySerializedAs("SyncChannel")]
		[Tooltip("Network channel to use.")]
		public byte SyncChannel;

		/// <summary>
		/// Send updates to remote peers.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("Authority")]
		[Tooltip("Send updates to remote peers.")]
		private bool Authority;

		[Header("Receive Configuration")]

		/// <summary>
		/// How many seconds into the past we see the animation at.
		/// </summary>
		[FormerlySerializedAs("ReceiveDelay")]
		[Tooltip("How many seconds into the past we see the animation at.")]
		public float ReceiveDelay;

		[Header("Send Configuration")]

		/// <summary>
		/// Minimum number of seconds to wait before sending an update.
		/// </summary>
		[FormerlySerializedAs("SendIntervalMin")]
		[Tooltip("Minimum number of seconds to wait before sending an update.")]
		public float SendIntervalMin;

		/// <summary>
		/// Send updates to remote peers.
		/// </summary>
		public bool IsAuthority => Authority;

		// Resources
		private AnimationState[] States;
		private ReaderWriterLockSlim SendLock;
		private bool[] SendEnabled;
		private float[] SendWeight;
		private WrapMode[] SendWrap;
		private float[] SendTime;
		private float[] SendSpeed;
		private int[] SendLayer;
		private string[] SendName;
		private AnimationBlendMode[] SendBlend;
		private HostTimestamp SendLastTime;
		private object AuthorityLock;
		private Peer AuthorityPeer;

		private void Reset() {
			ResetNetworkID();
			Animation = GetComponentInChildren<Animation>();
			SyncMethod = NetworkSyncModeMethod.Update;
			SyncChannel = 0;
			Authority = false;
			ReceiveDelay = 0.1f;
			SendIntervalMin = 0.03f;
		}

		private void Awake() {

			// Check if animation is set
			if (Animation == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimation - {0}] Animation not set.", NetworkID
					), this);
				}
				enabled = false;
				return;
			}

			// Allocate resources
			List<AnimationState> states = new List<AnimationState>();
			foreach (AnimationState state in Animation) states.Add(state);
			States = states.ToArray();
			SendLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			SendEnabled = new bool[States.Length];
			SendWeight = new float[States.Length];
			SendWrap = new WrapMode[States.Length];
			SendTime = new float[States.Length];
			SendSpeed = new float[States.Length];
			SendLayer = new int[States.Length];
			SendName = new string[States.Length];
			SendBlend = new AnimationBlendMode[States.Length];
			SendLastTime = HostTimestamp.MinValue;
			AuthorityLock = new object();
			AuthorityPeer = null;

			// Initialize resources
			for (int state = 0; state < States.Length; state++) {
				SendEnabled[state] = States[state].enabled;
				SendWeight[state] = States[state].weight;
				SendWrap[state] = States[state].wrapMode;
				SendTime[state] = States[state].time;
				SendSpeed[state] = States[state].speed;
				SendLayer[state] = States[state].layer;
				SendName[state] = States[state].name;
				SendBlend[state] = States[state].blendMode;
			}

			if (States.Length * 8 >= ushort.MaxValue) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimation - {0}] Animation has too many states.", NetworkID
					), this);
				}
				enabled = false;
			}

		}

		private void Update() {
			if (SyncMethod == NetworkSyncModeMethod.Update) SyncUpdate();
		}

		private void LateUpdate() {
			if (SyncMethod == NetworkSyncModeMethod.LateUpdate) SyncUpdate();
		}

		private void FixedUpdate() {
			if (SyncMethod == NetworkSyncModeMethod.FixedUpdate) SyncUpdate();
		}

		private void SyncUpdate() {

			// Check if animation is set
			if (Animation == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimation - {0}] Animation removed.", NetworkID
					), this);
				}
				enabled = false;
				return;
			}

			// If not registered, do nothing
			if (!IsRegisteredOnNetwork) {
				return;
			}

			// Send updates
			if (Authority) {
				SyncUpdateSend();
			}

		}

		private void SyncUpdateSend() {
			try {
				SendLock.EnterUpgradeableReadLock();

				// Get current time
				HostTimestamp now = Host.Now;

				// If not enough time has passed to send an update, do nothing
				if ((now - SendLastTime).Seconds < SendIntervalMin) {
					return;
				}

				for (int state = 0; state < States.Length; state++) {

					// Get current state
					bool enabled = States[state].enabled;
					float weight = States[state].weight;
					WrapMode wrap = States[state].wrapMode;
					float time = States[state].time;
					float speed = States[state].speed;
					int layer = States[state].layer;
					string name = States[state].name;
					AnimationBlendMode blend = States[state].blendMode;
					float timeNow = time - (float)(now - SendLastTime).Seconds * speed;

					// Check enabled
					if (enabled != SendEnabled[state]) {
						ushort header = (ushort)(state * 8 + 0);
						SendNetworkMessage(new NetworkMessageAnimationEnabled(header, enabled, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendEnabled[state] = States[state].enabled;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

					// Check weight
					if (Mathf.Abs(weight - SendWeight[state]) > 0.001f) {
						ushort header = (ushort)(state * 8 + 1);
						SendNetworkMessage(new NetworkMessageAnimationWeight(header, weight, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendWeight[state] = weight;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

					// Check wrap mode
					if (wrap != SendWrap[state]) {
						ushort header = (ushort)(state * 8 + 2);
						SendNetworkMessage(new NetworkMessageAnimationWrap(header, wrap, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendWrap[state] = wrap;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

					// Check time
					if (Mathf.Abs(timeNow - SendTime[state]) > 0.5f) {
						ushort header = (ushort)(state * 8 + 3);
						SendNetworkMessage(new NetworkMessageAnimationTime(header, time, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendTime[state] = time;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

					// Check speed
					if (Mathf.Abs(speed - SendSpeed[state]) > 0.001f) {
						ushort header = (ushort)(state * 8 + 4);
						SendNetworkMessage(new NetworkMessageAnimationSpeed(header, speed, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendSpeed[state] = speed;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

					// Check layer
					if (layer != SendLayer[state]) {
						ushort header = (ushort)(state * 8 + 5);
						SendNetworkMessage(new NetworkMessageAnimationLayer(header, layer, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendLayer[state] = layer;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}


					// Check name
					if (string.Compare(name, SendName[state]) != 0) {
						ushort header = (ushort)(state * 8 + 6);
						SendNetworkMessage(new NetworkMessageAnimationName(header, name, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendName[state] = name;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}


					// Check blend mode
					if (blend != SendBlend[state]) {
						ushort header = (ushort)(state * 8 + 7);
						SendNetworkMessage(new NetworkMessageAnimationBlendMode(header, blend, SyncChannel));
						try {
							SendLock.EnterWriteLock();
							SendBlend[state] = blend;
							SendLastTime = now;
						} finally {
							SendLock.ExitWriteLock();
						}
					}

				}

			} finally {
				SendLock.ExitUpgradeableReadLock();
			}
		}

		public override bool OnNetworkResend(Peer origin, Peer peer, Reader reader, MessageReceived info) {

			// Dont resend to local connections
			if (Host.IsLocal(peer.Remote)) {
				return false;
			}

			// Only resend if we don't have authority
			return !Authority;

		}

		public override void OnNetworkMessage(Peer peer, Reader reader, MessageReceived info) {

			// Ignore messages from local connections
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			lock (AuthorityLock) {

				// Check if this is a state request
				if (reader.Available <= 0) {
					if (Authority) SendNetworkState(peer);
					return;
				}

				// If receiving is not enabled, do nothing
				if (Authority) {
					if (NetworkManager.LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [NetworkAnimation - {0}] Recieved update from {1}. Ignoring.",
							NetworkID, peer.Remote
						), this);
					}
					return;
				}

				// Save authority peer
				Interlocked.Exchange(ref AuthorityPeer, peer);

			}

			// Get message time
			float ageSeconds = Mathf.Max(0f, (float)info.Timestamp.Elapsed.Seconds);
			float delay = Mathf.Max(0f, ReceiveDelay - ageSeconds);

			// Read header
			ushort header = reader.ReadUInt16();
			int state = header / 8;

			// Update animation
			switch (header % 8) {
				case 0:
					bool enabled = reader.ReadBoolean();
					Run(() => States[state].enabled = enabled, delay);
					break;
				case 1:
					float weight = reader.ReadSingle();
					Run(() => States[state].weight = weight, delay);
					break;
				case 2:
					WrapMode wrap = (WrapMode)reader.ReadByte();
					Run(() => States[state].wrapMode = wrap, delay);
					break;
				case 3:
					float time = reader.ReadSingle();
					Run(() => States[state].time = time + ageSeconds * States[state].speed, delay);
					break;
				case 4:
					float speed = reader.ReadSingle();
					Run(() => States[state].speed = speed, delay);
					break;
				case 5:
					int layer = reader.ReadInt32();
					Run(() => States[state].layer = layer, delay);
					break;
				case 6:
					string name = reader.ReadString();
					Run(() => States[state].name = name, delay);
					break;
				case 7:
					AnimationBlendMode blend = (AnimationBlendMode)reader.ReadByte();
					Run(() => States[state].blendMode = blend, delay);
					break;
			}

		}

		void INetworkAuthoritative.OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp) {

			// Update authority and resync state
			SetAuthority(authority);

		}

		/// <summary>
		/// Update authority on this component.
		/// </summary>
		/// <param name="authority">Authority to set.</param>
		public void SetAuthority(bool authority) {

			lock (AuthorityLock) {

				// Check if authority changed
				if (Authority == authority) {
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkAnimation - {0}] Authority {1}.",
						NetworkID, authority ? "gained" : "lost"
					), this);
				}

				// Update authority
				Authority = authority;

				// Remove authority peer if we are now authority
				if (Authority) {
					Interlocked.Exchange(ref AuthorityPeer, null);
				}

				// Send or request state
				if (IsRegisteredOnNetwork) {
					if (Authority) {
						SendNetworkState(null);
					} else {
						SendNetworkMessage(new NetworkMessageRequest(SyncChannel));
					}
				}

			}

		}

		public override void OnNetworkRegister() {

			lock (AuthorityLock) {

				// Send or request state
				if (Authority) {
					SendNetworkState(null);
				} else {
					SendNetworkMessage(new NetworkMessageRequest(SyncChannel));
				}

			}

		}

		public override void OnNetworkConnect(Peer peer) {

			// Ignore if local peer
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			lock (AuthorityLock) {

				// Request the latest state if no authority peer assigned
				if (!Authority && AuthorityPeer == null) {
					SendNetworkMessage(new NetworkMessageRequest(SyncChannel), peer);
				}

			}

		}

		public override void OnNetworkDisconnect(Peer peer) {

			// Ignore if local peer
			if (Host.IsLocal(peer.Remote)) {
				return;
			}

			lock (AuthorityLock) {

				// Remove authority peer if it disconnected
				Peer previousAuthority = Interlocked.CompareExchange(ref AuthorityPeer, null, peer);

				// Request state from a new authority
				if (!Authority && previousAuthority == peer) {
					SendNetworkMessage(new NetworkMessageRequest(SyncChannel));
				}

			}

		}

		private void SendNetworkState(Peer peer) {
			try {
				SendLock.EnterReadLock();

				for (int state = 0; state < States.Length; state++) {

					// Get current state
					bool enabled = SendEnabled[state];
					float weight = SendWeight[state];
					WrapMode wrap = SendWrap[state];
					float time = SendTime[state];
					float speed = SendSpeed[state];
					int layer = SendLayer[state];
					string name = SendName[state];
					AnimationBlendMode blend = SendBlend[state];
					ushort header = (ushort)(state * 8);

					// Send state
					if (peer == null) {
						SendNetworkMessage(new NetworkMessageAnimationEnabled((ushort)(header + 0), enabled, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationWeight((ushort)(header + 1), weight, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationWrap((ushort)(header + 2), wrap, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationTime((ushort)(header + 3), time, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationSpeed((ushort)(header + 4), speed, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationLayer((ushort)(header + 5), layer, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationName((ushort)(header + 6), name, SyncChannel));
						SendNetworkMessage(new NetworkMessageAnimationBlendMode((ushort)(header + 7), blend, SyncChannel));
					} else {
						SendNetworkMessage(new NetworkMessageAnimationEnabled((ushort)(header + 0), enabled, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationWeight((ushort)(header + 1), weight, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationWrap((ushort)(header + 2), wrap, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationTime((ushort)(header + 3), time, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationSpeed((ushort)(header + 4), speed, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationLayer((ushort)(header + 5), layer, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationName((ushort)(header + 6), name, SyncChannel), peer);
						SendNetworkMessage(new NetworkMessageAnimationBlendMode((ushort)(header + 7), blend, SyncChannel), peer);
					}

				}

			} finally {
				SendLock.ExitReadLock();
			}
		}

		private struct NetworkMessageAnimationEnabled : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly bool Enabled;
			public readonly byte Channel;

			public NetworkMessageAnimationEnabled(ushort header, bool enabled, byte channel) {
				Header = header;
				Enabled = enabled;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Enabled);
			}

		}

		private struct NetworkMessageAnimationWeight : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly float Weight;
			public readonly byte Channel;

			public NetworkMessageAnimationWeight(ushort header, float weight, byte channel) {
				Header = header;
				Weight = weight;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Weight);
			}

		}

		private struct NetworkMessageAnimationWrap : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly WrapMode Wrap;
			public readonly byte Channel;

			public NetworkMessageAnimationWrap(ushort header, WrapMode wrap, byte channel) {
				Header = header;
				Wrap = wrap;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write((byte)Wrap);
			}

		}

		private struct NetworkMessageAnimationTime : IMessage
        {

            HostTimestamp IMessage.Timestamp => Host.Now;
            byte IMessage.Channel => Channel;
            bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly float Time;
			public readonly byte Channel;

			public NetworkMessageAnimationTime(ushort header, float time, byte channel) {
				Header = header;
				Time = time;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Time);
			}

		}

		private struct NetworkMessageAnimationSpeed : IMessage
        {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly float Speed;
			public readonly byte Channel;

			public NetworkMessageAnimationSpeed(ushort header, float speed, byte channel) {
				Header = header;
				Speed = speed;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Speed);
			}

		}

		private struct NetworkMessageAnimationLayer : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly int Layer;
			public readonly byte Channel;

			public NetworkMessageAnimationLayer(ushort header, int layer, byte channel) {
				Header = header;
				Layer = layer;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Layer);
			}

		}

		private struct NetworkMessageAnimationName : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly string Name;
			public readonly byte Channel;

			public NetworkMessageAnimationName(ushort header, string name, byte channel) {
				Header = header;
				Name = name;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Name);
			}

		}

		private struct NetworkMessageAnimationBlendMode : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly ushort Header;
			public readonly AnimationBlendMode Mode;
			public readonly byte Channel;

			public NetworkMessageAnimationBlendMode(ushort header, AnimationBlendMode mode, byte channel) {
				Header = header;
				Mode = mode;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write((byte)Mode);
			}

		}

		private struct NetworkMessageRequest : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => false;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Channel;

			public NetworkMessageRequest(byte channel) {
				Channel = channel;
			}

			void IWritable.Write(Writer writer) { }

		}

	}

}
