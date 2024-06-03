using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OrmBenchmark;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ViennaNET.Orm.Application;

[MemoryDiagnoser]
[KeepBenchmarkFiles]
[HtmlExporter]
[RPlotExporter]
[PlainExporter]
public class OrmSelectTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Container _viennaContainer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Params(100, 1_000, 10_000, 100_000, 1_000_000)]
    public int NumBlogs; // number of records to write [once], and read [each pass]

    [GlobalSetup]
    public void Setup()
    {
        using var context = new TestContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.SeedData(NumBlogs);

        _viennaContainer = ViennaOrm.GetContainer();
    }

    [Benchmark]
    public IReadOnlyCollection<Blog> EfSelect()
    {
        using var ctx = new TestContext();
        return ctx.Blogs.ToArray();
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyCollection<Blog> ViennaSelect()
    {
        using (AsyncScopedLifestyle.BeginScope(_viennaContainer))
        {
            var entityFactoryService = _viennaContainer.GetInstance<IEntityFactoryService>();

            return entityFactoryService.Create<Blog>().Query().ToArray();
        }
    }

    public class TestContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=orm_test;Username=postgres;Password=pupalupa");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>()
                .ToTable("blogs").HasKey(p => p.Id);

            modelBuilder.Entity<Blog>()
                .Property(x=>x.Id).HasColumnName("id");
            modelBuilder.Entity<Blog>()
                .Property(x=>x.Name).HasColumnName("name");
            modelBuilder.Entity<Blog>()
                .Property(x=>x.Url).HasColumnName("url");
            modelBuilder.Entity<Blog>()
                .Property(x=>x.Rating).HasColumnName("rating");
        }

        public void SeedData(int numblogs)
        {
            Blogs.AddRange(
                Enumerable.Range(0, numblogs).Select(
                    i => new Blog
                    {
                       Name = $"Blog{i}", Url = $"blog{i}.blogs.net", Rating = i % 5
                    }));
            SaveChanges();
        }
    }
}
