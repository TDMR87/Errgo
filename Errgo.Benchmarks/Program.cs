using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Text.Json;

namespace Errgo.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var previousResult = ReadResultsJson(ResultsDirectory);

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance
                .AddExporter(JsonExporter.Default)
                .AddLogger(ConsoleLogger.Default));

        var newResult = ReadResultsJson(ResultsDirectory);
        CompareResults(previousResult, newResult);
    }

    private record BenchmarkStatistics(double Mean);

    private record ResultJson(List<BenchmarkResult> Benchmarks);

    private record BenchmarkResult(string MethodTitle, BenchmarkStatistics Statistics, BenchmarkMemory Memory);

    private record BenchmarkMemory(long BytesAllocatedPerOperation);

    static readonly string ResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");

    private static ResultJson? ReadResultsJson(string resultsDirectory)
    {
        var latestFile = Directory.EnumerateFiles(resultsDirectory, "*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        return latestFile is not null
            ? JsonSerializer.Deserialize<ResultJson>(File.ReadAllText(latestFile.FullName))
            : new ResultJson([]);
    }

    private static void CompareResults(ResultJson? previous, ResultJson? current)
    {
        if (previous is null || current is null) 
            throw new InvalidOperationException("Cannot compare results because one of them is null.");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n*** Comparing benchmark results from previous run ***");

        foreach (var currentBenchmark in current.Benchmarks)
        {
            var previousBenchmark = previous.Benchmarks
                .FirstOrDefault(b => b.MethodTitle == currentBenchmark.MethodTitle);

            if (previousBenchmark is null) continue;

            var meanCurrent =  $"Mean (now)    = {FormatTime(currentBenchmark.Statistics.Mean)}";
            var meanPrevious = $"Mean (before) = {FormatTime(previousBenchmark.Statistics.Mean)}";
            var allocatedCurrent =  $"Allocated (now)    = {currentBenchmark.Memory.BytesAllocatedPerOperation} B";
            var allocatedPrevious = $"Allocated (before) = {previousBenchmark.Memory.BytesAllocatedPerOperation} B";

            var hasRegression = 
                previousBenchmark.Statistics.Mean < currentBenchmark.Statistics.Mean ||
                previousBenchmark.Memory.BytesAllocatedPerOperation < currentBenchmark.Memory.BytesAllocatedPerOperation;

            if (hasRegression)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(
                    $"REGRESSION: {currentBenchmark.MethodTitle}:\n" +
                    $"{meanPrevious}\n" +
                    $"{meanCurrent}\n" +
                    $"{allocatedPrevious}\n" +
                    $"{allocatedCurrent}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"IMPROVEMENT: {currentBenchmark.MethodTitle}:\n" +
                    $"{meanPrevious}\n" +
                    $"{meanCurrent}\n" +
                    $"{allocatedPrevious}\n" +
                    $"{allocatedCurrent}");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("**********************************************");
        }
    }

    private static string FormatTime(double meanNanoseconds)
    {
        return meanNanoseconds switch
        {
             >= 1_000_000 => $"{meanNanoseconds / 1_000_000:F2} ms",
             >= 100_000 => $"{meanNanoseconds / 1000:F2} µs",
             >= 1 or _ => $"{meanNanoseconds:F4} ns",
        };
    }
}