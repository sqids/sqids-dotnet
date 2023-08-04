namespace Sqids;

// TODO: the suffix `Encoder` may be more accurate
public class SqidsGenerator
{
	private const int _minAlphabetLength = 5;
	private readonly SqidsOptions _options;

	public const int MinValue = 0;
	public const int MaxValue = int.MaxValue;

	public SqidsGenerator() : this(new()) { }

	public SqidsGenerator(SqidsOptions options)
	{
		if (options.Alphabet.Length < _minAlphabetLength)
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

	public string Encode(params int[] numbers)
	{
		if (numbers.Length == 0)
			return string.Empty;

		if (numbers.Any(n => n < MinValue || n > MaxValue))
			throw new ArgumentOutOfRangeException($"Encoding supports numbers between '{MinValue}' and '{MaxValue}'.");

		return EncodeNumbers(numbers);
	}

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

			if (!chunk.IsEmpty)
			{
				var alphabetWithoutSeparator = alphabetTemp[..^1]; // NOTE: Exclude the last character from the alphabet (which is the separator)
				var decodedNumber = ToNumber(chunk, alphabetWithoutSeparator);
				result.Add(decodedNumber);

				if (!id.IsEmpty)
					ConsistentShuffle(alphabetTemp);
			}
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

			if (i < numbers.Length - 1) // NOTE: If not the last
			{
				char separator = alphabetTemp[^1]; // NOTE: Exclude the last character

				builder.Append(
					partitioned && i == 0
						? partition
						: separator
				);

				ConsistentShuffle(alphabetTemp);
			}
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
				var LeftToMeetMinLength = _options.MinLength - result.Length;
				var paddingFromAlphabet = alphabetTemp[..LeftToMeetMinLength];
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
					throw new Exception("Ran out of range checking against the blocklist.");
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

			if ((id.Length <= 3 || word.Length <= 3) && id.Equals(word, StringComparison.OrdinalIgnoreCase))
				return true;
			if (word.Any(char.IsDigit) && (id.StartsWith(word, StringComparison.OrdinalIgnoreCase) || id.EndsWith(word, StringComparison.OrdinalIgnoreCase)))
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
		for (int i = 0; i < id.Length; i++)
		{
			char character = id[i];
			result = result * alphabet.Length + alphabet.IndexOf(character);
		}
		return result;
	}
}
