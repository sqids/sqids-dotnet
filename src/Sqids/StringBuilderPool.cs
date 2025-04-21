using System.Collections.Concurrent;

namespace Sqids;

/// <summary>
/// Provides a thread-safe object pool for reusing <see cref="StringBuilder"/> instances.
/// This helps to reduce memory allocations and garbage collection pressure, particularly
/// in scenarios involving frequent string manipulations.
/// </summary>
/// <remarks>
/// This class implements the Singleton pattern via the <see cref="Instance"/> property to ensure
/// a single, globally accessible pool. It uses a <see cref="ConcurrentBag{T}"/> internally,
/// making the <see cref="Rent"/> and <see cref="Return"/> operations thread-safe.
///
/// Usage pattern:
/// <code>
/// var sb = StringBuilderPool.Instance.Rent();
/// try
/// {
///     // Use the StringBuilder instance (sb)
///     sb.Append("example");
///     // ... other operations ...
///     string result = sb.ToString();
/// }
/// finally
/// {
///     StringBuilderPool.Instance.Return(sb);
/// }
/// </code>
/// It is crucial to always return the rented <see cref="StringBuilder"/> instance back to the pool
/// using the <see cref="Return"/> method within a finally block to prevent resource leaks
/// and ensure the pool remains effective. Failure to return instances will lead to the pool
/// creating new instances when depleted, negating the benefits of pooling.
/// </remarks>
public class StringBuilderPool
{
	private readonly ConcurrentBag<StringBuilder> _pool = [];
	private static StringBuilderPool? _instance;

	/// <summary>
	/// Gets the singleton instance of the <see cref="StringBuilderPool"/>.
	/// </summary>
	/// <value>The singleton <see cref="StringBuilderPool"/> instance.</value>
	public static StringBuilderPool Instance => _instance ??= new StringBuilderPool();

	/// <summary>
	/// Initializes a new instance of the <see cref="StringBuilderPool"/> class
	/// and optionally pre-populates the pool.
	/// </summary>
	/// <param name="initialCapacity">The initial number of <see cref="StringBuilder"/> instances to create and add to the pool. Defaults to 16.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="initialCapacity"/> is less than 0.</exception>
	public StringBuilderPool(int initialCapacity = 16)
	{
		if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));

		// Pre-populate the pool for better initial performance
		for (var i = 0; i < initialCapacity; i++)
		{
			_pool.Add(new StringBuilder());
		}
	}

	/// <summary>
	/// Rents a <see cref="StringBuilder"/> instance from the pool.
	/// </summary>
	/// <returns>
	/// A cleared <see cref="StringBuilder"/> instance obtained from the pool if available;
	/// otherwise, a new <see cref="StringBuilder"/> instance.
	/// </returns>
	/// <remarks>
	/// The returned <see cref="StringBuilder"/> is cleared before being provided to the caller.
	/// If the pool is empty, a new instance is allocated. Remember to return the instance
	/// using <see cref="Return"/> when finished.
	/// </remarks>
	public StringBuilder Rent()
	{
		if (_pool.TryTake(out var sb))
		{
			// Ensure the StringBuilder is clean before reuse
			sb.Clear();
			return sb;
		}

		// Pool is empty, create a new instance
		return new StringBuilder();
	}

	/// <summary>
	/// Returns a <see cref="StringBuilder"/> instance to the pool.
	/// </summary>
	/// <param name="sb">The <see cref="StringBuilder"/> instance to return to the pool.</param>
	/// <remarks>
	/// The provided <see cref="StringBuilder"/> instance is cleared before being added back
	/// to the pool, preparing it for the next renter. It is crucial to call this method
	/// for every instance obtained via <see cref="Rent"/> to ensure proper pool functioning.
	/// Typically called within a `finally` block.
	/// </remarks>
	public void Return(StringBuilder sb)
	{
		// Clear the StringBuilder before putting it back into the pool
		sb.Clear();
		_pool.Add(sb);
	}
}
