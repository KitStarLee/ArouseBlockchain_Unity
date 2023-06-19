using System;
using System.Diagnostics;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Stores a local timestamp of an event.
	/// </summary>
	public struct HostTimestamp : IComparable, IComparable<HostTimestamp>, IEquatable<HostTimestamp>, IFormattable {

		/// <summary>Timestamp at the time the system started.</summary>
		public static readonly HostTimestamp Start = new HostTimestamp(0L);

		/// <summary>Smallest possible timestamp.</summary>
		public static readonly HostTimestamp MinValue = new HostTimestamp(long.MinValue / 2L);

		/// <summary>Biggest possible timestamp.</summary>
		public static readonly HostTimestamp MaxValue = new HostTimestamp(long.MaxValue / 2L);

		/// <summary>Create a new timestamp at the current time.</summary>
		public static HostTimestamp Now => Create();

		/// <summary>Get elapsed time since the creation of this timestamp.</summary>
		public HostTimespan Elapsed => GetElapsed();

		/// <summary>Raw stopwatch ticks.</summary>
		private readonly long Ticks;

		/// <summary>Create a new timestamp from stopwatch ticks.</summary>
		private HostTimestamp(long ticks) {
			Ticks = ticks;
		}

		/// <summary>
		/// Create a timestamp at the current time.
		/// </summary>
		/// <returns>A new timestamp at the current time.</returns>
		private static HostTimestamp Create() {
			long ticks = Stopwatch.GetTimestamp();
			return new HostTimestamp(ticks);
		}

		/// <summary>
		/// Create a timestamp from the DateTimeOffset object.
		/// </summary>
		/// <param name="time">The DateTimeOffset object.</param>
		/// <returns>A new timestamp.</returns>
		public static HostTimestamp FromDateTimeOffset(DateTimeOffset time) {
			double ratio = Stopwatch.Frequency / (double)10000000d;
			DateTimeOffset now = DateTimeOffset.UtcNow;
			long ticksStopwatch = Stopwatch.GetTimestamp();
			long ticksNow = (long)(ratio * now.Ticks);
			long ticksTime = (long)(ratio * time.Ticks);
			return new HostTimestamp(ticksTime - ticksNow + ticksStopwatch);
		}

		/// <summary>
		/// Convert timestamp to a DateTimeOffset object.
		/// </summary>
		/// <param name="offset">UTC offset to use.</param>
		/// <returns>The DateTimeOffset equivalent.</returns>
		public DateTimeOffset ToDateTimeOffset(TimeSpan offset) {
			double ratio = 10000000d / Stopwatch.Frequency;
			DateTimeOffset now = DateTimeOffset.UtcNow;
			long ticksStopwatch = (long)(ratio * Stopwatch.GetTimestamp());
			long ticksTimestamp = (long)(ratio * Ticks);
			long ticksDate = now.Ticks - ticksStopwatch + ticksTimestamp;
			if (ticksDate < DateTimeOffset.MinValue.Ticks) {
				return DateTimeOffset.MinValue;
			} else if (ticksDate > DateTimeOffset.MaxValue.Ticks) {
				return DateTimeOffset.MaxValue;
			} else {
				return new DateTimeOffset(ticksDate, offset);
			}
		}

		/// <summary>
		/// Create a timestamp from raw ticks with provided frequency.
		/// </summary>
		/// <param name="ticks">Raw ticks.</param>
		/// <param name="frequency">Ticks per second.</param>
		/// <returns>A new timestamp.</returns>
		public static HostTimestamp FromTicks(long ticks, long frequency) {
			if (Stopwatch.Frequency == frequency) {
				return new HostTimestamp(ticks);
			} else {
				double ratio = Stopwatch.Frequency / (double)frequency;
				return new HostTimestamp((long)(ticks * ratio));
			}
		}

		/// <summary>
		/// Get raw ticks with provided frequency.
		/// </summary>
		/// <param name="frequency">Ticks per second.</param>
		/// <returns>Raw ticks.</returns>
		public long GetTicks(long frequency) {
			if (Stopwatch.Frequency == frequency) {
				return Ticks;
			} else {
				double ratio = frequency / (double)Stopwatch.Frequency;
				return (long)(Ticks * ratio);
			}
		}

		/// <summary>
		/// Create a timestamp with the provided ticks at stopwatch frequency.
		/// </summary>
		/// <param name="ticks">Raw ticks.</param>
		/// <returns>A new timestamp.</returns>
		public static HostTimestamp FromStopwatchTicks(long ticks) {
			return new HostTimestamp(ticks);
		}

		/// <summary>
		/// Get raw ticks with stopwatch frequency.
		/// </summary>
		/// <returns>Raw stopwatch ticks.</returns>
		public long GetStopwatchTicks() {
			return Ticks;
		}

		private HostTimespan GetElapsed() {
			long ticks = Stopwatch.GetTimestamp() - Ticks;
			return HostTimespan.FromStopwatchTicks(ticks);
		}

		public override bool Equals(object obj) {
			if (obj is HostTimestamp timestamp) {
				return Ticks.Equals(timestamp.Ticks);
			} else {
				return false;
			}
		}

		public int CompareTo(object obj) {
			if (obj is HostTimestamp timestamp) {
				return Ticks.CompareTo(timestamp.Ticks);
			} else {
				return 1;
			}
		}

		public bool Equals(HostTimestamp other) {
			return Ticks.Equals(other.Ticks);
		}

		public int CompareTo(HostTimestamp other) {
			return Ticks.CompareTo(other.Ticks);
		}

		public override int GetHashCode() {
			return Ticks.GetHashCode();
		}

		public override string ToString() {
			return ToDateTimeOffset(TimeSpan.Zero).LocalDateTime.ToString();
		}

		public string ToString(string format, IFormatProvider formatProvider) {
			return ToDateTimeOffset(TimeSpan.Zero).LocalDateTime.ToString(format, formatProvider);
		}

		public static bool operator ==(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks == rhs.Ticks;
		}

		public static bool operator !=(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks != rhs.Ticks;
		}

		public static HostTimestamp operator +(HostTimestamp lhs, HostTimespan rhs) {
			return new HostTimestamp(lhs.Ticks + rhs.GetStopwatchTicks());
		}

		public static HostTimestamp operator +(HostTimespan lhs, HostTimestamp rhs) {
			return new HostTimestamp(rhs.Ticks + lhs.GetStopwatchTicks());
		}

		public static HostTimestamp operator -(HostTimestamp lhs, HostTimespan rhs) {
			return new HostTimestamp(lhs.Ticks - rhs.GetStopwatchTicks());
		}

		public static HostTimespan operator -(HostTimestamp lhs, HostTimestamp rhs) {
			return HostTimespan.FromStopwatchTicks(lhs.Ticks - rhs.Ticks);
		}

		public static bool operator >(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks > rhs.Ticks;
		}

		public static bool operator <(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks < rhs.Ticks;
		}

		public static bool operator >=(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks >= rhs.Ticks;
		}

		public static bool operator <=(HostTimestamp lhs, HostTimestamp rhs) {
			return lhs.Ticks <= rhs.Ticks;
		}

	}

}
