using System;
using System.Numerics;

namespace Cryville.Seism.MomentTensor {
	/// <summary>
	/// Represents the three principal axes of a focal mechanism.
	/// </summary>
	/// <param name="T">The T axis.</param>
	/// <param name="N">The N axis.</param>
	/// <param name="P">The P axis.</param>
	public record struct PrincipalAxes(PrincipalAxis T, PrincipalAxis N, PrincipalAxis P) {
		/// <summary>
		/// The CLVD (compensated linear vector dipole) parameter (decimal fraction between 0 and 1).
		/// </summary>
		public readonly float CLVD => N.Length / MathF.Max(MathF.Abs(T.Length), MathF.Abs(P.Length));
		/// <summary>
		/// The double couple parameter (decimal fraction between 0 and 1).
		/// </summary>
		public readonly float DoubleCouple => MathF.Abs(1 - MathF.Abs(CLVD) / 0.5f);
		/// <summary>
		/// Gets the nodal planes from the principal axes.
		/// </summary>
		/// <returns>The nodal planes.</returns>
		public readonly NodalPlanes NodalPlanes() {
			var l = Vector3.Normalize(T.Vector - P.Vector);
			var n = Vector3.Normalize(T.Vector + P.Vector);
			return new NodalPlanes(CalculateNodalPlane(l, n), CalculateNodalPlane(n, l));
		}
		static Vector3 CalculateNodalPlane(Vector3 v1, Vector3 v2) {
			if (v1.Z > 0) {
				v1 = -v1;
				v2 = -v2;
			}
			float strike = MathF.Atan2(-v1.X, v1.Y);
			strike %= 2 * MathF.PI;
			if (strike < 0) strike += 2 * MathF.PI;
			return new Vector3(
				strike,
				MathF.Acos(-v1.Z),
				MathF.Atan2(-v2.Z, Vector3.Cross(v2, v1).Z)
			) * 180 / MathF.PI;
		}
	}
}
