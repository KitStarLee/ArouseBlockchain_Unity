using SuperNet.Netcode.Util;
using System;

namespace SuperNet.Unity.Core {
	
	/// <summary>
	/// ID used to syncronize components over network.
	/// </summary>
	[Serializable]
	public struct NetworkIdentity : IComparable, IComparable<NetworkIdentity>, IEquatable<NetworkIdentity>, IFormattable {

		/// <summary>
		/// Invalid value.
		/// </summary>
		public const uint VALUE_INVALID = 0;

		/// <summary>
		/// Raw network ID.
		/// </summary>
		public uint Value;

		/// <summary>
		/// This ID is invalid.
		/// </summary>
		public bool IsInvalid => Value == VALUE_INVALID;

		/// <summary>
		/// Create a new network ID.
		/// </summary>
		/// <param name="value">Raw network ID.</param>
		public NetworkIdentity(uint value) {
			Value = value;
		}
		
		public override bool Equals(object obj) {
			if (obj is NetworkIdentity id) {
				return Value.Equals(id.Value);
			} else {
				return false;
			}
		}

		public bool Equals(NetworkIdentity other) {
			return Value.Equals(other.Value);
		}

		public int CompareTo(NetworkIdentity other) {
			return Value.CompareTo(other.Value);
		}

		public int CompareTo(object obj) {
			if (obj is NetworkIdentity id) {
				return Value.CompareTo(id.Value);
			} else {
				return 1;
			}
		}

		public override int GetHashCode() {
			return Value.GetHashCode();
		}

		public override string ToString() {
			return Value.ToString();
		}

		public string ToString(string format, IFormatProvider provider) {
			return Value.ToString(format, provider);
		}

		public static bool operator ==(NetworkIdentity lhs, NetworkIdentity rhs) {
			return lhs.Value == rhs.Value;
		}

		public static bool operator !=(NetworkIdentity lhs, NetworkIdentity rhs) {
			return lhs.Value != rhs.Value;
		}

		public static explicit operator uint(NetworkIdentity id) {
			return id.Value;
		}

		public static implicit operator NetworkIdentity(uint value) {
			return new NetworkIdentity(value);
		}

		/// <summary>
		/// Generate a new dynamic ID.
		/// </summary>
		/// <param name="random">Random generator to use.</param>
		/// <returns>A new dynamic ID.</returns>
		public static NetworkIdentity GenerateRandomID(Random random) {
			uint value = VALUE_INVALID;
			while (value == VALUE_INVALID) {
				value = (uint)random.Next(int.MinValue, int.MaxValue);
			}
			return value;
		}

	}

	/// <summary>
	/// Network ID Extensions.
	/// </summary>
	public static class NetworkIdentityExtensions {

		/// <summary>
		/// Read ID from a network reader.
		/// </summary>
		/// <param name="reader">Reader to read from.</param>
		/// <returns>ID that was read.</returns>
		public static NetworkIdentity ReadNetworkID(this Reader reader) {
			return reader.ReadUInt32();
		}

		/// <summary>
		/// Write ID to a network writer.
		/// </summary>
		/// <param name="writer">Writer to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write(this Writer writer, NetworkIdentity value) {
			writer.Write(value.Value);
		}

	}

}
