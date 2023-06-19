using SuperNet.Netcode.Transport;

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Allows network components to be temporarily rolled back in time.
	/// </summary>
	public interface INetworkRollbackable {

		/// <summary>
		/// Update the state of the component to be the same as in the past.
		/// </summary>
		/// <param name="time">Which timestamp in the past to rollback to.</param>
		/// <param name="peer">Peer that we're performing the rollback for.</param>
		void OnNetworkRollbackBegin(HostTimestamp time, Peer peer);

		/// <summary>
		/// Revert any rollbacks.
		/// </summary>
		void OnNetworkRollbackEnd();

	}

}
