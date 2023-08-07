namespace Sqids.Tests;

public class MinLengthTests
{
	[Fact]
	public void Simple()
	{
		var sqids = new SqidsEncoder(new()
		{
			MinLength = new SqidsOptions().Alphabet.Length, // Gets the default alphabet
		});

		var numbers = new[] { 1, 2, 3 };
		var id = "75JILToVsGerOADWmHlY38xvbaNZKQ9wdFS0B6kcMEtnRpgizhjU42qT1cd0dL";

		sqids.Encode(numbers).Should().Be(id);
		sqids.Decode(id).Should().BeEquivalentTo(numbers);
	}

	[Fact]
	public void IncrementalNumbers()
	{
		var sqids = new SqidsEncoder(new()
		{
			MinLength = new SqidsOptions().Alphabet.Length, // Gets the default alphabet
		});

		var idsAndNumbers = new Dictionary<string, int[]>()
		{
			["jf26PLNeO5WbJDUV7FmMtlGXps3CoqkHnZ8cYd19yIiTAQuvKSExzhrRghBlwf"] = new[] { 0, 0 },
			["vQLUq7zWXC6k9cNOtgJ2ZK8rbxuipBFAS10yTdYeRa3ojHwGnmMV4PDhESI2jL"] = new[] { 0, 1 },
			["YhcpVK3COXbifmnZoLuxWgBQwtjsSaDGAdr0ReTHM16yI9vU8JNzlFq5Eu2oPp"] = new[] { 0, 2 },
			["OTkn9daFgDZX6LbmfxI83RSKetJu0APihlsrYoz5pvQw7GyWHEUcN2jBqd4kJ9"] = new[] { 0, 3 },
			["h2cV5eLNYj1x4ToZpfM90UlgHBOKikQFvnW36AC8zrmuJ7XdRytIGPawqYEbBe"] = new[] { 0, 4 },
			["7Mf0HeUNkpsZOTvmcj836P9EWKaACBubInFJtwXR2DSzgYGhQV5i4lLxoT1qdU"] = new[] { 0, 5 },
			["APVSD1ZIY4WGBK75xktMfTev8qsCJw6oyH2j3OnLcXRlhziUmpbuNEar05QCsI"] = new[] { 0, 6 },
			["P0LUhnlT76rsWSofOeyRGQZv1cC5qu3dtaJYNEXwk8Vpx92bKiHIz4MgmiDOF7"] = new[] { 0, 7 },
			["xAhypZMXYIGCL4uW0te6lsFHaPc3SiD1TBgw5O7bvodzjqUn89JQRfk2Nvm4JI"] = new[] { 0, 8 },
			["94dRPIZ6irlXWvTbKywFuAhBoECQOVMjDJp53s2xeqaSzHY8nc17tmkLGwfGNl"] = new[] { 0, 9 },
		};

		foreach (var (id, numbers) in idsAndNumbers)
		{
			sqids.Encode(numbers).Should().Be(id);
			sqids.Decode(id).Should().BeEquivalentTo(numbers);
		}
	}

	[Fact]
	public void MinLengths()
	{
		foreach (var minLength in new[] { 0, 1, 5, 10, new SqidsOptions().Alphabet.Length })
		{
			var numbersList = new[]
			{
				new[] { SqidsEncoder.MinValue },
				new[] { 0, 0, 0, 0, 0 },
				new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
				new[] { 100, 200, 300 },
				new[] { 1_000, 2_000, 3_000 },
				new[] { 1_000_000 },
				new[] { SqidsEncoder.MaxValue }
			};
			foreach (var numbers in numbersList)
			{
				var sqids = new SqidsEncoder(new()
				{
					MinLength = minLength
				});

				var id = sqids.Encode(numbers);
				id.Length.Should().BeGreaterThanOrEqualTo(minLength);
				sqids.Decode(id).Should().BeEquivalentTo(numbers);
			}
		}
	}

	[Fact]
	public void MinLengthOutOfRange()
	{
		var a1 = () => new SqidsEncoder(new()
		{
			MinLength = -1,
		});
		a1.Should().Throw<ArgumentException>();

		var a2 = () => new SqidsEncoder(new()
		{
			MinLength = new SqidsOptions().Alphabet.Length + 1,
		});
		a2.Should().Throw<ArgumentException>();
	}
}
