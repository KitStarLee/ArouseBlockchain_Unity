using System;
using System.Threading;

namespace SuperNet.Netcode.Util {

	/// <summary>
	/// Object pool for reusing objects to avoid too many allocations.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	public sealed class ObjectPool<T> where T : class {

		/// <summary>
		/// Saved objects.
		/// </summary>
		private readonly T[] Objects;

		/// <summary>
		/// Lock used for access to objects and index.
		/// Do not make this readonly, it's a mutable struct.
		/// </summary>
		private SpinLock Lock;

		/// <summary>
		/// Index of the next object to return to.
		/// </summary>
		private int Index;

		/// <summary>
		/// Create a new object pool.
		/// </summary>
		/// <param name="count">Number of objects this pool can hold.</param>
		public ObjectPool(int count) {

			// Validate
			if (count < 0) {
				throw new ArgumentOutOfRangeException(nameof(count), string.Format(
					"Object pool count {0} is not positive", count
				));
			}

			// Initialize
			Objects = new T[count];
			Lock = new SpinLock();
			Index = 0;

		}

		/// <summary>
		/// Extract an object from this pool or return null.
		/// </summary>
		/// <returns>Extracted object or null if none available.</returns>
		public T Rent() {

			// Extract object from the pool
			T obj = null;
			bool taken = false;
			try {
				Lock.Enter(ref taken);
				if (Index < Objects.Length) {
					obj = Objects[Index];
					Objects[Index++] = null;
				}
			} finally {
				if (taken) Lock.Exit(false);
			}

			// Return the object
			return obj;

		}

		/// <summary>
		/// Return an object back to this pool.
		/// </summary>
		/// <param name="obj">Object to return.</param>
		public void Return(T obj) {

			// If no object, do nothing
			if (obj == null) {
				return;
			}

			// Put object back into the pool
			bool taken = false;
			try {
				Lock.Enter(ref taken);
				if (Index > 0) {
					Objects[--Index] = obj;
				}
			} finally {
				if (taken) Lock.Exit(false);
			}

		}

	}

}
