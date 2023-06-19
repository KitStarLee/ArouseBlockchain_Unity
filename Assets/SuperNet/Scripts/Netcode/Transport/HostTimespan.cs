using System;
using System.Diagnostics;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Stores a difference between two timestamps as a duration.
	/// </summary>
	public struct HostTimespan : IComparable, IComparable<HostTimespan>, IEquatable<HostTimespan>, IFormattable {

		/// <summary>Timespan representing zero time.</summary>
		public static readonly HostTimespan Zero = new HostTimespan(0);

		/// <summary>Duration in number of days.</summary>
		public double Days => Ticks / (double)(Stopwatch.Frequency * 86400L);

		/// <summary>Duration in number of hours.</summary>
		public double Hours => Ticks / (double)(Stopwatch.Frequency * 3600L);

		/// <summary>Duration in number of minutes.</summary>
		public double Minutes => Ticks / (double)(Stopwatch.Frequency * 60L);

		/// <summary>Duration in number of seconds.</summary>
		public double Seconds => Ticks / (double)Stopwatch.Frequency;

		/// <summary>Duration in number of milliseconds.</summary>
		public double Milliseconds => (1000d * Ticks) / Stopwatch.Frequency;

		/// <summary>Raw stopwatch ticks.</summary>
		private readonly long Ticks;

		/// <summary>Create a new timespan.</summary>
		private HostTimespan(long ticks) {
			Ticks = ticks;
		}

		/// <summary>
		/// Create a timespan from the TimeSpan object.
		/// </summary>
		/// <param name="value">The TimeSpan object.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromTimeSpan(TimeSpan value) {
			double ratio = Stopwatch.Frequency / (double)10000000d;
			return new HostTimespan((long)(ratio * value.Ticks));
		}

		/// <summary>
		/// Get the TimeSpan equivalent.
		/// </summary>
		public TimeSpan ToTimeSpan() {
			double ratio = 10000000d / Stopwatch.Frequency;
			return new TimeSpan((long)(Ticks * ratio));
		}

		/// <summary>
		/// Create a timespan from raw ticks with provided frequency.
		/// </summary>
		/// <param name="ticks">Raw ticks.</param>
		/// <param name="frequency">Ticks per second.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromTicks(long ticks, long frequency) {
			if (Stopwatch.Frequency == frequency) {
				return new HostTimespan(ticks);
			} else {
				double ratio = Stopwatch.Frequency / (double)frequency;
				return new HostTimespan((long)(ticks * ratio));
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
		/// Create a timespan with the provided ticks at stopwatch frequency.
		/// </summary>
		/// <param name="ticks">Raw ticks.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromStopwatchTicks(long ticks) {
			return new HostTimespan(ticks);
		}

		/// <summary>
		/// Get raw ticks with stopwatch frequency.
		/// </summary>
		/// <returns>Raw stopwatch ticks.</returns>
		public long GetStopwatchTicks() {
			return Ticks;
		}

		/// <summary>
		/// Create a timespan from days.
		/// </summary>
		/// <param name="value">Number of days.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromDays(double value) {
			return new HostTimespan((long)(Stopwatch.Frequency * value * 86400));
		}

		/// <summary>
		/// Create a timespan from hours.
		/// </summary>
		/// <param name="value">Number of hours.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromHours(double value) {
			return new HostTimespan((long)(Stopwatch.Frequency * value * 3600));
		}

		/// <summary>
		/// Create a timespan from minutes.
		/// </summary>
		/// <param name="value">Number of minutes.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromMinutes(double value) {
			return new HostTimespan((long)(Stopwatch.Frequency * value * 60));
		}

		/// <summary>
		/// Create a timespan from seconds.
		/// </summary>
		/// <param name="value">Number of seconds.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromSeconds(double value) {
			return new HostTimespan((long)(Stopwatch.Frequency * value));
		}

		/// <summary>
		/// Create a timespan from milliseconds.
		/// </summary>
		/// <param name="value">Number of milliseconds.</param>
		/// <returns>A new timespan.</returns>
		public static HostTimespan FromMilliseconds(double value) {
			double ratio = Stopwatch.Frequency / (double)1000d;
			return new HostTimespan((long)(ratio * value));
		}

		/// <summary>
		/// Returns the absolute value of a timespan.
		/// </summary>
		/// <param name="timespan">Timespan to use.</param>
		/// <returns>The absolute value.</returns>
		public static HostTimespan Abs(HostTimespan timespan) {
			if (timespan.Ticks < 0) {
				return new HostTimespan(-timespan.Ticks);
			} else {
				return new HostTimespan(timespan.Ticks);
			}
		}

		/// <summary>
		/// Returns a clamped value of a timespan.
		/// </summary>
		/// <param name="value">Value to clamp.</param>
		/// <param name="min">Lower bound.</param>
		/// <param name="max">Upper bound.</param>
		/// <returns>Clamped value.</returns>
		public static HostTimespan Clamp(HostTimespan value, HostTimespan min, HostTimespan max) {
			if (value.Ticks < min.Ticks) {
				return min;
			} else if (value.Ticks > max.Ticks) {
				return max;
			} else {
				return value;
			}
		}

		/// <summary>
		/// Returns the smaller of the two timespans.
		/// </summary>
		/// <param name="a">First value.</param>
		/// <param name="b">Second value.</param>
		/// <returns>The smaller of the two values.</returns>
		public static HostTimespan Min(HostTimespan a, HostTimespan b) {
			if (a.Ticks < b.Ticks) {
				return a;
			} else {
				return b;
			}
		}

		/// <summary>
		/// Returns the larger of the two timespans.
		/// </summary>
		/// <param name="a">First value.</param>
		/// <param name="b">Second value.</param>
		/// <returns>The larger of the two values.</returns>
		public static HostTimespan Max(HostTimespan a, HostTimespan b) {
			if (a.Ticks > b.Ticks) {
				return a;
			} else {
				return b;
			}
		}

		public override bool Equals(object obj) {
			if (obj is HostTimespan timespan) {
				return Ticks.Equals(timespan.Ticks);
			} else {
				return false;
			}
		}

		public int CompareTo(object obj) {
			if (obj is HostTimespan timespan) {
				return Ticks.CompareTo(timespan.Ticks);
			} else {
				return 1;
			}
		}

		public bool Equals(HostTimespan other) {
			return Ticks.Equals(other.Ticks);
		}

		public int CompareTo(HostTimespan other) {
			return Ticks.CompareTo(other.Ticks);
		}

		public override int GetHashCode() {
			return Ticks.GetHashCode();
		}

		public override string ToString() {
			return ToTimeSpan().ToString();
		}

		public string ToString(string format, IFormatProvider formatProvider) {
			return ToTimeSpan().ToString(format, formatProvider);
		}

		public static bool operator ==(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks == rhs.Ticks;
		}

		public static bool operator !=(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks != rhs.Ticks;
		}

		public static double operator /(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks / (double)rhs.Ticks;
		}

		public static HostTimespan operator +(HostTimespan lhs, HostTimespan rhs) {
			return new HostTimespan(lhs.Ticks + rhs.Ticks);
		}

		public static HostTimespan operator -(HostTimespan lhs, HostTimespan rhs) {
			return new HostTimespan(lhs.Ticks - rhs.Ticks);
		}

		public static HostTimespan operator -(HostTimespan time) {
			return new HostTimespan(-time.Ticks);
		}

		public static bool operator >(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks > rhs.Ticks;
		}

		public static bool operator <(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks < rhs.Ticks;
		}

		public static bool operator >=(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks >= rhs.Ticks;
		}

		public static bool operator <=(HostTimespan lhs, HostTimespan rhs) {
			return lhs.Ticks <= rhs.Ticks;
		}

	}

}
