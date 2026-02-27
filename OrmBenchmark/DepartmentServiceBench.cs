using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Benchmarks;

[MemoryDiagnoser]
public class DepartmentServiceBench
{
  private NewDepartmentService _departmentService = null!;

  List<Department> _departmentList = null!;

  [Params(0, 1, 2, 3, 4, 5, 6)]
  public int Level;

  [GlobalSetup]
  public void Setup()
  {
    using var context = new TestContext();

    _departmentList = context.Departments
      .Where(x => !x.IsDeleted)
      .AsNoTracking()
      .ToList();

    _departmentService = new NewDepartmentService(_departmentList);
  }

  [Benchmark(Baseline = true)]
  public void FindByNameAtLevel()
  {
    _ = _departmentService.FindByNameAtLevel("сектор прямых доставок в г. Тольятти", level: Level);
  }

  [Benchmark]
  public void FindByNameAtLevelRecursive()
  {
    _ = _departmentService.FindByNameAtLevelRecursive("сектор прямых доставок в г. Тольятти", level: Level);
  }

  [Benchmark]
  public void FindByNameAtLevelRecursiveWithPredicate()
  {
    _ = _departmentService.FindByNameAtLevelFiltered("сектор прямых доставок в г. Тольятти", level: Level);
  }
}

public class TestContext : DbContext
{
  public DbSet<Department> Departments { get; set; }


  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=orm_test;Username=postgres;Password=pupalupa;Search Path=vacations-api_test;");
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Department>(builder =>
    {
      builder.ToTable("departments");

      builder.HasKey(x => x.Id);

      builder.Property(x => x.Id).HasColumnName("id");
      builder.Property(d => d.Name).HasColumnName("name");
      builder.Property(d => d.Group).HasColumnName("department_group");
      builder.Property(d => d.StructuralDepartment).HasColumnName("department");
      builder.Property(d => d.Division).HasColumnName("division");
      builder.Property(d => d.Region).HasColumnName("region");
      builder.Property(d => d.Directory).HasColumnName("directory");
      builder.Property(d => d.IsDeleted).HasColumnName("is_deleted");
      builder.Property(d => d.HrMailingGroup).HasColumnName("hr_mailing_group");
      builder.Property(d => d.ParentId).HasColumnName("parent_department_id");

      builder.HasOne(x => x.Parent)
          .WithMany()
          .HasForeignKey(x => x.ParentId)
          .OnDelete(DeleteBehavior.NoAction);
    });
  }
}

[ExcludeFromCodeCoverage]
public class Department
{
  public virtual int? ParentId { get; set; }

  public virtual Department? Parent { get; set; }

  public virtual string Name { get; set; }

  public virtual string? Group { get; set; }

  public virtual string? StructuralDepartment { get; set; }

  public virtual string? Division { get; set; }

  public virtual string? Region { get; set; }

  public virtual string? Directory { get; set; }

  public virtual bool IsDeleted { get; set; }

  public virtual string? HrMailingGroup { get; set; }


  public virtual int Id { get; set; }
}
