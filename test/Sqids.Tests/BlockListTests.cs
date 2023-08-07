namespace Sqids.Tests;

public class BlockListTests
{
	[Fact]
	public void DefaultBlockListIfNoneSet()
	{
		var sqids = new SqidsEncoder();

		sqids.Decode("sexy").Should().BeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).Should().Be("d171vI");
	}

	[Fact]
	public void NoBlockListIfReset()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new(),
		});

		sqids.Decode("sexy").Should().BeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).Should().Be("sexy");
	}

	[Fact]
	public void OnlyCustomBlockListIfSet()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"AvTg" // Originally encoded [100000]
			},
		});

		// Make sure the default blocklist isn't used
		sqids.Decode("sexy").Should().BeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).Should().Be("sexy");

		// Make sure the passed blocklist IS used:
		sqids.Decode("AvTg").Should().BeEquivalentTo(new[] { 100000 });
		sqids.Encode(100000).Should().Be("7T1X8k");
		sqids.Decode("7T1X8k").Should().BeEquivalentTo(new[] { 100000 });
	}

	[Fact]
	public void CustomBlockList()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"8QRLaD", // normal result of 1st encoding, let's block that word on purpose
				"7T1cd0dL", // result of 2nd encoding
				"UeIe", // result of 3rd encoding is `RA8UeIe7`, let's block a substring
				"imhw", // result of 4th encoding is `WM3Limhw`, let's block the postfix
				"LfUQ", // result of 4th encoding is `LfUQh4HN`, let's block the prefix
			},
		});

		sqids.Encode(1, 2, 3).Should().Be("TM0x1Mxz");
		sqids.Decode("TM0x1Mxz").Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}

	[Fact]
	public void DecodingBlockedWordsShouldWork()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"8QRLaD",
				"7T1cd0dL",
				"RA8UeIe7",
				"WM3Limhw",
				"LfUQh4HN",
			},
		});

		sqids.Decode("8QRLaD").Should().BeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("7T1cd0dL").Should().BeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("RA8UeIe7").Should().BeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("WM3Limhw").Should().BeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("LfUQh4HN").Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}

	[Fact]
	public void MatchAgainstShortBlockListWord()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"pPQ",
			},
		});

		sqids.Decode(sqids.Encode(1000)).Should().BeEquivalentTo(new[] { 1000 });
	}
}
