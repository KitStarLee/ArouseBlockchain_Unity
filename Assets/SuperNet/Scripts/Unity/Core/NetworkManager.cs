using UnityEngine;
using UnityEngine.Serialization;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("SuperNet/NetworkManager")]
	[HelpURL("https://superversus.com/netcode/api/SuperNet.Unity.Core.NetworkManager.html")]
	public partial class NetworkManager : MonoBehaviour {

		private static bool Initialized = false;
		private static NetworkManager Instance = null;

		private static NetworkManager GetInstance() {
			if (Initialized) return Instance;
			NetworkManager instance = FindObjectOfType<NetworkManager>();
			if (instance != null) {
				Instance = instance;
			} else {
				GameObject obj = new GameObject(nameof(NetworkManager));
				Instance = obj.AddComponent<NetworkManager>();
			}
			Initialized = true;
			return Instance;
		}

		static NetworkManager() {
			InitializeThread();
			InitializePeers();
			InitializeTracker();
			InitializeRollback();
		}

		private void Awake() {
			if (Initialized && Instance != this) {
				Destroy(this);
			} else {
				transform.parent = null;
				DontDestroyOnLoad(this);
				OnAwakeThread();
				OnAwakeTracker();
				Instance = this;
				Initialized = true;
			}
		}

		private void Update() {
			OnUpdateThread();
		}

		private void OnDestroy() {
			OnDestroyTracker();
		}

		/// <summary>
		/// How long in milliseconds to check message duplicates for.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("DuplicateTimeout")]
		[Tooltip("How long in milliseconds to check message duplicates for.")]
		private int ConfigDuplicateTimeout = 2000;

		/// <summary>
		/// Should exceptions be logged to the debug console.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("LogExceptions")]
		[Tooltip("Should exceptions be logged to the debug console.")]
		private bool ConfigLogExceptions = true;

		/// <summary>
		/// Should events be logged to the debug console.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("LogEvents")]
		[Tooltip("Should events be logged to the debug console.")]
		private bool ConfigLogEvents = true;

		/// <summary>
		/// Maximum number of hops each message can take before it is dropped.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("TTL")]
		[Tooltip("Maximum number of hops each message can take before it is dropped.")]
		private byte ConfigTTL = 8;

		/// <summary>
		/// How long in milliseconds to keep received messages for to check for duplicates.
		/// </summary>
		public static int DuplicateTimeout {
			get => GetInstance().ConfigDuplicateTimeout;
			set => GetInstance().ConfigDuplicateTimeout = value;
		}

		/// <summary>
		/// Should events be logged to the debug console.
		/// </summary>
		public static bool LogEvents {
			get => GetInstance().ConfigLogEvents;
			set => GetInstance().ConfigLogEvents = value;
		}

		/// <summary>
		/// Should exceptions be logged to the debug console.
		/// </summary>
		public static bool LogExceptions {
			get => GetInstance().ConfigLogExceptions;
			set => GetInstance().ConfigLogExceptions = value;
		}

		/// <summary>
		/// Maximum number of hops each message can take before it is dropped.
		/// </summary>
		public static byte TTL {
			get => GetInstance().ConfigTTL;
			set => GetInstance().ConfigTTL = value;
		}

		/// <summary>
		/// Initialize network manager.
		/// </summary>
		public static void Initialize() {
			GetInstance();
		}

	}

}
