using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class ComposedSpecification<TParent, TChild> : Specification<TParent>
{
    private readonly Expression<Func<TParent, TChild?>> _childSelector;
    private readonly Specification<TChild> _childSpec;

    public ComposedSpecification(
        Specification<TChild> childSpec,
        Expression<Func<TParent, TChild?>> childSelector)
    {
        _childSelector = childSelector;
        _childSpec = childSpec;
    }

    public override Expression<Func<TParent, bool>> ToExpression()
    {
        var parentParam = _childSelector.Parameters[0];
        var childExpr = _childSelector.Body;

        Expression nullCheck = typeof(TChild).IsValueType
            ? Expression.Property(childExpr, "HasValue")
            : Expression.NotEqual(childExpr, Expression.Constant(null));

        var valueExpr = Nullable.GetUnderlyingType(typeof(TChild)) != null
            ? Expression.Property(childExpr, "Value")
            : childExpr;

        var invokeSpec = Expression.Invoke(_childSpec.ToExpression(), valueExpr);
        var body = Expression.AndAlso(nullCheck, invokeSpec);

        return Expression.Lambda<Func<TParent, bool>>(body, parentParam);
    }
}