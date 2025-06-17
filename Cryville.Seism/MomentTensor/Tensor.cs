using System;

namespace Cryville.Seism.MomentTensor {
	/// <summary>
	/// Represents the six moment-tensor elements in the spherical coordinate system defined by local upward vertical (r), North-South (t), and West-East (p) directions.
	/// </summary>
	/// <param name="Mtt">The moment-tensor element Mtt.</param>
	/// <param name="Mpp">The moment-tensor element Mpp.</param>
	/// <param name="Mrr">The moment-tensor element Mrr.</param>
	/// <param name="Mrt">The moment-tensor element Mrt.</param>
	/// <param name="Mrp">The moment-tensor element Mrp.</param>
	/// <param name="Mtp">The moment-tensor element Mtp.</param>
	public record struct Tensor(float Mtt, float Mpp, float Mrr, float Mrt, float Mrp, float Mtp) {
		/// <summary>
		/// The scalar moment in newton-meters.
		/// </summary>
		public readonly float Moment => MathF.Sqrt(0.5f * (
			Mrr * Mrr + Mtt * Mtt + Mpp * Mpp +
			2 * (Mrt * Mrt + Mrp * Mrp + Mtp * Mtp)
		));
		/// <summary>
		/// The moment magnitude.
		/// </summary>
		public readonly float Magnitude => 2f / 3f * (MathF.Log10(Moment) - 9.1f);

		/// <summary>
		/// Gets the principal axes of the focal mechanism.
		/// </summary>
		/// <returns>The principal axes of the focal mechanism.</returns>
		/// <exception cref="InvalidOperationException">The principal axes cannot be calculated because the value fails to converge.</exception>
		public readonly PrincipalAxes PrincipalAxes() {
#pragma warning disable IDE0079 // False report
#pragma warning disable CA1814
			float[,] a = new float[3, 3] {
				{  Mtt, -Mtp,  Mrt },
				{ -Mtp,  Mpp, -Mrp },
				{  Mrt, -Mrp,  Mrr },
			};
			float[] e = [Mtt, Mpp, Mrr];
			float[,] v = new float[3, 3] {
				{ 1, 0, 0 },
				{ 0, 1, 0 },
				{ 0, 0, 1 },
			};
#pragma warning restore CA1814
#pragma warning restore IDE0079
			bool changed;
			int rotations = 0;
			do {
				changed = false;
				for (int p = 0; p < 3; p++) {
					for (int q = p + 1; q < 3; q++) {
						float app = e[p], aqq = e[q], apq = a[p, q];
						float phi = 0.5f * MathF.Atan2(2 * apq, aqq - app);
						float c = MathF.Cos(phi), s = MathF.Sin(phi);
						float app1 = c * c * app - 2 * s * c * apq + s * s * aqq;
						float aqq1 = s * s * app + 2 * s * c * apq + c * c * aqq;

						if (app1 == app && aqq1 == aqq) continue;
						changed = true;
						rotations++;

						e[p] = app1;
						e[q] = aqq1;
						a[p, q] = 0;
						for (int i = 0; i < p; i++) {
							float aip = a[i, p];
							float aiq = a[i, q];
							a[i, p] = c * aip - s * aiq;
							a[i, q] = c * aiq + s * aip;
						}
						for (int i = p + 1; i < q; i++) {
							float api = a[p, i];
							float aiq = a[i, q];
							a[p, i] = c * api - s * aiq;
							a[i, q] = c * aiq + s * api;
						}
						for (int i = q + 1; i < 3; i++) {
							float api = a[p, i];
							float aqi = a[q, i];
							a[p, i] = c * api - s * aqi;
							a[q, i] = c * aqi + s * api;
						}
						for (int i = 0; i < 3; i++) {
							float vip = v[i, p];
							float viq = v[i, q];
							v[i, p] = c * vip - s * viq;
							v[i, q] = c * viq + s * vip;
						}
					}
				}
			} while (changed && rotations < 100);

			if (changed) throw new InvalidOperationException("Failed to converge.");

			PrincipalAxis[] axes = [
				new PrincipalAxis(new(v[0, 0], v[1, 0], v[2, 0]), e[0]),
				new PrincipalAxis(new(v[0, 1], v[1, 1], v[2, 1]), e[1]),
				new PrincipalAxis(new(v[0, 2], v[1, 2], v[2, 2]), e[2]),
			];
			Array.Sort(axes, (a, b) => -a.Length.CompareTo(b.Length));
			return new(axes[0], axes[1], axes[2]);
		}

		/// <summary>
		/// Creates an instance of the <see cref="Tensor" /> struct from the strike, dip, and rake angles of a nodal plane.
		/// </summary>
		/// <param name="strike">The strike angle in degrees.</param>
		/// <param name="dip">The dip angle in degrees.</param>
		/// <param name="rake">The rake angle in degrees.</param>
		/// <param name="moment">The scalar moment of the focal mechanism in newton-meters.</param>
		/// <returns>The tensor.</returns>
		public static Tensor FromStrikeDipRake(float strike, float dip, float rake, float moment) {
			float s = strike * MathF.PI / 180,
				ss = MathF.Sin(s), cs = MathF.Cos(s),
				s2s = MathF.Sin(2 * s), c2s = MathF.Cos(2 * s);
			float d = dip * MathF.PI / 180,
				sd = MathF.Sin(d), cd = MathF.Cos(d),
				s2d = MathF.Sin(2 * d), c2d = MathF.Cos(2 * d);
			float r = (rake % 90 != 0 ? rake : rake + 1e-15f) * MathF.PI / 180,
				sr = MathF.Sin(r), cr = MathF.Cos(r);
			return new(
				-(sd * cr * s2s + s2d * sr * ss * ss) * moment,
				(sd * cr * s2s - s2d * sr * cs * cs) * moment,
				s2d * sr * moment,
				-(cd * cr * cs + c2d * sr * ss) * moment,
				(cd * cr * ss - c2d * sr * cs) * moment,
				-(sd * cr * c2s + s2d * sr * s2s * 0.5f) * moment
			);
		}
	}
}
