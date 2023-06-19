using SuperNet.Netcode.Transport;

namespace SuperNet.Unity.Components {

	/// <summary>
	/// Allows network components to listen for register events.
	/// </summary>
	public interface INetworkRegisterable {

		/// <summary>
		/// Called when this component is registered on a peer.
		/// </summary>
		/// <param name="remote">Peer the component is registered on.</param>
		void OnNetworkRegister(Peer remote);

		/// <summary>
		/// Called when this component is unregistered from a peer.
		/// </summary>
		/// <param name="remote">Peer the component is unregistered from.</param>
		void OnNetworkUnregister(Peer remote);

	}

}
