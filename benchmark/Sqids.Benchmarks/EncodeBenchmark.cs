using BenchmarkDotNet.Attributes;

namespace Sqids.Benchmarks;

[MemoryDiagnoser]
public class EncodeBenchmark
{
#if NET7_0_OR_GREATER
	private SqidsEncoder<int> _encoder = new SqidsEncoder<int>();
#else
	private SqidsEncoder _encoder = new SqidsEncoder();
#endif

	[Benchmark]
	public string EncodeSmall() => _encoder.Encode(42);

	[Benchmark]
	public string EncodeBig() => _encoder.Encode(int.MaxValue);

	[Benchmark]
	public string EncodeMany() => _encoder.Encode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

	[Benchmark]
	public IReadOnlyList<int> DecodeSmall() => _encoder.Decode("Jg");

	[Benchmark]
	public IReadOnlyList<int> DecodeBig() => _encoder.Decode("UKrsQ1F");

	[Benchmark]
	public IReadOnlyList<int> DecodeMany() => _encoder.Decode("hwB5vcCxfAyBnVKMtAaV");
}
