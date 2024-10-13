using System;

namespace Cryville.Seism {
	/// <summary>
	/// Represents a real-time filter for Shindo (JMA seismic intensity) calculation, applied on acceleration waveforms.
	/// </summary>
	/// <typeparam name="T">The type of the filtered value.</typeparam>
	/// <param name="deltaT">The sampling period (seconds).</param>
	/// <param name="operators">The vector operators.</param>
	/// <param name="f0"></param>
	/// <param name="f1"></param>
	/// <param name="f2"></param>
	/// <param name="f3"></param>
	/// <param name="f4"></param>
	/// <param name="f5"></param>
	/// <param name="h2a"></param>
	/// <param name="h2b"></param>
	/// <param name="h3"></param>
	/// <param name="h4"></param>
	/// <param name="h5"></param>
	/// <param name="g">The output gain.</param>
	/// <seealso href="https://doi.org/10.4294/zisin.65.223">㓛刀 卓・青井 真・中村 洋光・鈴木 亘・森川 信之・藤原 広行, 2013, 震度のリアルタイム演算に用いられる近似フィルタの改良</seealso>
	public class RealtimeShindoFilter<T>(
		double deltaT, IVectorOperators<T> operators,
		double f0 = 0.45, double f1 = 7.0, double f2 = 0.5, double f3 = 12.0, double f4 = 20.0, double f5 = 30.0,
		double h2a = 1.0, double h2b = 0.75, double h3 = 0.9, double h4 = 0.6, double h5 = 0.6,
		double g = 1.262
	) : IIRFilterGroup<T>(ComputeParameters(deltaT, f0, f1, f2, f3, f4, f5, h2a, h2b, h3, h4, h5), g, operators) {
		static double[][] ComputeParameters(
			double deltaT,
			double f0, double f1, double f2, double f3, double f4, double f5,
			double h2a, double h2b, double h3, double h4, double h5
		) {
			double deltaTsq = deltaT * deltaT;
			double tau = Math.PI * 2;
			double o0 = tau * f0;
			double o1 = tau * f1;
			double o2 = tau * f2;
			double o0o1 = o0 * o1;
			double o1sq = o1 * o1;
			double o2sq = o2 * o2;
			static double[] A(double p1p, double p1n, double p2, double p4p, double p4n, double p5) => [
				p1p + p1n, p2, p1p - p1n,
				p4p + p4n, p5, p4p - p4n,
			];
			double[] A14(double hc, double fc) {
				double oc = tau * fc;
				double ocsq = oc * oc;
				return A(12 / deltaTsq + ocsq, 12 * hc * oc / deltaT, 10 * oc * oc - 24 / deltaTsq, ocsq, 0, 10 * oc * oc);
			}
			return [
				A( 8 / deltaTsq + o0o1, (4 * o0 + 2 * o1) / deltaT,  2 * o0o1 - 16 / deltaTsq,  4 / deltaTsq       ,        2 * o1 / deltaT,             -8 / deltaTsq),
				A(16 / deltaTsq + o1sq,          17 * o1  / deltaT,  2 * o1sq - 32 / deltaTsq,  4 / deltaTsq + o1sq,      8.5 * o1 / deltaT,  2 * o1sq -  8 / deltaTsq),
				A(12 / deltaTsq + o2sq,    12 * h2b * o2  / deltaT, 10 * o2sq - 24 / deltaTsq, 12 / deltaTsq + o2sq, 12 * h2a * o2 / deltaT, 10 * o2sq - 24 / deltaTsq),
				A14(h3, f3),
				A14(h4, f4),
				A14(h5, f5),
			];
		}
	}
}
