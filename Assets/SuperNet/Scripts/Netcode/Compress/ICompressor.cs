using System;

namespace SuperNet.Netcode.Compress {

	/// <summary>
	/// Defines methods for compressing and decompressing network packets.
	/// </summary>
	public interface ICompressor : IDisposable {

		/// <summary>Compute the maximum compressed length before compressing.</summary>
		/// <param name="inputLength">Length of the uncompressed input.</param>
		/// <returns>Maximum possible compressed length.</returns>
		int MaxCompressedLength(int inputLength);

		/// <summary>Compress data.</summary>
		/// <param name="input">Array segment to compress.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		int Compress(ArraySegment<byte> input, byte[] output, int offset);

		/// <summary>Decompress data and resize output if needed.</summary>
		/// <param name="input">Array segment to decompress.</param>
		/// <param name="output">Output buffer to write to.</param>
		/// <param name="offset">Output offset to write to.</param>
		/// <returns>Total number of bytes written to the output.</returns>
		int Decompress(ArraySegment<byte> input, ref byte[] output, int offset);

	}

}
