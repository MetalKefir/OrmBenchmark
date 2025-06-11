using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class AlwaysTrueSpecification<T> : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression() => _ => true;
}