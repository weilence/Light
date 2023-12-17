using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using FreeSql;
using FreeSql.Aop;
using FreeSql.DataAnnotations;
using FreeSql.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Light.FreeSql
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddFreeSql(this IServiceCollection services,
            FreeSqlConfig config)
        {
            services.AddSingleton(provider =>
            {
                var freeSql = Build(provider, config);

                Aop(freeSql);

                Filter(provider, freeSql);

                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return freeSql;
            });

            return services;
        }

        private static void Filter(IServiceProvider provider, IFreeSql freeSql)
        {
            var auditProvider = provider.GetService<IAuditProvider>();
            var hasAudit = auditProvider != null;

            var tenantProvider = provider.GetService<ITenantProvider>();
            var hasTenant = tenantProvider != null;

            freeSql.Aop.AuditValue += (s, e) =>
            {
                if (e.AuditValueType == AuditValueType.Insert || e.AuditValueType == AuditValueType.InsertOrUpdate)
                {
                    switch (e.Column.CsName)
                    {
                        case nameof(ITenant<object>.Tenant) when hasTenant:
                            e.Value = tenantProvider!.Tenant;
                            break;
                        case nameof(ICreateBy<object>.CreateBy) when hasAudit:
                            e.Value = auditProvider!.UserId;
                            break;
                        case nameof(IUpdateBy<object>.UpdateBy) when hasAudit:
                            e.Value = auditProvider!.UserId;
                            break;
                    }
                }
                else if (e.AuditValueType == AuditValueType.Update)
                {
                    switch (e.Column.CsName)
                    {
                        case nameof(IUpdateBy<object>.UpdateBy) when hasAudit:
                            e.Value = auditProvider!.UserId;
                            break;
                    }
                }
            };

            if (hasTenant)
            {
                freeSql.GlobalFilter.Apply<ITenant<object>>(nameof(ITenant<object>.Tenant),
                    m => m.Tenant.Equals(tenantProvider.Tenant));
            }

            freeSql.GlobalFilter.Apply<ISoftDelete>(nameof(ISoftDelete.IsDelete), m => m.IsDelete == false);
        }

        private static void Aop(IFreeSql freeSql)
        {
            freeSql.Aop.ConfigEntity += (sender, args) =>
            {
                if (args.EntityType.GetInterface(typeof(IId<>).Name) != null)
                {
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(IId<object>.Id)).IsPrimary(true).IsIdentity(true)
                    );
                }

                if (args.EntityType.GetInterface(typeof(ICreateBy<>).Name) != null)
                {
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(ICreateBy<object>.CreateBy)).CanUpdate(false)
                    );
                }

                if (args.EntityType.GetInterface(nameof(ICreateAt)) != null)
                {
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(ICreateAt.CreateAt)).ServerTime(DateTimeKind.Local)
                            .CanUpdate(false)
                    );
                }

                if (args.EntityType.GetInterface(nameof(IUpdateAt)) != null)
                {
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(IUpdateAt.UpdateAt)).ServerTime(DateTimeKind.Local)
                    );
                }

                var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(args.EntityType);
                var isTenant = args.EntityType.GetInterface(typeof(ITenant<>).Name) != null;
                const string softDeleteName = nameof(ISoftDelete.IsDelete);
                const string tenantName = nameof(ITenant<object>.Tenant);
                switch (isSoftDelete, isTenant)
                {
                    case (true, true):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                        {
                            table.Index($"{{tableName}}_idx_{tenantName}_{softDeleteName}",
                                $"{tenantName}, {softDeleteName}");
                        });
                    }
                        break;
                    case (true, false):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType,
                            table => { table.Index($"{{tableName}}_idx_{softDeleteName}", softDeleteName); });
                    }
                        break;
                    case (false, true):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType,
                            table => { table.Index($"{{tableName}}_idx_{tenantName}", tenantName); });
                    }
                        break;
                }
            };
        }

        private static IFreeSql Build(IServiceProvider provider, FreeSqlConfig config)
        {
            var env = provider.GetService<IHostEnvironment>();
            var builder = new FreeSqlBuilder()
                .UseConnectionString(config.DataType, config.ConnectionString)
                .UseLazyLoading(true);

            if (env.IsDevelopment())
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(nameof(FreeSql));
                builder = builder.UseAutoSyncStructure(true);
                builder = builder.UseNoneCommandParameter(true)
                    .UseMonitorCommand(m => logger.LogDebug("{Message}", m.CommandText));
            }

            config.BuilderSetup?.Invoke(provider, builder);

            var freeSql = builder.Build();
            return freeSql;
        }

        static ThreadLocal<ExpressionCallContext> context = new ThreadLocal<ExpressionCallContext>();

        [ExpressionCall]
        public static bool FullTextMatch(this string str, string matchStr, bool isBoolMode = false)
        {
            var up = context.Value;
            if (up.DataType == DataType.MySql)
            {
                up.Result =
                    $"match({up.ParsedContent["str"]}) against({up.ParsedContent["matchStr"]}{(isBoolMode ? " in boolean mode" : "")})";
                return true;
            }

            return false;
        }
    }
}