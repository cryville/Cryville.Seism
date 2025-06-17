using System.Numerics;

namespace Cryville.Seism.MomentTensor {
	/// <summary>
	/// Represents a pair of nodal planes.
	/// </summary>
	/// <param name="NP1">The first nodal plane.</param>
	/// <param name="NP2">The second nodal plane.</param>
	/// <remarks>
	/// <para>The three components of each vector are as follows:</para>
	/// <list type="bullet">
	/// <item><term><see cref="Vector3.X" /></term><description>Strike</description></item>
	/// <item><term><see cref="Vector3.Y" /></term><description>Dip</description></item>
	/// <item><term><see cref="Vector3.Z" /></term><description>Rake (Slip)</description></item>
	/// </list>
	/// <para>All component values are in degrees.</para>
	/// </remarks>
	public record struct NodalPlanes(Vector3 NP1, Vector3 NP2);
}
