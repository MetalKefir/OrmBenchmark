using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class AnySpecification<TParent, TChild> : Specification<TParent>
{
    private readonly Expression<Func<TParent, IEnumerable<TChild>>> _childSelector;
    private readonly Specification<TChild> _childSpec;

    public AnySpecification(
        Specification<TChild> childSpec,
        Expression<Func<TParent, IEnumerable<TChild>>> childSelector)
    {
        _childSelector = childSelector;
        _childSpec = childSpec;
    }

    public override Expression<Func<TParent, bool>> ToExpression()
    {
        var param = _childSelector.Parameters[0];
        var collectionBody = Expression.Invoke(_childSelector, param);

        var temp1 = Expression.Call(typeof(Queryable), nameof(Queryable.AsQueryable), new[] { typeof(TChild), },
            collectionBody);

        var temp2 = Expression.Call(typeof(Queryable), nameof(Queryable.Any), new[] { typeof(TChild), },
            temp1, _childSpec.ToExpression());

        return Expression.Lambda<Func<TParent, bool>>(temp2, param);
    }
}