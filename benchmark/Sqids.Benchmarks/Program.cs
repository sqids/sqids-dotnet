using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Sqids.Benchmarks;

public static class Program
{
	public static void Main(string[] args)
	{
		IConfig config = ManualConfig.CreateMinimumViable()
			.AddJob(Job.Default.WithRuntime(ClrRuntime.Net472))
			.AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
			.AddJob(Job.Default.WithRuntime(CoreRuntime.Core70))
			.AddJob(Job.Default.WithRuntime(CoreRuntime.Core80));

		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
	}
}
