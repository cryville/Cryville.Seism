using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Cryville.Seism.Intensity {
	/// <summary>
	/// A calculator for calculating LPGM (long-period ground motion).
	/// </summary>
	/// <seealso href="https://github.com/fleneindre/lpgm-calculator">lpgm-calculator (c) 2021 François Le Neindre</seealso>
	public class RealtimeLPGMCalculator {
		readonly double _samplePeriod;
		readonly IIRFilterGroup<Vector3> _filter;

		const int _periodCount = 32;
		readonly Matrix2x2[] _matA = new Matrix2x2[_periodCount];
		readonly Matrix2x2[] _matB = new Matrix2x2[_periodCount];
		readonly Matrix2x2[] _buffer = new Matrix2x2[_periodCount];
		readonly double[] _sva = new double[_periodCount];

		Vector3 _lastAcceleration;
		Vector3 _velocity;

		/// <summary>
		/// The calculated SVA per period.
		/// </summary>
		public IReadOnlyList<double> SVA => _sva;
		/// <summary>
		/// The current maximum SVA among all the periods.
		/// </summary>
		public double MaxSVA => _sva.Max();
		/// <summary>
		/// The current computed velocity.
		/// </summary>
		public Vector3 Velocity => _velocity;
		/// <summary>
		/// The current filtered acceleration.
		/// </summary>
		public Vector3 FilteredAcceleration => _lastAcceleration;

		/// <summary>
		/// Creates an instance of the <see cref="RealtimeLPGMCalculator" /> class.
		/// </summary>
		/// <param name="sampleRate">The sample rate.</param>
		/// <param name="damping">The damping factor.</param>
		public RealtimeLPGMCalculator(int sampleRate, double damping = 0.05) {
			_samplePeriod = 1.0 / sampleRate;
			_filter = new([IIRFilters.ButterworthHighpass(0.05, sampleRate)], 1.0, Vector3Operators.Instance);

			double dampingSq = damping * damping;
			double dampingF = Math.Sqrt(1 - dampingSq);
			for (int periodIndex = 0; periodIndex < _periodCount; periodIndex++) {
				double period = 1.6 + 0.2 * periodIndex;
				double omega = 2 * Math.PI / period;
				double fOmega = omega * dampingF;
				double pOmega = fOmega * _samplePeriod;
				double sOmega = Math.Sin(pOmega);
				double cOmega = Math.Cos(pOmega);
				double e = Math.Exp(-damping * omega * _samplePeriod);

				double a1 = damping / dampingF * sOmega;
				double a2 = sOmega * e / dampingF;
				_matA[periodIndex] = new Matrix2x2(
					 e * (a1 + cOmega), a2 / omega,
					 -a2 * omega, e * (-a1 + cOmega)
				);
				double omegaSq = omega * omega;
				double omegaSqPeriod = omegaSq * _samplePeriod;
				double omegaCbPeriod = omegaSqPeriod * omega;
				double b0 = 2 * damping / omegaCbPeriod;
				double b01 = 1 / omegaSq;
				double b02 = 1 / omegaSqPeriod;
				double b10 = (2 * dampingSq - 1) / omegaSqPeriod;
				double b11 = b10 + damping / omega;
				double b12 = sOmega / fOmega;
				double b13 = (b0 + b01) * cOmega;
				double b14 = b0 * cOmega;
				double b2 = cOmega - damping * sOmega / dampingF;
				double b3 = omega * (dampingF * sOmega + damping * cOmega);
				_matB[periodIndex] = new Matrix2x2(
					e * (b11 * b12 + b13) - b0, -e * (b10 * b12 + b14) + b0 - b01,
					e * (b11 * b2 - (b0 + b01) * b3) + b02, -e * (b10 * b2 - b0 * b3) - b02
				);
			}
		}

		/// <summary>
		/// Feeds the next input acceleration vector.
		/// </summary>
		/// <param name="x">The NS component of the input acceleration vector.</param>
		/// <param name="y">The EW component of the input acceleration vector.</param>
		/// <param name="z">The UD component of the input acceleration vector.</param>
		public void Update(double x, double y, double z) => Update(new((float)x, (float)y, (float)z));
		/// <summary>
		/// Feeds the next acceleration vector.
		/// </summary>
		/// <param name="raw">The next acceleration vector.</param>
		/// <remarks>
		/// The X component of the vector is the NS component, the Y component is the EW component, and the Z component is the UD component.
		/// </remarks>
		public void Update(Vector3 raw) {
			var acceleration = _filter.Update(raw);
			_velocity += (_lastAcceleration + acceleration) * (float)_samplePeriod / 2;

			for (int periodIndex = 0; periodIndex < _periodCount; periodIndex++) {
				var a = _matA[periodIndex];
				var b = _matB[periodIndex];
				var c = _buffer[periodIndex];
				c = a * c + b * new Matrix2x2(_lastAcceleration.X, _lastAcceleration.Y, acceleration.X, acceleration.Y);
				_buffer[periodIndex] = c;
				double vx = c.M10 + _velocity.X;
				double vy = c.M11 + _velocity.Y;
				_sva[periodIndex] = Math.Sqrt(vx * vx + vy * vy);
			}

			_lastAcceleration = acceleration;
		}

		record struct Matrix2x2(double M00, double M01, double M10, double M11) {
			public static Matrix2x2 operator +(Matrix2x2 lhs, Matrix2x2 rhs) => new(
				lhs.M00 + rhs.M00, lhs.M01 + rhs.M01,
				lhs.M10 + rhs.M10, lhs.M11 + rhs.M11
			);
			public static Matrix2x2 operator *(Matrix2x2 lhs, Matrix2x2 rhs) => new(
				lhs.M00 * rhs.M00 + lhs.M01 * rhs.M10,
				lhs.M00 * rhs.M01 + lhs.M01 * rhs.M11,
				lhs.M10 * rhs.M00 + lhs.M11 * rhs.M10,
				lhs.M10 * rhs.M01 + lhs.M11 * rhs.M11
			);
		}
	}
}
