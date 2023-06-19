using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Platform independent serialization of values in Big-endian (network byte order).
	/// </summary>
	public static class Serializer {
		
		/// <summary>Character encoding to use when serializing strings.</summary>
		public static Encoding Encoding {
			get {
				if (UTFEncoding == null) {
					UTFEncoding = new UTF8Encoding(false, true);
					return UTFEncoding;
				} else {
					return UTFEncoding;
				}
			}
		}

		private static UTF8Encoding UTFEncoding = null;

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatUnion {

			[FieldOffset(0)]
			public uint IntData;

			[FieldOffset(0)]
			public float FloatData;

		}

		[StructLayout(LayoutKind.Explicit)]
		private struct DoubleUnion {

			[FieldOffset(0)]
			public ulong IntData;

			[FieldOffset(0)]
			public double DoubleData;

		}

#if BIGENDIAN
		
		/// <summary>Serialize short (2 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write16(byte[] buffer, int offset, short value) {
			buffer[offset + 1] = (byte)(value);
			buffer[offset] = (byte)(value >> 8);
		}

		/// <summary>Serialize ushort (2 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write16(byte[] buffer, int offset, ushort value) {
			buffer[offset + 1] = (byte)(value);
			buffer[offset] = (byte)(value >> 8);
		}
		
		/// <summary>Serialize int (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write32(byte[] buffer, int offset, int value) {
			buffer[offset + 3] = (byte)(value);
			buffer[offset + 2] = (byte)(value >> 8);
			buffer[offset + 1] = (byte)(value >> 16);
			buffer[offset] = (byte)(value >> 24);
		}

		/// <summary>Serialize uint (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write32(byte[] buffer, int offset, uint value) {
			buffer[offset + 3] = (byte)(value);
			buffer[offset + 2] = (byte)(value >> 8);
			buffer[offset + 1] = (byte)(value >> 16);
			buffer[offset] = (byte)(value >> 24);
		}

		/// <summary>Serialize long (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write64(byte[] buffer, int offset, long value) {
			buffer[offset + 7] = (byte)(value);
			buffer[offset + 6] = (byte)(value >> 8);
			buffer[offset + 5] = (byte)(value >> 16);
			buffer[offset + 4] = (byte)(value >> 24);
			buffer[offset + 3] = (byte)(value >> 32);
			buffer[offset + 2] = (byte)(value >> 40);
			buffer[offset + 1] = (byte)(value >> 48);
			buffer[offset] = (byte)(value >> 56);
		}

		/// <summary>Serialize ulong (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write64(byte[] buffer, int offset, ulong value) {
			buffer[offset + 7] = (byte)(value);
			buffer[offset + 6] = (byte)(value >> 8);
			buffer[offset + 5] = (byte)(value >> 16);
			buffer[offset + 4] = (byte)(value >> 24);
			buffer[offset + 3] = (byte)(value >> 32);
			buffer[offset + 2] = (byte)(value >> 40);
			buffer[offset + 1] = (byte)(value >> 48);
			buffer[offset] = (byte)(value >> 56);
		}

		/// <summary>Serialize float (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSingle(byte[] buffer, int offset, float value) {
			FloatUnion union = new FloatUnion { FloatData = value };
			buffer[offset + 3] = (byte)(union.IntData);
			buffer[offset + 2] = (byte)(union.IntData >> 8);
			buffer[offset + 1] = (byte)(union.IntData >> 16);
			buffer[offset] = (byte)(union.IntData >> 24);
		}

		/// <summary>Serialize double (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteDouble(byte[] buffer, int offset, double value) {
			DoubleUnion union = new DoubleUnion { DoubleData = value };
			buffer[offset + 7] = (byte)(union.IntData);
			buffer[offset + 6] = (byte)(union.IntData >> 8);
			buffer[offset + 5] = (byte)(union.IntData >> 16);
			buffer[offset + 4] = (byte)(union.IntData >> 24);
			buffer[offset + 3] = (byte)(union.IntData >> 32);
			buffer[offset + 2] = (byte)(union.IntData >> 40);
			buffer[offset + 1] = (byte)(union.IntData >> 48);
			buffer[offset] = (byte)(union.IntData >> 56);
		}

		/// <summary>Deserialize short (2 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static short ReadInt16(byte[] buffer, int offset) {
			ushort value = 0;
			value |= buffer[offset + 1];
			value |= (ushort)(buffer[offset] << 8);
			return (short)value;
		}

		/// <summary>Deserialize ushort (2 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static ushort ReadUInt16(byte[] buffer, int offset) {
			ushort value = 0;
			value |= buffer[offset + 1];
			value |= (ushort)(buffer[offset] << 8);
			return value;
		}

		/// <summary>Deserialize int (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static int ReadInt32(byte[] buffer, int offset) {
			uint value = 0;
			value |= buffer[offset + 3];
			value |= (uint)(buffer[offset + 2] << 8);
			value |= (uint)(buffer[offset + 1] << 16);
			value |= (uint)(buffer[offset] << 24);
			return (int)value;
		}

		/// <summary>Deserialize uint (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static uint ReadUInt32(byte[] buffer, int offset) {
			uint value = 0;
			value |= buffer[offset + 3];
			value |= (uint)(buffer[offset + 2] << 8);
			value |= (uint)(buffer[offset + 1] << 16);
			value |= (uint)(buffer[offset] << 24);
			return value;
		}

		/// <summary>Deserialize long (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static long ReadInt64(byte[] buffer, int offset) {
			ulong value = 0;
			value |= buffer[offset + 7];
			value |= ((ulong)buffer[offset + 6]) << 8;
			value |= ((ulong)buffer[offset + 5]) << 16;
			value |= ((ulong)buffer[offset + 4]) << 24;
			value |= ((ulong)buffer[offset + 3]) << 32;
			value |= ((ulong)buffer[offset + 2]) << 40;
			value |= ((ulong)buffer[offset + 1]) << 48;
			value |= ((ulong)buffer[offset]) << 56;
			return (long)value;
		}

		/// <summary>Deserialize ulong (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static ulong ReadUInt64(byte[] buffer, int offset) {
			ulong value = 0;
			value |= buffer[offset + 7];
			value |= ((ulong)buffer[offset + 6]) << 8;
			value |= ((ulong)buffer[offset + 5]) << 16;
			value |= ((ulong)buffer[offset + 4]) << 24;
			value |= ((ulong)buffer[offset + 3]) << 32;
			value |= ((ulong)buffer[offset + 2]) << 40;
			value |= ((ulong)buffer[offset + 1]) << 48;
			value |= ((ulong)buffer[offset]) << 56;
			return value;
		}

		/// <summary>Deserialize float (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static float ReadSingle(byte[] buffer, int offset) {
			FloatUnion union = new FloatUnion { IntData = 0 };
			union.IntData |= buffer[offset + 3];
			union.IntData |= (uint)(buffer[offset + 2] << 8);
			union.IntData |= (uint)(buffer[offset + 1] << 16);
			union.IntData |= (uint)(buffer[offset] << 24);
			return union.FloatData;
		}

		/// <summary>Deserialize double (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static double ReadDouble(byte[] buffer, int offset) {
			DoubleUnion union = new DoubleUnion { IntData = 0 };
			union.IntData |= buffer[offset + 7];
			union.IntData |= ((ulong)buffer[offset + 6]) << 8;
			union.IntData |= ((ulong)buffer[offset + 5]) << 16;
			union.IntData |= ((ulong)buffer[offset + 4]) << 24;
			union.IntData |= ((ulong)buffer[offset + 3]) << 32;
			union.IntData |= ((ulong)buffer[offset + 2]) << 40;
			union.IntData |= ((ulong)buffer[offset + 1]) << 48;
			union.IntData |= ((ulong)buffer[offset]) << 56;
			return union.DoubleData;
		}
		
