using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Light.EntityFrameworkCore;

public static class Extensions
{
    private static ConcurrentDictionary<Type, (ParameterExpression, Dictionary<string, Expression>)> _dicExpression = new();

    // EF.Property<bool>(e, nameof(ISoftDelete.IsDelete)) == false
    private static Expression CreateSoftDeleteExpression(ParameterExpression parameter)
    {
        var body = Expression.Equal(
            Expression.Call(typeof(EF), nameof(EF.Property), new[]
                {
                    typeof(bool),
                }, parameter,
                Expression.Constant(nameof(ISoftDelete.IsDelete))),
            Expression.Constant(false));

        return body;
    }

    // EF.Property<T>(e, nameof(ITenant<T>.Tenant)) == tenantProvider.Tenant
    private static Expression CreateTenantExpression<T>(ParameterExpression parameter, ITenantProvider<T> tenantProvider)
    {
        var body = Expression.Equal(
            Expression.Call(typeof(EF), nameof(EF.Property), new[]
                {
                    typeof(T),
                }, parameter,
                Expression.Constant(nameof(ITenant<T>.Tenant))),
            Expression.Constant(tenantProvider.Tenant));

        return body;
    }

    private static LambdaExpression CreateFilterExpression(ParameterExpression parameter, IEnumerable<Expression> expressions)
    {
        var joinExpression = expressions.Aggregate(Expression.AndAlso);

        return Expression.Lambda(joinExpression, parameter);
    }

    public static ModelBuilder AddFilter<T>(this ModelBuilder modelBuilder, ITenantProvider<T>? tenantProvider = null)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dicExpression = new Dictionary<string, Expression>();
            var parameter = Expression.Parameter(entityType.ClrType);

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                dicExpression[nameof(ISoftDelete)] = (CreateSoftDeleteExpression(parameter));
            }

            if (typeof(ITenant<T>).IsAssignableFrom(entityType.ClrType))
            {
                ArgumentNullException.ThrowIfNull(tenantProvider);

                dicExpression[nameof(ITenant<T>)] = CreateTenantExpression(parameter, tenantProvider);
            }

            if (dicExpression.Count > 0)
            {
                _dicExpression.TryAdd(entityType.ClrType, (parameter, dicExpression));

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(CreateFilterExpression(parameter, dicExpression.Values));
            }
        }

        return modelBuilder;
    }

    public static ChangeTracker Audit<T>(this ChangeTracker changeTracker, IAuditProvider<T> auditProvider, ITenantProvider<T> tenantProvider)
    {
        changeTracker.StateChanged += (o, e) => Audit(o, e, auditProvider, tenantProvider);
        changeTracker.Tracked += (o, e) => Audit(o, e, auditProvider, tenantProvider);

        return changeTracker;
    }

    private static void Audit<T>(object? sender, EntityEntryEventArgs e, IAuditProvider<T> auditProvider, ITenantProvider<T> tenantProvider)
    {
        switch (e.Entry.State)
        {
            case EntityState.Added:
                if (e.Entry.Entity is ICreateAt createAtEntity)
                {
                    createAtEntity.CreateAt = DateTime.UtcNow;
                }

                if (e.Entry.Entity is ICreateBy<T> createByEntity)
                {
                    createByEntity.CreateBy = auditProvider.Audit;
                }

                if (e.Entry.Entity is ITenant<T> tenantEntity)
                {
                    tenantEntity.Tenant = tenantProvider.Tenant;
                }

                break;
            case EntityState.Modified:
                if (e.Entry.Entity is IUpdateAt updateAtEntity)
                {
                    updateAtEntity.UpdateAt = DateTime.UtcNow;
                }

                if (e.Entry.Entity is IUpdateBy<T> updateByEntity)
                {
                    updateByEntity.UpdateBy = auditProvider.Audit;
                }

                break;
        }
    }

    public static IQueryable<T> DisableTenant<T>(this IQueryable<T> query) where T : class
    {
        var (parameter, expressions) = _dicExpression[typeof(T)];

        return query.IgnoreQueryFilters().Where((Expression<Func<T, bool>>)CreateFilterExpression(parameter, new[]
        {
            expressions[nameof(ISoftDelete)],
        }));
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, int, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static (long, List<T>) ToPage<T>(this IOrderedQueryable<T> query, int page, int size)
    {
        var total = query.LongCount();
        var data = query.Skip((page - 1) * size).Take(size).ToList();

        return (total, data);
    }
}