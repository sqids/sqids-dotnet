using System.Buffers;
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
	private const int MinAlphabetLength = 3;
	private const int MaxMinLength = 255;
	private const int MaxStackallocSize = 256; // NOTE: In bytes — this value is essentially arbitrary, the Microsoft docs is using 1024 but recommends being more conservative when choosing the value (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc), Hashids apparently uses 512 (https://github.com/ullmark/hashids.net/blob/9b1c69de4eedddf9d352c96117d8122af202e90f/src/Hashids.net/Hashids.cs#L17), and this article (https://vcsjones.dev/stackalloc/) uses 256. I've tried to be pretty cautious and gone with a low value.

	private readonly char[] _alphabet;
	private readonly int _minLength;
	private readonly string[] _blockList;

#if NET8_0_OR_GREATER
	private readonly SearchValues<char> _digitValues = SearchValues.Create("0123456789");
	private readonly SearchValues<char> _alphabetValues;
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
	/// <exception cref="T:System.ArgumentNullException" />
	/// <exception cref="T:System.ArgumentOutOfRangeException" />
#else
	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder" /> with custom options.
	/// </summary>
	/// <param name="options">
	/// The custom options.
	/// All properties of <see cref="SqidsOptions" /> are optional and will fall back to their
	/// defaults if not explicitly set.
	/// </param>
	/// <exception cref="T:System.ArgumentNullException" />
	/// <exception cref="T:System.ArgumentOutOfRangeException" />
#endif
	public SqidsEncoder(SqidsOptions options)
	{
		_ = options ?? throw new ArgumentNullException(nameof(options));
		_ = options.Alphabet ?? throw new ArgumentNullException(nameof(options.Alphabet));
		_ = options.BlockList ?? throw new ArgumentNullException(nameof(options.BlockList));

		if (options.Alphabet.Distinct().Count() != options.Alphabet.Length)
			throw new ArgumentOutOfRangeException(
				nameof(options.Alphabet),
				"The alphabet must not contain duplicate characters."
			);

		if (Encoding.UTF8.GetByteCount(options.Alphabet) != options.Alphabet.Length)
			throw new ArgumentOutOfRangeException(
				nameof(options.Alphabet),
				"The alphabet must not contain multi-byte characters."
			);

		if (options.Alphabet.Length < MinAlphabetLength)
			throw new ArgumentOutOfRangeException(
				nameof(options.Alphabet),
				$"The alphabet must contain at least {MinAlphabetLength} characters."
			);

		if (options.MinLength < 0 || options.MinLength > MaxMinLength)
			throw new ArgumentOutOfRangeException(
				nameof(options.MinLength),
				$"The minimum length must be between 0 and {MaxMinLength}."
			);

		_minLength = options.MinLength;

		// NOTE: Cleanup the blocklist:
		HashSet<string> blockList = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase // NOTE: Effectively removes items that differ only in casing — leaves one version of each word casing-wise which will then be compared against the generated IDs case-insensitively
		);

		foreach (string w in options.BlockList)
		{
			if (
				// NOTE: Removes words that are less than 3 characters long
				w.Length < 3 ||
				// NOTE: Removes words that contain characters not found in the alphabet
#if NETSTANDARD2_0
				w.Any(c => options.Alphabet.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) == -1)) // NOTE: A `string.Contains` overload with `StringComparison` didn't exist prior to .NET Standard 2.1, so we have to resort to `IndexOf` — see https://stackoverflow.com/a/52791476
#else
				w.Any(c => !options.Alphabet.Contains(c, StringComparison.OrdinalIgnoreCase)))
#endif
			continue;

			blockList.Add(w);
		}

		_blockList = blockList.ToArray(); // NOTE: Arrays are faster to iterate than HashSets, so we construct an array here.

		_alphabet = options.Alphabet.ToCharArray();
#if NET8_0_OR_GREATER
		_alphabetValues = SearchValues.Create(_alphabet);
#endif
		ConsistentShuffle(_alphabet);
	}

	/// <summary>
	/// Encodes a single number into a Sqids ID.
	/// </summary>
	/// <param name="number">The number to encode.</param>
	/// <returns>A string containing the encoded ID.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If the number passed is smaller than 0 (i.e. negative).</exception>
	/// <exception cref="T:System.ArgumentException">If the encoding reaches maximum re-generation attempts due to the blocklist.</exception>
#if NET7_0_OR_GREATER
	public string Encode(T number)
#else
	public string Encode(int number)
