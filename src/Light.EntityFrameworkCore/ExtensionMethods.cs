﻿using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Light.EntityFrameworkCore;

public static class ExtensionMethods
{
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
    private static Expression CreateTenantExpression<T>(ParameterExpression parameter, Expression tenantExpression)
    {
        var body = Expression.Equal(
            Expression.Call(typeof(EF), nameof(EF.Property), new[]
                {
                    typeof(T),
                }, parameter,
                Expression.Constant(nameof(ITenant<T>.Tenant))),
            tenantExpression
        );

        return body;
    }

    private static LambdaExpression CreateFilterExpression(ParameterExpression parameter, IEnumerable<Expression> expressions)
    {
        var joinExpression = expressions.Aggregate(Expression.AndAlso);
        return Expression.Lambda(joinExpression, parameter);
    }

    public static ModelBuilder AddFilter<T>(this ModelBuilder modelBuilder, Expression? tenantExpression = null)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var expressions = new List<Expression>();
            var parameter = Expression.Parameter(entityType.ClrType);

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                expressions.Add(CreateSoftDeleteExpression(parameter));
            }

            if (typeof(ITenant<T>).IsAssignableFrom(entityType.ClrType))
            {
                ArgumentNullException.ThrowIfNull(tenantExpression);

                expressions.Add(CreateTenantExpression<T>(parameter, tenantExpression));
                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ITenant<T>.Tenant));
            }

            if (expressions.Count > 0)
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(CreateFilterExpression(parameter, expressions));
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
            case EntityState.Deleted:
                if (e.Entry.Entity is ISoftDelete softDeleteEntity)
                {
                    e.Entry.State = EntityState.Modified;
                    softDeleteEntity.IsDelete = true;
                    if (e.Entry.Entity is IUpdateAt updateAtEntityDelete)
                    {
                        updateAtEntityDelete.UpdateAt = DateTime.UtcNow;
                    }

                    if (e.Entry.Entity is IUpdateBy<T> updateByEntityDelete)
                    {
                        updateByEntityDelete.UpdateBy = auditProvider.Audit;
                    }
                }

                break;
        }
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, int, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static (long, List<T>) ToPage<T>(this IQueryable<T> query, int page, int size)
    {
        if (query is not IOrderedQueryable<T>)
        {
            throw new Exception("ToPage must be used after OrderBy");
        }

        var total = query.LongCount();
        List<T> data;
        if (page == 0 && size == 0)
        {
            data = query.ToList();
        }
        else
        {
            data = query.Skip((page - 1) * size).Take(size).ToList();
        }

        return (total, data);
    }

    public static async Task<(long, List<T>)> ToPageAsync<T>(this IQueryable<T> query, int page, int size)
    {
        if (query is not IOrderedQueryable<T>)
        {
            throw new Exception("ToPage must be used after OrderBy");
        }

        var total = await query.LongCountAsync();
        List<T> data;
        if (page == 0 && size == 0)
        {
            data = await query.ToListAsync();
        }
        else
        {
            data = await query.Skip((page - 1) * size).Take(size).ToListAsync();
        }

        return (total, data);
    }
}