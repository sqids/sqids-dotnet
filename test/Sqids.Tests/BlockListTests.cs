namespace Sqids.Tests;

public class BlockListTests
{
	[Test]
	public void EncodeAndDecode_WithDefaultBlockList_BlocksWordsInDefaultBlockList()
	{
		var sqids = new SqidsEncoder();

		sqids.Decode("sexy").ShouldBeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).ShouldBe("d171vI");
	}

	[Test]
	public void EncodeAndDecode_WithEmptyBlockList_DoesNotBlockWords()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new(),
		});

		sqids.Decode("sexy").ShouldBeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).ShouldBe("sexy");
	}

	[Test]
	public void EncodeAndDecode_WithCustomBlockList_OnlyBlocksWordsInCustomBlockList()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"AvTg" // NOTE: The default encoding of 100000.
			},
		});

		// NOTE: Make sure the default blocklist isn't used
		sqids.Decode("sexy").ShouldBeEquivalentTo(new[] { 200044 });
		sqids.Encode(200044).ShouldBe("sexy");

		// NOTE: Make sure the passed blocklist IS used:
		sqids.Decode("AvTg").ShouldBeEquivalentTo(new[] { 100000 });
		sqids.Encode(100000).ShouldBe("7T1X8k");
		sqids.Decode("7T1X8k").ShouldBeEquivalentTo(new[] { 100000 });
	}

	[Test]
	public void EncodeAndDecode_WithBlockListBlockingMultipleEncodings_RespectsBlockList()
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

		sqids.Encode(1, 2, 3).ShouldBe("TM0x1Mxz");
		sqids.Decode("TM0x1Mxz").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
	}

	[Test]
	public void Decode_BlockedIds_StillDecodesSuccessfully()
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

		sqids.Decode("8QRLaD").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("7T1cd0dL").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("RA8UeIe7").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("WM3Limhw").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
		sqids.Decode("LfUQh4HN").ShouldBeEquivalentTo(new[] { 1, 2, 3 });
	}

	[Test]
	public void EncodeAndDecode_WithShortCustomBlockList_RoundTripsSuccessfully()
	{
		var sqids = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				"pPQ", // NOTE: This is the default encoding of `1000`.
			},
		});

		sqids.Decode(sqids.Encode(1000)).ShouldBeEquivalentTo(new[] { 1000 });
	}
}