#endif
	{
#if NET8_0_OR_GREATER
		ArgumentOutOfRangeException.ThrowIfLessThan(number, T.Zero, nameof(number));
#else
#if NET7_0
		if (number < T.Zero)
#else
		if (number < 0)
#endif
			throw new ArgumentOutOfRangeException(
				nameof(number),
				"Encoding is only supported for zero and positive numbers."
			);

		return Encode(stackalloc[] { number }); // NOTE: We use `stackalloc` here in order not to incur the cost of allocating an array on the heap, since we know the array will only have one element, we can use `stackalloc` safely.
#endif

#if NET8_0_OR_GREATER
		return Encode([number]);
#endif
	}

	/// <summary>
	/// Encodes multiple numbers into a Sqids ID.
	/// </summary>
	/// <param name="numbers">The numbers to encode.</param>
	/// <returns>A string containing the encoded IDs, or an empty string if the array passed is empty.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the numbers passed is smaller than 0 (i.e. negative).</exception>
	/// <exception cref="T:System.ArgumentException">If the encoding reaches maximum re-generation attempts due to the blocklist.</exception>
#if NET7_0_OR_GREATER
	public string Encode(params T[] numbers)
#else
	public string Encode(params int[] numbers)
#endif
	{
		if (numbers.Length == 0)
			return string.Empty;

		foreach (var number in numbers)
#if NET8_0_OR_GREATER
			ArgumentOutOfRangeException.ThrowIfLessThan(number, T.Zero, nameof(numbers));
#else
#if NET7_0
			if (number < T.Zero)
#else
			if (number < 0)
#endif
				throw new ArgumentOutOfRangeException(
					nameof(numbers),
					"Encoding is only supported for zero and positive numbers."
				);
#endif

		return Encode(numbers.AsSpan());
	}

	/// <summary>
	/// Encodes a collection of numbers into a Sqids ID.
	/// </summary>
	/// <param name="numbers">The numbers to encode.</param>
	/// <returns>A string containing the encoded IDs, or an empty string if the `IEnumerable` passed is empty.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the numbers passed is smaller than 0 (i.e. negative).</exception>
	/// <exception cref="T:System.ArgumentException">If the encoding reaches maximum re-generation attempts due to the blocklist.</exception>
#if NET7_0_OR_GREATER
	public string Encode(IEnumerable<T> numbers) =>
#else
	public string Encode(IEnumerable<int> numbers) =>
#endif
		Encode(numbers.ToArray());

	// TODO: Consider using `ArrayPool` if possible
#if NET7_0_OR_GREATER
	private string Encode(ReadOnlySpan<T> numbers, int increment = 0)
#else
	private string Encode(ReadOnlySpan<int> numbers, int increment = 0)
