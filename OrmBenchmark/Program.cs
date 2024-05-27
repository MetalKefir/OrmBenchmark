using BenchmarkDotNet.Running;
using OrmBenchmark;

public class Program
{
    public static void Main(string[] args) {
        BenchmarkRunner.Run<OrmUpdateTest>();

        Console.Read();
        Console.Read();
    }
}
