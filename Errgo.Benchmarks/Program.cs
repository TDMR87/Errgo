using BenchmarkDotNet.Running;

namespace Errgo.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<PerformanceBenchmarks>();
        //BenchmarkRunner.Run<ComparisonBenchmarks>();
    }
}