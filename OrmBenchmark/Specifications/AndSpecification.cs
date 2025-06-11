using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left, _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExp = _left.ToExpression();
        var rightExp = _right.ToExpression();

        var param = Expression.Parameter(typeof(T));

        var body = Expression.AndAlso(
            Expression.Invoke(leftExp, param),
            Expression.Invoke(rightExp, param)
        );

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}