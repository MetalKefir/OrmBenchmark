using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _origin;

    public NotSpecification(Specification<T> origin)
    {
        _origin = origin;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var exp = _origin.ToExpression();

        var param = Expression.Parameter(typeof(T));

        var body = Expression.Not(Expression.Invoke(exp, param));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
