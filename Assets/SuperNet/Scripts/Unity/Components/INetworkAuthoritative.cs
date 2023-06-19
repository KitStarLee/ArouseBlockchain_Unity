using SuperNet.Netcode.Transport;

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Allows network components to be controlled by a single authority on the network.
	/// </summary>
	public interface INetworkAuthoritative {

		/// <summary>
		/// Called when authority of this component changes.
		/// </summary>
		/// <param name="authority">True if component has local authority, false if not.</param>
		/// <param name="timestamp">Timestamp when authority changed.</param>
		void OnNetworkAuthorityUpdate(bool authority, HostTimestamp timestamp);

	}

}
