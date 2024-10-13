using System.Numerics;

namespace Cryville.Seism {
	/// <summary>
	/// Represents a series of operators on a specific vector type.
	/// </summary>
	/// <typeparam name="T">The type of the vector.</typeparam>
	public interface IVectorOperators<T> {
		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">The first vector.</param>
		/// <param name="b">The second vector.</param>
		/// <returns>The sum of the two vectors.</returns>
		T Add(T a, T b);
		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="k">The scalar.</param>
		/// <param name="v">The vector.</param>
		/// <returns>The result vector.</returns>
		T Multiply(double k, T v);
	}
	/// <summary>
	/// Represents a series of operators on the <see cref="double" /> vector type.
	/// </summary>
	public class DoubleOperators : IVectorOperators<double> {
		static DoubleOperators? s_instance;
		/// <summary>
		/// An instance of the <see cref="DoubleOperators" /> class.
		/// </summary>
		public static DoubleOperators Instance => s_instance ??= new();
		/// <inheritdoc />
		public double Add(double a, double b) => a + b;
		/// <inheritdoc />
		public double Multiply(double k, double v) => k * v;
	}
	/// <summary>
	/// Represents a series of operators on the <see cref="Vector3" /> vector type.
	/// </summary>
	public class Vector3Operators : IVectorOperators<Vector3> {
		static Vector3Operators? s_instance;
		/// <summary>
		/// An instance of the <see cref="Vector3Operators" /> class.
		/// </summary>
		public static Vector3Operators Instance => s_instance ??= new();
		/// <inheritdoc />
		public Vector3 Add(Vector3 a, Vector3 b) => a + b;
		/// <inheritdoc />
		public Vector3 Multiply(double k, Vector3 v) => (float)k * v;
	}
}
