using System;
using System.Globalization;

namespace Cryville.Seism.NIED {
	/// <summary>
	/// Represents a decimal number with the count of significant digits.
	/// </summary>
	/// <param name="Number">All the significant digits in the number.</param>
	/// <param name="Scale">A scale to applied to <paramref name="Number" />, where a value N indicates a scale of 10^N.</param>
	public record struct ScaledNumber(int Number, int Scale) : IFormattable {
		/// <summary>
		/// The count of significant digits.
		/// </summary>
		public readonly int SignificantDigits {
			get {
				int n = Number;
				int r = 0;
				while (n != 0) {
					n /= 10;
					r++;
				}
				return r;
			}
		}
		/// <inheritdoc />
		public override readonly string ToString() => ToDouble().ToString($"G{SignificantDigits}", CultureInfo.CurrentCulture);
		/// <summary>
		/// Formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use. -or- A <see langword="null" /> reference to use the default format defined for the type of the <see cref="IFormattable" /> implementation.</param>
		/// <returns>The value of the current instance in the specified format.</returns>
		public readonly string ToString(string? format) => ToDouble().ToString(format, CultureInfo.CurrentCulture);
		/// <inheritdoc />
		public readonly string ToString(string? format, IFormatProvider? formatProvider) => ToDouble().ToString(format, formatProvider);

		/// <summary>
		/// Converts the number to a <see cref="double" />.
		/// </summary>
		/// <returns>A <see cref="double" /> that represents the number.</returns>
		public readonly double ToDouble() => Number * Math.Pow(10, Scale);
		/// <inheritdoc />
		public static implicit operator double(ScaledNumber value) => value.ToDouble();
		/// <summary>
		/// Converts the number to a <see cref="float" />.
		/// </summary>
		/// <returns>A <see cref="float" /> that represents the number.</returns>
		public readonly float ToSingle() => Number * MathF.Pow(10, Scale);
		/// <inheritdoc />
		public static implicit operator float(ScaledNumber value) => value.ToSingle();
	}
}
