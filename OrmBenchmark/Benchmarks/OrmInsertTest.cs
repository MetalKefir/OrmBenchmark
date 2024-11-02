using BenchmarkDotNet.Attributes;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ViennaNET.Orm.Application;

namespace OrmBenchmark.Benchmarks;

[MemoryDiagnoser]
[KeepBenchmarkFiles]
[HtmlExporter]
[RPlotExporter]
[PlainExporter]
public class OrmInsertTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Container _viennaContainer;
    private Blog[] _data;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Params(1_000, 10_000, 100_000)]
    public int NumBlogs; // number of records to write [once], and read [each pass]

    [GlobalSetup]
    public void Setup()
    {
        using var context = new TestContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        _data = Enumerable
            .Range(0, NumBlogs)
            .Select(
                i => new Blog
                {
                    Name = $"Blog{i}",
                    Url = $"blog{i}.blogs.net",
                    Rating = i % 5
                }).ToArray();

        _viennaContainer = ViennaOrm.GetContainer();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        using var context = new TestContext();
        context.Truncate<Blog>();
    }

    [Benchmark(Baseline = true)]
    public void ViennaInsert()
    {
        using (AsyncScopedLifestyle.BeginScope(_viennaContainer))
        {
            var entityFactoryService = _viennaContainer.GetInstance<IEntityFactoryService>();

            using (var unitOfWork = entityFactoryService.Create())
            {
                foreach (var blog in _data)
                {
                    entityFactoryService.Create<Blog>().Add(blog);
                }

                unitOfWork.Commit();
            }
        }
    }

    [Benchmark]
    public void EfForeachInsert()
    {
        using var ctx = new TestContext();

        foreach (var blog in _data)
        {
            ctx.Blogs.Add(blog);
        }

        ctx.SaveChanges();
    }

    [Benchmark]
    public void EfAddRange()
    {
        using var ctx = new TestContext();

        ctx.Blogs.AddRange(_data);

        ctx.SaveChanges();
    }

    [Benchmark]
    public void EfBulkInsert()
    {
        using var ctx = new TestContext();

        ctx.BulkInsert(_data);
    }

    public class TestContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=orm_test;Username=postgres;Password=pupalupa");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>()
                .ToTable("blogs").HasKey(p => p.Id);

            modelBuilder.Entity<Blog>()
                .Property(x => x.Id).HasColumnName("id");
            modelBuilder.Entity<Blog>()
                .Property(x => x.Name).HasColumnName("name");
            modelBuilder.Entity<Blog>()
                .Property(x => x.Url).HasColumnName("url");
            modelBuilder.Entity<Blog>()
                .Property(x => x.Rating).HasColumnName("rating");
        }
    }
}