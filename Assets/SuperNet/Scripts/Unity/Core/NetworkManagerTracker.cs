using SuperNet.Netcode.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	public partial class NetworkManager : MonoBehaviour {

		// Registered components
		private static ReaderWriterLockSlim ComponentLock;
		private static Dictionary<NetworkIdentity, NetworkComponent> ComponentIdentities;
		private static NetworkComponent[] ComponentArray;
		private static System.Random ComponentRandom;
		
		// Received hashes to check for duplicates
		private static object ReceiveLock = new object();
		private static Dictionary<uint, HostTimestamp> ReceiveHash;
		private static CancellationTokenSource ReceiveCancel;
		private static Task ReceiveTask;

		// Interlocked send sequence
		private static int SendSequence = 0;

		private static void InitializeTracker() {
			ComponentLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			ComponentIdentities = new Dictionary<NetworkIdentity, NetworkComponent>();
			ComponentRandom = new System.Random();
			ComponentArray = new NetworkComponent[0];
			ReceiveLock = new object();
			ReceiveHash = new Dictionary<uint, HostTimestamp>();
			ReceiveCancel = new CancellationTokenSource();
			ReceiveTask = Task.CompletedTask;
		}

		private void OnAwakeTracker() {
			lock (ReceiveLock) {
				if (ReceiveTask.IsCompleted) {
					ReceiveTask = Task.Factory.StartNew(
						CleanupWorkerAsync,
						ReceiveCancel.Token,
						TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning,
						TaskScheduler.Default
					);
				}
			}
		}

		private void OnDestroyTracker() {
			try {
				ReceiveCancel.Cancel();
				ReceiveCancel.Dispose();
			} catch { }
		}

		/// <summary>
		/// Return all registered components.
		/// </summary>
		/// <returns>All registered components.</returns>
		public static IReadOnlyList<NetworkComponent> GetComponents() {
			return ComponentArray;
		}

		/// <summary>
		/// Check if a network ID is registered.
		/// </summary>
		/// <param name="registerID">Network ID to check for.</param>
		/// <returns>True if registered, false if not.</returns>
		public static bool IsRegistered(NetworkIdentity registerID) {
			try {
				ComponentLock.EnterReadLock();
				return ComponentIdentities.ContainsKey(registerID);
			} finally {
				ComponentLock.ExitReadLock();
			}
		}

		/// <summary>
		/// Find a registered component if it exists.
		/// </summary>
		/// <param name="registerID">Network ID to check for.</param>
		/// <returns>Registered component or null if not found.</returns>
		public static NetworkComponent FindComponent(NetworkIdentity registerID) {
			try {
				ComponentLock.EnterReadLock();
				ComponentIdentities.TryGetValue(registerID, out NetworkComponent component);
				return component;
			} finally {
				ComponentLock.ExitReadLock();
			}
		}

		/// <summary>
		/// Register a component to be able to start sending and receiving messages.
		/// </summary>
		/// <param name="component">Component to register.</param>
		/// <returns>Registered network ID.</returns>
		public static NetworkIdentity Register(NetworkComponent component) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			}

			// Register
			NetworkIdentity networkID = component.NetworkID;
			return Register(component, networkID);

		}

		/// <summary>
		/// Register a component to be able to start sending and receiving messages.
		/// </summary>
		/// <param name="component">Component to register.</param>
		/// <param name="registerID">Network ID to assign.</param>
		/// <returns>Registered network ID.</returns>
		public static NetworkIdentity Register(NetworkComponent component, NetworkIdentity registerID) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			}

			try {
				ComponentLock.EnterWriteLock();

				// Get existing ID and components
				NetworkIdentity existingID = component.NetworkID;
				ComponentIdentities.TryGetValue(existingID, out NetworkComponent existing);
				ComponentIdentities.TryGetValue(registerID, out NetworkComponent register);

				// Warn if ID is taken by another component
				if (existing != null && existing != component && LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [Manager] Another component is already registered with ID {0}.",
						existingID
					));
				}

				// Warn if ID is taken by another component
				if (register != null && register != component && LogExceptions) {
					Debug.LogWarning(string.Format(
						"[SuperNet] [Manager] Another component is already registered with ID {0}.",
						registerID
					));
				}

				// Log if component ID changed
				if (existing == component && !registerID.IsInvalid && register == null && LogEvents) {
					Debug.Log(string.Format(
						"[SuperNet] [Manager] Updating component ID from {0} to {1}.",
						existingID, registerID
					));
				}

				// Remove if already registered
				if (existing == component) ComponentIdentities.Remove(existingID);
				if (register == component) ComponentIdentities.Remove(registerID);

				// Generate a random ID if needed
				if (registerID.IsInvalid) registerID = existingID;
				while (registerID.IsInvalid || ComponentIdentities.ContainsKey(registerID)) {
					registerID = NetworkIdentity.GenerateRandomID(ComponentRandom);
				}

				// Update network ID
				component.SetNetworkID(registerID);

				// Register component
				ComponentIdentities[registerID] = component;

				// Create a new component array
				NetworkComponent[] components = new NetworkComponent[ComponentIdentities.Count];
				ComponentIdentities.Values.CopyTo(components, 0);
				Interlocked.Exchange(ref ComponentArray, components);

			} catch (Exception exception) {

				if (LogExceptions) {
					Debug.LogException(exception);
				}

			} finally {
				ComponentLock.ExitWriteLock();
			}

			// Notify component
			component.InvokeNetworkRegister();

			// Notify all components
			NetworkComponent[] array = ComponentArray;
			foreach (NetworkComponent invoke in array) {
				if (invoke != null) {
					invoke.InvokeNetworkRegister(component);
				}
			}

			// Return registered ID
			return registerID;

		}

		/// <summary>
		/// Unregister a component and make it stop receiving messages.
		/// </summary>
		/// <param name="component">Component to unregister.</param>
		public static void Unregister(NetworkComponent component) {

			// Validate
			if (component == null) {
				throw new ArgumentNullException(nameof(component), "No component provided");
			}
			
			// If not registered, don't unregister
			if (!component.IsRegisteredOnNetwork) {
				return;
			}

			try {
				ComponentLock.EnterWriteLock();

				// Check if component is registered
				NetworkIdentity networkID = component.NetworkID;
				bool exists = ComponentIdentities.TryGetValue(networkID, out NetworkComponent found);
				if (!exists || found != component) {
					if (LogExceptions) {
						Debug.LogWarning(string.Format(
							"[SuperNet] [Manager] Component with ID {0} is not registered. Can't unregister.",
							networkID
						));
					}
					return;
				}

				// Update network ID
				component.SetNetworkID(NetworkIdentity.VALUE_INVALID);

				// Unregister
				ComponentIdentities.Remove(networkID);

				// Create a new component array
				NetworkComponent[] components = new NetworkComponent[ComponentIdentities.Count];
				ComponentIdentities.Values.CopyTo(components, 0);
				Interlocked.Exchange(ref ComponentArray, components);

			} catch (Exception exception) {

				if (LogExceptions) {
					Debug.LogException(exception);
				}

			} finally {
				ComponentLock.ExitWriteLock();
			}

			// Notify component
			component.InvokeNetworkUnregister();

			// Notify all components
			NetworkComponent[] array = ComponentArray;
			foreach (NetworkComponent invoke in array) {
				if (invoke != null) {
					invoke.InvokeNetworkUnregister(component);
				}
			}

		}

		private static async Task CleanupWorkerAsync() {

			// List of all hashes that timed out
			List<uint> timeout = new List<uint>();

			while (!ReceiveCancel.IsCancellationRequested) {

				// Timeout delay
				try {
					await Task.Delay(DuplicateTimeout, ReceiveCancel.Token);
				} catch {
					return;
				}

				try {

					lock (ReceiveLock) {

						// Find all timed out hashes
						foreach (KeyValuePair<uint, HostTimestamp> pair in ReceiveHash) {
							if (pair.Value.Elapsed.GetTicks(Host.Frequency) > DuplicateTimeout) {
								timeout.Add(pair.Key);
							}
						}

						// Remove all saved hashes
						foreach (uint hash in timeout) {
							ReceiveHash.Remove(hash);
						}

						// Clear all saved hashes
						timeout.Clear();

					}

				} catch (Exception exception) {

					if (LogExceptions) {
						Debug.LogException(exception);
					}

				}

			}

		}

	}

}
