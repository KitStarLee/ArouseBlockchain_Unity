/*
 * Improved version to C# LibLZF Port:
 * Copyright (c) 2010 Roman Atachiants <kelindar@gmail.com>
 * 
 * Original CLZF Port:
 * Copyright (c) 2005 Oren J. Maurice <oymaurice@hazorea.org.il>
 * 
 * Original LibLZF Library & Algorithm:
 * Copyright (c) 2000-2008 Marc Alexander Lehmann <schmorp@schmorp.de>
 * 
 * Redistribution and use in source and binary forms, with or without modifica-
 * tion, are permitted provided that the following conditions are met:
 * 
 *   1.  Redistributions of source code must retain the above copyright notice,
 *       this list of conditions and the following disclaimer.
 * 
 *   2.  Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 * 
 *   3.  The name of the author may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MER-
 * CHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO
 * EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPE-
 * CIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTH-
 * ERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Alternatively, the contents of this file may be used under the terms of
 * the GNU General Public License version 2 (the "GPL"), in which case the
 * provisions of the GPL are applicable instead of the above. If you wish to
 * allow the use of your version of this file only under the terms of the
 * GPL and not to allow others to use your version of this file under the
 * BSD license, indicate your decision by deleting the provisions above and
 * replace them with the notice and other provisions required by the GPL. If
 * you do not delete the provisions above, a recipient may use your version
 * of this file under either the BSD or the GPL.
 */
using SuperNet.Netcode.Util;
using System;

#pragma warning disable IDE0016 // Use 'throw' expression

namespace SuperNet.Netcode.Compress {

	/// <summary>
	/// Compression based on the LZF algorithm.
	/// </summary>
	public sealed class CompressorLZF : ICompressor {

		private const uint HLOG = 14;
		private const uint HSIZE = (1 << 14);
		private const uint MAX_LIT = (1 << 5);
		private const uint MAX_OFF = (1 << 13);
		private const uint MAX_REF = ((1 << 8) + (1 << 3));

		private readonly Allocator Allocator;
		private bool Disposed;

