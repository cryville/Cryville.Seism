namespace Cryville.Seism.Intensity {
	/// <summary>
	/// Represents a 2-order filter group.
	/// </summary>
	/// <typeparam name="T">The type of the filtered value.</typeparam>
	public class IIRFilterGroup<T> {
		readonly double[][] _parameters;
		readonly double _gain;
		readonly IVectorOperators<T> _operators;
		readonly T[][] _delayLine;

		/// <summary>
		/// Creates an instance of the <see cref="IIRFilterGroup{T}" /> class.
		/// </summary>
		/// <param name="parameters">The parameters of the filter group.</param>
		/// <param name="gain">The output gain.</param>
		/// <param name="operators">The vector operators.</param>
		/// <remarks>
		/// Each array in <paramref name="parameters" /> represents a single 2-order filter with 6 parameters [a0, a1, a2, b0, b1, b2]. Let input be x and output be y. y(i) = (b0 * x(i) + b1 * x(i - 1) + b2 * x(i - 2) - a1 * y(i - 1) - a2 * y(i - 2)) / a0.
		/// </remarks>
		public IIRFilterGroup(double[][] parameters, double gain, IVectorOperators<T> operators) {
			_parameters = parameters;
			_gain = gain;
			_operators = operators;
			_delayLine = new T[_parameters.Length + 1][];
			for (int i = 0; i < _delayLine.Length; i++) _delayLine[i] = new T[2];
		}

		/// <summary>
		/// Feeds in the next input value and computes the next output value.
		/// </summary>
		/// <param name="x">The input value.</param>
		/// <returns>The output value.</returns>
		public T Update(T x) {
			for (int i = 0; i < _parameters.Length; i++) {
				double[] p = _parameters[i];
				T[] xd = _delayLine[i], yd = _delayLine[i + 1];
				T x1 = xd[1], x2 = xd[0], y1 = yd[1], y2 = yd[0];
				xd[0] = x1; xd[1] = x;
				x = _operators.Multiply(
					1.0 / p[0],
					_operators.Add(
						_operators.Add(
							_operators.Multiply(-p[1], y1),
							_operators.Multiply(-p[2], y2)
						),
						_operators.Add(
							_operators.Multiply(p[3], x),
							_operators.Add(
								_operators.Multiply(p[4], x1),
								_operators.Multiply(p[5], x2)
							)
						)
					)
				);
			}
			var ld = _delayLine[^1];
			ld[0] = ld[1]; ld[1] = x;
			return _operators.Multiply(_gain, x);
		}
	}
}
