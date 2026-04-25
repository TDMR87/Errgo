using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace Errgo.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance
                .AddExporter(JsonExporter.Default)
                .AddLogger(ConsoleLogger.Default));
    }
}