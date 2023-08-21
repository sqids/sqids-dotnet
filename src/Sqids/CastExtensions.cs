namespace Sqids;

internal static class CastExtensions
{
	/// <summary>
	/// Casts collection of <see cref="Int32"/> to collection of <see cref="Int64"/>
	/// </summary>
	/// <param name="source">Source collection</param>
	/// <returns>Collection of <see cref="Int64"/></returns>
	public static IEnumerable<long> CastToInt64(this IEnumerable<int> source) =>
		source.Select(value => (long)value);

	/// <summary>
	/// Casts collection of <see cref="Int64"/> to collection of <see cref="Int32"/>
	/// </summary>
	/// <param name="source">Source collection</param>
	/// <returns>Collection of <see cref="Int32"/></returns>
	public static IEnumerable<int> CastToInt32(this IEnumerable<long> source) =>
		source.Select(value => (int)value);
}
