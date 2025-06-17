using System;
using System.Numerics;

namespace Cryville.Seism.MomentTensor {
	/// <summary>
	/// Represents an eigenvector of a moment tensor expressed in its principal-axes system.
	/// </summary>
	/// <param name="Vector">The eigenvector.</param>
	/// <param name="Length">The eigenvalue.</param>
	public record struct PrincipalAxis(Vector3 Vector, float Length) {
		/// <summary>
		/// The azimuth of <see cref="Vector" /> in radians measured clockwise from South-North direction at epicenter.
		/// </summary>
		public readonly float Azimuth {
			get {
				float x = Vector.X, y = Vector.Y;
				if (x == 0 && y == 0) return 0;
				return MathF.Atan2(y, x);
			}
		}
		/// <summary>
		/// The plunge of <see cref="Vector" /> in radians measured against downward vertical direction at epicenter.
		/// </summary>
		public readonly float Plunge => MathF.Asin(Vector.Z / Vector.Length());
	}
}
