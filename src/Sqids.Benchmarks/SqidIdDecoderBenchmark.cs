using BenchmarkDotNet.Attributes;

namespace Sqids.Benchmarks;

[MemoryDiagnoser]
public class SqidIdDecoderBenchmark
{
	private readonly SqidsEncoder<long> _sqidsEncoder = new ();

	[Params("Uk", "DzPWXTJADcE", "bMZn4Y5Fq8QTCJoLjxPvGfB9Dh6mlz1Sgcu0KpkMyOEiIdrsHRW2VZtweX3aA7UNbhFm8ZG04y52lzNU6di")]
	public string EncodedId { get; set; } = string.Empty;

	[Benchmark]
	public IReadOnlyList<long> Decode() => _sqidsEncoder.Decode(EncodedId);
}
