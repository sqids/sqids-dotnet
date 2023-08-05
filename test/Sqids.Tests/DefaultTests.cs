namespace Sqids.Tests;

public class DefaultTests
{
	private readonly SqidsEncoder _encoder = new(); // todo: here or in the methods?

	[Theory]
	[InlineData(1, "U9")]
	[InlineData(2, "g8")]
	[InlineData(3, "Ez")]
	[InlineData(100, "8Qd")]
	[InlineData(1_000, "pPQ")]
	[InlineData(1_000_000, "gA3wp")]
	[InlineData(123_456, "HvNP")]
	[InlineData(32_129, "2mCX")]
	[InlineData(45_923, "tZnR")]
	[InlineData(SqidsEncoder.MinValue, "bV")]
	[InlineData(SqidsEncoder.MaxValue, "UwFNcZQ")]
	public void Encode_SingleNumber_ReturnsRightId(int number, string expected)
	{
		var encoded = _encoder.Encode(number);
		encoded.Should().Be(expected);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, "8QRLaD")]
	[InlineData(new[] { 0, 1 }, "nZqE")]
	[InlineData(new[] { 0, 2 }, "tJyf")]
	[InlineData(new[] { 1, 0 }, "nbqh")]
	[InlineData(new[] { 2, 0 }, "t4yj")]
	[InlineData(new[] { 2, 34, 123, 123, 453 }, "4TOvEVpJMglJ3")]
	[InlineData(new[] { 23, 129, 1892, 83, 9, 0, 12, 38, 9 }, "H2VQqalFJ6kwtUkUK242L")]
	public void Encode_MultipleNumbers_ReturnsRightId(int[] numbers, string expected)
	{
		var encoded = _encoder.Encode(numbers);
		encoded.Should().Be(expected);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-145)]
	[InlineData(int.MinValue)]
	public void Encode_NegativeNumber_Throws(int input)
	{
		var encoding = () => _encoder.Encode(input);
		encoding.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Encode_EmptyArray_ReturnsEmptyString()
	{
		var encoded = _encoder.Encode(new int[] { });
		encoded.Should().Be(string.Empty);
	}

	[Theory]
	[InlineData("U9", 1)]
	[InlineData("g8", 2)]
	[InlineData("Ez", 3)]
	[InlineData("8Qd", 100)]
	[InlineData("pPQ", 1_000)]
	[InlineData("gA3wp", 1_000_000)]
	[InlineData("HvNP", 123_456)]
	[InlineData("2mCX", 32_129)]
	[InlineData("tZnR", 45_923)]
	[InlineData("bV", SqidsEncoder.MinValue)]
	[InlineData("UwFNcZQ", SqidsEncoder.MaxValue)]
	// todo: a few [InlineData]s that are not also in the encoding test?
	public void Decode_SingleNumberId_ReturnsRightNumber(string id, int expected)
	{
		var decoded = _encoder.Decode(id);
		decoded.Should().BeEquivalentTo(new[] { expected });
	}

	[Theory]
	[InlineData("8QRLaD", new[] { 1, 2, 3 })]
	[InlineData("nZqE", new[] { 0, 1 })]
	[InlineData("tJyf", new[] { 0, 2 })]
	[InlineData("nbqh", new[] { 1, 0 })]
	[InlineData("t4yj", new[] { 2, 0 })]
	[InlineData("4TOvEVpJMglJ3", new[] { 2, 34, 123, 123, 453 })]
	[InlineData("H2VQqalFJ6kwtUkUK242L", new[] { 23, 129, 1892, 83, 9, 0, 12, 38, 9 })]
	// todo: a few [InlineData]s that are not also in the encoding test?
	public void Decode_MultipleNumberId_ReturnsRightNumber(string id, int[] expected)
	{
		var decoded = _encoder.Decode(id);
		decoded.Should().BeEquivalentTo(expected);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void Decode_NullOrWhitespaceInput_ReturnsEmptyArray(string? id)
	{
		var decoded = _encoder.Decode(id);
		decoded.Should().BeEmpty();
	}

	// todo: too large numbers?
}
