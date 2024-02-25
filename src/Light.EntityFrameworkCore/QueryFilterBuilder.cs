using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Light.EntityFrameworkCore;

public class QueryFilterBuilder
{
    private readonly ModelBuilder _modelBuilder;

    private readonly List<(string name, Func<IMutableEntityType, bool> skip, Func<ParameterExpression, Expression> body, Func<IMutableEntityType, Expression>? disable)>
        _filters = new();

    public QueryFilterBuilder(ModelBuilder modelBuilder)
    {
        _modelBuilder = modelBuilder;
    }

    private void AddFilter(string name, Func<IMutableEntityType, bool> func, Func<ParameterExpression, Expression> body,
        Func<IMutableEntityType, Expression>? disable = null)
    {
        _filters.Add((name, func, body, disable));
    }

    private Expression DisableFilter(Expression disable, Expression body)
    {
        // flag || body
        return Expression.OrElse(disable, body);
    }

    public void AddFilter<T>(Func<ParameterExpression, Expression> body, Func<IMutableEntityType, Expression>? disable = null)
    {
        var type = typeof(T);
        AddFilter(type.Name, entityType => type.IsAssignableFrom(entityType.ClrType), body, disable);
    }

    public void Build()
    {
        foreach (var entityType in _modelBuilder.Model.GetEntityTypes())
        {
            var parameter = Expression.Parameter(entityType.ClrType);
            var expressions = new List<Expression>();
            foreach (var (name, conditionFunc, expression, disable) in _filters)
            {
                if (!conditionFunc(entityType))
                {
                    continue;
                }

                var body = expression(parameter);
                expressions.Add(disable == null ? body : DisableFilter(disable(entityType), body));
            }

            if (expressions.Count == 0)
            {
                continue;
            }

            var aggregateExpression = expressions.Aggregate(Expression.AndAlso);
            var lambdaExpression = Expression.Lambda(aggregateExpression, parameter);
            _modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambdaExpression);
        }
    }

    public void AddSoftDeleteFilter(Func<IMutableEntityType, Expression>? disable = null)
    {
        // EF.Property<bool>(e, nameof(ISoftDelete.IsDelete)) == false
        var body = (ParameterExpression parameter) => Expression.Equal(
            Expression.Call(typeof(EF), nameof(EF.Property), new[]
                {
                    typeof(bool),
                }, parameter,
                Expression.Constant(nameof(ISoftDelete.IsDelete))),
            Expression.Constant(false));

        AddFilter<ISoftDelete>(body, disable);
    }

    public void AddTenantFilter<T>(Expression tenant, Func<IMutableEntityType, Expression>? disable = null)
    {
        // EF.Property<T>(e, nameof(ITenant<T>.Tenant)) == tenantProvider.Tenant
        var body = (ParameterExpression parameter) => Expression.Equal(
            Expression.Call(typeof(EF), nameof(EF.Property), new[]
                {
                    typeof(T),
                }, parameter,
                Expression.Constant(nameof(ITenant<T>.Tenant))),
            tenant
        );

        AddFilter<ITenant<T>>(body, disable);
    }
}