namespace Sqids.Tests;

public class AlphabetTests
{
	[Fact]
	public void Simple()
	{
		var sqids = new SqidsEncoder(new()
		{
			Alphabet = "0123456789abcdef",
		});

		var numbers = new[] { 1, 2, 3 };
		var id = "4d9fd2";

		sqids.Encode(numbers).Should().Be(id);
		sqids.Decode(id).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void ShortAlphabet()
	{
		var sqids = new SqidsEncoder(new()
		{
			Alphabet = "abcde",
		});

		var numbers = new[] { 1, 2, 3 };

		sqids.Decode(sqids.Encode(numbers)).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void LongAlphabet()
	{
		var sqids = new SqidsEncoder(new()
		{
			Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+|{}[];:'\"/?.>,<`~",
		});

		var numbers = new[] { 1, 2, 3 };

		sqids.Decode(sqids.Encode(numbers)).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void RepeatedCharactersInAlphabet()
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = "aabcdefg",
		});
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void TooShortOfAnAlphabet()
	{
		var act = () => new SqidsEncoder(new()
		{
			Alphabet = "abcd",
		});
		act.Should().Throw<ArgumentException>();
	}
}
