namespace Sqids;

/// <summary>
/// The Sqids encoder/decoder. This is the main class.
/// </summary>
public sealed class SqidsEncoder
{
	private const int MinAlphabetLength = 5;
	private const int MaxStackallocSize = 256; // NOTE: In bytes — this value is essentially arbitrary, the Microsoft docs is using 1024 but recommends being more conservative when choosing the value (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc), Hashids apparently uses 512 (https://github.com/ullmark/hashids.net/blob/9b1c69de4eedddf9d352c96117d8122af202e90f/src/Hashids.net/Hashids.cs#L17), and this article (https://vcsjones.dev/stackalloc/) uses 256. I've tried to be pretty cautious and gone with a low value.

	private readonly char[] _alphabet;
	private readonly int _minLength;
	private readonly string[] _blockList;

	/// <summary>
	/// The minimum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
	/// This is always zero across all ports of Sqids.
	/// </summary>
	public const int MinValue = 0;

	/// <summary>
	/// The maximum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
	/// It's equal to `int.MaxValue`.
	/// </summary>
	public const int MaxValue = int.MaxValue;

	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder" /> with the default options.
	/// </summary>
	public SqidsEncoder() : this(new()) { }

	/// <summary>
	/// Initializes a new instance of <see cref="SqidsEncoder" /> with custom options.
	/// </summary>
	/// <param name="options">
	/// The custom options.
	/// All properties of <see cref="SqidsOptions" /> are optional and will fall back to their
	/// defaults if not explicitly set.
	/// </param>
	public SqidsEncoder(SqidsOptions options)
	{
		if (options.Alphabet.Length < MinAlphabetLength)
			throw new ArgumentException("The alphabet must contain at least 5 characters.");

		if (options.Alphabet.Distinct().Count() != options.Alphabet.Length)
			throw new ArgumentException("The alphabet must not contain duplicate characters.");

		if (options.MinLength < MinValue || options.MinLength > options.Alphabet.Length)
			throw new ArgumentException($"The minimum length must be between {MinValue} and {options.Alphabet.Length}.");

		_minLength = options.MinLength;

		// NOTE: Cleanup the blocklist:
		options.BlockList = new HashSet<string>(
			options.BlockList,
			StringComparer.OrdinalIgnoreCase // NOTE: Effectively removes items that differ only in casing — leaves one version of each word casing-wise which will then be compared against the generated IDs case-insensitively
		);
		options.BlockList.RemoveWhere(w =>
			w.Length < 3 || // NOTE: Removes words that are less than 3 characters long
			w.Any(c => !options.Alphabet.Contains(c)) // NOTE: Removes words that contain characters not found in the alphabet
		);
		_blockList = options.BlockList.ToArray(); // NOTE: Arrays are faster to iterate than HashSets, so we construct an array here.

		Span<char> shuffledAlphabet = options.Alphabet.Length * sizeof(char) > MaxStackallocSize // NOTE: We multiply the number of characters by the size of a `char` to get the actual amount of memory that would be allocated.
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
	public string Encode(int number)
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
	public string Encode(params int[] numbers)
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
	public string Encode(IEnumerable<int> numbers) =>
		Encode(numbers.ToArray());

	// TODO: Consider using `ArrayPool` if possible
	private string Encode(ReadOnlySpan<int> numbers, bool partitioned = false)
	{
		int offset = 0;
		for (int i = 0; i < numbers.Length; i++)
			offset += _alphabet[numbers[i] % _alphabet.Length] + i;
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
			int number = numbers[i];

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
				Span<int> newNumbers = (numbers.Length + 1) * sizeof(int) > MaxStackallocSize
					? new int[numbers.Length + 1]
					: stackalloc int[numbers.Length + 1];
				newNumbers[0] = 0;
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
			Span<int> newNumbers = numbers.Length * sizeof(int) > MaxStackallocSize
				? new int[numbers.Length]
				: stackalloc int[numbers.Length];
			numbers.CopyTo(newNumbers);

			if (partitioned)
			{
				if (numbers[0] + 1 > MaxValue)
					throw new OverflowException("Ran out of range checking against the blocklist.");
				else
					newNumbers[0] += 1;
			}
			else
			{
				newNumbers = (numbers.Length + 1) * sizeof(int) > MaxStackallocSize
					? new int[numbers.Length + 1]
					: stackalloc int[numbers.Length + 1];
				newNumbers[0] = 0;
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
	public int[] Decode(ReadOnlySpan<char> id)
	{
		if (id.IsEmpty)
			return Array.Empty<int>();

		foreach (char c in id)
			if (!_alphabet.Contains(c))
				return Array.Empty<int>();

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

		var result = new List<int>();

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
					return Array.Empty<int>();

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

	private static ReadOnlySpan<char> ToId(int num, ReadOnlySpan<char> alphabet)
	{
		var id = new StringBuilder();
		int result = num;

		do
		{
			id.Insert(0, alphabet[result % alphabet.Length]);
			result = result / alphabet.Length;
		} while (result > 0);

		return id.ToString().AsSpan(); // TODO: possibly avoid creating a string
	}

	private static int ToNumber(ReadOnlySpan<char> id, ReadOnlySpan<char> alphabet)
	{
		int result = 0;
		foreach (var character in id)
			result = result * alphabet.Length + alphabet.IndexOf(character);
		return result;
	}
}
