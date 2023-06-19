using System;

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Defines methods for encrypting and decrypting network packets.
	/// </summary>
	public interface ICryptoEncryptor : IDisposable {

		/// <summary>Compute the maximum encrypted length before encrypting.</summary>
		/// <param name="inputLength">Length of the input that is about to be encrypted.</param>
		/// <returns>Maximum possible encrypted length.</returns>
		int MaxEncryptedLength(int inputLength);

		/// <summary>Compute the maximum decrypted length before decrypting.</summary>
		/// <param name="inputLength">Length of the input that is about to be decrypted.</param>
		/// <returns>Maximum possible decrypted length.</returns>
		int MaxDecryptedLength(int inputLength);

		/// <summary>Encrypt data.</summary>
		/// <param name="input">Array segment to encrypt.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		int Encrypt(ArraySegment<byte> input, byte[] output, int offset);

		/// <summary>Decrypt data.</summary>
		/// <param name="input">Array segment to decrypt.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		int Decrypt(ArraySegment<byte> input, byte[] output, int offset);

	}

}
