#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Sqids;

#if NET7_0_OR_GREATER
/// <summary>
/// The Sqids encoder/decoder. This is the main class.
/// </summary>
/// <typeparam name="T">
/// The integral numeric type that will be encoded/decoded.
/// Could be one of `int`, `long`, `byte`, `short`, and others. For the full list, check out
/// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
/// </typeparam>
public sealed class SqidsEncoder<T> where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
#else
/// <summary>
/// The Sqids encoder/decoder. This is the main class.
/// </summary>
public sealed class SqidsEncoder
#endif
{
	private const int MinAlphabetLength = 5;
	private const int MaxStackallocSize = 256; // NOTE: In bytes — this value is essentially arbitrary, the Microsoft docs is using 1024 but recommends being more conservative when choosing the value (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc), Hashids apparently uses 512 (https://github.com/ullmark/hashids.net/blob/9b1c69de4eedddf9d352c96117d8122af202e90f/src/Hashids.net/Hashids.cs#L17), and this article (https://vcsjones.dev/stackalloc/) uses 256. I've tried to be pretty cautious and gone with a low value.

	private readonly char[] _alphabet;
	private readonly int _minLength;
	private readonly string[] _blockList;

#if NET7_0_OR_GREATER
	/// <summary>
	/// The minimum numeric value that can be encoded/decoded using <see cref="SqidsEncoder{T}" />.
	/// This is always zero across all ports of Sqids.
	/// </summary>
	public static T MinValue => T.Zero;
#else
	/// <summary>
	/// The minimum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
	/// This is always zero across all ports of Sqids.
	/// </summary>
	public const int MinValue = 0;
#endif

#if NET7_0_OR_GREATER
	/// <summary>
	/// The maximum numeric value that can be encoded/decoded using <see cref="SqidsEncoder{T}" />.
	/// This is equal to `T.MaxValue`.
	/// </summary>
	public static T MaxValue => T.MaxValue;
#else
	/// <summary>
	/// The maximum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
	/// This is equal to `int.MaxValue`.
	/// </summary>
	public const int MaxValue = int.MaxValue;
#endif

#if NET7_0_OR_GREATER
	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder{T}" /> with the default options.
	/// </summary>
#else
	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder" /> with the default options.
	/// </summary>
#endif
	public SqidsEncoder() : this(new()) { }


#if NET7_0_OR_GREATER
	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder{T}" /> with custom options.
	/// </summary>
	/// <param name="options">
	/// The custom options.
	/// All properties of <see cref="SqidsOptions" /> are optional and will fall back to their
	/// defaults if not explicitly set.
	/// </param>
#else
	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder" /> with custom options.
	/// </summary>
	/// <param name="options">
	/// The custom options.
	/// All properties of <see cref="SqidsOptions" /> are optional and will fall back to their
	/// defaults if not explicitly set.
	/// </param>
#endif
	public SqidsEncoder(SqidsOptions options)
	{
		if (options.Alphabet.Length < MinAlphabetLength)
			throw new ArgumentException("The alphabet must contain at least 5 characters.");

		if (options.Alphabet.Distinct().Count() != options.Alphabet.Length)
			throw new ArgumentException("The alphabet must not contain duplicate characters.");

#if NET7_0_OR_GREATER
		if (T.CreateChecked(options.MinLength) < MinValue || options.MinLength > options.Alphabet.Length)
#else
		if (options.MinLength < MinValue || options.MinLength > options.Alphabet.Length)
#endif
			throw new ArgumentException($"The minimum length must be between {MinValue} and {options.Alphabet.Length}.");

		_minLength = options.MinLength;

		// NOTE: Cleanup the blocklist:
		options.BlockList = new HashSet<string>(
			options.BlockList,
			StringComparer.OrdinalIgnoreCase // NOTE: Effectively removes items that differ only in casing — leaves one version of each word casing-wise which will then be compared against the generated IDs case-insensitively
		);
		options.BlockList.RemoveWhere(w =>
			// NOTE: Removes words that are less than 3 characters long
			w.Length < 3 ||
			// NOTE: Removes words that contain characters not found in the alphabet
#if NETSTANDARD2_0
			w.Any(c => options.Alphabet.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) == -1) // NOTE: A `string.Contains` overload with `StringComparison` didn't exist prior to .NET Standard 2.1, so we have to resort to `IndexOf` — see https://stackoverflow.com/a/52791476
#else
			w.Any(c => !options.Alphabet.Contains(c, StringComparison.OrdinalIgnoreCase))
#endif
		);
		_blockList = options.BlockList.ToArray(); // NOTE: Arrays are faster to iterate than HashSets, so we construct an array here.

		// TODO: `sizeof(T)` doesn't work, so we resorted to `sizeof(long)`, but ideally we should get it to work somehow — see https://github.com/sqids/sqids-dotnet/pull/15#issue-1872663234
		Span<char> shuffledAlphabet = options.Alphabet.Length * sizeof(long) > MaxStackallocSize // NOTE: We multiply the number of characters by the size of a `char` to get the actual amount of memory that would be allocated.
			? new char[options.Alphabet.Length]
			: stackalloc char[options.Alphabet.Length];
		options.Alphabet.AsSpan().CopyTo(shuffledAlphabet);
		ConsistentShuffle(shuffledAlphabet);
		_alphabet = shuffledAlphabet.ToArray();
	}

	/// <summary>
	/// Encodes a single number into a Sqids ID.
	/// </summary>
	/// <param name="number">The number to encode.</param>
	/// <returns>A string containing the encoded ID.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the integers passed is smaller than <see cref="MinValue"/> (i.e. negative) or greater than <see cref="MaxValue"/> (i.e. `int.MaxValue`).</exception>
	/// <exception cref="T:System.OverflowException">If the decoded number overflows integer.</exception>
