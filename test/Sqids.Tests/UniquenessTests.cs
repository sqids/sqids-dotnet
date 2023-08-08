namespace Sqids.Tests;

public class UniquenessTests
{
	[TestCase(0, 1)] // NOTE: Low ranges
	[TestCase(100_000_000, 1)] // NOTE: High ranges
	[TestCase(0, 5)] // NOTE: Multiple numbers
	[TestCase(0, 1, true)] // NOTE: With maximum padding (i.e. min length)
	public void EncodeAndDecode_LargeRange_ReturnsUniqueIdsAndRoundTripsSuccessfully(
		int startingPoint,
		int numbersCount,
		bool maxPadding = false
	)
	{
		const int range = 1_000_000; // NOTE: We encode/decode one million numbers from `startingPoint`.

		var sqids = new SqidsEncoder(new()
		{
			MinLength = maxPadding ? new SqidsOptions().Alphabet.Length : 0,
		});

		var hashSet = new HashSet<string>();

		for (int i = startingPoint; i < startingPoint + range; i++)
		{
			var numbers = Enumerable.Repeat(i, numbersCount).ToArray();
			var id = sqids.Encode(numbers);
			hashSet.Add(id);
			sqids.Decode(id).ShouldBeEquivalentTo(numbers);
		}

		hashSet.Count.ShouldBe(range); // NOTE: Ensures that all the IDs were unique.
	}
}
