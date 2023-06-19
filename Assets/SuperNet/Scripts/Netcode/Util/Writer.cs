using System;

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Fast serializer for network messages.
	/// </summary>
	public class Writer : IDisposable {

		/// <summary>
		/// Allocator used to resize the internal buffer.
		/// </summary>
		internal readonly Allocator Allocator;

		/// <summary>
		/// Current write position within the internal buffer or 0 if the writer has been disposed.
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		/// True if writer has been disposed.
		/// </summary>
		public bool Disposed { get; private set; }

		/// <summary>
		/// Internal buffer to write to or null if the writer has been disposed.
		/// </summary>
		public byte[] Buffer => Internal;

		/// <summary>
		/// The internal buffer.
		/// </summary>
		private byte[] Internal;

		/// <summary>
		/// Allocate a new buffer and create a writer for it.
		/// The buffer is automatically resized by the allocator.
		/// </summary>
		/// <param name="allocator">Allocator to use for the internal buffer.</param>
		/// <param name="position">Initial position in the internal buffer.</param>
		public Writer(Allocator allocator, int position) {
			
			// Validate
			if (allocator == null) {
				throw new ArgumentNullException(nameof(allocator), "No allocator");
			} else if (position < 0) {
				throw new ArgumentException(string.Format(
					"Position {0} is negative", position
				), nameof(position));
			}

			// Initialize
			Allocator = allocator;
			Position = position;
			Disposed = false;
			Internal = allocator.CreateMessage(position);

		}

		/// <summary>
		/// Invalidate the underlying buffer when it gets used for something else.
		/// Calling this causes all future write operation to fail.
		/// </summary>
		public void Dispose() {
			Allocator.ReturnMessage(ref Internal);
			Disposed = true;
			Position = 0;
		}

		/// <summary>Manually set the write position.</summary>
		/// <param name="position">Position to set.</param>
		internal void Reset(int position = 0) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (position < 0) {
				throw new ArgumentException(string.Format(
					"Reset position {0} is negative", position
				), nameof(position));
			} else if (position > Position) {
				Internal = Allocator.ExpandMessage(Internal, Position, position - Position);
				Position = position;
			} else {
				Position = position;
			}
		}

		/// <summary>Advance the write position without writing anything.</summary>
		/// <param name="length">Number of bytes to skip for.</param>
		public void Skip(int length) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (length < 0) {
				throw new ArgumentException(string.Format(
					"Skip length {0} is negative", length
				), nameof(length));
			} else {
				Internal = Allocator.ExpandMessage(Internal, Position, length);
				Position += length;
			}
		}

		/// <summary>Write 4 bytes for length, then UTF8 encoded string.</summary>
		/// <param name="value">String to write.</param>
		public void Write(string value) {
			if (Disposed) {
				throw new ObjectDisposedException(GetType().FullName);
			} else if (value == null) {
				Internal = Allocator.ExpandMessage(Internal, Position, 4);
				Serializer.Write32(Internal, Position, -1);
				Position += 4;
			} else {
				int length = Serializer.Encoding.GetByteCount(value);
				Internal = Allocator.ExpandMessage(Internal, Position, length + 4);
				Serializer.Write32(Internal, Position, length);
				Serializer.Encoding.GetBytes(value, 0, value.Length, Internal, Position + 4);
				Position += length + 4;
			}
		}

		/// <summary>Copy a segment of bytes to the writer.</summary>
		/// <param name="segment">Segment to copy</param>
		public void WriteBytes(ArraySegment<byte> segment) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, segment.Count);
			Array.Copy(segment.Array, segment.Offset, Internal, Position, segment.Count);
			Position += segment.Count;
		}

		/// <summary>Copy a segment of bytes to the writer.</summary>
		/// <param name="buffer">Buffer to copy from.</param>
		/// <param name="offset">Offset within the provided buffer.</param>
		/// <param name="count">Number of bytes to copy.</param>
		public void WriteBytes(byte[] buffer, int offset, int count) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, count);
			Array.Copy(buffer, offset, Internal, Position, count);
			Position += count;
		}

		/// <summary>Copy an entire buffer to the writer.</summary>
		/// <param name="buffer">Buffer to copy.</param>
		public void WriteBytes(byte[] buffer) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, buffer.Length);
			Array.Copy(buffer, 0, Internal, Position, buffer.Length);
			Position += buffer.Length;
		}

		/// <summary>Write a single boolean value (1 byte) to the writer.</summary>
		/// <param name="value">Boolean to write.</param>
		public void Write(bool value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 1);
			Internal[Position] = (byte)(value ? 1 : 0);
			Position++;
		}

		/// <summary>Write 8 boolean values (1 byte) to the writer.</summary>
		/// <param name="v0">First boolean value.</param>
		/// <param name="v1">Second boolean value.</param>
		/// <param name="v2">Third boolean value.</param>
		/// <param name="v3">Fourth boolean value.</param>
		/// <param name="v4">Fifth boolean value.</param>
		/// <param name="v5">Sixth boolean value.</param>
		/// <param name="v6">Seventh boolean value.</param>
		/// <param name="v7">Eighth boolean value.</param>
		public void Write(bool v0, bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 1);
			byte value = 0;
			if (v0) value |= (1 << 0);
			if (v1) value |= (1 << 1);
			if (v2) value |= (1 << 2);
			if (v3) value |= (1 << 3);
			if (v4) value |= (1 << 4);
			if (v5) value |= (1 << 5);
			if (v6) value |= (1 << 6);
			if (v7) value |= (1 << 7);
			Internal[Position] = value;
			Position++;
		}

		/// <summary>Write a single character (2 bytes) to the writer.</summary>
		/// <param name="value">Character to write.</param>
		public void Write(char value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 2);
			Serializer.Write16(Internal, Position, value);
			Position += 2;
		}

		/// <summary>Write a single byte (1 byte) to the writer.</summary>
		/// <param name="value">Byte to write.</param>
		public void Write(byte value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 1);
			Internal[Position] = value;
			Position++;
		}

		/// <summary>Write a signed byte (1 byte) to the writer.</summary>
		/// <param name="value">Signed byte to write.</param>
		public void Write(sbyte value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 1);
			Internal[Position] = (byte)value;
			Position++;
		}

		/// <summary>Write a short (2 bytes) to the writer.</summary>
		/// <param name="value">Short to write.</param>
		public void Write(short value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 2);
			Serializer.Write16(Internal, Position, value);
			Position += 2;
		}

		/// <summary>Write an unsigned short (2 bytes) to the writer.</summary>
		/// <param name="value">Unsigned short to write.</param>
		public void Write(ushort value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 2);
			Serializer.Write16(Internal, Position, value);
			Position += 2;
		}

		/// <summary>Write an integer (4 bytes) to the writer.</summary>
		/// <param name="value">Integer to write.</param>
		public void Write(int value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 4);
			Serializer.Write32(Internal, Position, value);
			Position += 4;
		}

		/// <summary> Write an unsigned integer (4 bytes) to the writer.</summary>
		/// <param name="value">Unsigned integer to write.</param>
		public void Write(uint value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 4);
			Serializer.Write32(Internal, Position, value);
			Position += 4;
		}

		/// <summary>Write a long (8 bytes) to the writer.</summary>
		/// <param name="value">Long to write.</param>
		public void Write(long value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 8);
			Serializer.Write64(Internal, Position, value);
			Position += 8;
		}

		/// <summary>Write an unsigned long (8 bytes) to the writer.</summary>
		/// <param name="value">Unsigned long to write.</param>
		public void Write(ulong value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 8);
			Serializer.Write64(Internal, Position, value);
			Position += 8;
		}

		/// <summary>Write a float (4 bytes) to the writer.</summary>
		/// <param name="value">Float to write.</param>
		public void Write(float value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 4);
			Serializer.WriteSingle(Internal, Position, value);
			Position += 4;
		}

		/// <summary>Write a double (8 bytes) to the writer.</summary>
		/// <param name="value">Double to write.</param>
		public void Write(double value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 8);
			Serializer.WriteDouble(Internal, Position, value);
			Position += 8;
		}

		/// <summary>Write a decimal (16 bytes) to the writer.</summary>
		/// <param name="value">Decimal to write.</param>
		public void Write(decimal value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			int[] bits = decimal.GetBits(value);
			Serializer.Write32(Internal, Position, bits[0]);
			Serializer.Write32(Internal, Position + 4, bits[1]);
			Serializer.Write32(Internal, Position + 8, bits[2]);
			Serializer.Write32(Internal, Position + 12, bits[3]);
			Position += 16;
		}

		/// <summary>
		/// Write an enum (1, 2 or 4 bytes) to the writer.
		/// <para>Number of bytes written is dependant on the underlying type enum is backed by.</para>
		/// </summary>
		/// <typeparam name="T">Enum type.</typeparam>
		/// <param name="value">Enum value.</param>
		public void WriteEnum<T>(T value) where T : struct, IConvertible {
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
					Internal = Allocator.ExpandMessage(Internal, Position, 4);
					Serializer.Write32(Internal, Position, Convert.ToInt32(value));
					Position += 4;
					break;
				case TypeCode.UInt32:
					Internal = Allocator.ExpandMessage(Internal, Position, 4);
					Serializer.Write32(Internal, Position, Convert.ToUInt32(value));
					Position += 4;
					break;
				case TypeCode.Int16:
					Internal = Allocator.ExpandMessage(Internal, Position, 2);
					Serializer.Write16(Internal, Position, Convert.ToInt16(value));
					Position += 2;
					break;
				case TypeCode.UInt16:
					Internal = Allocator.ExpandMessage(Internal, Position, 2);
					Serializer.Write16(Internal, Position, Convert.ToUInt16(value));
					Position += 2;
					break;
				case TypeCode.Byte:
					Internal = Allocator.ExpandMessage(Internal, Position, 1);
					Internal[Position] = Convert.ToByte(value);
					Position += 1;
					break;
				case TypeCode.SByte:
					Internal = Allocator.ExpandMessage(Internal, Position, 1);
					Internal[Position] = (byte)Convert.ToSByte(value);
					Position += 1;
					break;
			}
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_5_3_OR_NEWER

		/// <summary>Write 2D vector (8 bytes) to the writer.</summary>
		/// <param name="value">2D vector to write.</param>
		public void Write(UnityEngine.Vector2 value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 8);
			Serializer.WriteSingle(Internal, Position, value.x);
			Serializer.WriteSingle(Internal, Position + 4, value.y);
			Position += 8;
		}

		/// <summary>Write 2D integer vector (8 bytes) to the writer.</summary>
		/// <param name="value">2D ineger vector to write.</param>
		public void Write(UnityEngine.Vector2Int value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 8);
			Serializer.Write32(Internal, Position, value.x);
			Serializer.Write32(Internal, Position + 4, value.y);
			Position += 8;
		}

		/// <summary>Write 3D vector (12 bytes) to the writer.</summary>
		/// <param name="value">3D vector to write.</param>
		public void Write(UnityEngine.Vector3 value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 12);
			Serializer.WriteSingle(Internal, Position, value.x);
			Serializer.WriteSingle(Internal, Position + 4, value.y);
			Serializer.WriteSingle(Internal, Position + 8, value.z);
			Position += 12;
		}

		/// <summary>Write 3D integer vector (12 bytes) to the writer.</summary>
		/// <param name="value">3D integer vector to write.</param>
		public void Write(UnityEngine.Vector3Int value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 12);
			Serializer.Write32(Internal, Position, value.x);
			Serializer.Write32(Internal, Position + 4, value.y);
			Serializer.Write32(Internal, Position + 8, value.z);
			Position += 12;
		}

		/// <summary>Write 4D vector (16 bytes) to the writer.</summary>
		/// <param name="value">4D vector to write.</param>
		public void Write(UnityEngine.Vector4 value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			Serializer.WriteSingle(Internal, Position, value.x);
			Serializer.WriteSingle(Internal, Position + 4, value.y);
			Serializer.WriteSingle(Internal, Position + 8, value.z);
			Serializer.WriteSingle(Internal, Position + 12, value.w);
			Position += 16;
		}

		/// <summary>Write color (16 bytes) to the writer.</summary>
		/// <param name="value">Color to write.</param>
		public void Write(UnityEngine.Color value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			Serializer.WriteSingle(Internal, Position, value.r);
			Serializer.WriteSingle(Internal, Position + 4, value.g);
			Serializer.WriteSingle(Internal, Position + 8, value.b);
			Serializer.WriteSingle(Internal, Position + 12, value.a);
			Position += 16;
		}

		/// <summary>Write 32-bit color (4 bytes) to the writer.</summary>
		/// <param name="value">Color to write.</param>
		public void Write(UnityEngine.Color32 value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 4);
			Internal[Position] = value.r;
			Internal[Position + 1] = value.g;
			Internal[Position + 2] = value.b;
			Internal[Position + 3] = value.a;
			Position += 4;
		}

		/// <summary>Write rotation (16 bytes) to the writer.</summary>
		/// <param name="value">Rotation to write.</param>
		public void Write(UnityEngine.Quaternion value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			Serializer.WriteSingle(Internal, Position, value.x);
			Serializer.WriteSingle(Internal, Position + 4, value.y);
			Serializer.WriteSingle(Internal, Position + 8, value.z);
			Serializer.WriteSingle(Internal, Position + 12, value.w);
			Position += 16;
		}

		/// <summary>Write rect (16 bytes) to the writer.</summary>
		/// <param name="value">Rect to write.</param>
		public void Write(UnityEngine.Rect value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			Serializer.WriteSingle(Internal, Position, value.xMin);
			Serializer.WriteSingle(Internal, Position + 4, value.yMin);
			Serializer.WriteSingle(Internal, Position + 8, value.width);
			Serializer.WriteSingle(Internal, Position + 12, value.height);
			Position += 16;
		}

		/// <summary>Write a plane (16 bytes) to the writer.</summary>
		/// <param name="value">Plane to write.</param>
		public void Write(UnityEngine.Plane value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 16);
			Serializer.WriteSingle(Internal, Position, value.normal.x);
			Serializer.WriteSingle(Internal, Position + 4, value.normal.y);
			Serializer.WriteSingle(Internal, Position + 8, value.normal.z);
			Serializer.WriteSingle(Internal, Position + 12, value.distance);
			Position += 16;
		}

		/// <summary>Write a ray (24 bytes) to the writer.</summary>
		/// <param name="value">Ray to write.</param>
		public void Write(UnityEngine.Ray value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 24);
			Serializer.WriteSingle(Internal, Position, value.origin.x);
			Serializer.WriteSingle(Internal, Position + 4, value.origin.y);
			Serializer.WriteSingle(Internal, Position + 8, value.origin.z);
			Serializer.WriteSingle(Internal, Position + 12, value.direction.x);
			Serializer.WriteSingle(Internal, Position + 16, value.direction.y);
			Serializer.WriteSingle(Internal, Position + 20, value.direction.z);
			Position += 24;
		}

		/// <summary>Write a 4x4 matrix (64 bytes) to the writer.</summary>
		/// <param name="value">Matrix to write.</param>
		public void Write(UnityEngine.Matrix4x4 value) {
			if (Disposed) throw new ObjectDisposedException(GetType().FullName);
			Internal = Allocator.ExpandMessage(Internal, Position, 64);
			Serializer.WriteSingle(Internal, Position, value.m00);
			Serializer.WriteSingle(Internal, Position + 4, value.m01);
			Serializer.WriteSingle(Internal, Position + 8, value.m02);
			Serializer.WriteSingle(Internal, Position + 12, value.m03);
			Serializer.WriteSingle(Internal, Position + 16, value.m10);
			Serializer.WriteSingle(Internal, Position + 20, value.m11);
			Serializer.WriteSingle(Internal, Position + 24, value.m12);
			Serializer.WriteSingle(Internal, Position + 28, value.m13);
			Serializer.WriteSingle(Internal, Position + 32, value.m20);
			Serializer.WriteSingle(Internal, Position + 36, value.m21);
			Serializer.WriteSingle(Internal, Position + 40, value.m22);
			Serializer.WriteSingle(Internal, Position + 44, value.m23);
			Serializer.WriteSingle(Internal, Position + 48, value.m30);
			Serializer.WriteSingle(Internal, Position + 52, value.m31);
			Serializer.WriteSingle(Internal, Position + 56, value.m32);
			Serializer.WriteSingle(Internal, Position + 60, value.m33);
			Position += 64;
		}

#endif

	}

}
