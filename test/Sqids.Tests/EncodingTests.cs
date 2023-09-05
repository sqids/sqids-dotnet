#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Sqids.Tests;

public class EncodingTests
{
	// NOTE: Incremental
	[TestCase(0, "bM")]
	[TestCase(1, "Uk")]
	[TestCase(2, "gb")]
	[TestCase(3, "Ef")]
	[TestCase(4, "Vq")]
	[TestCase(5, "uw")]
	[TestCase(6, "OI")]
	[TestCase(7, "AX")]
	[TestCase(8, "p6")]
	[TestCase(9, "nJ")]
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
	[TestCase(new[] { 1, 2, 3 }, "86Rf07")]
	// NOTE: Incremental
	[TestCase(new[] { 0, 0 }, "SvIz")]
	[TestCase(new[] { 0, 1 }, "n3qa")]
	[TestCase(new[] { 0, 2 }, "tryF")]
	[TestCase(new[] { 0, 3 }, "eg6q")]
	[TestCase(new[] { 0, 4 }, "rSCF")]
	[TestCase(new[] { 0, 5 }, "sR8x")]
	[TestCase(new[] { 0, 6 }, "uY2M")]
	[TestCase(new[] { 0, 7 }, "74dI")]
	[TestCase(new[] { 0, 8 }, "30WX")]
	[TestCase(new[] { 0, 9 }, "moxr")]
	// NOTE: Incremental
	[TestCase(new[] { 0, 0 }, "SvIz")]
	[TestCase(new[] { 1, 0 }, "nWqP")]
	[TestCase(new[] { 2, 0 }, "tSyw")]
	[TestCase(new[] { 3, 0 }, "eX68")]
	[TestCase(new[] { 4, 0 }, "rxCY")]
	[TestCase(new[] { 5, 0 }, "sV8a")]
	[TestCase(new[] { 6, 0 }, "uf2K")]
	[TestCase(new[] { 7, 0 }, "7Cdk")]
	[TestCase(new[] { 8, 0 }, "3aWP")]
	[TestCase(new[] { 9, 0 }, "m2xn")]
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

	[TestCase(new[] { 0, 0, 0, 1, 2, 3, 100, 1_000, 100_000, 1_000_000, int.MaxValue })]
	[TestCase(new[]
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
		48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70,
		71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93,
		94, 95, 96, 97, 98, 99
	})]
	public void EncodeAndDecode_MultipleNumbers_RoundTripsSuccessfully(int[] numbers)
	{
#if NET7_0_OR_GREATER
		var sqids = new SqidsEncoder<int>();
#else
		var sqids = new SqidsEncoder();
#endif

		sqids.Decode(sqids.Encode(numbers)).ShouldBeEquivalentTo(numbers);
	}

	[TestCase("*")] // NOTE: Character not found in the alphabet
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
#else
		var sqids = new SqidsEncoder();
#endif
		var act = () => sqids.Encode(-1);
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

	private static TestCaseData[] MultipleNumbersOfDifferentIntegerTypesTestCaseSource => new TestCaseData[]
	{
		new(GenerateMultipleNumbersOfType<byte>()),
		new(GenerateMultipleNumbersOfType<sbyte>()),
		new(GenerateMultipleNumbersOfType<int>()),
		new(GenerateMultipleNumbersOfType<uint>()),
		new(GenerateMultipleNumbersOfType<short>()),
		new(GenerateMultipleNumbersOfType<ushort>()),
		new(GenerateMultipleNumbersOfType<long>()),
		new(GenerateMultipleNumbersOfType<ulong>()),
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
