using System;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Fast deserializer for network messages.
	/// </summary>
	public class Reader : IDisposable {

		/// <summary>
		/// Number of bytes still available to be read or 0 if the reader has been disposed.
		/// </summary>
		public int Available => Last - Position;

		/// <summary>
		/// Current position in the internal buffer or 0 if the reader has been disposed.
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		/// Index of the first byte that is included in the message.
		/// </summary>
		public readonly int First;

		/// <summary>
		/// Index of the last byte that is not included in the message.
		/// </summary>
		public readonly int Last;

		/// <summary>
		/// True if reader has been disposed.
		/// </summary>
		public bool Disposed { get; private set; }

		/// <summary>
		/// Internal buffer that contains the serialized message or null if the reader has been disposed.
		/// </summary>
		public byte[] Buffer { get; private set; }

		/// <summary>Create a new reader from the provided array segment.</summary>
		/// <param name="array">Array to read from.</param>
		/// <param name="offset">Offset in the array to start reading from.</param>
		/// <param name="count">Number of bytes to read.</param>
		public Reader(byte[] array, int offset, int count) {

			// Validate
			if (array == null) {
				throw new ArgumentNullException(nameof(array), "Array is null");
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

			// Initialize
			Position = offset;
			First = offset;
			Last = offset + count;
			Disposed = false;
			Buffer = array;

		}

		/// <summary>Create a new reader from the provided array segment.</summary>
		/// <param name="segment">Array segment to read from.</param>
		public Reader(ArraySegment<byte> segment) {
			Buffer = segment.Array;
			Position = segment.Offset;
			Disposed = false;
			First = segment.Offset;
			Last = segment.Offset + segment.Count;
		}

		/// <summary>
		/// Invalidate the underlying buffer when it gets used for something else.
		/// Calling this causes all future read operation to fail.
		/// </summary>
		public void Dispose() {
          //  UnityEngine.Debug.Log("----03");
            Disposed = true;
			Position = 0;
			Buffer = null;
        }

		/// <summary>Manually set the read position.</summary>
		/// <param name="position">Position to set.</param>
		public void Reset(int position) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (position < First) {
				throw new ArgumentException(string.Format(
					"Reset position {0} is before the first byte at {1}",
					position, First
				), nameof(position));
			} else if (position > Last) {
				throw new ArgumentException(string.Format(
					"Reset position {0} is after the last byte at {1}",
					position, Last
				), nameof(position));
			} else {
				Position = position;
			}
		}

		/// <summary>Advance the read position without reading anything.</summary>
		/// <param name="length">Number of bytes to skip for.</param>
		public void Skip(int length) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (length < 0) {
				throw new ArgumentException(string.Format(
					"Skip length {0} is negative", length
				), nameof(length));
			} else if (Position + length > Last) {
				throw new ArgumentException(string.Format(
					"Skip length {0} from position {1} extends beyond {2}",
					length, Position, Last
				));
			} else {
				Position += length;
			}
		}

		/// <summary>Check if message has enough bytes left, throw exception if not.</summary>
		/// <param name="length">Length to check for.</param>
		public void CheckAvailableSpace(int length) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (length < 0) {
				throw new ArgumentException(string.Format(
					"Read length {0} is negative", length
				), nameof(length));
			} else if (Position + length > Last) {
				throw new ArgumentException(string.Format(
					"Read length {0} from position {1} extends beyond {2}",
					length, Position, Last
				));
			}
		}

		/// <summary>Read 4 bytes length, then UTF8 encoded string.</summary>
		/// <returns>String value or null if length is negative.</returns>
		public string ReadString() {
			CheckAvailableSpace(4);
			int length = Serializer.ReadInt32(Buffer, Position);
			if (length < 0) {
				Position += 4;
				return null;
			} else {
				CheckAvailableSpace(length + 4);
				char[] chars = Serializer.Encoding.GetChars(Buffer, Position + 4, length);
				Position += length + 4;
				return new string(chars);
			}
		}

		/// <summary>Read into an array segment.</summary>
		/// <param name="array">Array to read to.</param>
		/// <param name="offset">Array offset to read to.</param>
		/// <param name="count">Number of bytes to read.</param>
		public void ReadBytes(byte[] array, int offset, int count) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (array == null) {
				throw new ArgumentNullException(nameof(array), "Array is null");
			} else if (offset < 0) {
				throw new ArgumentException(string.Format(
					"Offset {0} is negative", count
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
			} else {
				CheckAvailableSpace(count);
				Array.Copy(Buffer, Position, array, offset, count);
				Position += count;
			}
		}

		/// <summary>Read a single boolean (1 byte).</summary>
		/// <returns>Boolean value.</returns>
		public bool ReadBoolean() {
			CheckAvailableSpace(1);
			bool value = (Buffer[Position] & 1) != 0;
			Position++;
			return value;
		}

		/// <summary>Read 8 booleans (1 byte).</summary>
		/// <param name="v0">First boolean value.</param>
		/// <param name="v1">Second boolean value.</param>
		/// <param name="v2">Third boolean value.</param>
		/// <param name="v3">Fourth boolean value.</param>
		/// <param name="v4">Fifth boolean value.</param>
		/// <param name="v5">Sixth boolean value.</param>
		/// <param name="v6">Seventh boolean value.</param>
		/// <param name="v7">Eighth boolean value.</param>
		public void ReadBoolean(
			out bool v0, out bool v1, out bool v2, out bool v3,
			out bool v4, out bool v5, out bool v6, out bool v7
		) {
			CheckAvailableSpace(1);
			byte value = Buffer[Position];
			v0 = (value & (1 << 0)) != 0;
			v1 = (value & (1 << 1)) != 0;
			v2 = (value & (1 << 2)) != 0;
			v3 = (value & (1 << 3)) != 0;
			v4 = (value & (1 << 4)) != 0;
			v5 = (value & (1 << 5)) != 0;
			v6 = (value & (1 << 6)) != 0;
			v7 = (value & (1 << 7)) != 0;
			Position++;
		}

		/// <summary>Read a single character (2 bytes).</summary>
		/// <returns>Character value.</returns>
		public char ReadChar() {
			CheckAvailableSpace(2);
			ushort value = Serializer.ReadUInt16(Buffer, Position);
			Position += 2;
			return (char)value;
		}

		/// <summary>Read byte (1 byte).</summary>
		/// <returns>Byte value.</returns>
		public byte ReadByte() {
			CheckAvailableSpace(1);
			byte value = Buffer[Position];
			Position++;
			return value;
		}

		/// <summary>Read signed byte (1 byte).</summary>
		/// <returns>Signed byte value.</returns>
		public sbyte ReadSByte() {
			CheckAvailableSpace(1);
			sbyte value = (sbyte)Buffer[Position];
			Position++;
			return value;
		}

		/// <summary>Read short (2 bytes).</summary>
		/// <returns>Short value.</returns>
		public short ReadInt16() {
			CheckAvailableSpace(2);
			short value = Serializer.ReadInt16(Buffer, Position);
			Position += 2;
			return value;
		}

		/// <summary>Read unsigned short (2 bytes).</summary>
		/// <returns>Unsigned short value.</returns>
		public ushort ReadUInt16() {
			CheckAvailableSpace(2);
			ushort value = Serializer.ReadUInt16(Buffer, Position);
			Position += 2;
			return value;
		}

		/// <summary>Read integer (4 bytes).</summary>
		/// <returns>Integer value.</returns>
		public int ReadInt32() {
			CheckAvailableSpace(4);
			int value = Serializer.ReadInt32(Buffer, Position);
			Position += 4;
			return value;
		}

		/// <summary>Read unsigned integer (4 bytes).</summary>
		/// <returns>Unsigned integer value.</returns>
		public uint ReadUInt32() {
			CheckAvailableSpace(4);
			uint value = Serializer.ReadUInt32(Buffer, Position);
			Position += 4;
			return value;
		}

		/// <summary>Read long integer (8 bytes).</summary>
		/// <returns>Long integer value.</returns>
		public long ReadInt64() {
			CheckAvailableSpace(8);
			long value = Serializer.ReadInt64(Buffer, Position);
			Position += 8;
			return value;
		}

		/// <summary>Read unsigned long integer (8 bytes).</summary>
		/// <returns>Unsigned long integer value.</returns>
		public ulong ReadUInt64() {
			CheckAvailableSpace(8);
			ulong value = Serializer.ReadUInt64(Buffer, Position);
			Position += 8;
			return value;
		}

		/// <summary>Read float (4 bytes).</summary>
		/// <returns>Float value.</returns>
		public float ReadSingle() {
			CheckAvailableSpace(4);
			float value = Serializer.ReadSingle(Buffer, Position);
			Position += 4;
			return value;
		}

		/// <summary>Read double (8 bytes).</summary>
		/// <returns>Double value.</returns>
		public double ReadDouble() {
			CheckAvailableSpace(8);
			double value = Serializer.ReadDouble(Buffer, Position);
			Position += 8;
			return value;
		}

		/// <summary>Read decimal (16 bytes).</summary>
		/// <returns>Decimal value.</returns>
		public decimal ReadDecimal() {
			CheckAvailableSpace(16);
			int[] bits = new int[4];
			bits[0] = Serializer.ReadInt32(Buffer, Position);
			bits[1] = Serializer.ReadInt32(Buffer, Position + 4);
			bits[2] = Serializer.ReadInt32(Buffer, Position + 8);
			bits[3] = Serializer.ReadInt32(Buffer, Position + 12);
			Position += 16;
			return new decimal(bits);
		}

		/// <summary>
		/// Read enum (1, 2 or 4 bytes).
		/// <para>Number of bytes read is dependant on the underlying type the enum is backed by.</para>
		/// </summary>
		/// <typeparam name="T">Enum type.</typeparam>
		/// <returns>Enum value.</returns>
		public T ReadEnum<T>() where T : struct, IConvertible {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Type type_enum = typeof(T);
			Type type_underlying = Enum.GetUnderlyingType(type_enum);
			TypeCode typecode = Type.GetTypeCode(type_underlying);
			switch (typecode) {
				default:
					throw new ArgumentException(string.Format(
						"Enum {0} typecode {1} not supported", type_enum, typecode
					), nameof(T));
				case TypeCode.Int32:
					CheckAvailableSpace(4);
					int value_int32 = Serializer.ReadInt32(Buffer, Position);
					Position += 4;
					return (T)Enum.ToObject(type_enum, value_int32);
				case TypeCode.UInt32:
					CheckAvailableSpace(4);
					int value_uint32 = Serializer.ReadInt32(Buffer, Position);
					Position += 4;
					return (T)Enum.ToObject(type_enum, value_uint32);
				case TypeCode.Int16:
					CheckAvailableSpace(2);
					short value_int16 = Serializer.ReadInt16(Buffer, Position);
					Position += 2;
					return (T)Enum.ToObject(type_enum, value_int16);
				case TypeCode.UInt16:
					CheckAvailableSpace(2);
					ushort value_uint16 = Serializer.ReadUInt16(Buffer, Position);
					Position += 2;
					return (T)Enum.ToObject(type_enum, value_uint16);
				case TypeCode.Byte:
					CheckAvailableSpace(1);
					byte value_byte = Buffer[Position];
					Position++;
					return (T)Enum.ToObject(type_enum, value_byte);
				case TypeCode.SByte:
					CheckAvailableSpace(1);
					sbyte value_sbyte = (sbyte)Buffer[Position];
					Position++;
					return (T)Enum.ToObject(type_enum, value_sbyte);
			}
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_5_3_OR_NEWER

		/// <summary>Read 2D vector (8 bytes).</summary>
		/// <returns>2D vector value.</returns>
		public UnityEngine.Vector2 ReadVector2() {
			CheckAvailableSpace(8);
			float x = Serializer.ReadSingle(Buffer, Position);
			float y = Serializer.ReadSingle(Buffer, Position + 4);
			Position += 8;
			return new UnityEngine.Vector2(x, y);
		}

		/// <summary>Read 2D integer vector (8 bytes). </summary>
		/// <returns>2D integer vector value.</returns>
		public UnityEngine.Vector2Int ReadVector2Int() {
			CheckAvailableSpace(8);
			int x = Serializer.ReadInt32(Buffer, Position);
			int y = Serializer.ReadInt32(Buffer, Position + 4);
			Position += 8;
			return new UnityEngine.Vector2Int(x, y);
		}

		/// <summary>Read 3D vector (12 bytes).</summary>
		/// <returns>3D vector value.</returns>
		public UnityEngine.Vector3 ReadVector3() {
			CheckAvailableSpace(12);
			float x = Serializer.ReadSingle(Buffer, Position);
			float y = Serializer.ReadSingle(Buffer, Position + 4);
			float z = Serializer.ReadSingle(Buffer, Position + 8);
			Position += 12;
			return new UnityEngine.Vector3(x, y, z);
		}

		/// <summary>Read 3D integer vector (12 bytes).</summary>
		/// <returns>3D integer vector value.</returns>
		public UnityEngine.Vector3Int ReadVector3Int() {
			CheckAvailableSpace(12);
			int x = Serializer.ReadInt32(Buffer, Position);
			int y = Serializer.ReadInt32(Buffer, Position + 4);
			int z = Serializer.ReadInt32(Buffer, Position + 8);
			Position += 12;
			return new UnityEngine.Vector3Int(x, y, z);
		}

		/// <summary>Read 4D vector (16 bytes).</summary>
		/// <returns>4D vector value.</returns>
		public UnityEngine.Vector4 ReadVector4() {
			CheckAvailableSpace(16);
			float x = Serializer.ReadSingle(Buffer, Position);
			float y = Serializer.ReadSingle(Buffer, Position + 4);
			float z = Serializer.ReadSingle(Buffer, Position + 8);
			float w = Serializer.ReadSingle(Buffer, Position + 12);
			Position += 16;
			return new UnityEngine.Vector4(x, y, z, w);
		}

		/// <summary>Read color (16 bytes).</summary>
		/// <returns>Color value.</returns>
		public UnityEngine.Color ReadColor() {
			CheckAvailableSpace(16);
			float r = Serializer.ReadSingle(Buffer, Position);
			float g = Serializer.ReadSingle(Buffer, Position + 4);
			float b = Serializer.ReadSingle(Buffer, Position + 8);
			float a = Serializer.ReadSingle(Buffer, Position + 12);
			Position += 16;
			return new UnityEngine.Color(r, g, b, a);
		}

		/// <summary>Read 32-bit color (4 bytes).</summary>
		/// <returns>32-bit color value.</returns>
		public UnityEngine.Color32 ReadColor32() {
			CheckAvailableSpace(4);
			byte r = Buffer[Position];
			byte g = Buffer[Position + 1];
			byte b = Buffer[Position + 2];
			byte a = Buffer[Position + 3];
			Position += 4;
			return new UnityEngine.Color32(r, g, b, a);
		}

		/// <summary>Read rotation (16 bytes).</summary>
		/// <returns>Rotation value.</returns>
		public UnityEngine.Quaternion ReadQuaternion() {
			CheckAvailableSpace(16);
			float x = Serializer.ReadSingle(Buffer, Position);
			float y = Serializer.ReadSingle(Buffer, Position + 4);
			float z = Serializer.ReadSingle(Buffer, Position + 8);
			float w = Serializer.ReadSingle(Buffer, Position + 12);
			Position += 16;
			return new UnityEngine.Quaternion(x, y, z, w);
		}

		/// <summary>Read rect (16 bytes).</summary>
		/// <returns>Rect value.</returns>
		public UnityEngine.Rect ReadRect() {
			CheckAvailableSpace(16);
			float x = Serializer.ReadSingle(Buffer, Position);
			float y = Serializer.ReadSingle(Buffer, Position + 4);
			float width = Serializer.ReadSingle(Buffer, Position + 8);
			float height = Serializer.ReadSingle(Buffer, Position + 12);
			Position += 16;
			return new UnityEngine.Rect(x, y, width, height);
		}

		/// <summary>Read ray (24 bytes).</summary>
		/// <returns>Ray value.</returns>
		public UnityEngine.Ray ReadRay() {
			CheckAvailableSpace(24);
			float origin_x = Serializer.ReadSingle(Buffer, Position);
			float origin_y = Serializer.ReadSingle(Buffer, Position + 4);
			float origin_z = Serializer.ReadSingle(Buffer, Position + 8);
			float direction_x = Serializer.ReadSingle(Buffer, Position + 12);
			float direction_y = Serializer.ReadSingle(Buffer, Position + 16);
			float direction_z = Serializer.ReadSingle(Buffer, Position + 20);
			UnityEngine.Vector3 origin = new UnityEngine.Vector3(origin_x, origin_y, origin_z);
			UnityEngine.Vector3 direction = new UnityEngine.Vector3(direction_x, direction_y, direction_z);
			Position += 24;
			return new UnityEngine.Ray(origin, direction);
		}

		/// <summary>Read 4x4 matrix (64 bytes).</summary>
		/// <returns>Matrix value.</returns>
		public UnityEngine.Matrix4x4 ReadMatrix4x4() {
			CheckAvailableSpace(64);
			float m00 = Serializer.ReadSingle(Buffer, Position);
			float m01 = Serializer.ReadSingle(Buffer, Position + 4);
			float m02 = Serializer.ReadSingle(Buffer, Position + 8);
			float m03 = Serializer.ReadSingle(Buffer, Position + 12);
			float m10 = Serializer.ReadSingle(Buffer, Position + 16);
			float m11 = Serializer.ReadSingle(Buffer, Position + 20);
			float m12 = Serializer.ReadSingle(Buffer, Position + 24);
			float m13 = Serializer.ReadSingle(Buffer, Position + 28);
			float m20 = Serializer.ReadSingle(Buffer, Position + 32);
			float m21 = Serializer.ReadSingle(Buffer, Position + 36);
			float m22 = Serializer.ReadSingle(Buffer, Position + 40);
			float m23 = Serializer.ReadSingle(Buffer, Position + 44);
			float m30 = Serializer.ReadSingle(Buffer, Position + 48);
			float m31 = Serializer.ReadSingle(Buffer, Position + 52);
			float m32 = Serializer.ReadSingle(Buffer, Position + 56);
			float m33 = Serializer.ReadSingle(Buffer, Position + 60);
			Position += 64;
			return new UnityEngine.Matrix4x4 {
				m00 = m00, m01 = m01, m02 = m02, m03 = m03,
				m10 = m10, m11 = m11, m12 = m12, m13 = m13,
				m20 = m20, m21 = m21, m22 = m22, m23 = m23,
				m30 = m30, m31 = m31, m32 = m32, m33 = m33
			};
		}

#endif
		
	}

}
