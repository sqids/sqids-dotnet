namespace Sqids.Tests;

public class MinLengthTests
{
	[TestCase(new[] { 1, 2, 3 }, "86Rf07xd4zBmiJXQG6otHEbew02c3PWsUOLZxADhCpKj7aVFv9I8RquYrNlSTM")]
	[TestCase(new[] { 0, 0 }, "SvIzsqYMyQwI3GWgJAe17URxX8V924Co0DaTZLtFjHriEn5bPhcSkfmvOslpBu")]
	[TestCase(new[] { 0, 1 }, "n3qafPOLKdfHpuNw3M61r95svbeJGk7aAEgYn4WlSjXURmF8IDqZBy0CT2VxQc")]
	[TestCase(new[] { 0, 2 }, "tryFJbWcFMiYPg8sASm51uIV93GXTnvRzyfLleh06CpodJD42B7OraKtkQNxUZ")]
	[TestCase(new[] { 0, 3 }, "eg6ql0A3XmvPoCzMlB6DraNGcWSIy5VR8iYup2Qk4tjZFKe1hbwfgHdUTsnLqE")]
	[TestCase(new[] { 0, 4 }, "rSCFlp0rB2inEljaRdxKt7FkIbODSf8wYgTsZM1HL9JzN35cyoqueUvVWCm4hX")]
	[TestCase(new[] { 0, 5 }, "sR8xjC8WQkOwo74PnglH1YFdTI0eaf56RGVSitzbjuZ3shNUXBrqLxEJyAmKv2")]
	[TestCase(new[] { 0, 6 }, "uY2MYFqCLpgx5XQcjdtZK286AwWV7IBGEfuS9yTmbJvkzoUPeYRHr4iDs3naN0")]
	[TestCase(new[] { 0, 7 }, "74dID7X28VLQhBlnGmjZrec5wTA1fqpWtK4YkaoEIM9SRNiC3gUJH0OFvsPDdy")]
	[TestCase(new[] { 0, 8 }, "30WXpesPhgKiEI5RHTY7xbB1GnytJvXOl2p0AcUjdF6waZDo9Qk8VLzMuWrqCS")]
	[TestCase(new[] { 0, 9 }, "moxr3HqLAK0GsTND6jowfZz3SUx7cQ8aC54Pl1RbIvFXmEJuBMYVeW9yrdOtin")]
	public void EncodeAndDecode_WithHighMinLength_ReturnsExactMatch(int[] numbers, string id)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			MinLength = new SqidsOptions().Alphabet.Length // NOTE: This is how we get the default alphabet
		});

		sqids.Encode(numbers).ShouldBe(id);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}

	[TestCaseSource(nameof(IncrementalMinLengthsSource))]
	public void EncodeAndDecode_WithIncrementalMinLengths_RespectsMinLengthAndRoundTripsSuccessfully(
		int minLength,
		string id
	)
	{
		var numbers = new[] { 1, 2, 3 }; // NOTE: Constant

#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new()
#else
		var sqids = new SqidsEncoder(new()
#endif
		{
			MinLength = minLength
		});

		sqids.Encode(numbers).ShouldBe(id);
		sqids.Encode(numbers).Length.ShouldBeGreaterThanOrEqualTo(minLength);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}
	private static TestCaseData[] IncrementalMinLengthsSource => new TestCaseData[]
	{
		new(6, "86Rf07"),
		new(7, "86Rf07x"),
		new(8, "86Rf07xd"),
		new(9, "86Rf07xd4"),
		new(10, "86Rf07xd4z"),
		new(11, "86Rf07xd4zB"),
		new(12, "86Rf07xd4zBm"),
		new(13, "86Rf07xd4zBmi"),
		new(
			new SqidsOptions().Alphabet.Length,
			"86Rf07xd4zBmiJXQG6otHEbew02c3PWsUOLZxADhCpKj7aVFv9I8RquYrNlSTM"
		),
		new(
			new SqidsOptions().Alphabet.Length + 1,
			"86Rf07xd4zBmiJXQG6otHEbew02c3PWsUOLZxADhCpKj7aVFv9I8RquYrNlSTMy"
		),
		new(
			new SqidsOptions().Alphabet.Length + 2,
			"86Rf07xd4zBmiJXQG6otHEbew02c3PWsUOLZxADhCpKj7aVFv9I8RquYrNlSTMyf"
		),
		new(
			new SqidsOptions().Alphabet.Length + 3,
			"86Rf07xd4zBmiJXQG6otHEbew02c3PWsUOLZxADhCpKj7aVFv9I8RquYrNlSTMyf1"
		)
	};

	[Test, Combinatorial]
	public void EncodeAndDecode_WithDifferentMinLengths_RespectsMinLengthAndRoundTripsSuccessfully(
		[ValueSource(nameof(MinLengthsValueSource))] int minLength,
		[Values(
			new[] { 0 },
			new[] { 0, 0, 0, 0, 0 },
			new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
			new[] { 100, 200, 300 },
			new[] { 1_000, 2_000, 3_000 },
			new[] { 1_000_000 },
			new[] { int.MaxValue }
		)]
		int[] numbers
	)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>(new() { MinLength = minLength });
#else
		var sqids = new SqidsEncoder(new() { MinLength = minLength });
#endif

		var id = sqids.Encode(numbers);
		id.Length.ShouldBeGreaterThanOrEqualTo(minLength);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}
	private static int[] MinLengthsValueSource => new[]
	{
		0, 1, 5, 10, new SqidsOptions().Alphabet.Length  // NOTE: We can't use `new SqidsOptions().Alphabet.Length` in the `[Values]` attribute since only constants are allowed for attribute arguments; so we have to use a value source like this.
	};

	[TestCase(-1)] // NOTE: Negative min lengths are not acceptable
	[TestCase(256)] // NOTE: Max min length is 255
	public void Instantiate_WithOutOfRangeMinLength_Throws(int outOfRangeMinLength)
	{
#if NET7_0_OR_GREATER
		var act = () => new SqidsEncoder<int>(new() { MinLength = outOfRangeMinLength });
#else
		var act = () => new SqidsEncoder(new() { MinLength = outOfRangeMinLength });
#endif
		act.ShouldThrow<ArgumentOutOfRangeException>();
	}
}