#if NET7_0_OR_GREATER
	public string Encode(T number)
#else
	public string Encode(int number)
#endif
	{
		if (number < MinValue || number > MaxValue)
			throw new ArgumentOutOfRangeException($"Encoding supports numbers between '{MinValue}' and '{MaxValue}'.");

		return Encode(stackalloc[] { number }); // NOTE: We use `stackalloc` here in order not to incur the cost of allocating an array on the heap, since we know the array will only have one element, we can use `stackalloc` safely.
	}

	/// <summary>
	/// Encodes multiple numbers into a Sqids ID.
	/// </summary>
	/// <param name="numbers">The numbers to encode.</param>
	/// <returns>A string containing the encoded IDs, or an empty string if the array passed is empty.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the integers passed is smaller than <see cref="MinValue"/> (i.e. negative) or greater than <see cref="MaxValue"/> (i.e. `int.MaxValue`).</exception>
	/// <exception cref="T:System.OverflowException">If the decoded number overflows integer.</exception>
#if NET7_0_OR_GREATER
	public string Encode(params T[] numbers)
#else
	public string Encode(params int[] numbers)
#endif
	{
		if (numbers.Length == 0)
			return string.Empty;

		if (numbers.Any(n => n < MinValue || n > MaxValue))
			throw new ArgumentOutOfRangeException($"Encoding supports numbers between '{MinValue}' and '{MaxValue}'.");

		return Encode(numbers.AsSpan());
	}

	/// <summary>
	/// Encodes a collection of numbers into a Sqids ID.
	/// </summary>
	/// <param name="numbers">The numbers to encode.</param>
	/// <returns>A string containing the encoded IDs, or an empty string if the `IEnumerable` passed is empty.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the integers passed is smaller than <see cref="MinValue"/> (i.e. negative) or greater than <see cref="MaxValue"/> (i.e. `int.MaxValue`).</exception>
	/// <exception cref="T:System.OverflowException">If the decoded number overflows integer.</exception>
#if NET7_0_OR_GREATER
	public string Encode(IEnumerable<T> numbers) =>
#else
	public string Encode(IEnumerable<int> numbers) =>
#endif
		Encode(numbers.ToArray());

	// TODO: Consider using `ArrayPool` if possible
#if NET7_0_OR_GREATER
	private string Encode(ReadOnlySpan<T> numbers, bool partitioned = false)
#else
	private string Encode(ReadOnlySpan<int> numbers, bool partitioned = false)
