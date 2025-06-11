using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public abstract class Specification<T>
{
    private Func<T, bool>? _cachedPredicate;

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        _cachedPredicate ??= ToExpression().Compile();
        return _cachedPredicate(entity);
    }

    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        => new AndSpecification<T>(left, right);

    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        => new OrSpecification<T>(left, right);

    public static Specification<T> operator !(Specification<T> spec)
        => new NotSpecification<T>(spec);
}