		/// <summary>Create a new LZF compressor.</summary>
		/// <param name="allocator">Allocator to use for resizing buffers.</param>
		public CompressorLZF(Allocator allocator) {

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
			return (((inputLength * 33) >> 5) + 1);
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

			int lit = 0;
			uint hval, iidx = 0, oidx = 0;
			long hslot, reference, off;
			long[] HashTable = null;

			try {
				
				HashTable = Allocator.HashTableCreate((int)HSIZE);
				Array.Clear(HashTable, 0, (int)HSIZE);
				hval = (uint)(((input.Array[input.Offset + iidx]) << 8) | input.Array[input.Offset + iidx + 1]);

				while (true) {

					if (iidx < input.Count - 2) {

						hval = (hval << 8) | input.Array[input.Offset + iidx + 2];
						hslot = ((hval ^ (hval << 5)) >> (int)(((3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1));
						reference = HashTable[hslot];
						HashTable[hslot] = (long)iidx;

						if ((off = iidx - reference - 1) < MAX_OFF
							&& iidx + 4 < input.Count
							&& reference > 0
							&& input.Array[input.Offset + reference + 0] == input.Array[input.Offset + iidx + 0]
							&& input.Array[input.Offset + reference + 1] == input.Array[input.Offset + iidx + 1]
							&& input.Array[input.Offset + reference + 2] == input.Array[input.Offset + iidx + 2]
						) {

							uint len = 2;
							uint maxlen = (uint)input.Count - iidx - len;
							maxlen = maxlen > MAX_REF ? MAX_REF : maxlen;

							// Make sure output is large enough
							if (offset + oidx + lit + 4 >= output.Length) {
								throw new InvalidOperationException(string.Format(
									"Compressed chunk {0} extends beyond output length {1} at offset {2}",
									(oidx + lit + 4), output.Length, offset
								));
							}

							do {
								len++;
							} while (
								len < maxlen &&
								input.Array[input.Offset + reference + len] == input.Array[input.Offset + iidx + len]
							);

							if (lit != 0) {
								output[offset + oidx++] = (byte)(lit - 1);
								lit = -lit;
								do {
									output[offset + oidx++] = input.Array[input.Offset + iidx + lit];
								} while ((++lit) != 0);
							}

							len -= 2;
							iidx++;

							if (len < 7) {
								output[offset + oidx++] = (byte)((off >> 8) + (len << 5));
							} else {
								output[offset + oidx++] = (byte)((off >> 8) + (7 << 5));
								output[offset + oidx++] = (byte)(len - 7);
							}

							output[offset + oidx++] = (byte)off;

							iidx += len - 1;
							hval = (uint)(((input.Array[input.Offset + iidx]) << 8) | input.Array[input.Offset + iidx + 1]);

							hval = (hval << 8) | input.Array[input.Offset + iidx + 2];
							HashTable[((hval ^ (hval << 5)) >> (int)(((3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1))] = iidx;
							iidx++;

							hval = (hval << 8) | input.Array[input.Offset + iidx + 2];
							HashTable[((hval ^ (hval << 5)) >> (int)(((3 * 8 - HLOG)) - hval * 5) & (HSIZE - 1))] = iidx;
							iidx++;
							continue;

						}

					} else if (iidx == input.Count) {
						break;
					}

					lit++;
					iidx++;

					if (lit == MAX_LIT) {

						// Make sure output is large enough
						if (offset + oidx + 1 + MAX_LIT >= output.Length) {
							throw new InvalidOperationException(string.Format(
								"Compressed chunk {0} extends beyond output length {1} at offset {2}",
								oidx + 1 + MAX_LIT, output.Length, offset
							));
						}

						output[offset + oidx++] = (byte)(MAX_LIT - 1);
						lit = -lit;

						do {
							output[offset + oidx++] = input.Array[input.Offset + iidx + lit];
						} while ((++lit) != 0);

					}

				}

			} finally {
				Allocator.HashTableReturn(HashTable);
			}

			if (lit != 0) {

				// Make sure output is large enough
				if (offset + oidx + lit + 1 >= output.Length) {
					throw new InvalidOperationException(string.Format(
						"Compressed chunk {0} extends beyond output length {1} at offset {2}",
						oidx + lit + 1, output.Length, offset
					));
				}
					
				output[offset + oidx++] = (byte)(lit - 1);
				lit = -lit;

				do {
					output[offset + oidx++] = input.Array[input.Offset + iidx + lit];
				} while ((++lit) != 0);

			}

			return (int)oidx;

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

			uint iidx = 0;
			uint oidx = 0;

			do {

				uint ctrl = input.Array[input.Offset + iidx++];

				if (ctrl < (1 << 5)) {

					ctrl++;

					// Expand output if needed
					if (output == null || output.Length < oidx + ctrl) {
						output = Allocator.ExpandMessage(output, (int)oidx, (int)ctrl);
					}

					do {
						output[oidx++] = input.Array[input.Offset + iidx++];
					} while ((--ctrl) != 0);

				} else {

					uint len = ctrl >> 5;

					int reference = (int)(oidx - ((ctrl & 0x1f) << 8) - 1);

					if (len == 7) {
						len += input.Array[input.Offset + iidx++];
					}

					reference -= input.Array[input.Offset + iidx++];

					if (reference < 0) {
						throw new InvalidOperationException("Bad compressed data");
					}

					// Expand output if needed
					if (output == null || output.Length < oidx + len + 2) {
						output = Allocator.ExpandMessage(output, (int)oidx, (int)(len + 2));
					}

					output[oidx++] = output[reference++];
					output[oidx++] = output[reference++];

					do {
						output[oidx++] = output[reference++];
					} while ((--len) != 0);

				}

			} while (iidx < input.Count);

			return (int)oidx;

		}
		
	}

}
