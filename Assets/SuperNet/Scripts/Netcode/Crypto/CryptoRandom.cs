using System;
using System.Security.Cryptography;

namespace SuperNet.Netcode.Crypto {

	/// <summary>
	/// Cryptographically secure random number generator.
	/// </summary>
	public class CryptoRandom : ICryptoRandom {

		private readonly Lazy<RandomNumberGenerator> Generator;
		private bool Disposed;
		
		/// <summary>Create random number generator.</summary>
		public CryptoRandom() {
			Generator = new Lazy<RandomNumberGenerator>(RandomNumberGenerator.Create, true);
			Disposed = false;
		}

		/// <summary>Instantly dispose of all resources.</summary>
		public void Dispose() {
			Disposed = true;
			if (Generator.IsValueCreated) {
				Generator.Value.Dispose();
			}
		}

		/// <summary>
		/// Generate cryptographically secure random data.
		/// <para>This method is thread safe.</para>
		/// </summary>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <param name="count">Number of bytes to write.</param>
		public void GetBytes(byte[] output, int offset, int count) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output == null) {
				throw new ArgumentNullException(nameof(output), "Output buffer is null");
			} else if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), string.Format(
					"Offset {0} is negative", offset
				));
			} else if (offset + count > output.Length) {
				throw new ArgumentOutOfRangeException(nameof(count), string.Format(
					"Count {0} from {1} extends beyond output length {2}",
					count, offset, output.Length
				));
			}
			
			// Generate random data
			Generator.Value.GetBytes(output, offset, count);

		}

	}

}
