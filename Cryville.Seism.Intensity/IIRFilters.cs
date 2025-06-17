using System;

namespace Cryville.Seism.Intensity {
	/// <summary>
	/// Provides methods that generate 2-order filter parameters to be used in <see cref="IIRFilterGroup{T}" />.
	/// </summary>
	public static class IIRFilters {
		/// <summary>
		/// Generates parameters of a 2-order Butterworth highpass filter.
		/// </summary>
		/// <param name="freq">The critical frequency.</param>
		/// <param name="sampleRate">The sample rate.</param>
		/// <returns>The parameters generated.</returns>
		/// <seealso href="https://github.com/cmdwtf/UnityTools">UnityTools (C) 2021 by Chris Marc Dailey (nitz) &lt; https://cmd.wtf &gt;</seealso>
		public static double[] ButterworthHighpass(double freq, double sampleRate) {
			double c = Math.Tan(Math.PI * freq / sampleRate);
			double csq = c * c;
			double p1p = 1 + csq;
			double p1n = Math.Sqrt(2) * c;
			return [p1p + p1n, 2 * (csq - 1), p1p - p1n, 1.0, -2.0, 1.0];
		}
	}
}
