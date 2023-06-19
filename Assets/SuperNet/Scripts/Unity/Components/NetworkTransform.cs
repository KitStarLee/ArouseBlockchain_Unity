using SuperNet.Netcode.Transport;
using SuperNet.Unity.Core;
using SuperNet.Netcode.Util;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Synchronizes a transform over the network.
	/// </summary>
	[AddComponentMenu("SuperNet/NetworkTransform")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Components.NetworkTransform.html")]
	public sealed class NetworkTransform : NetworkComponent, INetworkAuthoritative, INetworkRollbackable {

		/// <summary>
		/// Transform component to synchronize. Required.
		/// </summary>
		[FormerlySerializedAs("Transform")]
		[Tooltip("Transform component to synchronize. Required.")]
		public Transform Transform;

		/// <summary>
		/// Rigidbody component to synchronize. Optional.
		/// </summary>
		[FormerlySerializedAs("Rigidbody")]
		[Tooltip("Rigidbody component to synchronize. Optional.")]
		public Rigidbody Rigidbody;

		/// <summary>
		/// Rigidbody2D component to synchronize. Required.
		/// </summary>
		[FormerlySerializedAs("Rigidbody2D")]
		[Tooltip("Rigidbody2D component to synchronize. Optional.")]
		public Rigidbody2D Rigidbody2D;

		/// <summary>
		/// RectTransform component to synchronize. Optional.
		/// </summary>
		[FormerlySerializedAs("RectTransform")]
		[Tooltip("RectTransform component to synchronize. Optional.")]
		public RectTransform RectTransform;

		/// <summary>
		/// Which method to synchronize in.
		/// </summary>
		[FormerlySerializedAs("SyncMethod")]
		[Tooltip("Which method to synchronize in.")]
		public NetworkSyncModeMethod SyncMethod;

		/// <summary>
		/// Which rotation components to synchronize.
		/// </summary>
		[FormerlySerializedAs("SyncRotation")]
		[Tooltip("Which rotation components to synchronize.")]
		public NetworkSyncModeVector3 SyncRotation;

		/// <summary>
		/// Which position components to synchronize.
		/// </summary>
		[FormerlySerializedAs("SyncPosition")]
		[Tooltip("Which position components to synchronize.")]
		public NetworkSyncModeVector3 SyncPosition;

		/// <summary>
		/// Which scale components to synchronize.
		/// </summary>
		[FormerlySerializedAs("SyncScale")]
		[Tooltip("Which scale components to synchronize.")]
		public NetworkSyncModeVector3 SyncScale;

		/// <summary>
		/// Which velocity components to synchronize.
		/// </summary>
		[FormerlySerializedAs("SyncVelocity")]
		[Tooltip("Which velocity components to synchronize.")]
		public NetworkSyncModeVector3 SyncVelocity;

		/// <summary>
		/// Which angular velocity components to synchronize.
		/// </summary>
		[FormerlySerializedAs("SyncAngularVelocity")]
		[Tooltip("Which angular velocity components to synchronize.")]
		public NetworkSyncModeVector3 SyncAngularVelocity;

		/// <summary>
		/// Should RectTransform anchorMin be synchronized.
		/// </summary>
		[FormerlySerializedAs("SyncRectAnchorMin")]
		[Tooltip("Should RectTransform anchorMin be synchronized.")]
		public NetworkSyncModeVector2 SyncRectAnchorMin;

		/// <summary>
		/// Should RectTransform anchorMax be synchronized.
		/// </summary>
		[FormerlySerializedAs("SyncRectAnchorMax")]
		[Tooltip("Should RectTransform anchorMax be synchronized.")]
		public NetworkSyncModeVector2 SyncRectAnchorMax;

		/// <summary>
		/// Should RectTransform sizeDelta be synchronized.
		/// </summary>
		[FormerlySerializedAs("SyncRectSizeDelta")]
		[Tooltip("Should RectTransform sizeDelta be synchronized.")]
		public NetworkSyncModeVector2 SyncRectSizeDelta;

		/// <summary>
		/// Should RectTransform pivot be synchronized.
		/// </summary>
		[FormerlySerializedAs("SyncRectPivot")]
		[Tooltip("Should RectTransform pivot be synchronized.")]
		public NetworkSyncModeVector2 SyncRectPivot;

		/// <summary>
		/// Network channel to use.
		/// </summary>
		[FormerlySerializedAs("SyncChannel")]
		[Tooltip("Network channel to use.")]
		public byte SyncChannel;

		/// <summary>
		/// Synchronize position and rotation in local space.
		/// </summary>
		[FormerlySerializedAs("SyncLocalTransform")]
		[Tooltip("Synchronize position and rotation in local space.")]
		public bool SyncLocalTransform;

		/// <summary>
		/// Send updates to remote peers.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("Authority")]
		[Tooltip("Send updates to remote peers.")]
		private bool Authority;

		[Header("Receive Configuration")]

		/// <summary>
		/// How many seconds into the past we see the transform at.
		/// Smaller values make the transform more correct to where it actually is but make it more jittery.
		/// If updates we receive are older than this value (high ping), they are extrapolated instead of interpolated.
		/// </summary>
		[FormerlySerializedAs("ReceiveDelay")]
		[Tooltip("How many seconds into the past we see the transform at.")]
		public float ReceiveDelay;

		/// <summary>
		/// How many seconds to keep received values for.
		/// </summary>
		[FormerlySerializedAs("ReceiveDuration")]
		[Tooltip("How many seconds to keep received values for.")]
		public float ReceiveDuration;

		/// <summary>
		/// Seconds after the last update is received to extrapolate for.
		/// Bigger values make the transform less likely to jitter during lag spikes but can introduce rubber banding.
		/// </summary>
		[FormerlySerializedAs("ReceiveExtrapolate")]
		[Tooltip("Seconds after the last update is received to extrapolate for.")]
		public float ReceiveExtrapolate;

		/// <summary>
		/// How many seconds to delay authority transfer for to allow smooth transitions.
		/// </summary>
		[FormerlySerializedAs("ReceiveAuthorityDelay")]
		[Tooltip("How many seconds to delay authority transfer for to allow smooth transitions.")]
		public float ReceiveAuthorityDelay;

		/// <summary>
		/// Maximum seconds between two updates still allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapDuration")]
		[Tooltip("Maximum seconds between two updates still allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapDuration;

		/// <summary>
		/// Maximum rotation angle allowed to interpolate before teleporting. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapRotation")]
		[Tooltip("Maximum rotation angle allowed to interpolate before teleporting. Zero to disable.")]
		public float ReceiveSnapRotation;

		/// <summary>
		/// Maximum distance allowed to interpolate before teleporting. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapPosition")]
		[Tooltip("Maximum distance allowed to interpolate before teleporting. Zero to disable.")]
		public float ReceiveSnapPosition;

		/// <summary>
		/// Maximum scale difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapScale")]
		[Tooltip("Maximum scale difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapScale;

		/// <summary>
		/// Maximum velocity difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapVelocity")]
		[Tooltip("Maximum velocity difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapVelocity;

		/// <summary>
		/// Maximum angular velocity difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapAngularVelocity")]
		[Tooltip("Maximum angular velocity difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapAngularVelocity;

		/// <summary>
		/// Maximum RectTransform anchor difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapRectAnchors")]
		[Tooltip("Maximum RectTransform anchor difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapRectAnchors;

		/// <summary>
		/// Maximum RectTransform size difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapRectSizeDelta")]
		[Tooltip("Maximum RectTransform size difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapRectSizeDelta;

		/// <summary>
		/// Maximum RectTransform pivot difference allowed to interpolate before snapping. Zero to disable.
		/// </summary>
		[FormerlySerializedAs("ReceiveSnapRectPivot")]
		[Tooltip("Maximum RectTransform pivot difference allowed to interpolate before snapping. Zero to disable.")]
		public float ReceiveSnapRectPivot;

		[Header("Send Configuration")]

		/// <summary>
		/// Minimum number of seconds to wait before sending an update.
		/// </summary>
		[FormerlySerializedAs("SendIntervalMin")]
		[Tooltip("Minimum number of seconds to wait before sending an update.")]
		public float SendIntervalMin;

		/// <summary>
		/// Maximum number of seconds to wait before sending an update.
		/// </summary>
		[FormerlySerializedAs("SendIntervalMax")]
		[Tooltip("Maximum number of seconds to wait before sending an update.")]
		public float SendIntervalMax;

		/// <summary>
		/// Minimum amount rotation angle (in radians) is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdRotation")]
		[Tooltip("Minimum amount rotation angle (in radians) is able to change before an update is sent.")]
		public float SendThresholdRotation;

		/// <summary>
		/// Minimum distance transform is able to move before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdPosition")]
		[Tooltip("Minimum distance transform is able to move before an update is sent.")]
		public float SendThresholdPosition;

		/// <summary>
		/// Minimum amount transform is able to scale before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdScale")]
		[Tooltip("Minimum amount transform is able to scale before an update is sent.")]
		public float SendThresholdScale;

		/// <summary>
		/// Minimum amount velocity is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdVelocity")]
		[Tooltip("Minimum amount velocity is able to change before an update is sent.")]
		public float SendThresholdVelocity;

		/// <summary>
		/// Minimum amount angular velocity is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdAngularVelocity")]
		[Tooltip("Minimum amount angular velocity is able to change before an update is sent.")]
		public float SendThresholdAngularVelocity;

		/// <summary>
		/// Minimum RectTransform anchor is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdRectAnchors")]
		[Tooltip("Minimum amount RectTransform anchor is able to change before an update is sent.")]
		public float SendThresholdRectAnchors;

		/// <summary>
		/// Minimum amount RectTransform size is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdRectSizeDelta")]
		[Tooltip("Minimum amount RectTransform size is able to change before an update is sent.")]
		public float SendThresholdRectSizeDelta;

		/// <summary>
		/// Minimum amount RectTransform pivot is able to change before an update is sent.
		/// </summary>
		[FormerlySerializedAs("SendThresholdRectPivot")]
		[Tooltip("Minimum amount RectTransform pivot is able to change before an update is sent.")]
		public float SendThresholdRectPivot;

		/// <summary>
		/// Should remote peer extrapolation be taken into account when checking for thresholds.
		/// </summary>
		[FormerlySerializedAs("SendThresholdExtrapolate")]
		[Tooltip("Should remote peer extrapolation be taken into account when checking for thresholds.")]
		public bool SendThresholdExtrapolate;

		/// <summary>
		/// True if a rigidbody is attached.
		/// </summary>
		public bool HasRigidbody => Rigidbody != null || Rigidbody2D != null;

		/// <summary>
		/// True if a RectTransform is attached.
		/// </summary>
		public bool HasRectTransform => RectTransform != null;

		/// <summary>
		/// Send updates to remote peers.
		/// </summary>
		public bool IsAuthority => Authority;

		// State sent over the network
		private struct SyncState {
			public HostTimestamp Time;
			public SyncHeader Header;
			public Quaternion Rotation;
			public Vector3 Position;
			public Vector3 Scale;
			public Vector3 Velocity;
			public Vector3 AngularVelocity;
			public Vector2 RectAnchorMin;
			public Vector2 RectAnchorMax;
			public Vector2 RectSizeDelta;
			public Vector2 RectPivot;
		}

		[Flags]
		private enum SyncHeader : byte {
			None = 0b00000000,
			Rotation = 0b00000001,
			Position = 0b00000010,
			Scale = 0b00000100,
			Velocity = 0b00001000,
			AngularVelocity = 0b00010000,
			RectAnchors = 0b00100000,
			RectSizeDelta = 0b01000000,
			RectPivot = 0b10000000,
			All = 0b11111111,
		}

		// Resources
		private bool RollbackEnabled;
		private SyncState RollbackState;
		private ReaderWriterLockSlim ReceiveLock;
		private SyncState[] ReceiveArray;
		private int ReceiveIndexLast;
		private ReaderWriterLockSlim SendLock;
		private SyncState SendPrevState;
		private SyncState SendLastState;
		private HostTimestamp SendTimeRotation;
		private HostTimestamp SendTimePosition;
		private HostTimestamp SendTimeScale;
		private HostTimestamp SendTimeVelocity;
		private HostTimestamp SendTimeAngularVelocity;
		private HostTimestamp SendTimeRectAnchors;
		private HostTimestamp SendTimeRectSizeDelta;
		private HostTimestamp SendTimeRectPivot;
		private HostTimestamp AuthorityLoseTime;
		private object AuthorityLock;
		private Peer AuthorityPeer;

		private void Reset() {
			ResetNetworkID();
			Transform = transform;
			RectTransform = GetComponent<RectTransform>();
			Rigidbody = GetComponent<Rigidbody>();
			Rigidbody2D = GetComponent<Rigidbody2D>();
			SyncMethod = NetworkSyncModeMethod.Update;
			SyncRotation = NetworkSyncModeVector3.XYZ;
			SyncPosition = NetworkSyncModeVector3.XYZ;
			SyncScale = NetworkSyncModeVector3.XYZ;
			SyncVelocity = HasRigidbody ? NetworkSyncModeVector3.XYZ : NetworkSyncModeVector3.None;
			SyncAngularVelocity = HasRigidbody ? NetworkSyncModeVector3.XYZ : NetworkSyncModeVector3.None;
			SyncRectAnchorMin = HasRectTransform ? NetworkSyncModeVector2.XY : NetworkSyncModeVector2.None;
			SyncRectAnchorMax = HasRectTransform ? NetworkSyncModeVector2.XY : NetworkSyncModeVector2.None;
			SyncRectSizeDelta = HasRectTransform ? NetworkSyncModeVector2.XY : NetworkSyncModeVector2.None;
			SyncRectPivot = HasRectTransform ? NetworkSyncModeVector2.XY : NetworkSyncModeVector2.None;
			SyncChannel = 0;
			SyncLocalTransform = false;
			Authority = false;
			ReceiveDelay = 0.1f;
			ReceiveDuration = 1.2f;
			ReceiveExtrapolate = 0.4f;
			ReceiveAuthorityDelay = 0.4f;
			ReceiveSnapDuration = 0.8f;
			ReceiveSnapRotation = 60f;
			ReceiveSnapPosition = 2f;
			ReceiveSnapScale = 1f;
			ReceiveSnapVelocity = 1f;
			ReceiveSnapAngularVelocity = 10f;
			ReceiveSnapRectAnchors = 0.05f;
			ReceiveSnapRectSizeDelta = 0.25f;
			ReceiveSnapRectPivot = 0.05f;
			SendIntervalMin = 0.030f;
			SendIntervalMax = 0.300f;
			SendThresholdRotation = 0.03f;
			SendThresholdPosition = 0.002f;
			SendThresholdScale = 0.001f;
			SendThresholdVelocity = 0.001f;
			SendThresholdAngularVelocity = 0.01f;
			SendThresholdRectAnchors = 0.001f;
			SendThresholdRectSizeDelta = 0.01f;
			SendThresholdRectPivot = 0.001f;
			SendThresholdExtrapolate = true;
		}

		private void Awake() {

			// Check if transform is set
			if (Transform == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkTransform - {0}] Transform not set.", NetworkID
					), this);
				}
				enabled = false;
				return;
			}

			// Initialize resources
			RollbackEnabled = false;
			RollbackState = GetLocalState(Host.Now);
			ReceiveLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			ReceiveArray = null;
			ReceiveIndexLast = 0;
			SendLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			SendPrevState = RollbackState;
			SendLastState = RollbackState;
			SendTimeRotation = HostTimestamp.MinValue;
			SendTimePosition = HostTimestamp.MinValue;
			SendTimeScale = HostTimestamp.MinValue;
			SendTimeVelocity = HostTimestamp.MinValue;
			SendTimeAngularVelocity = HostTimestamp.MinValue;
			SendTimeRectAnchors = HostTimestamp.MinValue;
			SendTimeRectSizeDelta = HostTimestamp.MinValue;
			SendTimeRectPivot = HostTimestamp.MinValue;
			AuthorityLoseTime = HostTimestamp.MinValue;
			AuthorityLock = new object();
			AuthorityPeer = null;

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
			if (Transform == null) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkTransform - {0}] Transform removed.", NetworkID
					), this);
				}
				enabled = false;
				return;
			}

			// If not registered, do nothing
			if (!IsRegisteredOnNetwork) {
				return;
			}

			// Send or receive updates
			if (Authority) {
				SyncUpdateSend();
			} else {
				SyncUpdateReceive();
			}

		}

		private void SyncUpdateSend() {
			try {
				SendLock.EnterUpgradeableReadLock();

				// Get current time
				HostTimestamp now = Host.Now;
			
				// If not enough time has passed to send an update, do nothing
				if ((now - SendLastState.Time).Seconds < SendIntervalMin) {
					return;
				}

				// Get current state
				SyncState local = GetLocalState(now);
				SyncHeader header = SyncHeader.None;

				// Calculate extrapolation factor for remote peers to check thresholds
				float factor = 1f;
				if (SendThresholdExtrapolate && SendLastState.Time > SendPrevState.Time) {
					HostTimespan duration = SendLastState.Time - SendPrevState.Time;
					factor = (float)((local.Time - SendPrevState.Time) / duration);
				}

				// Check rotation threshold
				if (SyncRotation != NetworkSyncModeVector3.None) {
					Quaternion rotationValue = Quaternion.SlerpUnclamped(SendPrevState.Rotation, SendLastState.Rotation, factor);
					float rotationDelta = Quaternion.Angle(local.Rotation, rotationValue);
					float timeDelta = (float)(local.Time - SendTimeRotation).Seconds;
					if (rotationDelta > SendThresholdRotation) header |= SyncHeader.Rotation;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.Rotation;
				}

				// Check position threshold
				if (SyncPosition != NetworkSyncModeVector3.None) {
					Vector3 positionValue = Vector3.LerpUnclamped(SendPrevState.Position, SendLastState.Position, factor);
					float positionDelta = Vector3.Distance(local.Position, positionValue);
					float timeDelta = (float)(local.Time - SendTimePosition).Seconds;
					if (positionDelta > SendThresholdPosition) header |= SyncHeader.Position;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.Position;
				}

				// Check scale threshold
				if (SyncScale != NetworkSyncModeVector3.None) {
					Vector3 scaleValue = Vector3.LerpUnclamped(SendPrevState.Scale, SendLastState.Scale, factor);
					float scaleDelta = Vector3.Distance(local.Scale, scaleValue);
					float timeDelta = (float)(local.Time - SendTimeScale).Seconds;
					if (scaleDelta > SendThresholdScale) header |= SyncHeader.Scale;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.Scale;
				}

				// Check velocity threshold
				if (SyncVelocity != NetworkSyncModeVector3.None && HasRigidbody) {
					Vector3 velocityValue = Vector3.LerpUnclamped(SendPrevState.Velocity, SendLastState.Velocity, factor);
					float velocityDelta = Vector3.Distance(local.Velocity, velocityValue);
					float timeDelta = (float)(local.Time - SendTimeVelocity).Seconds;
					if (velocityDelta > SendThresholdVelocity) header |= SyncHeader.Velocity;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.Velocity;
				}

				// Check angular velocity threshold
				if (SyncAngularVelocity != NetworkSyncModeVector3.None && HasRigidbody) {
					Vector3 angularVelocityValue = Vector3.LerpUnclamped(SendPrevState.AngularVelocity, SendLastState.AngularVelocity, factor);
					float angularVelocityDelta = Vector3.Distance(local.AngularVelocity, angularVelocityValue);
					float timeDelta = (float)(local.Time - SendTimeAngularVelocity).Seconds;
					if (angularVelocityDelta > SendThresholdAngularVelocity) header |= SyncHeader.AngularVelocity;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.AngularVelocity;
				}

				// Check rect anchors
				if ((SyncRectAnchorMax != NetworkSyncModeVector2.None || SyncRectAnchorMin != NetworkSyncModeVector2.None) && HasRectTransform) {
					Vector2 anchorMinValue = Vector2.LerpUnclamped(SendPrevState.RectAnchorMin, SendLastState.RectAnchorMin, factor);
					Vector2 anchorMaxValue = Vector2.LerpUnclamped(SendPrevState.RectAnchorMax, SendLastState.RectAnchorMax, factor);
					float anchorMinDelta = Vector2.Distance(local.RectAnchorMin, anchorMinValue);
					float anchorMaxDelta = Vector2.Distance(local.RectAnchorMax, anchorMaxValue);
					float anchorDelta = Math.Max(anchorMinDelta, anchorMaxDelta);
					float timeDelta = (float)(local.Time - SendTimeRectAnchors).Seconds;
					if (anchorDelta > SendThresholdRectAnchors) header |= SyncHeader.RectAnchors;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.RectAnchors;
				}

				// Check rect size
				if (SyncRectSizeDelta != NetworkSyncModeVector2.None && HasRectTransform) {
					Vector2 sizeValue = Vector2.LerpUnclamped(SendPrevState.RectSizeDelta, SendLastState.RectSizeDelta, factor);
					float sizeDelta = Vector2.Distance(local.RectSizeDelta, sizeValue);
					float timeDelta = (float)(local.Time - SendTimeRectSizeDelta).Seconds;
					if (sizeDelta > SendThresholdRectSizeDelta) header |= SyncHeader.RectSizeDelta;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.RectSizeDelta;
				}

				// Check rect pivot
				if (SyncRectPivot != NetworkSyncModeVector2.None && HasRectTransform) {
					Vector2 sizeValue = Vector2.LerpUnclamped(SendPrevState.RectPivot, SendLastState.RectPivot, factor);
					float sizeDelta = Vector2.Distance(local.RectPivot, sizeValue);
					float timeDelta = (float)(local.Time - SendTimeRectPivot).Seconds;
					if (sizeDelta > SendThresholdRectPivot) header |= SyncHeader.RectPivot;
					if (timeDelta > SendIntervalMax) header |= SyncHeader.RectPivot;
				}

				// Send message if needed
				if (header != SyncHeader.None) {
					try {
						SendLock.EnterWriteLock();

						// Update send times
						if (header.HasFlag(SyncHeader.Rotation)) SendTimeRotation = local.Time;
						if (header.HasFlag(SyncHeader.Position)) SendTimePosition = local.Time;
						if (header.HasFlag(SyncHeader.Scale)) SendTimeScale = local.Time;
						if (header.HasFlag(SyncHeader.Velocity)) SendTimeVelocity = local.Time;
						if (header.HasFlag(SyncHeader.AngularVelocity)) SendTimeAngularVelocity = local.Time;
						if (header.HasFlag(SyncHeader.RectAnchors)) SendTimeRectAnchors = local.Time;
						if (header.HasFlag(SyncHeader.RectSizeDelta)) SendTimeRectSizeDelta = local.Time;
						if (header.HasFlag(SyncHeader.RectPivot)) SendTimeRectPivot = local.Time;

						// Save state
						SendPrevState = SendLastState;
						SendLastState = local;

						// Save state to the received array
						local.Header = SyncHeader.All;
						AddReceiveState(local);

						// Send message to all peers
						local.Header = header;
						SendNetworkMessage(new NetworkMessageState(this, local, SyncChannel));

					} finally {
						SendLock.ExitWriteLock();
					}

				}

			} finally {
				SendLock.ExitUpgradeableReadLock();
			}
		}

		private void SyncUpdateReceive() {

			// Get current time
			HostTimestamp now = Host.Now;

			// Check if we recently lost authority
			if (ReceiveAuthorityDelay > 0f && (now - AuthorityLoseTime).Seconds < ReceiveAuthorityDelay) {

				// Interpolate between local and remote state for a smooth transition
				float factor = (float)((now - AuthorityLoseTime).Seconds / ReceiveAuthorityDelay);
				HostTimestamp time = now - HostTimespan.FromSeconds(ReceiveDelay * factor);
				if (time < AuthorityLoseTime) time = AuthorityLoseTime;
				SyncState state = FindReceiveState(time);
				SetLocalState(state, 1f - factor);

			} else {

				// Find state in the past and update
				HostTimestamp time = now - HostTimespan.FromSeconds(ReceiveDelay);
				SyncState state = FindReceiveState(time);
				SetLocalState(state);

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
							"[SuperNet] [NetworkTransform - {0}] Recieved update from {1}. Ignoring.",
							NetworkID, peer.Remote
						), this);
					}
					return;
				}

				// Save authority peer
				Interlocked.Exchange(ref AuthorityPeer, peer);

			}

			// Read state from message and save it
			SyncState state = ReadState(reader, info.Timestamp);
			HostTimespan late = AddReceiveState(state);

			// Warn if the received state was outdated
			if (late > HostTimespan.Zero && NetworkManager.LogExceptions) {
				Debug.LogWarning(string.Format(
					"[SuperNet] [NetworkTransform - {0}] Recieved outdated update from {1} for {2} seconds.",
					NetworkID, peer.Remote, late.Seconds
				), this);
			}

		}

		void INetworkRollbackable.OnNetworkRollbackBegin(HostTimestamp time, Peer peer) {

			// Rollback can only be done in the main thread 
			if (Thread.CurrentThread != NetworkManager.GetMainThread()) {
				if (NetworkManager.LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [NetworkTransform - {0}] Asynchronous network rollback. Ignoring.",
						NetworkID
					), this);
				}
				return;
			}

			// Save rollback state
			if (!RollbackEnabled) {
				RollbackState = GetLocalState(Host.Now);
				RollbackEnabled = true;
			}

			lock (AuthorityLock) {

				if (peer == null) {

					// No peer provided, rollback for the provided time
					
				} else if (peer == AuthorityPeer) {

					// Rollback is performed for the peer that has authority
					// The authority peer sees this transform without any delays
					// No additional delay is needed
					
				} else {

					// Rollback is performed for the peer that doesn't have authority
					// The peer sees this transform ReceiveDelay seconds in the past
					// ReceiveDelay additional seconds is added
					time -= HostTimespan.FromSeconds(ReceiveDelay);
					
				}

			}

			// Find receive state and update
			SyncState state = FindReceiveState(time);
			SetLocalState(state);

		}

		void INetworkRollbackable.OnNetworkRollbackEnd() {

			// Rollback can only be done in the main thread 
			if (Thread.CurrentThread != NetworkManager.GetMainThread()) {
				return;
			}

			// If rollback is not enabled, do nothing
			if (!RollbackEnabled) {
				return;
			}

			// Revert rollback state
			RollbackEnabled = false;
			SetLocalState(RollbackState);

		}

		void INetworkAuthoritative.OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp) {

			// Update authority and resync state
			SetAuthority(authority, timestamp);

		}

		/// <summary>
		/// Update authority on this component.
		/// </summary>
		/// <param name="authority">Authority to set.</param>
		public void SetAuthority(bool authority) {

			// Update authority and resync state
			SetAuthority(authority, Host.Now);

		}

		private void SetAuthority(bool authority, HostTimestamp timestamp) {
			lock (AuthorityLock) {

				// Check if authority changed
				if (Authority == authority) {
					return;
				}

				// Log
				if (NetworkManager.LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [NetworkTransform - {0}] Authority {1}.",
						NetworkID, authority ? "gained" : "lost"
					), this);
				}

				// Update authority
				Authority = authority;

				// Remove authority peer if we are now authority
				if (Authority) {
					Interlocked.Exchange(ref AuthorityPeer, null);
				}

				// Check if we just lost authority
				if (!authority) {

					// Save current time for a smooth transition
					AuthorityLoseTime = Host.Now;

					// Remove all states after authority transition
					try {
						ReceiveLock.EnterWriteLock();
						if (ReceiveArray != null && ReceiveArray.Length > 0) {
							int last = ReceiveIndexLast;
							while (ReceiveArray[last].Time > timestamp) {
								ReceiveArray[last].Time = HostTimestamp.MinValue;
								last = (last + ReceiveArray.Length - 1) % ReceiveArray.Length;
							}
							ReceiveIndexLast = last;
						}
					} finally {
						ReceiveLock.ExitWriteLock();
					}

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
			Run(() => {

				// Get local state
				SyncState state = GetLocalState(Host.Now);

				if (peer == null) {
					// Send state to all peers
					SendNetworkMessage(new NetworkMessageState(this, state, SyncChannel));
				} else {
					// Send state to a specific peer
					SendNetworkMessage(new NetworkMessageState(this, state, SyncChannel), peer);
				}

			});
		}

		private bool FindReceiveIndex(SyncHeader flag, HostTimestamp time, out int prev, out int next, out float factor) {

			// Initialize output parameters
			prev = ReceiveIndexLast;
			next = ReceiveIndexLast;
			factor = 0f;

			// Find the last value that has the flag
			int last = ReceiveIndexLast;
			while (!ReceiveArray[last].Header.HasFlag(flag)) {
				last = (last + ReceiveArray.Length - 1) % ReceiveArray.Length;
				if (last == ReceiveIndexLast) {
					return false;
				}
			}

			// Find prev and next index
			prev = next = last;
			do {
				int search = prev;
				do {
					search = (search + ReceiveArray.Length - 1) % ReceiveArray.Length;
					if (search == ReceiveIndexLast) break;
				} while (!ReceiveArray[search].Header.HasFlag(flag));
				if (search == ReceiveIndexLast) {
					break;
				} else if (prev == next) {
					prev = search;
				} else {
					next = prev;
					prev = search;
				}
			} while (ReceiveArray[prev].Time > time);

			// Get duration between values
			HostTimespan duration = ReceiveArray[next].Time - ReceiveArray[prev].Time;

			// If only one value available, use it
			if (prev == next || duration <= HostTimespan.Zero) {
				return true;
			}

			// Get interpolation/extrapolation factor between the two values
			factor = (float)((time - ReceiveArray[prev].Time) / duration);

			// If duration between received values is too big, snap to a single value
			if (ReceiveSnapDuration > 0f && duration.Seconds > ReceiveSnapDuration) {
				factor = factor < 1f ? 0f : 1f;
				return true;
			}

			// Stop extrapolating if enough time passed without an update
			if ((time - ReceiveArray[next].Time).Seconds > ReceiveExtrapolate) {
				factor = 1f;
				return true;
			}

			// Finish
			return true;

		}

		private SyncState FindReceiveState(HostTimestamp time) {
			try {
				ReceiveLock.EnterReadLock();

				// Create empty state
				SyncState state;
				state.Time = time;
				state.Header = SyncHeader.None;
				state.Rotation = Quaternion.identity;
				state.Position = Vector3.zero;
				state.Scale = Vector3.zero;
				state.Velocity = Vector3.zero;
				state.AngularVelocity = Vector3.zero;
				state.RectAnchorMin = Vector2.zero;
				state.RectAnchorMax = Vector2.zero;
				state.RectSizeDelta = Vector2.zero;
				state.RectPivot = Vector2.zero;

				// If no received states yet, do nothing
				if (ReceiveArray == null || ReceiveArray.Length <= 1) {
					return state;
				}

				// Interplate/Extrapolate rotation
				if (SyncRotation != NetworkSyncModeVector3.None) {
					bool found = FindReceiveIndex(SyncHeader.Rotation, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.Rotation;
						Quaternion prevRotation = ReceiveArray[prev].Rotation;
						Quaternion nextRotation = ReceiveArray[next].Rotation;
						if (ReceiveSnapRotation > 0f && Quaternion.Angle(prevRotation, nextRotation) > ReceiveSnapRotation) {
							state.Rotation = (factor < 1f ? prevRotation : nextRotation);
						} else {
							state.Rotation = Quaternion.SlerpUnclamped(prevRotation, nextRotation, factor);
						}
					}
				}

				// Interplate/Extrapolate position
				if (SyncPosition != NetworkSyncModeVector3.None) {
					bool found = FindReceiveIndex(SyncHeader.Position, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.Position;
						Vector3 prevPosition = ReceiveArray[prev].Position;
						Vector3 nextPosition = ReceiveArray[next].Position;
						if (ReceiveSnapPosition > 0f && Vector3.Distance(prevPosition, nextPosition) > ReceiveSnapPosition) {
							state.Position = factor < 1f ? prevPosition : nextPosition;
						} else {
							state.Position = Vector3.LerpUnclamped(prevPosition, nextPosition, factor);
						}
					}
				}

				// Interplate/Extrapolate scale
				if (SyncScale != NetworkSyncModeVector3.None) {
					bool found = FindReceiveIndex(SyncHeader.Scale, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.Scale;
						Vector3 prevScale = ReceiveArray[prev].Scale;
						Vector3 nextScale = ReceiveArray[next].Scale;
						if (ReceiveSnapScale > 0f && Vector3.Distance(prevScale, nextScale) > ReceiveSnapScale) {
							state.Scale = factor < 1f ? prevScale : nextScale;
						} else {
							state.Scale = Vector3.LerpUnclamped(prevScale, nextScale, factor);
						}
					}
				}

				// Interplate/Extrapolate velocity
				if (SyncVelocity != NetworkSyncModeVector3.None && HasRigidbody) {
					bool found = FindReceiveIndex(SyncHeader.Velocity, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.Velocity;
						Vector3 prevVelocity = ReceiveArray[prev].Velocity;
						Vector3 nextVelocity = ReceiveArray[next].Velocity;
						if (ReceiveSnapVelocity > 0f && Vector3.Distance(prevVelocity, nextVelocity) > ReceiveSnapVelocity) {
							state.Velocity = factor < 1f ? prevVelocity : nextVelocity;
						} else {
							state.Velocity = Vector3.LerpUnclamped(prevVelocity, nextVelocity, factor);
						}
					}
				}

				// Interplate/Extrapolate angular velocity
				if (SyncAngularVelocity != NetworkSyncModeVector3.None && HasRigidbody) {
					bool found = FindReceiveIndex(SyncHeader.AngularVelocity, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.AngularVelocity;
						Vector3 prevAngularVelocity = ReceiveArray[prev].AngularVelocity;
						Vector3 nextAngularVelocity = ReceiveArray[next].AngularVelocity;
						if (ReceiveSnapAngularVelocity > 0f && Vector3.Distance(prevAngularVelocity, nextAngularVelocity) > ReceiveSnapAngularVelocity) {
							state.AngularVelocity = factor < 1f ? prevAngularVelocity : nextAngularVelocity;
						} else {
							state.AngularVelocity = Vector3.LerpUnclamped(prevAngularVelocity, nextAngularVelocity, factor);
						}
					}
				}

				// Interplate/Extrapolate rect anchors
				bool syncRectAnchorMinMax = SyncRectAnchorMin != NetworkSyncModeVector2.None || SyncRectAnchorMax != NetworkSyncModeVector2.None;
				if (syncRectAnchorMinMax && HasRectTransform) {
					bool found = FindReceiveIndex(SyncHeader.RectAnchors, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.RectAnchors;
						Vector2 prevAnchorMin = ReceiveArray[prev].RectAnchorMin;
						Vector2 prevAnchorMax = ReceiveArray[prev].RectAnchorMax;
						Vector2 nextAnchorMin = ReceiveArray[next].RectAnchorMin;
						Vector2 nextAnchorMax = ReceiveArray[next].RectAnchorMax;
						float distanceAnchorMin = Vector2.Distance(prevAnchorMin, nextAnchorMin);
						float distanceAnchorMax = Vector2.Distance(prevAnchorMax, nextAnchorMax);
						if (ReceiveSnapRectAnchors > 0f && Mathf.Max(distanceAnchorMin, distanceAnchorMax) > ReceiveSnapRectAnchors) {
							state.RectAnchorMin = factor < 1f ? prevAnchorMin : nextAnchorMin;
							state.RectAnchorMax = factor < 1f ? prevAnchorMax : nextAnchorMax;
						} else {
							state.RectAnchorMin = Vector2.LerpUnclamped(prevAnchorMin, nextAnchorMin, factor);
							state.RectAnchorMax = Vector2.LerpUnclamped(prevAnchorMax, nextAnchorMax, factor);
						}
					}
				}

				// Interplate/Extrapolate rect size
				if (SyncRectSizeDelta != NetworkSyncModeVector2.None && HasRectTransform) {
					bool found = FindReceiveIndex(SyncHeader.RectSizeDelta, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.RectSizeDelta;
						Vector2 prevSizeDelta = ReceiveArray[prev].RectSizeDelta;
						Vector2 nextSizeDelta = ReceiveArray[next].RectSizeDelta;
						if (ReceiveSnapRectSizeDelta > 0f && Vector2.Distance(prevSizeDelta, nextSizeDelta) > ReceiveSnapRectSizeDelta) {
							state.RectSizeDelta = factor < 1f ? prevSizeDelta : nextSizeDelta;
						} else {
							state.RectSizeDelta = Vector2.LerpUnclamped(prevSizeDelta, nextSizeDelta, factor);
						}
					}
				}

				// Interplate/Extrapolate rect pivot
				if (SyncRectPivot != NetworkSyncModeVector2.None && HasRectTransform) {
					bool found = FindReceiveIndex(SyncHeader.RectPivot, time, out int prev, out int next, out float factor);
					if (found) {
						state.Header |= SyncHeader.RectPivot;
						Vector2 prevPivot = ReceiveArray[prev].RectPivot;
						Vector2 nextPivot = ReceiveArray[next].RectPivot;
						if (ReceiveSnapRectPivot > 0f && Vector2.Distance(prevPivot, nextPivot) > ReceiveSnapRectPivot) {
							state.RectPivot = factor < 1f ? prevPivot : nextPivot;
						} else {
							state.RectPivot = Vector2.LerpUnclamped(prevPivot, nextPivot, factor);
						}
					}
				}

				// Return state
				return state;

			} finally {
				ReceiveLock.ExitReadLock();
			}
		}

		private HostTimespan AddReceiveState(SyncState state) {
			try {
				ReceiveLock.EnterWriteLock();

				// If no received states yet, just save
				if (ReceiveArray == null || ReceiveArray.Length <= 0) {
					ReceiveIndexLast = 0;
					ReceiveArray = new SyncState[] { state };
					return HostTimespan.Zero;
				}

				// Get the latest timestamp
				HostTimestamp timeAdd = state.Time;
				HostTimestamp timeLast = ReceiveArray[ReceiveIndexLast].Time;
				HostTimestamp timeLatest = timeLast > timeAdd ? timeLast : timeAdd;

				// Duration to keep received states for
				HostTimespan keep = HostTimespan.FromSeconds(ReceiveDuration);
				if (keep <= HostTimespan.Zero) {
					keep = HostTimespan.FromSeconds(ReceiveDelay + ReceiveExtrapolate + Math.Max(ReceiveDelay + ReceiveExtrapolate, ReceiveSnapDuration));
				}

				// Resize receive array if needed
				int next = (ReceiveIndexLast + 1) % ReceiveArray.Length;
				if (timeLatest - ReceiveArray[next].Time <= keep) {
					SyncState[] copy = new SyncState[ReceiveArray.Length + 1];
					for (int i = 0; i <= next; i++) copy[i] = ReceiveArray[i];
					for (int i = next + 1; i < copy.Length; i++) copy[i] = ReceiveArray[i - 1];
					if (ReceiveIndexLast >= next) ReceiveIndexLast++;
					ReceiveArray = copy;
				}

				// Find index of insertion
				int last = ReceiveIndexLast;
				while (ReceiveArray[last].Time > state.Time) {
					int prev = (last + ReceiveArray.Length - 1) % ReceiveArray.Length;
					if (ReceiveArray[prev].Time < ReceiveArray[last].Time) {
						ReceiveArray[next] = ReceiveArray[last];
						next = last;
						last = prev;
					} else {
						ReceiveArray[next] = ReceiveArray[last];
						next = last;
						break;
					}
				}

				// Save state
				ReceiveIndexLast = (ReceiveIndexLast + 1) % ReceiveArray.Length;
				ReceiveArray[next] = state;

				// Return duration between added state and last state
				return timeLast - timeAdd;

			} finally {
				ReceiveLock.ExitWriteLock();
			}
		}

		private SyncState GetLocalState(HostTimestamp time) {

			// Set current time & header
			SyncState state;
			state.Header = SyncHeader.All;
			state.Time = time;

			// Get transform state
			if (SyncLocalTransform) {
				state.Rotation = Transform.localRotation;
				state.Position = Transform.localPosition;
			} else {
				state.Rotation = Transform.rotation;
				state.Position = Transform.position;
			}
			state.Scale = Transform.localScale;

			// Get rigidbody state
			state.Velocity = Vector3.zero;
			state.AngularVelocity = Vector3.zero;
			if (Rigidbody != null) {
				state.Velocity = Rigidbody.velocity;
				state.AngularVelocity = Rigidbody.angularVelocity;
			} else if (Rigidbody2D != null) {
				state.Velocity = Rigidbody2D.velocity;
				state.AngularVelocity = new Vector3(Rigidbody2D.angularVelocity, 0f, 0f);
			}

			// Get rect state
			state.RectAnchorMin = Vector2.zero;
			state.RectAnchorMax = Vector2.zero;
			state.RectSizeDelta = Vector2.zero;
			state.RectPivot = Vector2.zero;
			if (RectTransform != null) {
				state.RectAnchorMin = RectTransform.anchorMin;
				state.RectAnchorMax = RectTransform.anchorMax;
				state.RectSizeDelta = RectTransform.sizeDelta;
				state.RectPivot = RectTransform.pivot;
			}

			// Return state
			return state;

		}

		private void SetLocalState(SyncState state, float factor = 0f) {

			// Set rotation
			if (state.Header.HasFlag(SyncHeader.Rotation)) {
				if (SyncRotation != NetworkSyncModeVector3.XYZ) {
					Vector3 euler = state.Rotation.eulerAngles;
					if (!SyncRotation.HasFlag(NetworkSyncModeVector3.X)) euler.x = Transform.localRotation.eulerAngles.x;
					if (!SyncRotation.HasFlag(NetworkSyncModeVector3.Y)) euler.y = Transform.localRotation.eulerAngles.y;
					if (!SyncRotation.HasFlag(NetworkSyncModeVector3.Z)) euler.z = Transform.localRotation.eulerAngles.z;
					state.Rotation = Quaternion.Euler(euler);
				}
				if (SyncLocalTransform) {
					if (Rigidbody != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Quaternion local = Quaternion.Slerp(state.Rotation, Transform.localRotation, factor);
						if (Transform.parent == null) {
							Rigidbody.MoveRotation(local);
						} else {
							Rigidbody.MoveRotation(Transform.parent.rotation * local);
						}
					} else if (Rigidbody2D != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Quaternion local = Quaternion.Slerp(state.Rotation, Transform.localRotation, factor);
						if (Transform.parent == null) {
							Rigidbody2D.MoveRotation(local.eulerAngles.z);
						} else {
							Rigidbody2D.MoveRotation((Transform.parent.rotation * local).eulerAngles.z);
						}
					} else {
						Transform.localRotation = Quaternion.Slerp(state.Rotation, Transform.localRotation, factor);
					}
				} else if (Rigidbody != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
					Rigidbody.MoveRotation(Quaternion.Slerp(state.Rotation, Rigidbody.rotation, factor));
				} else if (Rigidbody2D != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
					Rigidbody2D.MoveRotation(Mathf.Lerp(state.Rotation.eulerAngles.z, Rigidbody2D.rotation, factor));
				} else {
					Transform.rotation = Quaternion.Slerp(state.Rotation, Transform.rotation, factor);
				}
			}

			// Set position
			if (state.Header.HasFlag(SyncHeader.Position)) {
				if (SyncLocalTransform) {
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.X)) state.Position.x = Transform.localPosition.x;
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.Y)) state.Position.y = Transform.localPosition.y;
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.Z)) state.Position.z = Transform.localPosition.z;
					if (Rigidbody != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Vector3 local = Vector3.Lerp(state.Position, Transform.localPosition, factor);
						if (Transform.parent == null) {
							Rigidbody.MovePosition(local);
						} else {
							Rigidbody.MovePosition(Transform.parent.TransformPoint(local));
						}
					} else if (Rigidbody2D != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Vector3 local = Vector3.Lerp(state.Position, Transform.localPosition, factor);
						if (Transform.parent == null) {
							Rigidbody2D.MovePosition(local);
						} else {
							Rigidbody2D.MovePosition(Transform.parent.TransformPoint(local));
						}
					} else {
						Transform.localPosition = Vector3.Lerp(state.Position, Transform.localPosition, factor);
					}
				} else {
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.X)) state.Position.x = Transform.position.x;
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.Y)) state.Position.y = Transform.position.y;
					if (!SyncPosition.HasFlag(NetworkSyncModeVector3.Z)) state.Position.z = Transform.position.z;
					if (Rigidbody != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Rigidbody.MovePosition(Vector3.Lerp(state.Position, Rigidbody.position, factor));
					} else if (Rigidbody2D != null && SyncMethod == NetworkSyncModeMethod.FixedUpdate) {
						Rigidbody2D.MovePosition(Vector3.Lerp(state.Position, Rigidbody2D.position, factor));
					} else {
						Transform.position = Vector3.Lerp(state.Position, Transform.position, factor);
					}
				}
			}

			// Set scale
			if (state.Header.HasFlag(SyncHeader.Scale)) {
				if (!SyncScale.HasFlag(NetworkSyncModeVector3.X)) state.Scale.x = Transform.localScale.x;
				if (!SyncScale.HasFlag(NetworkSyncModeVector3.Y)) state.Scale.y = Transform.localScale.y;
				if (!SyncScale.HasFlag(NetworkSyncModeVector3.Z)) state.Scale.z = Transform.localScale.z;
				Transform.localScale = Vector3.Lerp(state.Scale, Transform.localScale, factor);
			}

			// Set rigidbody velocity
			if (state.Header.HasFlag(SyncHeader.Velocity)) {
				if (Rigidbody != null) {
					if (!SyncVelocity.HasFlag(NetworkSyncModeVector3.X)) state.Velocity.x = Rigidbody.velocity.x;
					if (!SyncVelocity.HasFlag(NetworkSyncModeVector3.Y)) state.Velocity.y = Rigidbody.velocity.y;
					if (!SyncVelocity.HasFlag(NetworkSyncModeVector3.Z)) state.Velocity.z = Rigidbody.velocity.z;
					Rigidbody.velocity = Vector3.Lerp(state.Velocity, Rigidbody.velocity, factor);
				} else if (Rigidbody2D != null) {
					if (!SyncVelocity.HasFlag(NetworkSyncModeVector3.X)) state.Velocity.x = Rigidbody2D.velocity.x;
					if (!SyncVelocity.HasFlag(NetworkSyncModeVector3.Y)) state.Velocity.y = Rigidbody2D.velocity.y;
					Rigidbody2D.velocity = Vector3.Lerp(state.Velocity, Rigidbody2D.velocity, factor);
				}
			}

			// Set rigidbody angular velocity
			if (state.Header.HasFlag(SyncHeader.AngularVelocity)) {
				if (Rigidbody != null) {
					if (!SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.X)) state.AngularVelocity.x = Rigidbody.angularVelocity.x;
					if (!SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Y)) state.AngularVelocity.y = Rigidbody.angularVelocity.y;
					if (!SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Z)) state.AngularVelocity.z = Rigidbody.angularVelocity.z;
					Rigidbody.angularVelocity = Vector3.Lerp(state.AngularVelocity, Rigidbody.angularVelocity, factor);
				} else if (Rigidbody2D != null) {
					if (!SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.X)) state.AngularVelocity.x = Rigidbody2D.angularVelocity;
					Rigidbody2D.angularVelocity = Mathf.Lerp(state.AngularVelocity.x, Rigidbody2D.angularVelocity, factor);
				}
			}

			if (RectTransform != null) {

				// Set rect anchors
				if (state.Header.HasFlag(SyncHeader.RectAnchors)) {
					if (!SyncRectAnchorMin.HasFlag(NetworkSyncModeVector3.X)) state.RectAnchorMin.x = RectTransform.anchorMin.x;
					if (!SyncRectAnchorMin.HasFlag(NetworkSyncModeVector3.Y)) state.RectAnchorMin.y = RectTransform.anchorMin.y;
					if (!SyncRectAnchorMax.HasFlag(NetworkSyncModeVector3.X)) state.RectAnchorMax.x = RectTransform.anchorMax.x;
					if (!SyncRectAnchorMax.HasFlag(NetworkSyncModeVector3.Y)) state.RectAnchorMax.y = RectTransform.anchorMax.y;
					RectTransform.anchorMin = Vector2.Lerp(state.RectAnchorMin, RectTransform.anchorMin, factor);
					RectTransform.anchorMax = Vector2.Lerp(state.RectAnchorMax, RectTransform.anchorMax, factor);
				}

				// Set rect size delta
				if (state.Header.HasFlag(SyncHeader.RectSizeDelta)) {
					if (!SyncRectSizeDelta.HasFlag(NetworkSyncModeVector3.X)) state.RectSizeDelta.x = RectTransform.sizeDelta.x;
					if (!SyncRectSizeDelta.HasFlag(NetworkSyncModeVector3.Y)) state.RectSizeDelta.y = RectTransform.sizeDelta.y;
					RectTransform.sizeDelta = Vector2.Lerp(state.RectSizeDelta, RectTransform.sizeDelta, factor);
				}

				// Set rect pivot
				if (state.Header.HasFlag(SyncHeader.RectPivot)) {
					if (!SyncRectPivot.HasFlag(NetworkSyncModeVector3.X)) state.RectPivot.x = RectTransform.pivot.x;
					if (!SyncRectPivot.HasFlag(NetworkSyncModeVector3.Y)) state.RectPivot.y = RectTransform.pivot.y;
					RectTransform.pivot = Vector2.Lerp(state.RectPivot, RectTransform.pivot, factor);
				}

			}

		}

		private SyncState ReadState(Reader reader, HostTimestamp time) {

			// Create empty state
			SyncState state;
			state.Time = time;
			state.Header = SyncHeader.None;
			state.Rotation = Quaternion.identity;
			state.Position = Vector3.zero;
			state.Scale = Vector3.zero;
			state.Velocity = Vector3.zero;
			state.AngularVelocity = Vector3.zero;
			state.RectAnchorMin = Vector2.zero;
			state.RectAnchorMax = Vector2.zero;
			state.RectSizeDelta = Vector2.zero;
			state.RectPivot = Vector2.zero;
			
			// Read header
			state.Header = (SyncHeader)reader.ReadByte();

			// Read rotation
			if (state.Header.HasFlag(SyncHeader.Rotation)) {
				Vector3 euler = Vector3.zero;
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.X)) euler.x = reader.ReadSingle();
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.Y)) euler.y = reader.ReadSingle();
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.Z)) euler.z = reader.ReadSingle();
				state.Rotation = Quaternion.Euler(euler);
			}

			// Read position
			if (state.Header.HasFlag(SyncHeader.Position)) {
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.X)) state.Position.x = reader.ReadSingle();
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.Y)) state.Position.y = reader.ReadSingle();
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.Z)) state.Position.z = reader.ReadSingle();
			}

			// Read scale
			if (state.Header.HasFlag(SyncHeader.Scale)) {
				if (SyncScale.HasFlag(NetworkSyncModeVector3.X)) state.Scale.x = reader.ReadSingle();
				if (SyncScale.HasFlag(NetworkSyncModeVector3.Y)) state.Scale.y = reader.ReadSingle();
				if (SyncScale.HasFlag(NetworkSyncModeVector3.Z)) state.Scale.z = reader.ReadSingle();
			}

			// Read velocity
			if (state.Header.HasFlag(SyncHeader.Velocity)) {
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.X)) state.Velocity.x = reader.ReadSingle();
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.Y)) state.Velocity.y = reader.ReadSingle();
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.Z)) state.Velocity.z = reader.ReadSingle();
			}

			// Read angular velocity
			if (state.Header.HasFlag(SyncHeader.AngularVelocity)) {
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.X)) state.AngularVelocity.x = reader.ReadSingle();
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Y)) state.AngularVelocity.y = reader.ReadSingle();
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Z)) state.AngularVelocity.z = reader.ReadSingle();
			}

			// Read rect anchors
			if (state.Header.HasFlag(SyncHeader.RectAnchors)) {
				if (SyncRectAnchorMin.HasFlag(NetworkSyncModeVector2.X)) state.RectAnchorMin.x = reader.ReadSingle();
				if (SyncRectAnchorMin.HasFlag(NetworkSyncModeVector2.Y)) state.RectAnchorMin.y = reader.ReadSingle();
				if (SyncRectAnchorMax.HasFlag(NetworkSyncModeVector2.X)) state.RectAnchorMax.x = reader.ReadSingle();
				if (SyncRectAnchorMax.HasFlag(NetworkSyncModeVector2.Y)) state.RectAnchorMax.y = reader.ReadSingle();
			}

			// Read rect size
			if (state.Header.HasFlag(SyncHeader.RectSizeDelta)) {
				if (SyncRectSizeDelta.HasFlag(NetworkSyncModeVector2.X)) state.RectSizeDelta.x = reader.ReadSingle();
				if (SyncRectSizeDelta.HasFlag(NetworkSyncModeVector2.Y)) state.RectSizeDelta.y = reader.ReadSingle();
			}

			// Read rect pivot
			if (state.Header.HasFlag(SyncHeader.RectPivot)) {
				if (SyncRectPivot.HasFlag(NetworkSyncModeVector2.X)) state.RectPivot.x = reader.ReadSingle();
				if (SyncRectPivot.HasFlag(NetworkSyncModeVector2.Y)) state.RectPivot.y = reader.ReadSingle();
			}

			// Return state
			return state;

		}

		private void WriteState(Writer writer, SyncState state) {

			// Write header
			writer.Write((byte)state.Header);

			// Write rotation
			if (state.Header.HasFlag(SyncHeader.Rotation)) {
				Vector3 euler = state.Rotation.eulerAngles;
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.X)) writer.Write(euler.x);
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.Y)) writer.Write(euler.y);
				if (SyncRotation.HasFlag(NetworkSyncModeVector3.Z)) writer.Write(euler.z);
			}

			// Write position
			if (state.Header.HasFlag(SyncHeader.Position)) {
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.X)) writer.Write(state.Position.x);
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.Y)) writer.Write(state.Position.y);
				if (SyncPosition.HasFlag(NetworkSyncModeVector3.Z)) writer.Write(state.Position.z);
			}

			// Write scale
			if (state.Header.HasFlag(SyncHeader.Scale)) {
				if (SyncScale.HasFlag(NetworkSyncModeVector3.X)) writer.Write(state.Scale.x);
				if (SyncScale.HasFlag(NetworkSyncModeVector3.Y)) writer.Write(state.Scale.y);
				if (SyncScale.HasFlag(NetworkSyncModeVector3.Z)) writer.Write(state.Scale.z);
			}

			// Write velocity
			if (state.Header.HasFlag(SyncHeader.Velocity)) {
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.X)) writer.Write(state.Velocity.x);
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.Y)) writer.Write(state.Velocity.y);
				if (SyncVelocity.HasFlag(NetworkSyncModeVector3.Z)) writer.Write(state.Velocity.z);
			}

			// Write angular velocity
			if (state.Header.HasFlag(SyncHeader.AngularVelocity)) {
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.X)) writer.Write(state.AngularVelocity.x);
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Y)) writer.Write(state.AngularVelocity.y);
				if (SyncAngularVelocity.HasFlag(NetworkSyncModeVector3.Z)) writer.Write(state.AngularVelocity.z);
			}

			// Write rect anchors
			if (state.Header.HasFlag(SyncHeader.RectAnchors)) {
				if (SyncRectAnchorMin.HasFlag(NetworkSyncModeVector2.X)) writer.Write(state.RectAnchorMin.x);
				if (SyncRectAnchorMin.HasFlag(NetworkSyncModeVector2.Y)) writer.Write(state.RectAnchorMin.y);
				if (SyncRectAnchorMax.HasFlag(NetworkSyncModeVector2.X)) writer.Write(state.RectAnchorMax.x);
				if (SyncRectAnchorMax.HasFlag(NetworkSyncModeVector2.Y)) writer.Write(state.RectAnchorMax.y);
			}

			// Write rect size
			if (state.Header.HasFlag(SyncHeader.RectSizeDelta)) {
				if (SyncRectSizeDelta.HasFlag(NetworkSyncModeVector2.X)) writer.Write(state.RectSizeDelta.x);
				if (SyncRectSizeDelta.HasFlag(NetworkSyncModeVector2.Y)) writer.Write(state.RectSizeDelta.y);
			}

			// Write rect pivot
			if (state.Header.HasFlag(SyncHeader.RectPivot)) {
				if (SyncRectPivot.HasFlag(NetworkSyncModeVector2.X)) writer.Write(state.RectPivot.x);
				if (SyncRectPivot.HasFlag(NetworkSyncModeVector2.Y)) writer.Write(state.RectPivot.y);
			}

		}

		private struct NetworkMessageState : IMessage {

			HostTimestamp IMessage.Timestamp => Host.Now;
			byte IMessage.Channel => Channel;
			bool IMessage.Timed => true;
			bool IMessage.Reliable => false;
			bool IMessage.Ordered => false;
			bool IMessage.Unique => false;
			
			public readonly NetworkTransform Transform;
			public readonly SyncState State;
			public readonly byte Channel;

			public NetworkMessageState(NetworkTransform transform, SyncState state, byte channel) {
				Transform = transform;
				State = state;
				Channel = channel;
			}

			void IWritable.Write(Writer writer) {
				Transform.WriteState(writer, State);
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
