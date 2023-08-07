namespace Sqids.Tests;

public class EncodingTests
{
	[Fact]
	public void Simple()
	{
		var sqids = new SqidsEncoder();

		var numbers = new[] { 1, 2, 3 };
		var id = "8QRLaD";

		sqids.Encode(numbers).Should().Be(id);
		sqids.Decode(id).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void DifferentInputs()
	{
		var sqids = new SqidsEncoder();

		var numbers = new[] { 0, 0, 0, 1, 2, 3, 100, 1_000, 100_000, 1_000_000, SqidsEncoder.MaxValue };

		sqids.Decode(sqids.Encode(numbers)).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void IncrementalNumbers()
	{
		var sqids = new SqidsEncoder();

		var idsAndNumbers = new Dictionary<string, int>()
		{
			["bV"] = 0,
			["U9"] = 1,
			["g8"] = 2,
			["Ez"] = 3,
			["V8"] = 4,
			["ul"] = 5,
			["O3"] = 6,
			["AF"] = 7,
			["ph"] = 8,
			["n8"] = 9,
		};

		foreach (var (id, number) in idsAndNumbers)
		{
			sqids.Encode(number).Should().Be(id);
			sqids.Decode(id).Should().BeEquivalentTo(new[] { number });
		}
	}

	[Fact]
	public void IncrementalNumbersSameIndex0()
	{
		var sqids = new SqidsEncoder();

		var idsAndNumbers = new Dictionary<string, int[]>()
		{
			["SrIu"] = new[] { 0, 0 },
			["nZqE"] = new[] { 0, 1 },
			["tJyf"] = new[] { 0, 2 },
			["e86S"] = new[] { 0, 3 },
			["rtC7"] = new[] { 0, 4 },
			["sQ8R"] = new[] { 0, 5 },
			["uz2n"] = new[] { 0, 6 },
			["7Td9"] = new[] { 0, 7 },
			["3nWE"] = new[] { 0, 8 },
			["mIxM"] = new[] { 0, 9 },
		};

		foreach (var (id, numbers) in idsAndNumbers)
		{
			sqids.Encode(numbers).Should().Be(id);
			sqids.Decode(id).Should().BeEquivalentTo(numbers);
		}
	}

	[Fact]
	public void IncrementalNumbersSameIndex1()
	{
		var sqids = new SqidsEncoder();

		var idsAndNumbers = new Dictionary<string, int[]>()
		{
			["SrIu"] = new[] { 0, 0 },
			["nbqh"] = new[] { 1, 0 },
			["t4yj"] = new[] { 2, 0 },
			["eQ6L"] = new[] { 3, 0 },
			["r4Cc"] = new[] { 4, 0 },
			["sL82"] = new[] { 5, 0 },
			["uo2f"] = new[] { 6, 0 },
			["7Zdq"] = new[] { 7, 0 },
			["36Wf"] = new[] { 8, 0 },
			["m4xT"] = new[] { 9, 0 },
		};

		foreach (var (id, numbers) in idsAndNumbers)
		{
			sqids.Encode(numbers).Should().Be(id);
			sqids.Decode(id).Should().BeEquivalentTo(numbers);
		}
	}

	[Fact]
	public void MultiInput()
	{
		var sqids = new SqidsEncoder();

		var numbers = new[]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,
			26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
			50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73,
			74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97,
			98, 99
		};

		sqids.Decode(sqids.Encode(numbers)).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void EncodingNoNumbers()
	{
		var sqids = new SqidsEncoder();
		sqids.Encode(new int[] { }).Should().Be(string.Empty);
	}

	[Fact]
	public void DecodingEmptyString()
	{
		var sqids = new SqidsEncoder();
		sqids.Decode(string.Empty).Should().HaveCount(0);
	}

	[Fact]
	public void DecodingIdWithInvalidCharacters()
	{
		var sqids = new SqidsEncoder();
		sqids.Decode("*").Should().HaveCount(0);
	}

	[Fact]
	public void EncodeOutOfRangeNumbers()
	{
		var sqids = new SqidsEncoder();

		var act = () => sqids.Encode(SqidsEncoder.MinValue - 1);
		act.Should().Throw<ArgumentOutOfRangeException>();
		// We don't check for `MaxValue + 1` because that's a compile time error anyway
	}
}
