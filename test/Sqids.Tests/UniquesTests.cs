namespace Sqids.Tests;

public class UniquesTests
{
	private const int Upper = 1_000_000;

	[Fact]
	public void UniquesWithPadding()
	{
		var sqids = new SqidsEncoder(new()
		{
			MinLength = new SqidsOptions().Alphabet.Length,
		});

		var hashSet = new HashSet<string>();

		for (int i = 0; i < Upper; i++)
		{
			var id = sqids.Encode(i);
			hashSet.Add(id);
			sqids.Decode(id).Should().BeEquivalentTo(new[] { i });
		}

		hashSet.Count.Should().Be(Upper);
	}

	[Fact]
	public void UniquesLowRanges()
	{
		var sqids = new SqidsEncoder();

		var hashSet = new HashSet<string>();

		for (int i = 0; i < Upper; i++)
		{
			var id = sqids.Encode(i);
			hashSet.Add(id);
			sqids.Decode(id).Should().BeEquivalentTo(new[] { i });
		}

		hashSet.Count.Should().Be(Upper);
	}

	[Fact]
	public void UniquesHighRanges()
	{
		var sqids = new SqidsEncoder();

		var hashSet = new HashSet<string>();

		for (int i = 100_000_000; i < 100_000_000 + Upper; i++)
		{
			var id = sqids.Encode(i);
			hashSet.Add(id);
			sqids.Decode(id).Should().BeEquivalentTo(new[] { i });
		}

		hashSet.Count.Should().Be(Upper);
	}

	[Fact]
	public void UniquesMulti()
	{
		var sqids = new SqidsEncoder();

		var hashSet = new HashSet<string>();

		for (int i = 0; i < Upper; i++)
		{
			var numbers = new[] { i, i, i, i, i };
			var id = sqids.Encode(numbers);
			hashSet.Add(id);
			sqids.Decode(id).Should().BeEquivalentTo(numbers);
		}

		hashSet.Count.Should().Be(Upper);
	}
}
