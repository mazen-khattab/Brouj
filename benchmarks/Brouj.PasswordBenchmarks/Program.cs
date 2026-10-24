using BenchmarkDotNet.Running;
using Brouj.PasswordBenchmarks;

BenchmarkSwitcher.FromAssembly(typeof(PasswordBenchmarks).Assembly).Run(args);
