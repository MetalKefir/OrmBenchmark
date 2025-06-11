using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public sealed class DerivedToBaseSpecification<TBase, TDerived>
    : Specification<TBase>
    where TDerived : TBase
{
    private readonly Specification<TDerived> _specification;

    public DerivedToBaseSpecification(Specification<TDerived> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<TBase, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TBase));

        var isDerived = Expression.TypeIs(param, typeof(TDerived));

        var cast = Expression.TypeAs(param, typeof(TDerived));

        var invoke = Expression.Invoke(_specification.ToExpression(), Expression.Convert(cast, typeof(TDerived)));

        var body = Expression.AndAlso(isDerived, invoke);

        return Expression.Lambda<Func<TBase, bool>>(body, param);
    }
}