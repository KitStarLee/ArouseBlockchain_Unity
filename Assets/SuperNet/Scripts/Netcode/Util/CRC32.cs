using System;
using System.Threading;

#if NETCOREAPP3_0
	using System.Runtime.Intrinsics.X86;
#endif

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Fast CRC32 error-checking code calculation for network packets.
	/// </summary>
	public static class CRC32 {

		private const uint Poly = 0x82F63B78u;
		private static uint[] Table = null;

		#if NETCOREAPP3_0
			private readonly bool _sseAvailable;
			private readonly bool _x64Available;
			public ChecksumCrc32() {
				_sseAvailable = Sse42.IsSupported;
				_x64Available = Sse42.X64.IsSupported;
			}
		#endif

		/// <summary>Compute a CRC32 code for the input array segment.</summary>
		/// <param name="array">Array to read from.</param>
		/// <param name="offset">Offset in the array to start reading from.</param>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>CRC32 code of the input.</returns>
		public static uint Compute(byte[] array, int offset, int count) {

			// Validate
			if (array == null) {
				throw new ArgumentNullException(nameof(array), "Array is null.");
			} else if (offset < 0) {
				throw new ArgumentException(string.Format(
					"Offset {0} is negative", offset
				), nameof(offset));
			} else if (count < 0) {
				throw new ArgumentException(string.Format(
					"Count {0} is negative", count
				), nameof(count));
			} else if (offset + count > array.Length) {
				throw new ArgumentException(string.Format(
					"Count {0} from offset {1} extends beyond array length {2}",
					count, offset, array.Length
				), nameof(count));
			}
			
			uint crcLocal = uint.MaxValue;

			// Use SSE4 if available
			#if NETCOREAPP3_0
				if (_sseAvailable) {
					if (_x64Available) {
						while (count >= 8) {
							crcLocal = (uint)Sse42.X64.Crc32(crcLocal, BitConverter.ToUInt64(array, offset));
							offset += 8;
							count -= 8;
						}
					}
					while (count > 0) {
						crcLocal = Sse42.Crc32(crcLocal, array[offset]);
						offset++;
						count--;
					}
					return crcLocal ^ uint.MaxValue;
				}
			#endif

			// Generate lookup table if not yet generated
			if (Table == null) {
				uint[] table = new uint[16 * 256];
				for (uint i = 0; i < 256; i++) {
					uint res = i;
					for (int t = 0; t < 16; t++) {
						for (int k = 0; k < 8; k++) {
							res = (res & 1) == 1 ? Poly ^ (res >> 1) : (res >> 1);
						}
						table[t * 256 + i] = res;
					}
				}
				Interlocked.Exchange(ref Table, table);
			}

			while (count >= 16) {
				var a = Table[(3 * 256) + array[offset + 12]]
						^ Table[(2 * 256) + array[offset + 13]]
						^ Table[(1 * 256) + array[offset + 14]]
						^ Table[(0 * 256) + array[offset + 15]];

				var b = Table[(7 * 256) + array[offset + 8]]
						^ Table[(6 * 256) + array[offset + 9]]
						^ Table[(5 * 256) + array[offset + 10]]
						^ Table[(4 * 256) + array[offset + 11]];

				var c = Table[(11 * 256) + array[offset + 4]]
						^ Table[(10 * 256) + array[offset + 5]]
						^ Table[(9 * 256) + array[offset + 6]]
						^ Table[(8 * 256) + array[offset + 7]];

				var d = Table[(15 * 256) + ((byte)crcLocal ^ array[offset])]
						^ Table[(14 * 256) + ((byte)(crcLocal >> 8) ^ array[offset + 1])]
						^ Table[(13 * 256) + ((byte)(crcLocal >> 16) ^ array[offset + 2])]
						^ Table[(12 * 256) + ((crcLocal >> 24) ^ array[offset + 3])];

				crcLocal = d ^ c ^ b ^ a;
				offset += 16;
				count -= 16;
			}

			while (--count >= 0) {
				crcLocal = Table[(byte)(crcLocal ^ array[offset++])] ^ crcLocal >> 8;
			}

			return crcLocal ^ uint.MaxValue;

		}

	}

}
