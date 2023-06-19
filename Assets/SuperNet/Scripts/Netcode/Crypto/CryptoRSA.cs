using SuperNet.Netcode.Util;
using System;
using System.Security.Cryptography;

#pragma warning disable IDE0063 // Use simple 'using' statement

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Authenticator based on 2048-bit RSA (Rivest–Shamir–Adleman).
	/// </summary>
	public sealed class CryptoRSA : ICryptoAuthenticator {

		/// <summary>Number of bytes in the signature. For RSA this is 256.</summary>
		public int SignatureLength => 256;

		// Resources
		private readonly Allocator Allocator;
		private readonly Lazy<RSA> RSA;
		private bool Disposed;

		/// <summary>
		/// Create a new RSA authenticator.
		/// </summary>
		/// <param name="allocator">Allocator to use for keys or null for none.</param>
		public CryptoRSA(Allocator allocator = null) {
			Allocator = allocator ?? new Allocator();
			RSA = new Lazy<RSA>(CreateRSA, true);
			Disposed = false;
		}

		/// <summary>Instantly dispose of all resources.</summary>
		public void Dispose() {
			Disposed = true;
			if (RSA.IsValueCreated) {
				RSA.Value.Dispose();
			}
		}

		/// <summary>Export RSA modulus as a Base64 string.</summary>
		/// <returns>Base64 encoded RSA modulus.</returns>
		public string ExportPublicKey() {

			// Validate
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);

			// Export parameters
			RSAParameters p = RSA.Value.ExportParameters(false);
			if (p.Modulus.Length != 256) {
				throw new InvalidOperationException(string.Format(
					"Modulus length {0} is not 256", p.Modulus.Length
				));
			} else if (p.Exponent.Length != 3) {
				throw new InvalidOperationException(string.Format(
					"Exponent length {0} is not 3", p.Exponent.Length
				));
			} else if (p.Exponent[0] != 1 || p.Exponent[1] != 0 || p.Exponent[2] != 1) {
				throw new InvalidOperationException(string.Format(
					"Exponent {0}, {1}, {2} is invalid",
					p.Exponent[0], p.Exponent[1], p.Exponent[2]
				));
			}

			// Convert to Base64
			return Convert.ToBase64String(p.Modulus);

		}

		/// <summary>Export all RSA parameters as a Base64 string.</summary>
		/// <returns>Base64 encoded parameters.</returns>
		public string ExportPrivateKey() {

			// Validate
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);

			// Export parameters
			RSAParameters p = RSA.Value.ExportParameters(true);
			if (p.D.Length != 256) {
				throw new InvalidOperationException(string.Format(
					"D length {0} is not 256", p.D.Length
				));
			} else if (p.DP.Length != 128) {
				throw new InvalidOperationException(string.Format(
					"DP length {0} is not 128", p.DP.Length
				));
			} else if (p.DQ.Length != 128) {
				throw new InvalidOperationException(string.Format(
					"DQ length {0} is not 128", p.DQ.Length
				));
			} else if (p.Exponent.Length != 3) {
				throw new InvalidOperationException(string.Format(
					"Exponent length {0} is not 3", p.Exponent.Length
				));
			} else if (p.Exponent[0] != 1 || p.Exponent[1] != 0 || p.Exponent[2] != 1) {
				throw new InvalidOperationException(string.Format(
					"Exponent {0}, {1}, {2} is invalid",
					p.Exponent[0], p.Exponent[1], p.Exponent[2]
				));
			} else if (p.InverseQ.Length != 128) {
				throw new InvalidOperationException(string.Format(
					"InverseQ length {0} is not 128", p.InverseQ.Length
				));
			} else if (p.Modulus.Length != 256) {
				throw new InvalidOperationException(string.Format(
					"Modulus length {0} is not 256", p.Modulus.Length
				));
			} else if (p.P.Length != 128) {
				throw new InvalidOperationException(string.Format(
					"P length {0} is not 128", p.P.Length
				));
			} else if (p.Q.Length != 128) {
				throw new InvalidOperationException(string.Format(
					"Q length {0} is not 128", p.Q.Length
				));
			}

			// Convert to Base64
			byte[] key = Allocator.CreatePacket(1152);
			try {
				Array.Copy(p.D, 0, key, 0, 256);
				Array.Copy(p.DP, 0, key, 256, 128);
				Array.Copy(p.DQ, 0, key, 384, 128);
				Array.Copy(p.InverseQ, 0, key, 512, 128);
				Array.Copy(p.Modulus, 0, key, 640, 256);
				Array.Copy(p.P, 0, key, 896, 128);
				Array.Copy(p.Q, 0, key, 1024, 128);
				return Convert.ToBase64String(key, 0, 1152);
			} finally {
				Allocator.ReturnPacket(ref key);
			}

		}

		/// <summary>Import previously exported private key.</summary>
		/// <param name="privateKey">Private key to import.</param>
		public void ImportPrivateKey(string privateKey) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (privateKey == null) {
				throw new ArgumentNullException(nameof(privateKey), "Private key is null");
			}
			
			// Decode key
			byte[] key = Convert.FromBase64String(privateKey);
			if (key.Length != 1152) {
				throw new ArgumentOutOfRangeException(nameof(privateKey), string.Format(
					"Private key length {0} is not 1152", key.Length
				));
			}
			
			// Create parameters
			byte[] D = new byte[256];
			byte[] DP = new byte[128];
			byte[] DQ = new byte[128];
			byte[] Exponent = new byte[] { 1, 0, 1 };
			byte[] InverseQ = new byte[128];
			byte[] Modulus = new byte[256];
			byte[] P = new byte[128];
			byte[] Q = new byte[128];
			Array.Copy(key, 0, D, 0, 256);
			Array.Copy(key, 256, DP, 0, 128);
			Array.Copy(key, 384, DQ, 0, 128);
			Array.Copy(key, 512, InverseQ, 0, 128);
			Array.Copy(key, 640, Modulus, 0, 256);
			Array.Copy(key, 896, P, 0, 128);
			Array.Copy(key, 1024, Q, 0, 128);

			// Import parameters
			RSA.Value.ImportParameters(new RSAParameters() {
				D = D, DP = DP, DQ = DQ,
				Exponent = Exponent,
				InverseQ = InverseQ,
				Modulus = Modulus,
				P = P, Q = Q,
			});

		}

		/// <summary>Generate a signature of that can then be verified by the public key.</summary>
		/// <param name="data">Data to sign.</param>
		/// <param name="output">Output to write signature to.</param>
		public void Sign(ArraySegment<byte> data, ArraySegment<byte> output) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output.Count != 256) {
				throw new ArgumentOutOfRangeException(nameof(output), string.Format(
					"Output signature count {0} is not 256", output.Count
				));
			}

			// Create signature
			HashAlgorithmName hash = HashAlgorithmName.SHA256;
			RSASignaturePadding padding = RSASignaturePadding.Pkcs1;
			byte[] signed = RSA.Value.SignData(data.Array, data.Offset, data.Count, hash, padding);

			// Validate signature
			if (signed.Length != 256) {
				throw new InvalidOperationException(string.Format(
					"RSA signature length {0} is not 256", signed.Length
				));
			}
			
			// Copy signature to output
			Array.Copy(signed, 0, output.Array, output.Offset, 256);

		}

		/// <summary>Verify that signature has been signed by someone with the private key.</summary>
		/// <param name="data">Data that has been signed.</param>
		/// <param name="signature">Signature that has been generated.</param>
		/// <param name="remotePublicKey">Public key corresponding to the private key that signed the data.</param>
		/// <returns>True if verified, false if not.</returns>
		public bool Verify(ArraySegment<byte> data, ArraySegment<byte> signature, string remotePublicKey) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (remotePublicKey == null) {
				throw new ArgumentNullException(nameof(remotePublicKey), "Remote public key is null");
			} else if (signature.Count != 256) {
				throw new ArgumentOutOfRangeException(nameof(signature), string.Format(
					"Signature count {0} is not 256", signature.Count
				));
			}

			// Decode key
			byte[] Modulus = Convert.FromBase64String(remotePublicKey);
			if (Modulus.Length != 256) {
				throw new ArgumentOutOfRangeException(nameof(remotePublicKey), string.Format(
					"Remote public key length {0} is not 256", Modulus.Length
				));
			}
			
			// Verify
			using (RSA rsa = CreateRSA()) {

				// Import key
				rsa.ImportParameters(new RSAParameters() {
					Exponent = new byte[] { 1, 0, 1 },
					Modulus = Modulus,
				});

				// If provided signature is a single array, use it
				if (signature.Array.Length == 256) {
					HashAlgorithmName hash = HashAlgorithmName.SHA256;
					RSASignaturePadding padding = RSASignaturePadding.Pkcs1;
					return rsa.VerifyData(data.Array, data.Offset, data.Count, signature.Array, hash, padding);
				}

				// Copy signature to a single array and verify
				byte[] copy = Allocator.CreatePacket(256);
				try {
					Array.Copy(signature.Array, signature.Offset, copy, 0, 256);
					HashAlgorithmName hash = HashAlgorithmName.SHA256;
					RSASignaturePadding padding = RSASignaturePadding.Pkcs1;
					return rsa.VerifyData(data.Array, data.Offset, data.Count, copy, hash, padding);
				} finally {
					Allocator.ReturnPacket(ref copy);
				}

			}

		}

		private static RSA CreateRSA() {

			// Create RSA
			RSA rsa = System.Security.Cryptography.RSA.Create();
			rsa.KeySize = 2048;

			// If key size is invalid, recreate
			if (rsa.KeySize != 2048 && rsa is RSACryptoServiceProvider) {
				rsa.Dispose();
				rsa = new RSACryptoServiceProvider(2048);
			}

			// If key size still invalid, RSA creation failed
			if (rsa.KeySize != 2048) {
				throw new InvalidOperationException(string.Format(
					"RSA key size {0} is not 2048", rsa.KeySize
				));
			}
			
			return rsa;

		}

	}

}
