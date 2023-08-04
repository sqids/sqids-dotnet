namespace Sqids.Tests;

public class BlockListTests
{
	[Theory]
	[InlineData(200044, "sexy")] // NOTE: Without the blocklist, the number `200044` would yield `sexy` when encoded.
	public void Encode_WithDefaultBlockList_BlocksDefaultBlockListWords(
		int number,
		string defaultBlockedEncoded
	)
	{
		var encoder = new SqidsEncoder();
		var encoded = encoder.Encode(number);
		encoded.Should().NotBe(defaultBlockedEncoded);
	}

	[Theory]
	[InlineData("sexy", 200044)]
	public void Decode_WithDefaultBlockList_StillDecodesBlockListWords(
		string id,
		int expected
	)
	{
		var encoder = new SqidsEncoder();
		var encoded = encoder.Decode(id);
		encoded.Should().BeEquivalentTo(new[] { expected });
	}

	[Theory]
	[InlineData(200044, "sexy")]
	public void Encode_WithEmptyBlockList_DoesNotBlockAnything(
		int number,
		string expected
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new(),
		});
		var encoded = encoder.Encode(number);
		encoded.Should().Be(expected);
	}

	[Theory]
	[InlineData(2000, "srV")]
	public void Encode_WithCustomBlockList_BlocksCustomBlockListWords(
		int number,
		string normalEncoded
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new() { normalEncoded },
		});
		var encoded = encoder.Encode(number);
		encoded.Should().NotBe(normalEncoded);
	}

	[Theory]
	[InlineData(new[] { "foo" }, 200044, "sexy")]
	public void Encode_WithCustomBlockList_DoesNotBlockDefaultBlockListWords(
		string[] blocklist,
		int number,
		string byDefaultBlockedEncoded
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new(blocklist),
		});
		var encoded = encoder.Encode(number);
		encoded.Should().Be(byDefaultBlockedEncoded);
	}

	[Theory]
	[InlineData(982938132, "IrHOxXX")]
	public void Encode_WithCustomBlockList_BlocksSubstring(
		int number,
		string normalEncoded
	)
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new()
			{
				normalEncoded[3..] // NOTE: First four characters
			},
		});
		var encoded = encoder.Encode(number);
		encoded.Should().NotBe(normalEncoded);
	}

	[Fact]
	public void Encode_WithTooShortBlockListWords_RemovesFromBlockList()
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new() { "U9" } // NOTE: `U9` is the normal encoding of `1`, and it's less than 3 characters, so its inclusion in the blocklist shouldn't not actually make a difference.
		});
		var encoded = encoder.Encode(1);
		encoded.Should().Be("U9");
	}

	[Fact]
	public void Encode_WithCustomBlockList_IgnoresCasing()
	{
		var encoder = new SqidsEncoder(new()
		{
			BlockList = new() { "uAVP" } // `Uavp` is the normal encoding of `98765` — we block a different casing of it, and we expect it to be blocked, since the blocklist should work case-insensitively.
		});
		var encoded = encoder.Encode(98765);
		encoded.Should().NotBe("Uavp");
	}
}
