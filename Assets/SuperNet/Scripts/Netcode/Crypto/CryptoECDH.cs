using SuperNet.Netcode.Util;
using System;

#pragma warning disable IDE0016 // Use 'throw' expression

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Implements Elliptic Curve Diffie Hellman key exchange.
	/// </summary>
	public sealed class CryptoECDH : ICryptoExchanger {

		/// <summary>Size of exchange key in bytes. For ECDH this is 32.</summary>
		public const int KeyLength = 32;

		/// <summary>Size of exchange key in bytes. For ECDH this is 32.</summary>
		int ICryptoExchanger.KeyLength => 32;

		private readonly Allocator Allocator;
		private byte[] PrivateKey;
		private byte[] PublicKey;
		private bool Disposed;

		/// <summary>Create a new ECDH key pair for key exchange.</summary>
		/// <param name="random">Random number generator to use for generating keys.</param>
		/// <param name="allocator">Allocator to use for allocating keys.</param>
		public CryptoECDH(ICryptoRandom random, Allocator allocator) {

			// Validate
			if (allocator == null) {
				throw new ArgumentNullException(nameof(allocator), "No allocator");
			}

			// Initialize
			Allocator = allocator;
			Disposed = false;

			// Generate private key
			PrivateKey = Allocator.CreateKey(32);
			random.GetBytes(PrivateKey, 0, 32);
			Curve25519.ClampPrivateKeyInline(PrivateKey);

			// Generate public key
			PublicKey = Allocator.CreateKey(32);
			Curve25519.GetPublicKeyInline(PrivateKey, PublicKey);

		}
		
		/// <summary>Returns key pair back to the allocator.</summary>
		public void Dispose() {
			Disposed = true;
			Allocator.ReturnKey(ref PrivateKey);
			Allocator.ReturnKey(ref PublicKey);
		}

		/// <summary>Copy exchange key to the output.</summary>
		/// <param name="output">Output to write to.</param>
		public void ExportKey(ArraySegment<byte> output) {

			// Validate
			if (output.Count != 32) {
				throw new ArgumentOutOfRangeException(nameof(output), string.Format(
					"Key count {0} is not 32", output.Count
				));
			}

			// Copy the key
			Array.Copy(PublicKey, 0, output.Array, output.Offset, 32);

		}

		/// <summary>Generate a shared encryptor.</summary>
		/// <param name="remoteKey">Received remote exchange key.</param>
		/// <returns>Shared encryptor that is guaranteed to be the same on both peers.</returns>
		public ICryptoEncryptor DeriveEncryptor(ArraySegment<byte> remoteKey) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (remoteKey.Count != 32) {
				throw new ArgumentOutOfRangeException(nameof(remoteKey), string.Format(
					"Remote exchange key count {0} is not 32", remoteKey.Count
				));
			}

			// Generate shared secret inline
			if (remoteKey.Array.Length == 32) {
				byte[] secret = Allocator.CreateKey(32);
				Curve25519.GetSharedSecretInline(PrivateKey, remoteKey.Array, secret);
				return new CryptoAES(secret, Allocator);
			}

			// Copy key then generate shared secret
			byte[] key = Allocator.CreateKey(32);
			try {
				Array.Copy(remoteKey.Array, remoteKey.Offset, key, 0, 32);
				byte[] secret = Allocator.CreateKey(32);
				Curve25519.GetSharedSecretInline(PrivateKey, key, secret);
				return new CryptoAES(secret, Allocator);
			} finally {
				Allocator.ReturnKey(ref key);
			}

		}
		
	}

}
