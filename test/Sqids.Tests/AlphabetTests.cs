namespace Sqids.Tests;

public class AlphabetTests
{
	[TestCase("0123456789abcdef", new[] { 1, 2, 3 }, "4d9fd2")]
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

	[TestCase("abcde", new[] { 1, 2, 3 })] // NOTE: Short alphabet
	[TestCase("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+|{}[];:'\"/?.>,<`~", new[] { 1, 2, 3 })] // NOTE: Long short
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
	[TestCase("abcd")] // NOTE: Too short
	public void Instantiate_WithInvalidAlphabet_Throws(string invalidAlphabet)
	{
#if NET7_0_OR_GREATER
		var act = () => new SqidsEncoder<int>(new() { Alphabet = invalidAlphabet });
#else
		var act = () => new SqidsEncoder(new() { Alphabet = invalidAlphabet });
#endif
		act.ShouldThrow<ArgumentException>();
	}
}
