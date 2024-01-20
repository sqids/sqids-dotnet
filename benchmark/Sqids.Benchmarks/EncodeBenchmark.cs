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
	public string Encode() => _encoder.Encode(42);

	[Benchmark]
	public IReadOnlyList<int> Decode() => _encoder.Decode("Jg");
}
