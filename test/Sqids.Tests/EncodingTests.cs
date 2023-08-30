#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Sqids.Tests;

public class EncodingTests
{
	// NOTE: Incremental
	[TestCase(0, "bV")]
	[TestCase(1, "U9")]
	[TestCase(2, "g8")]
	[TestCase(3, "Ez")]
	[TestCase(4, "V8")]
	[TestCase(5, "ul")]
	[TestCase(6, "O3")]
	[TestCase(7, "AF")]
	[TestCase(8, "ph")]
	[TestCase(9, "n8")]
	public void EncodeAndDecode_SingleNumber_ReturnsExactMatch(int number, string id)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Encode(number).ShouldBe(id);
		sqids.Decode(id).ShouldBeEquivalentTo(new[] { number });
	}

	// NOTE: Simple case
	[TestCase(new[] { 1, 2, 3 }, "8QRLaD")]
	// NOTE: Incremental
	[TestCase(new[] { 0, 0 }, "SrIu")]
	[TestCase(new[] { 0, 1 }, "nZqE")]
	[TestCase(new[] { 0, 2 }, "tJyf")]
	[TestCase(new[] { 0, 3 }, "e86S")]
	[TestCase(new[] { 0, 4 }, "rtC7")]
	[TestCase(new[] { 0, 5 }, "sQ8R")]
	[TestCase(new[] { 0, 6 }, "uz2n")]
	[TestCase(new[] { 0, 7 }, "7Td9")]
	[TestCase(new[] { 0, 8 }, "3nWE")]
	[TestCase(new[] { 0, 9 }, "mIxM")]
	// NOTE: Incremental
	[TestCase(new[] { 0, 0 }, "SrIu")]
	[TestCase(new[] { 1, 0 }, "nbqh")]
	[TestCase(new[] { 2, 0 }, "t4yj")]
	[TestCase(new[] { 3, 0 }, "eQ6L")]
	[TestCase(new[] { 4, 0 }, "r4Cc")]
	[TestCase(new[] { 5, 0 }, "sL82")]
	[TestCase(new[] { 6, 0 }, "uo2f")]
	[TestCase(new[] { 7, 0 }, "7Zdq")]
	[TestCase(new[] { 8, 0 }, "36Wf")]
	[TestCase(new[] { 9, 0 }, "m4xT")]
	// NOTE: Empty array should encode into empty string
	[TestCase(new int[] { }, "")]
	public void EncodeAndDecode_MultipleNumbers_ReturnsExactMatch(int[] numbers, string id)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Encode(numbers).ShouldBe(id);
		sqids.Encode(numbers.ToList()).ShouldBe(id); // NOTE: Selects the `IEnumerable<int>` overload
		sqids.Decode(id).ShouldBeEquivalentTo(numbers);
	}

	[TestCaseSource(nameof(MultipleNumbersTestCaseSource))]
	public void EncodeAndDecode_MultipleNumbers_RoundTripsSuccessfully(int[] numbers)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Decode(sqids.Encode(numbers)).ShouldBeEquivalentTo(numbers);
	}

	private static int[][] MultipleNumbersTestCaseSource => new[]
	{
#if NET7_0_OR_GREATER
		new[]
		{
			0, 0, 0, 1, 2, 3, 100, 1_000, 100_000, 1_000_000, SqidsEncoder<int>.MaxValue
		},
#else
		new[]
		{
			0, 0, 0, 1, 2, 3, 100, 1_000, 100_000, 1_000_000, SqidsEncoder.MaxValue
		},
#endif
		new[] {
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
			25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
			71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93,
			94, 95, 96, 97, 98, 99
		}
	};

	[TestCase("*")] // NOTE: Character not found in the alphabet
	[TestCase("fff")] // NOTE: Repeating reserved character
	public void Decode_WithInvalidCharacters_ReturnsEmptyArray(string id)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Decode(id).ShouldBeEmpty();
	}

	[Test]
	public void Encode_OutOfRangeNumber_Throws()
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
		var act = () => sqids.Encode(SqidsEncoder<int>.MinValue - 1);
#else
		var sqids = new SqidsEncoder();
		var act = () => sqids.Encode(SqidsEncoder.MinValue - 1);
#endif
		act.ShouldThrow<ArgumentOutOfRangeException>();
		// NOTE: We don't check for `MaxValue + 1` because that's a compile time error anyway
	}

#if NET7_0_OR_GREATER
	[TestCase(byte.MaxValue)]
	[TestCase(sbyte.MaxValue)]
	[TestCase(int.MaxValue)]
	[TestCase(uint.MaxValue)]
	[TestCase(short.MaxValue)]
	[TestCase(ushort.MaxValue)]
	[TestCase(long.MaxValue)]
	[TestCase(ulong.MaxValue)]
	public void EncodeAndDecode_SingleNumberOfDifferentIntegerTypes_RoundTripsSuccessfully<T>(
		T number
	) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
	{
		var sqids = new SqidsEncoder<T>();
		sqids.Decode(sqids.Encode(number)).ShouldBeEquivalentTo(new[] { number });
	}

	[TestCaseSource(nameof(MultipleNumbersOfDifferentIntegerTypesTestCaseSource))]
	public void EncodeAndDecode_MultipleNumbersOfDifferentIntegerTypes_RoundTripsSuccessfully<T>(
		T[] numbers
	) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
	{
		var sqids = new SqidsEncoder<T>();
		sqids.Decode(sqids.Encode(numbers)).ShouldBeEquivalentTo(numbers);
	}

	private static TestCaseData[] MultipleNumbersOfDifferentIntegerTypesTestCaseSource => new[]
	{
		new TestCaseData(GenerateMultipleNumbersOfType<byte>()),
		new TestCaseData(GenerateMultipleNumbersOfType<sbyte>()),
		new TestCaseData(GenerateMultipleNumbersOfType<int>()),
		new TestCaseData(GenerateMultipleNumbersOfType<uint>()),
		new TestCaseData(GenerateMultipleNumbersOfType<short>()),
		new TestCaseData(GenerateMultipleNumbersOfType<ushort>()),
		new TestCaseData(GenerateMultipleNumbersOfType<long>()),
		new TestCaseData(GenerateMultipleNumbersOfType<ulong>()),
	};

	private static T[] GenerateMultipleNumbersOfType<T>()
		where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
	{
		T part = T.MaxValue / T.CreateChecked(10);
		return Enumerable.Range(0, 5)
			.Select(x => T.CreateChecked(x) * part)
			.Append(T.MaxValue)
			.ToArray();
	}
#endif
}
