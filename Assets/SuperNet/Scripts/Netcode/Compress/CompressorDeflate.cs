using SuperNet.Netcode.Util;
using System;
using System.IO;
using System.IO.Compression;

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0063 // Use simple 'using' statement

namespace SuperNet.Netcode.Compress {

	/// <summary>
	/// Compression based on the DEFLATE algorithm.
	/// </summary>
	public sealed class CompressorDeflate : ICompressor {

		private readonly Allocator Allocator;
		private bool Disposed;

		/// <summary>Create a new DEFLATE compressor.</summary>
		/// <param name="allocator">Allocator to use for resizing buffers.</param>
		public CompressorDeflate(Allocator allocator) {

			// Validate
			if (allocator == null) {
				throw new ArgumentNullException(nameof(allocator), "No allocator");
			}

			// Initialize
			Allocator = allocator;
			Disposed = false;

		}

		/// <summary>Instantly dispose of all resources.</summary>
		public void Dispose() {
			Disposed = true;
		}

		/// <summary>Compute the maximum compressed length before compressing.</summary>
		/// <param name="inputLength">Length of the uncompressed input.</param>
		/// <returns>Maximum possible compressed length.</returns>
		public int MaxCompressedLength(int inputLength) {
			return inputLength + 5 * (1 + inputLength / 16383);
		}

		/// <summary>Compress data.</summary>
		/// <param name="input">Array segment to compress.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		public int Compress(ArraySegment<byte> input, byte[] output, int offset) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output == null) {
				throw new ArgumentNullException(nameof(output), "Output buffer is null");
			} else if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), string.Format(
					"Output offset {0} is negative", offset
				));
			}

			// Compress
			using (Stream stream = new MemoryStream(output, offset, output.Length - offset, true)) {
				using (Stream zip = new DeflateStream(stream, CompressionLevel.Fastest, true)) {
					zip.Write(input.Array, input.Offset, input.Count);
				}
				return (int)stream.Position;
			}

		}

		/// <summary>Decompress data and resize output if needed.</summary>
		/// <param name="input">Array segment to decompress.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		public int Decompress(ArraySegment<byte> input, ref byte[] output, int offset) {

			// Validate
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (output == null) {
				throw new ArgumentNullException(nameof(output), "Output buffer is null");
			} else if (offset < 0) {
				throw new ArgumentOutOfRangeException(nameof(offset), string.Format(
					"Output offset {0} is negative", offset
				));
			}

			// Decompress
			using (Stream stream = new MemoryStream(input.Array, input.Offset, input.Count, false))
			using (Stream zip = new DeflateStream(stream, CompressionMode.Decompress)) {
				int total = 0, outputOffset = offset, count;
				do {

					// Expand output if needed
					if (output == null || outputOffset >= output.Length)
						output = Allocator.ExpandMessage(output, outputOffset);

					// Process next chunk
					count = zip.Read(output, outputOffset, output.Length - outputOffset);
					outputOffset += count;
					total += count;

				} while (count > 0);
				return total;
			}

		}
		
	}

}
