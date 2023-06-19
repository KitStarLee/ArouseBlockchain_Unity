using SuperNet.Netcode.Util;
using System;
using System.IO;
using System.Security.Cryptography;

#pragma warning disable IDE0063 // Use simple 'using' statement
#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Encryptor based on 256-bit Advanced Encryption Standard (AES).
	/// </summary>
	public sealed class CryptoAES : ICryptoEncryptor {

		private readonly Allocator Allocator;
		private readonly Aes Aes;
		private bool Disposed;
		private byte[] Key;
		private byte[] IV;
		
		/// <summary>Create a new AES encryptor with the provided key.</summary>
		/// <param name="key">Encryption key to use.</param>
		/// <param name="allocator">Allocator to use for allocating keys.</param>
		public CryptoAES(byte[] key, Allocator allocator) {

			// Validate
			if (key == null) {
				throw new ArgumentNullException(nameof(key), "Key is null");
			} else if (allocator == null) {
				throw new ArgumentNullException(nameof(allocator), "No allocator");
			} else if (key.Length != 32) {
				throw new ArgumentOutOfRangeException(nameof(key), string.Format(
					"Key length {0} is not 32", key.Length
				));
			}

			// Initialize
			Allocator = allocator;
			Key = key;
			IV = Allocator.CreateIV(16);
			Disposed = false;

			// Create AES
			Aes = Aes.Create();
			Aes.Padding = PaddingMode.PKCS7;
			Aes.Mode = CipherMode.CBC;
			//Aes.FeedbackSize = 0;
			Aes.BlockSize = 128;
			Aes.KeySize = 256;
			Aes.Key = Key;
			Aes.IV = IV;
			
		}

		/// <summary>Instantly dispose of all resources.</summary>
		public void Dispose() {
			Disposed = true;
			Aes.Dispose();
			Allocator.ReturnKey(ref Key);
			Allocator.ReturnIV(ref IV);
		}

		/// <summary>Compute the maximum encrypted length before encrypting.</summary>
		/// <param name="inputLength">Length of the input that is about to be encrypted.</param>
		/// <returns>Maximum possible encrypted length.</returns>
		public int MaxEncryptedLength(int inputLength) {

			// Validate
			if (inputLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(inputLength), string.Format(
					"Input unencrypted length {0} is negative", inputLength
				));
			}

			// Compute length
			return 16 + (inputLength / 16 + 1) * 16;

		}

		/// <summary>Compute the maximum decrypted length before decrypting.</summary>
		/// <param name="inputLength">Length of the input that is about to be decrypted.</param>
		/// <returns>Maximum possible decrypted length.</returns>
		public int MaxDecryptedLength(int inputLength) {

			// Validate
			if (inputLength <= 16) {
				throw new ArgumentOutOfRangeException(nameof(inputLength), string.Format(
					"Input encrypted length {0} must be over 16", inputLength
				));
			}

			// Compute length
			return inputLength - 16;

		}

		/// <summary>Encrypt data.</summary>
		/// <param name="input">Array segment to encrypt.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		public int Encrypt(ArraySegment<byte> input, byte[] output, int offset) {

			// Validate
			int maxLength = MaxEncryptedLength(input.Count);
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output == null) {
				throw new ArgumentNullException(nameof(output), "Output buffer is null");
			} else if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), string.Format(
					"Output offset {0} is negative", offset
				));
			} else if (output.Length - offset < maxLength) {
				throw new ArgumentOutOfRangeException(nameof(output), string.Format(
					"Output encrypt length {0} from {1} is less than {2}",
					output.Length, offset, maxLength
				));
			}
			
			// Encrypt
			lock (Aes) {
				Aes.GenerateIV();
				Array.Copy(Aes.IV, 0, output, offset, 16);
				using (ICryptoTransform encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV))
				using (MemoryStream stream = new MemoryStream(output, offset + 16, output.Length - offset - 16, true))
				using (CryptoStream crypto = new CryptoStream(stream, encryptor, CryptoStreamMode.Write)) {
					crypto.Write(input.Array, input.Offset, input.Count);
					crypto.FlushFinalBlock();
					return 16 + (int)stream.Position;
				}
			}

		}

		/// <summary>Decrypt data.</summary>
		/// <param name="input">Array segment to decrypt.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		public int Decrypt(ArraySegment<byte> input, byte[] output, int offset) {

			// Validate
			int maxLength = MaxDecryptedLength(input.Count);
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output == null) {
				throw new ArgumentNullException(nameof(output), "Output buffer is null");
			} else if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), string.Format(
					"Output offset {0} is negative", offset
				));
			} else if (output.Length - offset < maxLength) {
				throw new ArgumentOutOfRangeException(nameof(output), string.Format(
					"Output decrypt length {0} from {1} is less than {2}",
					output.Length, offset, maxLength
				));
			}

			// Decrypt
			lock (Aes) {
				Array.Copy(input.Array, input.Offset, IV, 0, 16);
				using (ICryptoTransform decryptor = Aes.CreateDecryptor(Aes.Key, IV))
				using (MemoryStream stream = new MemoryStream(output, offset, output.Length - offset, true))
				using (CryptoStream crypto = new CryptoStream(stream, decryptor, CryptoStreamMode.Write)) {
					crypto.Write(input.Array, input.Offset + 16, input.Count - 16);
					crypto.FlushFinalBlock();
					return (int)stream.Position;
				}
			}

		}
		
	}

}
