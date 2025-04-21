using BenchmarkDotNet.Running;

BenchmarkSwitcher
	.FromAssembly(typeof(Program).Assembly)
	.RunAllJoined();
