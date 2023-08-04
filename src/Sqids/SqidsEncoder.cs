namespace Sqids;

/// <summary>
/// The Sqids encoder/decoder.
/// </summary>
public class SqidsEncoder
{
	private const int MinAlphabetLength = 5;

	private readonly SqidsOptions _options;

	/// <summary>
	/// The minimum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
	/// It's always zero across all ports of Sqids.
	/// </summary>
	public const int MinValue = 0;

	/// <summary>
	/// The minimum numeric value that can be encoded/decoded using <see cref="SqidsEncoder" />.
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
	/// defaults unless explicitly set.
	/// </param>
	public SqidsEncoder(SqidsOptions options)
	{
		if (options.Alphabet.Length < MinAlphabetLength)
			throw new ArgumentException("The alphabet must contain at least 5 characters.");

		if (options.Alphabet.Distinct().Count() != options.Alphabet.Length)
			throw new ArgumentException("The alphabet must not contain duplicate characters.");

		if (options.MinLength < MinValue || options.MinLength > options.Alphabet.Length) // TODO: Why should `MinLength` not be greater than the length of the alphabet? The letter can be repeated in the generated IDs.
			throw new ArgumentException($"The minimum length must be between {MinValue} and {options.Alphabet.Length}.");

		// NOTE: Cleanup the blocklist:
		options.BlockList = new HashSet<string>(
			options.BlockList,
			StringComparer.OrdinalIgnoreCase // NOTE: Effectively removes items that differ only in casing — leaves one version of each word casing-wise which will then be compared against the generated IDs case-insensitively
		);
		options.BlockList.RemoveWhere(w =>
			w.Length < 3 || // NOTE: Removes words that are less than 3 characters long
			w.Any(c => !options.Alphabet.Contains(c)) // NOTE: Removes words that contain characters not found in the alphabet
		);

		Span<char> shuffledAlphabet = stackalloc char[options.Alphabet.Length];
		options.Alphabet.AsSpan().CopyTo(shuffledAlphabet);
		ConsistentShuffle(shuffledAlphabet);
		options.Alphabet = shuffledAlphabet.ToString();

		_options = options;
	}

	/// <summary>
	/// Encodes one or more integers into a Sqids ID.
	/// </summary>
	/// <param name="numbers">The array of integers to encode.</param>
	/// <returns>A string containing the encoded ID(s), or an empty string if the array passed is empty.</returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">If any of the integers passed is smaller than <see cref="MinValue"/> (i.e. negative) or greater than <see cref="MaxValue"/> (i.e. `int.MaxValue`).</exception>
	/// <exception cref="T:System.OverflowException">If the decoded number overflows integer.</exception>
	public string Encode(params int[] numbers)
	{
		if (numbers.Length == 0)
			return string.Empty;

		if (numbers.Any(n => n < MinValue || n > MaxValue))
			throw new ArgumentOutOfRangeException($"Encoding supports numbers between '{MinValue}' and '{MaxValue}'.");

		return EncodeNumbers(numbers);
	}

	/// <summary>
	/// Decodes a string into its original .
	/// </summary>
	/// <param name="id">The encoded ID.</param>
	/// <returns>
	/// A collection of integers containing the decoded number(s); or empty an collection if the
	/// input string is null, empty, contains fewer characters than the configured minimum length,
	/// or includes characters not found in the alphabet.
	/// </returns>
	public IReadOnlyList<int> Decode(ReadOnlySpan<char> id)
	{
		if (id.IsEmpty)
			return Array.Empty<int>();

		if (id.Length < _options.MinLength) // TODO: This wasn't in the reference implementation — but it makes sense to me?
			return Array.Empty<int>();

		foreach (char c in id)
			if (!_options.Alphabet.Contains(c))
				return Array.Empty<int>();

		var alphabet = _options.Alphabet.AsSpan();

		char prefix = id[0];
		int offset = alphabet.IndexOf(prefix);

		Span<char> alphabetTemp = stackalloc char[_options.Alphabet.Length]; // TODO: check stack max size against alphabet.Length a la Hashids?
		alphabet[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabet[..offset].CopyTo(alphabetTemp[^offset..]);

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
			var chunk = separatorIndex == -1 ? id : id[..separatorIndex]; // NOTE: The first part of `id` to the left of the separator is the number that we ought to decode.
			id = id[chunk.Length..].TrimStart(separator); // NOTE: The `id` for the next iteration would exclude the current `chunk` (and also any following comma, if there is one)

			if (chunk.IsEmpty)
				continue;

			var alphabetWithoutSeparator = alphabetTemp[..^1]; // NOTE: Exclude the last character from the alphabet (which is the separator)
			var decodedNumber = ToNumber(chunk, alphabetWithoutSeparator);
			result.Add(decodedNumber);

			if (!id.IsEmpty)
				ConsistentShuffle(alphabetTemp);
		}

		return result;
	}

