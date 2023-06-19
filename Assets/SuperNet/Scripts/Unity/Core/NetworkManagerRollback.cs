using SuperNet.Netcode.Transport;
using SuperNet.Unity.Components;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#pragma warning disable IDE0083 // Use pattern matching

namespace SuperNet.Unity.Core {

	/// <summary>
	/// Provides API access to static network methods.
	/// </summary>
	public partial class NetworkManager : MonoBehaviour {

		// Resources
		private static object RollbackLock;
		private static Dictionary<NetworkComponent, RollbackState> RollbackComponents;
		private static NetworkComponent[] RollbackArray;
		
		private struct RollbackState {
			public bool Kinematic;
			public bool Enabled;
		}

		private static void InitializeRollback() {
			RollbackLock = new object();
			RollbackComponents = new Dictionary<NetworkComponent, RollbackState>();
			RollbackArray = new NetworkComponent[0];
		}

		/// <summary>
		/// Update the state of all components to be the same as in the past.
		/// </summary>
		/// <param name="time">Which timestamp in the past to rollback to.</param>
		/// <param name="peer">Peer that we're performing the rollback for.</param>
		/// <param name="persist">If true, persist this rollback across multiple frames.</param>
		/// <param name="includeDisabled">If true, include disabled components.</param>
		public static void RollbackBegin(
			HostTimestamp time, Peer peer,
			bool persist = false, bool includeDisabled = false
		) {
			IReadOnlyList<NetworkComponent> components = GetComponents();
			RollbackBegin(components, time, peer, persist, includeDisabled);
		}

		/// <summary>
		/// Update the state of all provided components to be the same as in the past.
		/// </summary>
		/// <param name="components">Components to update.</param>
		/// <param name="time">Which timestamp in the past to rollback to.</param>
		/// <param name="peer">Peer that we're performing the rollback for.</param>
		/// <param name="persist">If true, persist this rollback across multiple frames.</param>
		/// <param name="includeDisabled">If true, include disabled components.</param>
		public static void RollbackBegin(
			IEnumerable<NetworkComponent> components, HostTimestamp time, Peer peer,
			bool persist = false, bool includeDisabled = false
		) {
			foreach (NetworkComponent component in components) {

				// Make sure the component exists and is rollbackable
				if (component == null) continue;
				if (!(component is INetworkRollbackable rollbackable)) continue;

				if (persist) {

					lock (RollbackLock) {

						// Check if already persistent
						bool found = RollbackComponents.ContainsKey(component);

						// If disabled, ignore
						if (!found && !includeDisabled && !component.enabled) {
							continue;
						}

						// Perform rollback
						rollbackable.OnNetworkRollbackBegin(time, peer);

						if (!found) {

							// Make rigidbody kinematic
							bool kinematic = false;
							Rigidbody rigidbody = component.GetComponent<Rigidbody>();
							if (rigidbody != null) {
								kinematic = rigidbody.isKinematic;
								Run(() => rigidbody.isKinematic = true);
							}

							// Disable component
							bool enabled = component.enabled;
							Run(() => component.enabled = false);

							// Save component state
							RollbackComponents[component] = new RollbackState() {
								Kinematic = kinematic,
								Enabled = enabled,
							};

							// Update array
							NetworkComponent[] array = new NetworkComponent[RollbackComponents.Count];
							RollbackComponents.Keys.CopyTo(array, 0);
							Interlocked.Exchange(ref RollbackArray, array);

						}

					}

				} else {

					// If disabled, ignore
					if (!includeDisabled && !component.enabled) {
						continue;
					}

				}

				// Perform rollback
				rollbackable.OnNetworkRollbackBegin(time, peer);

			}
		}

		/// <summary>
		/// Revert all rollbacks.
		/// </summary>
		public static void RollbackEnd() {

			// Revert all registered components
			IReadOnlyList<NetworkComponent> components = GetComponents();
			RollbackEnd(components);

			// Revert all persistent components if any left
			NetworkComponent[] array = RollbackArray;
			RollbackEnd(array);

		}

		/// <summary>
		/// Revert any rollbacks.
		/// </summary>
		/// <param name="components">Components to revert for.</param>
		public static void RollbackEnd(IEnumerable<NetworkComponent> components) {
			foreach (NetworkComponent component in components) {

				// Make sure the component exists and is rollbackable
				if (component == null) continue;
				if (!(component is INetworkRollbackable rollbackable)) continue;

				// End rollback
				rollbackable.OnNetworkRollbackEnd();

				lock (RollbackLock) {

					// Check if component is persisted
					bool found = RollbackComponents.TryGetValue(component, out RollbackState state);
					if (!found) {
						continue;
					}

					// Remove component
					RollbackComponents.Remove(component);

					// Update array
					NetworkComponent[] array = new NetworkComponent[RollbackComponents.Count];
					RollbackComponents.Keys.CopyTo(array, 0);
					Interlocked.Exchange(ref RollbackArray, array);

					Run(() => {

						// Revert enabled state
						component.enabled = state.Enabled;

						// Revert kinematic state
						Rigidbody rigidbody = component.GetComponent<Rigidbody>();
						if (rigidbody != null) rigidbody.isKinematic = state.Kinematic;

					});

				}

			}
		}

	}

}
