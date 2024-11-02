using BenchmarkDotNet.Attributes;

namespace OrmBenchmark.Benchmarks;

public class DateRangesCount
{
    [Params(10, 50, 100, 150)]
    public int RangesNumbers;

    private static readonly Random Rnd = new();
    private VacationPeriodDto[] VacationPeriods;

    [GlobalSetup]
    public void Setup()
    {
        VacationPeriods = Array.Empty<VacationPeriodDto>();
    }

    public static DateTime GetRandomDate(DateTime from, DateTime to)
    {
        var range = to - from;

        var randTimeSpan = new TimeSpan((long)(Rnd.NextDouble() * range.Ticks)); 

        return from + randTimeSpan;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);

        var startDate = GetRandomDate(start, end);

        VacationPeriods = Enumerable
            .Range(0, RangesNumbers)
            .Select(x => new VacationPeriodDto(startDate, startDate.AddDays(10), $"period_{x}"))
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public int BaseAlg()
    {
        if (VacationPeriods.Length == 1)
        {
            return 0;
        }

        var accumulator = new Dictionary<string, int>();
        foreach (var vacationPeriod in VacationPeriods)
        {
            var count = VacationPeriods
                .Where(period => period != vacationPeriod)
                .Where(period => !accumulator.ContainsKey(period.PeriodId))
                .Count(period => period.IntersectsWith(vacationPeriod));

            accumulator.Add(vacationPeriod.PeriodId, count);
        }

        return accumulator.Values.Sum();
    }

    [Benchmark]
    public int SweepLine()
    {
        var events = new List<(DateTime time, int type, string rangeId)>();

        foreach (var vacationPeriod in VacationPeriods)
        {
            events.Add((vacationPeriod.From, 1, vacationPeriod.PeriodId));
            events.Add((vacationPeriod.To, -1, vacationPeriod.PeriodId));
        }

        events.Sort((a, b) =>
        {
            if (a.time == b.time)
                return a.type.CompareTo(b.type);

            return a.time.CompareTo(b.time);
        });

        var activeRanges = new SortedSet<string>();
        var unique = new HashSet<(string, string)>();

        foreach (var e in events)
        {
            if (e.type == 1)
            {
                foreach (var activeRangesKey in activeRanges)
                {
                    unique.Add((string.Compare(e.rangeId, activeRangesKey) < 0 ? e.rangeId : activeRangesKey,
                        string.Compare(e.rangeId, activeRangesKey) < 0 ? activeRangesKey : e.rangeId));
                }

                activeRanges.Add(e.rangeId);
            }
            else
            {
                activeRanges.Remove(e.rangeId);
            }
        }

        return unique.Count;
    }
}

public sealed class VacationPeriodDto
{
    public VacationPeriodDto(DateTime from, DateTime to, string periodId)
    {
        From = from;
        To = to;
        PeriodId = periodId;
    }

    public string PeriodId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public bool IntersectsWith(DateTime from, DateTime to) =>
        (From.Date <= to.Date && To.Date >= from.Date)
        || (from.Date <= To.Date && to.Date >= From.Date);

    public bool IntersectsWith(VacationPeriodDto otherPeriodDto) =>
        IntersectsWith(otherPeriodDto.From, otherPeriodDto.To);
}