#endif
	{
		if (increment > _alphabet.Length)
			throw new ArgumentException("Reached max attempts to re-generate the ID.");

		int offset = 0;
		for (int i = 0; i < numbers.Length; i++)
#if NET7_0_OR_GREATER
			offset += _alphabet[int.CreateChecked(numbers[i] % T.CreateChecked(_alphabet.Length))] + i;
#else
			offset += _alphabet[numbers[i] % _alphabet.Length] + i;
#endif
		offset = (numbers.Length + offset) % _alphabet.Length;
		offset = (offset + increment) % _alphabet.Length;

		Span<char> alphabetTemp = _alphabet.Length * sizeof(char) > MaxStackallocSize // NOTE: We multiply the number of characters by the size of a `char` to get the actual amount of memory that would be allocated.
			? new char[_alphabet.Length]
			: stackalloc char[_alphabet.Length];
		var alphabetSpan = _alphabet.AsSpan();
		alphabetSpan[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabetSpan[..offset].CopyTo(alphabetTemp[^offset..]);

		char prefix = alphabetTemp[0];
		alphabetTemp.Reverse();

		var builder = StringBuilderPool.Instance.Rent();
		try
		{
			builder.Append(prefix);

			for (int i = 0; i < numbers.Length; i++)
			{
				var number = numbers[i];
				var alphabetWithoutSeparator = alphabetTemp[1..]; // NOTE: Excludes the first character — which is the separator
				var tempBuffer = ArrayPool<char>.Shared.Rent(256);

				try
				{
					var encodedNumber = ToId(number, alphabetWithoutSeparator, tempBuffer);
					builder.Append(encodedNumber);

					if (i >= numbers.Length - 1) // NOTE: If the last one
						continue;

					char separator = alphabetTemp[0];
					builder.Append(separator);
					ConsistentShuffle(alphabetTemp);
				}
				finally
				{
					ArrayPool<char>.Shared.Return(tempBuffer);
				}
			}

			if (builder.Length < _minLength)
			{
				char separator = alphabetTemp[0];
				builder.Append(separator);

				while (builder.Length < _minLength)
				{
					ConsistentShuffle(alphabetTemp);
					int toIndex = Math.Min(_minLength - builder.Length, _alphabet.Length);
					builder.Append(alphabetTemp[..toIndex]);
				}
			}

			string result = builder.ToString();

			if (IsBlockedId(result.AsSpan()))
				result = Encode(numbers, increment + 1);

			return result;
		}
		finally
		{
			StringBuilderPool.Instance.Return(builder);
		}
	}

	/// <summary>
	/// Decodes an ID into numbers.
	/// </summary>
	/// <param name="id">The encoded ID.</param>
	/// <returns>
	/// An array containing the decoded number(s) (it would contain only one element
	/// if the ID represents a single number); or an empty array if the input ID is null,
	/// empty, or includes characters not found in the alphabet.
	/// </returns>
#if NET7_0_OR_GREATER
	public IReadOnlyList<T> Decode(ReadOnlySpan<char> id)
#else
	public IReadOnlyList<int> Decode(ReadOnlySpan<char> id)
#endif
	{
		if (id.IsEmpty)
#if NET7_0_OR_GREATER
			return Array.Empty<T>();
#else
			return Array.Empty<int>();
#endif

#if NET8_0_OR_GREATER
		foreach (var c in id)
		{
			if (!_alphabetValues.Contains(c))
				return [];
		}
#else
		foreach (char c in id)
			if (!_alphabet.Contains(c))
				return [];
#endif

		var alphabetSpan = _alphabet.AsSpan();

		char prefix = id[0];
		int offset = alphabetSpan.IndexOf(prefix);

		Span<char> alphabetTemp = _alphabet.Length * sizeof(char) > MaxStackallocSize
			? new char[_alphabet.Length]
			: stackalloc char[_alphabet.Length];
		alphabetSpan[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabetSpan[..offset].CopyTo(alphabetTemp[^offset..]);

		alphabetTemp.Reverse();

		id = id[1..]; // NOTE: Exclude the prefix

#if NET7_0_OR_GREATER
		var result = new List<T>();
#else
		var result = new List<int>();
#endif
		while (!id.IsEmpty)
		{
			char separator = alphabetTemp[0];

			var separatorIndex = id.IndexOf(separator);
			var chunk = separatorIndex == -1 ? id : id[..separatorIndex]; // NOTE: The first part of `id` (every thing to the left of the separator) represents the number that we ought to decode.
			id = separatorIndex == -1 ? default : id[(separatorIndex + 1)..]; // NOTE: Everything to the right of the separator will be `id` for the next iteration

			if (chunk.IsEmpty)
				return result;

			var alphabetWithoutSeparator = alphabetTemp[1..]; // NOTE: Exclude the first character — which is the separator
			var decodedNumber = ToNumber(chunk, alphabetWithoutSeparator);
			result.Add(decodedNumber);

			if (!id.IsEmpty)
				ConsistentShuffle(alphabetTemp);
		}

		return result;
	}

	// NOTE: Implicit `string` => `Span<char>` conversion was introduced in .NET Standard 2.1 (see https://learn.microsoft.com/en-us/dotnet/api/system.string.op_implicit), which means without this overload, calling `Decode` with a string on versions older than .NET Standard 2.1 would require calling `.AsSpan()` on the string, which is cringe.
#if NETSTANDARD2_0
	/// <summary>
	/// Decodes an ID into numbers.
	/// </summary>
	/// <param name="id">The encoded ID.</param>
	/// <returns>
	/// An array containing the decoded number(s) (it would contain only one element
	/// if the ID represents a single number); or an empty array if the input ID is null,
	/// empty, or includes characters not found in the alphabet.
	/// </returns>
	public IReadOnlyList<int> Decode(string id) => Decode(id.AsSpan());
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

			if (ContainsDigit(word.AsSpan()) &&
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
	private static ReadOnlySpan<char> ToId(T num, ReadOnlySpan<char> alphabet, Span<char> buffer)
#else
	private static ReadOnlySpan<char> ToId(int num, ReadOnlySpan<char> alphabet, Span<char> buffer)
#endif
	{
		var baseLength = alphabet.Length;

		var result = num;
		var index = buffer.Length;

#if NET7_0_OR_GREATER
		do
		{
			index--;
			buffer[index] = alphabet[int.CreateChecked(result % T.CreateChecked(baseLength))];
			result /= T.CreateChecked(baseLength);
		} while (result > T.Zero);
#else
		do
		{
			index--;
			buffer[index] = alphabet[result % baseLength];
			result /= baseLength;
		} while (result > 0);
#endif
		return buffer.Slice(index, buffer.Length - index);
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

	private bool ContainsDigit(ReadOnlySpan<char> value)
	{
#if NET8_0_OR_GREATER
		return value.ContainsAny(_digitValues);
#else
		for (var i = 0; i < value.Length; i++)
		{
			if (char.IsDigit(value[i]))
				return true;
		}

		return false;
#endif
	}
}
