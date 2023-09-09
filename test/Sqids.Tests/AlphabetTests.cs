namespace Sqids.Tests;

public class AlphabetTests
{
	[TestCase("0123456789abcdef", new[] { 1, 2, 3 }, "489158")]
	public void EncodeAndDecode_WithCustomAlphabet_ReturnsExactMatch(
		string alphabet,
		int[] numbers,
		string id
	)
	{

#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new() { Alphabet = alphabet });
#else
		var sqids = new SqidsEncoder(new() { Alphabet = alphabet });
#endif

		sqids.Encode(numbers).ShouldBe(id);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}

	[TestCase("abc", new[] { 1, 2, 3 })] // NOTE: Shortest possible alphabet
	[TestCase("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+|{}[];:'\"/?.>,<`~", new[] { 1, 2, 3 })] // NOTE: Long alphabet
	public void EncodeAndDecode_WithCustomAlphabet_RoundTripsSuccessfully(
		string alphabet,
		int[] numbers
	)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new() { Alphabet = alphabet });
#else
		var sqids = new SqidsEncoder(new() { Alphabet = alphabet });
#endif


		sqids.Decode(sqids.Encode(numbers)).ShouldBe(numbers);
	}

	[TestCase("aabcdefg")] // NOTE: Repeated characters
	[TestCase("ab")] // NOTE: Too short
	[TestCase("ë1092")] // NOTE: Contains a multi-byte character
	public void Instantiate_WithInvalidAlphabet_Throws(string invalidAlphabet)
	{
#if NET7_0_OR_GREATER
		var act = () => new SqidsEncoder<int>(new() { Alphabet = invalidAlphabet });
#else
		var act = () => new SqidsEncoder(new() { Alphabet = invalidAlphabet });
#endif
		act.ShouldThrow<ArgumentOutOfRangeException>();
	}
}
