namespace Sqids.Tests;

public class AlphabetTests
{
	[Theory]
	// NOTE: Too short
	[InlineData("")]
	[InlineData("a")]
	[InlineData("1234")]
	// NOTE: Repeated characters
	[InlineData("aabcdefg")]
	[InlineData("01234567789")]
	public void Constructor_InvalidAlphabet_Throws(string alphabet)
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = alphabet,
		});
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("abcedfghijklmnop")]
	[InlineData("0123456789")]
	[InlineData("0123456789abcedfghijklmnop!@#$%^&*()")]
	public void Constructor_ValidAlphabet_DoesNotThrow(string alphabet)
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = alphabet,
		});
		act.Should().NotThrow();
	}

	[Theory]
	[InlineData("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+|{}[];:\'\" /?.>,<`~", 420, "xP7")]
	[InlineData("ABCDEFGHIJKLMNOP", 120, "EHB")]
	[InlineData("abcde", 942, "dbbbcbcbbbc")]
	public void Encode_WithCustomAlphabet_ReturnsRightId(
		string alphabet,
		int number,
		string expected
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			Alphabet = alphabet,
		});
		var encoded = encoder.Encode(number);
		encoded.Should().Be(expected); // todo: only check the characters?
	}

	[Theory]
	[InlineData("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+|{}[];:\'\" /?.>,<`~", "xP7", 420)]
	[InlineData("ABCDEFGHIJKLMNOP", "EHB", 120)]
	[InlineData("abcde", "dbbbcbcbbbc", 942)]
	public void Decode_WithCustomAlphabet_ReturnsRightId(
		string alphabet,
		string id,
		int expected
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			Alphabet = alphabet,
		});
		var decoded = encoder.Decode(id);
		decoded.Should().BeEquivalentTo(new[] { expected });
	}

	[Theory]
	[InlineData("ABCDEFGHIJKLMNOP", "7dB")]
	[InlineData("abcde", "98731232")]
	public void Decode_InputWithCharactersNotInAlphabet_ReturnsRightId(
		string alphabet,
		string id
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			Alphabet = alphabet,
		});
		var decoded = encoder.Decode(id);
		decoded.Should().BeEmpty();
	}
}
