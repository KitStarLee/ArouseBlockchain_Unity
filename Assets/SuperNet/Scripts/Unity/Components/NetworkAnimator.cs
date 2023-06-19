using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using System;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Synchronizes an animator over the network. 
	/// </summary>
	[AddComponentMenu("SuperNet/NetworkAnimator")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkAnimator.html")]
	public sealed class NetworkAnimator : NetworkComponent, INetworkAuthoritative {

		/// <summary>
		/// Animator component to synchronize. Required.
		/// </summary>
		[FormerlySerializedAs("Animator")]
		[Tooltip("Animator component to synchronize. Required.")]
		public Animator Animator;

		/// <summary>
		/// Which method to synchronize in.
		/// </summary>
		[FormerlySerializedAs("SyncMethod")]
		[Tooltip("Which method to synchronize in.")]
		public NetworkSyncModeMethod SyncMethod;

		/// <summary>
		/// Syncronize animator parameters.
		/// </summary>
		[FormerlySerializedAs("SyncParameters")]
		[Tooltip("Syncronize animator parameters.")]
		public bool SyncParameters;

		/// <summary>
		/// Syncronize animator states.
		/// </summary>
		[FormerlySerializedAs("SyncStates")]
		[Tooltip("Syncronize animator states.")]
		public bool SyncStates;

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
		/// How many seconds into the past we see the animator at.
		/// </summary>
		[FormerlySerializedAs("ReceiveDelay")]
		[Tooltip("How many seconds into the past we see the animator at.")]
		public float ReceiveDelay;

		[Header("Send Configuration")]

		/// <summary>
		/// Minimum number of seconds to wait before sending an update.
		/// </summary>
		[FormerlySerializedAs("SendIntervalMin")]
		[Tooltip("Minimum number of seconds to wait before sending an update.")]
		public float SendIntervalMin;

		/// <summary>
		/// Called when trigger parameter is set remotely.
		/// </summary>
		/// <param name="hash">Trigger hash ID.</param>
		public delegate void OnNetworkTriggerHandler(int hash);
		
		/// <summary>
		/// Called when trigger parameter is set remotely.
		/// </summary>
		public event OnNetworkTriggerHandler OnNetworkTrigger;

		/// <summary>
		/// Send updates to remote peers.
		/// </summary>
		public bool IsAuthority => Authority;

		// Resources
		private int Layers;
		private AnimatorControllerParameter[] Parameters;
		private ReaderWriterLockSlim SendLock;
		private float[] SendParametersFloat;
		private bool[] SendParametersBool;
		private int[] SendParametersInt;
		private int[] SendStates;
		private HostTimestamp SendTime;
		private object AuthorityLock;
		private Peer AuthorityPeer;

		private void Reset() {
			ResetNetworkID();
			Animator = GetComponentInChildren<Animator>();
			SyncMethod = NetworkSyncModeMethod.Update;
			SyncParameters = true;
			SyncStates = true;
			SyncChannel = 0;
			Authority = false;
			ReceiveDelay = 0.1f;
			SendIntervalMin = 0.03f;
		}

		private void Awake() {

			// Check if animator is set
			if (Animator == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimator - {0}] Animator not set.", NetworkID
					), this);
				}
				enabled = false;
				return;
			}

			// Initialize
			Layers = Animator.layerCount;
			List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
			foreach (AnimatorControllerParameter parameter in Animator.parameters) {
				if (Animator.IsParameterControlledByCurve(parameter.nameHash)) continue;
				bool isInt = parameter.type == AnimatorControllerParameterType.Int;
				bool isBool = parameter.type == AnimatorControllerParameterType.Bool;
				bool isFloat = parameter.type == AnimatorControllerParameterType.Float;
				if (isInt || isBool || isFloat) parameters.Add(parameter);
			}
			Parameters = parameters.ToArray();
			SendLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			SendParametersFloat = new float[Parameters.Length];
			SendParametersBool = new bool[Parameters.Length];
			SendParametersInt = new int[Parameters.Length];
			SendStates = new int[Layers];
			SendTime = HostTimestamp.MinValue;
			AuthorityLock = new object();
			AuthorityPeer = null;

			// Get current local state
			for (int layer = 0; layer < Layers; layer++) SendStates[layer] = GetAnimatorState(layer).fullPathHash;
			for (int i = 0; i < Parameters.Length; i++) {
				if (Parameters[i].type == AnimatorControllerParameterType.Int)
					SendParametersInt[i] = Animator.GetInteger(Parameters[i].nameHash);
				if (Parameters[i].type == AnimatorControllerParameterType.Bool)
					SendParametersBool[i] = Animator.GetBool(Parameters[i].nameHash);
				if (Parameters[i].type == AnimatorControllerParameterType.Float)
					SendParametersFloat[i] = Animator.GetFloat(Parameters[i].nameHash);
			}

			// Make sure parameters and layer states can be sent over network
			if (Layers + Parameters.Length > 254) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimator - {0}] Animator has too many parameters.", NetworkID
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

			// Check if transform was removed
			if (Animator == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimator - {0}] Animator removed.", NetworkID
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
				if ((now - SendTime).Seconds < SendIntervalMin) {
					return;
				}

				// Check if any layer states changed
				if (SyncStates) {
					for (int layer = 0; layer < Layers; layer++) {
						AnimatorStateInfo info = GetAnimatorState(layer);
						int hash = info.fullPathHash;
						if (hash != SendStates[layer]) {
							byte header = (byte)layer;
							float time = info.normalizedTime;
							SendNetworkMessage(new NetworkMessageAnimatorHash(header, hash, time, SyncChannel));
							try {
								SendLock.EnterWriteLock();
								SendStates[layer] = hash;
								SendTime = now;
							} finally {
								SendLock.ExitWriteLock();
							}
						}
					}
				}

				// Check if any parameters changed
				if (SyncParameters) {
					for (int i = 0; i < Parameters.Length; i++) {
						if (Parameters[i].type == AnimatorControllerParameterType.Int) {
							int value = Animator.GetInteger(Parameters[i].nameHash);
							if (value != SendParametersInt[i]) {
								byte header = (byte)(Layers + i);
								SendNetworkMessage(new NetworkMessageAnimatorInt(header, value, SyncChannel));
								try {
									SendLock.EnterWriteLock();
									SendParametersInt[i] = value;
									SendTime = now;
								} finally {
									SendLock.ExitWriteLock();
								}
							}
						} else if (Parameters[i].type == AnimatorControllerParameterType.Bool) {
							bool value = Animator.GetBool(Parameters[i].nameHash);
							if (value != SendParametersBool[i]) {
								byte header = (byte)(Layers + i);
								SendNetworkMessage(new NetworkMessageAnimatorBool(header, value, SyncChannel));
								try {
									SendLock.EnterWriteLock();
									SendParametersBool[i] = value;
									SendTime = now;
								} finally {
									SendLock.ExitWriteLock();
								}
							}
						} else if (Parameters[i].type == AnimatorControllerParameterType.Float) {
							float value = Animator.GetFloat(Parameters[i].nameHash);
							if (Mathf.Abs(value - SendParametersFloat[i]) > 0.001f) {
								byte header = (byte)(Layers + i);
								SendNetworkMessage(new NetworkMessageAnimatorFloat(header, value, SyncChannel));
								try {
									SendLock.EnterWriteLock();
									SendParametersFloat[i] = value;
									SendTime = now;
								} finally {
									SendLock.ExitWriteLock();
								}
							}
						}
					}
				}
				
			} finally {
				SendLock.ExitUpgradeableReadLock();
			}
		}

		/// <summary>
		/// Sets a trigger locally and sends it to everybody on the network regardless of authority.
		/// </summary>
		/// <param name="triggerName">Trigger name.</param>
		public void SetTrigger(string triggerName) {
			SetTrigger(Animator.StringToHash(triggerName));
		}

		/// <summary>
		/// Sets a trigger locally and sends it to everybody on the network regardless of authority.
		/// </summary>
		/// <param name="id">Trigger hash ID.</param>
		public void SetTrigger(int id) {

			// Set trigger
			Run(() => Animator.SetTrigger(id));

			// Send trigger to everybody
			if (IsRegisteredOnNetwork) {
				SendNetworkMessage(new NetworkMessageAnimatorInt(0xFF, id, SyncChannel));
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
							"[SuperNet] [NetworkAnimator - {0}] Recieved update from {1}. Ignoring.",
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
			byte header = reader.ReadByte();

			if (header == 0xFF) {

				// Update trigger
				int hash = reader.ReadInt32();
				Run(() => {
					Animator.SetTrigger(hash);
					try {
						OnNetworkTrigger?.Invoke(hash);
					} catch (Exception exception) {
						if (NetworkManager.LogExceptions) {
							Debug.LogException(exception);
						}
					}
				}, delay);

			} else if (header < Layers) {

				// Update layer state
				if (SyncStates) {
					int hash = reader.ReadInt32();
					if (reader.Available >= 4) {
						float normalizedTime = reader.ReadSingle();
						Run(() => Animator.Play(hash, header, normalizedTime), delay);
					} else {
						Run(() => Animator.Play(hash, header), delay);
					}
				}

			} else if (header - Layers < Parameters.Length) {

				// Update parameter
				if (SyncParameters) {
					AnimatorControllerParameter parameter = Parameters[header - Layers];
					if (parameter.type == AnimatorControllerParameterType.Int) {
						int value = reader.ReadInt32();
						Run(() => Animator.SetInteger(parameter.nameHash, value), delay);
					} else if (parameter.type == AnimatorControllerParameterType.Bool) {
						bool value = reader.ReadBoolean();
						Run(() => Animator.SetBool(parameter.nameHash, value), delay);
					} else if (parameter.type == AnimatorControllerParameterType.Float) {
						float value = reader.ReadSingle();
						Run(() => Animator.SetFloat(parameter.nameHash, value), delay);
					}
				}

			} else {

				// Header extends beyond the last parameter
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkAnimator - {0}] Recieved update from {1} with invalid header {2}. Ignoring.",
						NetworkID, peer.Remote, header
					), this);
				}

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
						"[SuperNet] [NetworkAnimator - {0}] Authority {1}.",
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

				if (peer == null) {

					// Send layer states
					for (int layer = 0; layer < Layers; layer++) {
						byte header = (byte)layer;
						int hash = SendStates[layer];
						SendNetworkMessage(new NetworkMessageAnimatorInt(header, hash, SyncChannel));
					}

					// Send parameters
					for (int i = 0; i < Parameters.Length; i++) {
						byte header = (byte)(Layers + i);
						if (Parameters[i].type == AnimatorControllerParameterType.Int) {
							int value = SendParametersInt[i];
							SendNetworkMessage(new NetworkMessageAnimatorInt(header, value, SyncChannel));
						} else if (Parameters[i].type == AnimatorControllerParameterType.Bool) {
							bool value = SendParametersBool[i];
							SendNetworkMessage(new NetworkMessageAnimatorBool(header, value, SyncChannel));
						} else if (Parameters[i].type == AnimatorControllerParameterType.Float) {
							float value = SendParametersFloat[i];
							SendNetworkMessage(new NetworkMessageAnimatorFloat(header, value, SyncChannel));
						}
					}

				} else {

					// Send layer states
					for (int layer = 0; layer < Layers; layer++) {
						byte header = (byte)layer;
						int hash = SendStates[layer];
						SendNetworkMessage(new NetworkMessageAnimatorInt(header, hash, SyncChannel), peer);
					}

					// Send parameters
					for (int i = 0; i < Parameters.Length; i++) {
						byte header = (byte)(Layers + i);
						if (Parameters[i].type == AnimatorControllerParameterType.Int) {
							int value = SendParametersInt[i];
							SendNetworkMessage(new NetworkMessageAnimatorInt(header, value, SyncChannel), peer);
						} else if (Parameters[i].type == AnimatorControllerParameterType.Bool) {
							bool value = SendParametersBool[i];
							SendNetworkMessage(new NetworkMessageAnimatorBool(header, value, SyncChannel), peer);
						} else if (Parameters[i].type == AnimatorControllerParameterType.Float) {
							float value = SendParametersFloat[i];
							SendNetworkMessage(new NetworkMessageAnimatorFloat(header, value, SyncChannel), peer);
						}
					}

				}

			} finally {
				SendLock.ExitReadLock();
			}
		}

		private AnimatorStateInfo GetAnimatorState(int layer) {
			if (Animator.IsInTransition(layer)) {
				return Animator.GetNextAnimatorStateInfo(layer);
			} else {
				return Animator.GetCurrentAnimatorStateInfo(layer);
			}
		}

		private struct NetworkMessageAnimatorFloat : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Header;
			public readonly float Value;
			public readonly byte Channel;

			public NetworkMessageAnimatorFloat(byte header, float value, byte channel) {
				Header = header;
				Value = value;
				Channel = channel;
			}

			public void Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Value);
			}

		}

		private struct NetworkMessageAnimatorBool : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Header;
			public readonly bool Value;
			public readonly byte Channel;

			public NetworkMessageAnimatorBool(byte header, bool value, byte channel) {
				Header = header;
				Value = value;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Value);
			}

		}

		private struct NetworkMessageAnimatorInt : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Header;
			public readonly int Value;
			public readonly byte Channel;

			public NetworkMessageAnimatorInt(byte header, int value, byte channel) {
				Header = header;
				Value = value;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Value);
			}

		}

		private struct NetworkMessageAnimatorHash : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => true;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => true;
			
			public readonly byte Header;
			public readonly int Hash;
			public readonly float Time;
			public readonly byte Channel;

			public NetworkMessageAnimatorHash(byte header, int hash, float time, byte channel) {
				Header = header;
				Hash = hash;
				Time = time;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				writer.Write(Header);
				writer.Write(Hash);
				writer.Write(Time);
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
