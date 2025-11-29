using BenchmarkDotNet.Running;
using Benchmarks;
using OrmBenchmark.Benchmarks;

public class Program
{
    public static void Main(string[] args) {
        BenchmarkRunner.Run<DepartmentServiceBench>();
    }
}
