using BenchmarkDotNet.Attributes;

namespace Sqids.Benchmarks;

[MemoryDiagnoser]
public class SqidIdEncoderBenchmark
{
	private readonly SqidsEncoder<long> _sqidsEncoder = new ();

	[Params(1, 100, 1_000, 1_000_000, 1_000_000_000)]
	public int Id { get; set; }

	[Benchmark]
	public string Encode() => _sqidsEncoder.Encode(Id);
}
