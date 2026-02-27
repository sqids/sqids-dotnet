using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<SquidsBenchmarks>();

// Define the Custom Config
public class PackageComparisonConfig : ManualConfig
{
    public PackageComparisonConfig() {
        var packageVersions = new[] { "3.2.0", "3.1.0" };

        var runtimes = new Runtime[]
        {
            ClrRuntime.Net48,
            CoreRuntime.Core70,
            CoreRuntime.Core80
        };

        foreach (var version in packageVersions) {
            foreach (var runtime in runtimes) {
                AddJob(Job.Default
                    .WithRuntime(runtime)
                    .WithMsBuildArguments($"/p:BenchmarksPackageVersion={version}")
                    .WithId($"{runtime.Name} - v{version}"));
            }
        }
    }
}

[Config(typeof(PackageComparisonConfig))]
[MemoryDiagnoser]
public class SquidsBenchmarks
{
#if NET7_0_OR_GREATER
    private readonly Sqids.SqidsEncoder<long> sqid = new();
#else
    private readonly Sqids.SqidsEncoder sqid = new();
#endif

#if WITH_LONG_SUPPORT
    static readonly long longValue = (long)int.MaxValue;
#else
    static readonly int intValue = int.MaxValue;
#endif

    static readonly string EncodedValue = "UKrsQ1F";

    [Benchmark]
    public string SqidEncode() {
#if WITH_LONG_SUPPORT
        var encoded = sqid.Encode(longValue);
#else
        var encoded = sqid.Encode(intValue);
#endif
        return encoded;
    }

    [Benchmark]
    public void SqidDecode() {
        var decoded = sqid.Decode(EncodedValue)[0];
        if (decoded != int.MaxValue)
            throw new Exception("Decoded value does not match original");
    }
}