#endif
	{
		int offset = 0;
		for (int i = 0; i < numbers.Length; i++)
#if NET7_0_OR_GREATER
			offset += _alphabet[int.CreateChecked(numbers[i] % T.CreateChecked(_alphabet.Length))] + i;
#else
			offset += _alphabet[numbers[i] % _alphabet.Length] + i;
#endif

		offset = (numbers.Length + offset) % _alphabet.Length;

		Span<char> alphabetTemp = _alphabet.Length * sizeof(char) > MaxStackallocSize
			? new char[_alphabet.Length]
			: stackalloc char[_alphabet.Length];
		var alphabetSpan = _alphabet.AsSpan();
		alphabetSpan[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabetSpan[..offset].CopyTo(alphabetTemp[^offset..]);

		char prefix = alphabetTemp[0];
		char partition = alphabetTemp[1];
		alphabetTemp = alphabetTemp[2..];

		var builder = new StringBuilder(); // TODO: pool a la Hashids.net?
		builder.Append(prefix);

		for (int i = 0; i < numbers.Length; i++)
		{
			var number = numbers[i];
			var alphabetWithoutSeparator = alphabetTemp[..^1];
			var encodedNumber = ToId(number, alphabetWithoutSeparator);
			builder.Append(encodedNumber);

			if (i >= numbers.Length - 1) // NOTE: If the last one
				continue;

			char separator = alphabetTemp[^1]; // NOTE: Exclude the last character

			builder.Append(
				partitioned && i == 0
					? partition
					: separator
			);

			ConsistentShuffle(alphabetTemp);
		}

		string result = builder.ToString(); // TODO: Can't we get a span here as opposed to allocating a string?

		if (result.Length < _minLength)
		{
			if (!partitioned)
			{
#if NET7_0_OR_GREATER
				Span<T> newNumbers = (numbers.Length + 1) * sizeof(long) > MaxStackallocSize
					? new T[numbers.Length + 1]
					: stackalloc T[numbers.Length + 1];

				newNumbers[0] = T.Zero;
#else
				Span<int> newNumbers = (numbers.Length + 1) * sizeof(int) > MaxStackallocSize
					? new int[numbers.Length + 1]
					: stackalloc int[numbers.Length + 1];

				newNumbers[0] = 0;
#endif

				numbers.CopyTo(newNumbers[1..]);
				result = Encode(newNumbers, partitioned: true);
			}

			if (result.Length < _minLength)
			{
				var leftToMeetMinLength = _minLength - result.Length;
				var paddingFromAlphabet = alphabetTemp[..leftToMeetMinLength];
				builder.Insert(1, paddingFromAlphabet);
				result = builder.ToString();
			}
		}

		if (IsBlockedId(result.AsSpan()))
		{
#if NET7_0_OR_GREATER
			Span<T> newNumbers = numbers.Length * sizeof(long) > MaxStackallocSize
				? new T[numbers.Length]
				: stackalloc T[numbers.Length];
#else
			Span<int> newNumbers = numbers.Length * sizeof(int) > MaxStackallocSize
				? new int[numbers.Length]
				: stackalloc int[numbers.Length];
#endif
			numbers.CopyTo(newNumbers);

			if (partitioned)
			{
#if NET7_0_OR_GREATER
				if (numbers[0] + T.One > MaxValue)
					throw new OverflowException("Ran out of range checking against the blocklist.");
				else
					newNumbers[0] += T.One;
#else
				if (numbers[0] + 1 > MaxValue)
					throw new OverflowException("Ran out of range checking against the blocklist.");
				else
					newNumbers[0] += 1;
#endif
			}
			else
			{
#if NET7_0_OR_GREATER
				newNumbers = (numbers.Length + 1) * sizeof(long) > MaxStackallocSize
					? new T[numbers.Length + 1]
					: stackalloc T[numbers.Length + 1];

				newNumbers[0] = T.Zero;
#else
				newNumbers = (numbers.Length + 1) * sizeof(int) > MaxStackallocSize
					? new int[numbers.Length + 1]
					: stackalloc int[numbers.Length + 1];

				newNumbers[0] = 0;
#endif
				numbers.CopyTo(newNumbers[1..]);
			}

			result = Encode(newNumbers, partitioned: true);
		}

		return result;
	}

	/// <summary>
	/// Decodes an ID into numbers.
	/// </summary>
	/// <param name="id">The encoded ID.</param>
	/// <returns>
	/// An array of integers containing the decoded number(s) (it would contain only one element
	/// if the ID represents a single number); or an empty array if the input ID is null,
	/// empty, or includes characters not found in the alphabet.
	/// </returns>
#if NET7_0_OR_GREATER
	public T[] Decode(ReadOnlySpan<char> id)
#else
	public int[] Decode(ReadOnlySpan<char> id)
#endif
	{
		if (id.IsEmpty)
#if NET7_0_OR_GREATER
			return Array.Empty<T>();
#else
			return Array.Empty<int>();
#endif

		foreach (char c in id)
			if (!_alphabet.Contains(c))
#if NET7_0_OR_GREATER
				return Array.Empty<T>();
#else
				return Array.Empty<int>();
#endif

		var alphabetSpan = _alphabet.AsSpan();

		char prefix = id[0];
		int offset = alphabetSpan.IndexOf(prefix);

		Span<char> alphabetTemp = _alphabet.Length * sizeof(char) > MaxStackallocSize
			? new char[_alphabet.Length]
			: stackalloc char[_alphabet.Length];
		alphabetSpan[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabetSpan[..offset].CopyTo(alphabetTemp[^offset..]);

		char partition = alphabetTemp[1];
		alphabetTemp = alphabetTemp[2..];
		id = id[1..];

		int partitionIndex = id.IndexOf(partition);
		if (partitionIndex > 0 && partitionIndex < id.Length - 1)
		{
			id = id[(partitionIndex + 1)..];
			ConsistentShuffle(alphabetTemp);
		}

#if NET7_0_OR_GREATER
		var result = new List<T>();
#else
		var result = new List<int>();
#endif
		while (!id.IsEmpty)
		{
			char separator = alphabetTemp[^1];

			var separatorIndex = id.IndexOf(separator);
			var chunk = separatorIndex == -1 ? id : id[..separatorIndex]; // NOTE: The first part of `id` (every thing to the left of the separator) represents the number that we ought to decode.
			id = separatorIndex == -1 ? default : id[(separatorIndex + 1)..]; // NOTE: Everything to the right of the separator will be `id` for the next iteration

			if (chunk.IsEmpty)
				continue;

			var alphabetWithoutSeparator = alphabetTemp[..^1]; // NOTE: Exclude the last character from the alphabet (which is the separator)

			foreach (char c in chunk)
				if (!alphabetWithoutSeparator.Contains(c))
#if NET7_0_OR_GREATER
					return Array.Empty<T>();
#else
					return Array.Empty<int>();
#endif

			var decodedNumber = ToNumber(chunk, alphabetWithoutSeparator);
			result.Add(decodedNumber);

			if (!id.IsEmpty)
				ConsistentShuffle(alphabetTemp);
		}

		return result.ToArray(); // TODO: A way to return an array without creating a new array from the list like this?
	}

	// NOTE: Implicit `string` => `Span<char>` conversion was introduced in .NET Standard 2.1 (see https://learn.microsoft.com/en-us/dotnet/api/system.string.op_implicit), which means without this overload, calling `Decode` with a string on versions older than .NET Standard 2.1 would require calling `.AsSpan()` on the string, which is cringe.
#if NETSTANDARD2_0
	/// <summary>
	/// Decodes an ID into numbers.
	/// </summary>
	/// <param name="id">The encoded ID.</param>
	/// <returns>
	/// An array of integers containing the decoded number(s) (it would contain only one element
	/// if the ID represents a single number); or an empty array if the input ID is null,
	/// empty, or includes characters not found in the alphabet.
	/// </returns>
	public int[] Decode(string id) => Decode(id.AsSpan());
#endif

	private bool IsBlockedId(ReadOnlySpan<char> id)
	{
		foreach (string word in _blockList)
		{
			if (word.Length > id.Length)
				continue;

			if ((id.Length <= 3 || word.Length <= 3) &&
				id.Equals(word.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;

			if (word.Any(char.IsDigit) &&
				(id.StartsWith(word.AsSpan(), StringComparison.OrdinalIgnoreCase) ||
				 id.EndsWith(word.AsSpan(), StringComparison.OrdinalIgnoreCase)))
				return true;

			if (id.Contains(word.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	// NOTE: Shuffles a span of characters in place. The shuffle produces consistent results.
	private static void ConsistentShuffle(Span<char> chars)
	{
		for (int i = 0, j = chars.Length - 1; j > 0; i++, j--)
		{
			int r = (i * j + chars[i] + chars[j]) % chars.Length;
			(chars[i], chars[r]) = (chars[r], chars[i]);
		}
	}

#if NET7_0_OR_GREATER
	private static ReadOnlySpan<char> ToId(T num, ReadOnlySpan<char> alphabet)
#else
	private static ReadOnlySpan<char> ToId(int num, ReadOnlySpan<char> alphabet)
#endif
	{
		var id = new StringBuilder();
		var result = num;

#if NET7_0_OR_GREATER
		do
		{
			id.Insert(0, alphabet[int.CreateChecked(result % T.CreateChecked(alphabet.Length))]);
			result = result / T.CreateChecked(alphabet.Length);
		} while (result > T.Zero);
#else
		do
		{
			id.Insert(0, alphabet[result % alphabet.Length]);
			result = result / alphabet.Length;
		} while (result > 0);
#endif

		return id.ToString().AsSpan(); // TODO: possibly avoid creating a string
	}

#if NET7_0_OR_GREATER
	private static T ToNumber(ReadOnlySpan<char> id, ReadOnlySpan<char> alphabet)
#else
	private static int ToNumber(ReadOnlySpan<char> id, ReadOnlySpan<char> alphabet)
#endif
	{
#if NET7_0_OR_GREATER
		T result = T.Zero;
		foreach (var character in id)
			result = result * T.CreateChecked(alphabet.Length) + T.CreateChecked(alphabet.IndexOf(character));
#else
		int result = 0;
		foreach (var character in id)
			result = result * alphabet.Length + alphabet.IndexOf(character);
#endif
		return result;
	}
}
