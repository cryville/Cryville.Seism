using System;
using System.Collections.Generic;

namespace Cryville.Seism {
	/// <summary>
	/// Represents a delay line that computes a value so that the duration when the value is over the computed value is exactly the given bleeding duration.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="duration">The maximum count of values in the delay line.</param>
	/// <param name="bleedingDuration">The target bleeding count of values.</param>
	/// <param name="defaultValue">The default computed value to be returned when the count of values is insufficient.</param>
	public class BleedingDelayLine<T>(int duration, int bleedingDuration, T defaultValue) where T : IComparable<T> {
		readonly Queue<T> _data = new(duration);
		readonly List<T> _stack = new(duration);

		/// <summary>
		/// The computed value.
		/// </summary>
		public T ComputedValue => _data.Count < bleedingDuration ? defaultValue : _stack[^bleedingDuration];
		/// <summary>
		/// Adds a value to the delay line.
		/// </summary>
		/// <param name="value">The value.</param>
		public void Add(T value) {
			if (_data.Count == duration) {
				var oldValue = _data.Dequeue();
				_stack.RemoveAt(_stack.BinarySearch(oldValue));
			}
			_data.Enqueue(value);
			int index = _stack.BinarySearch(value);
			if (index < 0) index = ~index;
			_stack.Insert(index, value);
		}
	}
}
