using System;

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Defines methods used for a key exchange that is able to derive a shared encryptor.
	/// </summary>
	public interface ICryptoExchanger : IDisposable {

		/// <summary>Size of exchange key in bytes.</summary>
		int KeyLength { get; }

		/// <summary>Copy exchange key to the output.</summary>
		/// <param name="output">Output to write to.</param>
		void ExportKey(ArraySegment<byte> output);

		/// <summary>Generate a shared encryptor.</summary>
		/// <param name="remoteKey">Received remote exchange key.</param>
		/// <returns>Shared encryptor that is guaranteed to be the same on both peers.</returns>
		ICryptoEncryptor DeriveEncryptor(ArraySegment<byte> remoteKey);

	}
	
}