	// Private Helpers:
	private string EncodeNumbers(int[] numbers, bool partitioned = false)
	{
		var alphabet = _options.Alphabet.AsSpan();

		int offset = 0;
		for (int i = 0; i < numbers.Length; i++)
			offset += alphabet[numbers[i] % alphabet.Length] + i;
		offset = (numbers.Length + offset) % alphabet.Length;

		Span<char> alphabetTemp = stackalloc char[alphabet.Length]; // TODO: check stack max size against alphabet.Length a la Hashids?
		alphabet[offset..].CopyTo(alphabetTemp[..^offset]);
		alphabet[..offset].CopyTo(alphabetTemp[^offset..]);

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

		string result = builder.ToString(); // TODO: preferably don't `ToString()` this early

		if (result.Length < _options.MinLength)
		{
			if (!partitioned)
			{
				var newNumbers = new int[numbers.Length + 1];
				newNumbers[0] = 0;
				numbers.CopyTo(newNumbers, 1);
				result = EncodeNumbers(newNumbers, partitioned: true);
			}

			if (result.Length < _options.MinLength)
			{
				var leftToMeetMinLength = _options.MinLength - result.Length;
				var paddingFromAlphabet = alphabetTemp[..leftToMeetMinLength];
				builder.Insert(1, paddingFromAlphabet);
				result = builder.ToString();
			}
		}

		if (IsBlockedId(result))
		{
			int[] newNumbers = numbers;
			if (partitioned)
			{
				if (numbers[0] + 1 > MaxValue)
					throw new OverflowException("Ran out of range checking against the blocklist.");
				else
					newNumbers[0] += 1;
			}
			else
			{
				newNumbers = new int[numbers.Length + 1]; // TODO: heap allocation
				newNumbers[0] = 0;
				numbers.CopyTo(newNumbers, 1);
			}

			result = EncodeNumbers(newNumbers, true);
		}

		return result;
	}

	private bool IsBlockedId(ReadOnlySpan<char> id)
	{
		foreach (string word in _options.BlockList)
		{
			if (word.Length > id.Length)
				continue;

			if ((id.Length <= 3 || word.Length <= 3) &&
			    id.Equals(word, StringComparison.OrdinalIgnoreCase))
				return true;

			if (word.Any(char.IsDigit) &&
				(id.StartsWith(word, StringComparison.OrdinalIgnoreCase) ||
				 id.EndsWith(word, StringComparison.OrdinalIgnoreCase)))
				return true;
			if (id.Contains(word, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Shuffles a span of characters in place.
	/// The shuffle produces consistent results.
	/// </summary>
	private static void ConsistentShuffle(Span<char> alphabet)
	{
		for (int i = 0, j = alphabet.Length - 1; j > 0; i++, j--)
		{
			int r = (i * j + alphabet[i] + alphabet[j]) % alphabet.Length;
			(alphabet[i], alphabet[r]) = (alphabet[r], alphabet[i]);
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

		return id.ToString(); // TODO: possibly avoid creating a string
	}

	private static int ToNumber(ReadOnlySpan<char> id, ReadOnlySpan<char> alphabet)
	{
		int result = 0;
		foreach (var character in id)
			result = result * alphabet.Length + alphabet.IndexOf(character);
		return result;
	}
}
