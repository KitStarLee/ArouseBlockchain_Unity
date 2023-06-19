using System.Collections.Generic;
using System.Net;

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Equality comparer used by the netcode to distinguish between peers.
	/// </summary>
	public sealed class IPComparer : IEqualityComparer<IPEndPoint> {

		/// <summary>Check if both address and port match.</summary>
		/// <param name="x">First IP</param>
		/// <param name="y">Second IP</param>
		/// <returns>True if they match, false if not.</returns>
		public bool Equals(IPEndPoint x, IPEndPoint y) {
			return x.Address.Equals(y.Address) && x.Port == y.Port;
		}

		/// <summary>Construct a hash code based on address and port.</summary>
		/// <param name="obj">Object to construct the hash code from.</param>
		/// <returns>Constructed hash code.</returns>
		public int GetHashCode(IPEndPoint obj) {
			int hash = 17;
			hash = hash * 31 + obj.Address.GetHashCode();
			hash = hash * 31 + obj.Port.GetHashCode();
			return hash;
		}

	}

}
