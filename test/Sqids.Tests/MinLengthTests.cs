namespace Sqids.Tests;

public class MinLengthTests
{
	[TestCase(new[] { 1, 2, 3 }, "75JILToVsGerOADWmHlY38xvbaNZKQ9wdFS0B6kcMEtnRpgizhjU42qT1cd0dL")]
	[TestCase(new[] { 0, 0 }, "jf26PLNeO5WbJDUV7FmMtlGXps3CoqkHnZ8cYd19yIiTAQuvKSExzhrRghBlwf")]
	[TestCase(new[] { 0, 1 }, "vQLUq7zWXC6k9cNOtgJ2ZK8rbxuipBFAS10yTdYeRa3ojHwGnmMV4PDhESI2jL")]
	[TestCase(new[] { 0, 2 }, "YhcpVK3COXbifmnZoLuxWgBQwtjsSaDGAdr0ReTHM16yI9vU8JNzlFq5Eu2oPp")]
	[TestCase(new[] { 0, 3 }, "OTkn9daFgDZX6LbmfxI83RSKetJu0APihlsrYoz5pvQw7GyWHEUcN2jBqd4kJ9")]
	[TestCase(new[] { 0, 4 }, "h2cV5eLNYj1x4ToZpfM90UlgHBOKikQFvnW36AC8zrmuJ7XdRytIGPawqYEbBe")]
	[TestCase(new[] { 0, 5 }, "7Mf0HeUNkpsZOTvmcj836P9EWKaACBubInFJtwXR2DSzgYGhQV5i4lLxoT1qdU")]
	[TestCase(new[] { 0, 6 }, "APVSD1ZIY4WGBK75xktMfTev8qsCJw6oyH2j3OnLcXRlhziUmpbuNEar05QCsI")]
	[TestCase(new[] { 0, 7 }, "P0LUhnlT76rsWSofOeyRGQZv1cC5qu3dtaJYNEXwk8Vpx92bKiHIz4MgmiDOF7")]
	[TestCase(new[] { 0, 8 }, "xAhypZMXYIGCL4uW0te6lsFHaPc3SiD1TBgw5O7bvodzjqUn89JQRfk2Nvm4JI")]
	[TestCase(new[] { 0, 9 }, "94dRPIZ6irlXWvTbKywFuAhBoECQOVMjDJp53s2xeqaSzHY8nc17tmkLGwfGNl")]
	public void EncodeAndDecode_WithMaximumMinLength_ReturnsExactMatch(int[] numbers, string id)
	{
		var sqids = new SqidsEncoder(new() { MinLength = new SqidsOptions().Alphabet.Length }); // NOTE: This is how we get the default alphabet

		sqids.Encode(numbers).ShouldBe(id);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}

	[Test, Combinatorial]
	public void EncodeAndDecode_WithDifferentMinLengths_RespectsMinLengthAndRoundTripsSuccessfully(
		[ValueSource(nameof(MinLengths))] int minLength,
		[ValueSource(nameof(Numbers))] int[] numbers
	)
	{
		var sqids = new SqidsEncoder(new() { MinLength = minLength });

		var id = sqids.Encode(numbers);
		id.Length.ShouldBeGreaterThanOrEqualTo(minLength);
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}
	private static int[] MinLengths => new[] { 0, 1, 5, 10, new SqidsOptions().Alphabet.Length }; // NOTE: We can't use `new SqidsOptions().Alphabet.Length` in the `[Values]` attribute since only constants are allowed for attribute arguments; so we have to use a value source like this.
	private static int[][] Numbers => new[]
	{
		new[] { SqidsEncoder.MinValue },
		new[] { 0, 0, 0, 0, 0 },
		new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
		new[] { 100, 200, 300 },
		new[] { 1_000, 2_000, 3_000 },
		new[] { 1_000_000 },
		new[] { SqidsEncoder.MaxValue }
	};

	[TestCaseSource(nameof(OutOfRangeMinLengths))]
	public void Instantiate_WithOutOfRangeMinLength_Throws(int outOfRangeMinLength)
	{
		var a2 = () => new SqidsEncoder(new() { MinLength = outOfRangeMinLength });
		a2.ShouldThrow<ArgumentException>();
	}
	private static int[] OutOfRangeMinLengths => new[] { -1, new SqidsOptions().Alphabet.Length + 1 }; // NOTE: We can't use `new SqidsOptions().Alphabet.Length` in the `[TestCase]` attribute since only constants are allowed for attribute arguments; so we have to use a value source like this.
}
