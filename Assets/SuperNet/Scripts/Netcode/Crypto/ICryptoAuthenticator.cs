using System;

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Defines methods used for authenticating secure hosts.
	/// </summary>
	public interface ICryptoAuthenticator : IDisposable {

		/// <summary>Number of bytes in the signature.</summary>
		int SignatureLength { get; }

		/// <summary>Export the current public key to a human readable format.</summary>
		/// <returns>Human readable public key.</returns>
		string ExportPublicKey();

		/// <summary>Export the current private key to a human readable format.</summary>
		/// <returns>Human readable private key</returns>
		string ExportPrivateKey();

		/// <summary>Import private key.</summary>
		/// <param name="privateKey">Private key to import.</param>
		void ImportPrivateKey(string privateKey);

		/// <summary>Generate a signature that can be verified by the public key.</summary>
		/// <param name="data">Data to sign.</param>
		/// <param name="output">Output to write signature to.</param>
		void Sign(ArraySegment<byte> data, ArraySegment<byte> output);

		/// <summary>Verify signature.</summary>
		/// <param name="data">Data that has been signed.</param>
		/// <param name="signature">Signature that has been generated.</param>
		/// <param name="remotePublicKey">Public key corresponding to the private key that signed the data.</param>
		/// <returns>True if verified, false if not.</returns>
		bool Verify(ArraySegment<byte> data, ArraySegment<byte> signature, string remotePublicKey);

	}

}
