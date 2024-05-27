using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ViennaNET.Orm.Application;

namespace OrmBenchmark;

[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, iterationCount: 3, id: "OrmUpdateTest")]
public class OrmUpdateTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Container _viennaContainer;
    private Blog[] _data;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Params(10)]
    public int NumBlogs; // number of records to write [once], and read [each pass]

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(0, NumBlogs).Select(
            i => new Blog
            {
                Name = $"Blog{i}", Url = $"blog{i}.blogs.net", Rating = i % 5
            }).ToArray();

        _viennaContainer = ViennaOrm.GetContainer();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        using var context = new TestContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Blogs.AddRange(_data);
    }

    [Benchmark(Baseline = true)]
    public void ViennaSelect()
    {
        using (AsyncScopedLifestyle.BeginScope(_viennaContainer))
        {
            var entityFactoryService = _viennaContainer.GetInstance<IEntityFactoryService>();

            using (var unitOfWork = entityFactoryService.Create())
            {
                var blogs = entityFactoryService.Create<Blog>().Query().ToArray();
                foreach (var blog in blogs)
                {
                    blog.Url = "Updated";
                }

                unitOfWork.Commit();
            }
        }
    }


    [Benchmark]
    public void EfClassicUpdate()
    {
        using var ctx = new TestContext();
        var blogs = ctx.Blogs.ToArray();
        foreach (var blog in blogs)
        {
            blog.Url = "Updated";
        }

        ctx.SaveChanges();
    }

    [Benchmark]
    public void EfClassicUpdateBulkSaveChanges()
    {
        using var ctx = new TestContext();
        var blogs = ctx.Blogs.ToArray();
        foreach (var blog in blogs)
        {
            blog.Url = "Updated";
        }

        ctx.BulkSaveChanges();
    }

    [Benchmark]
    public void EfExecuteUpdate()
    {
        using var ctx = new TestContext();
        ctx.Blogs.ExecuteUpdate(setters => setters.SetProperty(p => p.Url, "Updated"));
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
    }
}