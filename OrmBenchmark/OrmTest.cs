using BenchmarkDotNet.Attributes;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

[MemoryDiagnoser]
public class OrmTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Blog[] data;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Params(1000)]
    public int NumBlogs; // number of records to write [once], and read [each pass]

    [GlobalSetup]
    public void Setup()
    {
        using var context = new EfContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        data = Enumerable.Range(0, NumBlogs).Select(
            i => new Blog
            {
                BlogId = Guid.NewGuid(),
                Name = $"Blog{i}",
                Url = $"blog{i}.blogs.net",
                Rating = i % 5
            }).ToArray();
    }


    [Benchmark(Baseline = true)]
    public void EfInsert()
    {
        using var ctx = new EfContext();
        ctx.Truncate<Blog>();
        ctx.Blogs.AddRange(data);
        ctx.SaveChanges();
    }

    [Benchmark]
    public void BulkEfInsert()
    {
        using var ctx = new EfContext();
        ctx.Truncate<Blog>();
        ctx.BulkInsert(data);
    }

    [Benchmark]
    public void ViennaInsert()
    {
        using var ctx = new EfContext();
        ctx.Truncate<Blog>();
        ctx.BulkInsert(data);
    }

    public class EfContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ef_test;Username=postgres;Password=pupalupa");
    }

    public class Blog
    {
        public Guid BlogId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Rating { get; set; }
    }
}
