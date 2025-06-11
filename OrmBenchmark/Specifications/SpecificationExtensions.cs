using System.Linq.Expressions;

namespace OrmBenchmark.Specifications;

public static class SpecificationExtensions
{
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> entityCollection,
        IEnumerable<Specification<T>> filters)
    {
        return filters.Aggregate(entityCollection, (current, filter) => current.Where(filter.ToExpression()));
    }

    public static IEnumerable<T> ApplyFilters<T>(
        this IEnumerable<T> entityCollection,
        IEnumerable<Specification<T>> filters)
    {
        return filters.Aggregate(entityCollection, (current, filter) => current.Where(filter.IsSatisfiedBy));
    }

    public static Specification<TParent> Compose<TParent, TChild>(
        this Specification<TChild> childSpec,
        Expression<Func<TParent, TChild?>> selector
    ) =>
        new ComposedSpecification<TParent, TChild>(childSpec, selector);

    public static Specification<TParent> Any<TParent, TChild>(
        this Specification<TChild> childSpec,
        Expression<Func<TParent, IEnumerable<TChild>>> selector
    ) =>
        new AnySpecification<TParent, TChild>(childSpec, selector);

    public static Specification<TBase> AsBase<TBase, TDerived>(
        this Specification<TDerived> specification
    )
        where TDerived : TBase =>
        new DerivedToBaseSpecification<TBase, TDerived>(specification);
}