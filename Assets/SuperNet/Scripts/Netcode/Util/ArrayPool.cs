using System;
using System.Threading;

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Array pool for reusing arrays to avoid too many allocations.
	/// </summary>
	/// <typeparam name="T">Underlying array type.</typeparam>
	public sealed class ArrayPool<T> {

		/// <summary>
		/// Maximum length arrays can still be saved at.
		/// </summary>
		private readonly int MaxLength;

		/// <summary>
		/// Saved arrays.
		/// </summary>
		private readonly T[][] Arrays;

		/// <summary>
		/// Lock used for access to arrays and index.
		/// Do not make this readonly, it's a mutable struct.
		/// </summary>
		private SpinLock Lock;

		/// <summary>
		/// Index of the next array to return to.
		/// </summary>
		private int Index;

		/// <summary>
		/// Create a new array pool.
		/// </summary>
		/// <param name="count">Number of arrays this pool can hold.</param>
		/// <param name="maxLength">Maximum length arrays can be saved at.</param>
		public ArrayPool(int count, int maxLength) {
			
			// Validate
			if (count < 0) {
				throw new ArgumentOutOfRangeException(nameof(count), string.Format(
					"Array pool count {0} is not positive", count
				));
			} else if (maxLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(maxLength), string.Format(
					"Array pool max length {0} is not positive", maxLength
				));
			}

			// Initialize
			MaxLength = maxLength;
			Arrays = new T[count][];
			Lock = new SpinLock();
			Index = 0;

		}

		/// <summary>
		/// Extract an array from this pool or allocate a new one.
		/// </summary>
		/// <param name="minimumLength">Minimum length that the returned array has to be.</param>
		/// <returns>An unused array.</returns>
		public T[] Rent(int minimumLength) {

			// Validate
			if (minimumLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(minimumLength), string.Format(
					"Minimum length {0} is negative", minimumLength
				));
			}

			// Extract array from the pool
			T[] array = null;
			bool taken = false;
			try {
				Lock.Enter(ref taken);
				if (Index < Arrays.Length) {
					array = Arrays[Index];
					Arrays[Index++] = null;
				}
			} finally {
				if (taken) Lock.Exit(false);
			}

			// Resize array if needed
			if (array == null || array.Length < minimumLength) {
				Array.Resize(ref array, minimumLength);
			}

			// Return the array
			return array;

		}

		/// <summary>
		/// Return an array back to this pool.
		/// </summary>
		/// <param name="array">Array to return.</param>
		public void Return(T[] array) {

			// If no array, do nothing
			if (array == null) {
				return;
			}
			
			// If array is too long, don't put it in the pool
			if (array.Length > MaxLength) {
				return;
			}

			// Put array back into the pool
			bool token = false;
			try {
				Lock.Enter(ref token);
				if (Index > 0) {
					Arrays[--Index] = array;
				}
			} finally {
				if (token) Lock.Exit(false);
			}

		}

		/// <summary>
		/// Resize an array created by this pool.
		/// </summary>
		/// <param name="array">Array to resize.</param>
		/// <param name="copyLength">Number of bytes to copy to the new array.</param>
		/// <param name="addLength">Number of bytes to add after the copy length.</param>
		/// <param name="expandLength">Array length multiplier.</param>
		/// <returns>A new resized array.</returns>
		public T[] Expand(T[] array, int copyLength, int addLength, int expandLength) {

			// Validate
			if (copyLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(copyLength), string.Format(
					"Copy length {0} is negative", copyLength
				));
			} else if (addLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(addLength), string.Format(
					"Add length {0} is negative", addLength
				));
			} else if (expandLength <= 0) {
				throw new ArgumentOutOfRangeException(nameof(expandLength), string.Format(
					"Expand length {0} is not positive", expandLength
				));
			}

			if (array == null) {

				// Array is null, rent a new one
				return Rent(copyLength + addLength);

			} else if (array.Length < copyLength + addLength) {

				// Calculate new array length
				int expandCount = (copyLength + addLength - array.Length + expandLength - 1) / expandLength;
				int newLength = array.Length + expandLength * expandCount;

				// Create a new array copy
				T[] copy = new T[newLength];
				Array.Copy(array, 0, copy, 0, copyLength);

				// Return the original array back to the pool if the copy is too big
				if (newLength > MaxLength) {
					Return(array);
				}

				// Return the copy
				return copy;

			} else {

				// Array is big enough, just return it
				return array;

			}

		}

	}

}
