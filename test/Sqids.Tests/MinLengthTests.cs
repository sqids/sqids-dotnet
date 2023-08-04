namespace Sqids.Tests;

public class MinLengthTests
{
	[Theory]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void Constructor_MinLengthTooSmall_Throws(int minLength)
	{
		var act = () => new SqidsEncoder(new()
		{
			MinLength = minLength,
		});
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("abcde")]
	[InlineData("0123456789")]
	public void Constructor_MinLengthBiggerThanAlphabetLength_Throws(string alphabet)
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = alphabet,
			MinLength = alphabet.Length + 1,
		});
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("abcde")]
	[InlineData("0123456789")]
	public void Constructor_ValidMinLength_DoesNotThrow(string alphabet)
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = alphabet,
			MinLength = alphabet.Length / 2,
		});
		act.Should().NotThrow<ArgumentException>();
	}

	[Theory]
	[InlineData(5, new[] { 0 }, "SurCu")]
	[InlineData(7, new[] { 1 }, "nhERZME")]
	[InlineData(10, new[] { 100 }, "4gMAwl7dM0")]
	[InlineData(23, new[] { 250 }, "tjMfYDh9xIqbXnswW8TJaDw")]
	[InlineData(50, new[] { 1, 2, 3 }, "75JILToVsGerOADWmHlY38xvbaNZKQ9wdFS0B6kcMEtT1cd0dL")]
	public void Encode_WithCustomMinLength_ReturnsRightId(
		int minLength,
		int[] numbers,
		string expected
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			MinLength = minLength,
		});
		var encoded = encoder.Encode(numbers);
		encoded.Should().Be(expected);
	}

	[Theory]
	[InlineData(5, "SurCu", new[] { SqidsEncoder.MinValue })]
	[InlineData(7, "nhERZME", new[] { 1 })]
	[InlineData(10, "4gMAwl7dM0", new[] { 100 })]
	[InlineData(23, "75JILToVsGerOADWmHlY38xvbaNZKQ9wdFS0B6kcMEtT1cd0dL", new[] { 1, 2, 3 })]
	public void Decode_WithCustomMinLength_ReturnsRightNumber(
		int minLength,
		string id,
		int[] expected
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			MinLength = minLength,
		});
		var decoded = encoder.Decode(id);
		decoded.Should().BeEquivalentTo(expected);
	}

	[Theory]
	[InlineData(5, "SurC")]
	[InlineData(10, "4gMAwl7dM")]
	[InlineData(23, "tjMfYDh9xIqbXnswW8")]
	public void Decode_BelowMinLength_ReturnsEmptyArray(
		int minLength,
		string id
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			MinLength = minLength,
		});
		var decoded = encoder.Decode(id);
		decoded.Should().BeEmpty();
	}
}