#else

		/// <summary>Serialize short (2 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write16(byte[] buffer, int offset, short value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
		}

		/// <summary>Serialize ushort (2 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write16(byte[] buffer, int offset, ushort value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
		}

		/// <summary>Serialize int (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write32(byte[] buffer, int offset, int value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
		}

		/// <summary>Serialize uint (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write32(byte[] buffer, int offset, uint value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
		}

		/// <summary>Serialize long (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write64(byte[] buffer, int offset, long value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
			buffer[offset + 4] = (byte)(value >> 32);
			buffer[offset + 5] = (byte)(value >> 40);
			buffer[offset + 6] = (byte)(value >> 48);
			buffer[offset + 7] = (byte)(value >> 56);
		}

		/// <summary>Serialize ulong (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void Write64(byte[] buffer, int offset, ulong value) {
			buffer[offset] = (byte)(value);
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
			buffer[offset + 4] = (byte)(value >> 32);
			buffer[offset + 5] = (byte)(value >> 40);
			buffer[offset + 6] = (byte)(value >> 48);
			buffer[offset + 7] = (byte)(value >> 56);
		}

		/// <summary>Serialize float (4 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSingle(byte[] buffer, int offset, float value) {
			FloatUnion union = new FloatUnion { FloatData = value };
			buffer[offset] = (byte)(union.IntData);
			buffer[offset + 1] = (byte)(union.IntData >> 8);
			buffer[offset + 2] = (byte)(union.IntData >> 16);
			buffer[offset + 3] = (byte)(union.IntData >> 24);
		}

		/// <summary>Serialize double (8 bytes) to the buffer.</summary>
		/// <param name="buffer">Buffer to write to.</param>
		/// <param name="offset">Buffer offset to write to.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteDouble(byte[] buffer, int offset, double value) {
			DoubleUnion union = new DoubleUnion { DoubleData = value };
			buffer[offset] = (byte)(union.IntData);
			buffer[offset + 1] = (byte)(union.IntData >> 8);
			buffer[offset + 2] = (byte)(union.IntData >> 16);
			buffer[offset + 3] = (byte)(union.IntData >> 24);
			buffer[offset + 4] = (byte)(union.IntData >> 32);
			buffer[offset + 5] = (byte)(union.IntData >> 40);
			buffer[offset + 6] = (byte)(union.IntData >> 48);
			buffer[offset + 7] = (byte)(union.IntData >> 56);
		}

		/// <summary>Deserialize short (2 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static short ReadInt16(byte[] buffer, int offset) {
			ushort value = 0;
			value |= buffer[offset];
			value |= (ushort)(buffer[offset + 1] << 8);
			return (short)value;
		}

		/// <summary>Deserialize ushort (2 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static ushort ReadUInt16(byte[] buffer, int offset) {
			ushort value = 0;
			value |= buffer[offset];
			value |= (ushort)(buffer[offset + 1] << 8);
			return value;
		}

		/// <summary>Deserialize int (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static int ReadInt32(byte[] buffer, int offset) {
			uint value = 0;
			value |= buffer[offset];
			value |= (uint)(buffer[offset + 1] << 8);
			value |= (uint)(buffer[offset + 2] << 16);
			value |= (uint)(buffer[offset + 3] << 24);
			return (int)value;
		}

		/// <summary>Deserialize uint (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static uint ReadUInt32(byte[] buffer, int offset) {
			uint value = 0;
			value |= buffer[offset];
			value |= (uint)(buffer[offset + 1] << 8);
			value |= (uint)(buffer[offset + 2] << 16);
			value |= (uint)(buffer[offset + 3] << 24);
			return value;
		}

		/// <summary>Deserialize long (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static long ReadInt64(byte[] buffer, int offset) {
			ulong value = 0;
			value |= buffer[offset];
			value |= ((ulong)buffer[offset + 1]) << 8;
			value |= ((ulong)buffer[offset + 2]) << 16;
			value |= ((ulong)buffer[offset + 3]) << 24;
			value |= ((ulong)buffer[offset + 4]) << 32;
			value |= ((ulong)buffer[offset + 5]) << 40;
			value |= ((ulong)buffer[offset + 6]) << 48;
			value |= ((ulong)buffer[offset + 7]) << 56;
			return (long)value;
		}

		/// <summary>Deserialize ulong (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static ulong ReadUInt64(byte[] buffer, int offset) {
			ulong value = 0;
			value |= buffer[offset];
			value |= ((ulong)buffer[offset + 1]) << 8;
			value |= ((ulong)buffer[offset + 2]) << 16;
			value |= ((ulong)buffer[offset + 3]) << 24;
			value |= ((ulong)buffer[offset + 4]) << 32;
			value |= ((ulong)buffer[offset + 5]) << 40;
			value |= ((ulong)buffer[offset + 6]) << 48;
			value |= ((ulong)buffer[offset + 7]) << 56;
			return value;
		}

		/// <summary>Deserialize float (4 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static float ReadSingle(byte[] buffer, int offset) {
			FloatUnion union = new FloatUnion { IntData = 0 };
			union.IntData |= buffer[offset];
			union.IntData |= (uint)(buffer[offset + 1] << 8);
			union.IntData |= (uint)(buffer[offset + 2] << 16);
			union.IntData |= (uint)(buffer[offset + 3] << 24);
			return union.FloatData;
		}

		/// <summary>Deserialize double (8 bytes) from the buffer.</summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Buffer offset to read from.</param>
		/// <returns>Deserialized value.</returns>
		public static double ReadDouble(byte[] buffer, int offset) {
			DoubleUnion union = new DoubleUnion { IntData = 0 };
			union.IntData |= buffer[offset];
			union.IntData |= ((ulong)buffer[offset + 1]) << 8;
			union.IntData |= ((ulong)buffer[offset + 2]) << 16;
			union.IntData |= ((ulong)buffer[offset + 3]) << 24;
			union.IntData |= ((ulong)buffer[offset + 4]) << 32;
			union.IntData |= ((ulong)buffer[offset + 5]) << 40;
			union.IntData |= ((ulong)buffer[offset + 6]) << 48;
			union.IntData |= ((ulong)buffer[offset + 7]) << 56;
			return union.DoubleData;
		}

#endif

	}

}
