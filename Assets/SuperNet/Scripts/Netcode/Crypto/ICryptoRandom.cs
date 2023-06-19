using System;

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Defines methods for generating random data.
	/// </summary>
	public interface ICryptoRandom : IDisposable {

		/// <summary>
		/// Generate cryptographically secure random data.
		/// <para>This method is thread safe.</para>
		/// </summary>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <param name="count">Number of bytes to write.</param>
		void GetBytes(byte[] output, int offset, int count);

	}

}
