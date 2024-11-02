using BenchmarkDotNet.Running;
using OrmBenchmark.Benchmarks;

public class Program
{
    public static void Main(string[] args) {
        BenchmarkRunner.Run<DateRangesCount>();
    }
}
