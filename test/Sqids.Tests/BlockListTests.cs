namespace Sqids.Tests;

public class BlockListTests
{
	[Test]
	public void EncodeAndDecode_WithDefaultBlockList_BlocksWordsInDefaultBlockList()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Decode("aho1e").ShouldBe(new[] { 4572721 });
		sqids.Encode(4572721).ShouldBe("JExTR");
	}

	[Test]
	public void EncodeAndDecode_WithEmptyBlockList_DoesNotBlockWords()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			BlockList = new(),
		});

		sqids.Decode("aho1e").ShouldBe(new[] { 4572721 });
		sqids.Encode(4572721).ShouldBe("aho1e");
	}

	[Test]
	public void EncodeAndDecode_WithCustomBlockList_OnlyBlocksWordsInCustomBlockList()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			BlockList = new()
			{
				"ArUO" // NOTE: The default encoding of 100000.
			},
		});

		// NOTE: Make sure the default blocklist isn't used
		sqids.Decode("aho1e").ShouldBe(new[] { 4572721 });
		sqids.Encode(4572721).ShouldBe("aho1e");

		// NOTE: Make sure the passed blocklist IS used:
		sqids.Decode("ArUO").ShouldBe(new[] { 100000 });
		sqids.Encode(100000).ShouldBe("QyG4");
		sqids.Decode("QyG4").ShouldBe(new[] { 100000 });
	}

	[Test]
	public void EncodeAndDecode_WithBlockListBlockingMultipleEncodings_RespectsBlockList()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			BlockList = new()
			{
				"JSwXFaosAN", // normal result of 1st encoding, let's block that word on purpose
				"OCjV9JK64o", // result of 2nd encoding
				"rBHf", // result of 3rd encoding is `4rBHfOiqd3`, let's block a substring
				"79SM", // result of 4th encoding is `dyhgw479SM`, let's block the postfix
				"7tE6", // result of 4th encoding is `7tE6jdAHLe`, let's block the prefix
			},
		});

		sqids.Encode(1_000_000, 2_000_000).ShouldBe("1aYeB7bRUt");
		sqids.Decode("1aYeB7bRUt").ShouldBe(new[] { 1_000_000, 2_000_000 });
	}

	[Test]
	public void Decode_BlockedIds_StillDecodesSuccessfully()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			BlockList = new()
			{
				"86Rf07",
				"se8ojk",
				"ARsz1p",
				"Q8AI49",
				"5sQRZO",
			},
		});

		sqids.Decode("86Rf07").ShouldBe(new[] { 1, 2, 3 });
		sqids.Decode("se8ojk").ShouldBe(new[] { 1, 2, 3 });
		sqids.Decode("ARsz1p").ShouldBe(new[] { 1, 2, 3 });
		sqids.Decode("Q8AI49").ShouldBe(new[] { 1, 2, 3 });
		sqids.Decode("5sQRZO").ShouldBe(new[] { 1, 2, 3 });
	}

	[Test]
	public void EncodeAndDecode_WithShortCustomBlockList_RoundTripsSuccessfully()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			BlockList = new()
			{
				"pnd", // NOTE: This is the default encoding of `1000` — and blocklist words with three characters are the shortest possible
			},
		});

		sqids.Decode(sqids.Encode(1000)).ShouldBe(new[] { 1000 });
	}

	[Test]
	public void EncodeAndDecode_WithLowerCaseBlockListAndUpperCaseAlphabet_IgnoresCasing()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
			BlockList = new()
			{
				"sxnzkl", // NOTE: The uppercase version of this is the default encoding of [1,2,3]
			},
		});

		sqids.Encode(1, 2, 3).ShouldBe("IBSHOZ"); // NOTE: Without the blocklist, would've been "SQNMPN".
		sqids.Decode("IBSHOZ").ShouldBe(new[] { 1, 2, 3 });
	}

	[Test]
	public void Encode_WithNumerousEncodingsBlocked_ThrowsExceptionForReachingMaxReEncodingAttempts()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			Alphabet = "abc",
			MinLength = 3,
			BlockList = new()
			{
				"cab",
				"abc",
				"bca",
			},
		});

		var act = () => sqids.Encode(0);
		act.ShouldThrow<ArgumentException>(); // TODO: It might be better if we actually check the exception messages too, to make sure it threw exactly the specific exception we expected.
	}
}
