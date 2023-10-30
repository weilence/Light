using System;
using System.Threading;
using FreeSql;
using FreeSql.Aop;
using FreeSql.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Light.FreeSql
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddFreeSql<TAudit, TTenant>(this IServiceCollection services,
            FreeSqlConfig<TAudit, TTenant> config)
            where TAudit : IEquatable<TAudit>
            where TTenant : IEquatable<TTenant>
        {
            services.AddSingleton(provider =>
            {
                var freeSql = Build(provider, config);

                Aop<TAudit, TTenant>(freeSql);

                Filter(provider, freeSql, config);

                freeSql.UseJsonMap();

                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return freeSql;
            });

            return services;
        }

        private static void Filter<TAudit, TTenant>(IServiceProvider provider, IFreeSql freeSql,
            FreeSqlConfig<TAudit, TTenant> config)
            where TAudit : IEquatable<TAudit> where TTenant : IEquatable<TTenant>
        {
            var hasTenant = config.ResolveTenant != null;
            var hasAudit = config.ResolveAudit != null;

            freeSql.Aop.AuditValue += (s, e) =>
            {
                if (e.AuditValueType == AuditValueType.Insert || e.AuditValueType == AuditValueType.InsertOrUpdate)
                {
                    switch (e.Column.CsName)
                    {
                        case nameof(ITenant<TTenant>.Tenant) when hasTenant:
                            e.Value = config.ResolveTenant.Invoke(provider);
                            break;
                        case nameof(ICreateBy<TAudit>.CreateBy) when hasAudit:
                            e.Value = config.ResolveAudit.Invoke(provider);
                            break;
                        case nameof(IUpdateBy<TAudit>.UpdateBy) when hasAudit:
                            e.Value = config.ResolveAudit.Invoke(provider);
                            break;
                    }
                }
                else if (e.AuditValueType == AuditValueType.Update)
                {
                    switch (e.Column.CsName)
                    {
                        case nameof(IUpdateBy<TAudit>.UpdateBy) when hasAudit:
                            e.Value = config.ResolveAudit.Invoke(provider);
                            break;
                    }
                }
            };

            if (hasTenant)
            {
                freeSql.GlobalFilter.Apply<ITenant<TTenant>>(nameof(ITenant<TTenant>.Tenant),
                    m => m.Tenant.Equals(config.ResolveTenant.Invoke(provider)));
            }

            freeSql.GlobalFilter.Apply<ISoftDelete>(nameof(ISoftDelete.IsDelete), m => m.IsDelete == false);
        }

        private static void Aop<TAudit, TTenant>(IFreeSql freeSql)
            where TAudit : IEquatable<TAudit> where TTenant : IEquatable<TTenant>
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
                var tableName = args.ModifyResult.Name;
                switch (isSoftDelete, isTenant)
                {
                    case (true, true):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                        {
                            table.Index($"idx_{tableName}_{tenantName}_{softDeleteName}",
                                $"{tenantName}, {softDeleteName}");
                        });
                    }
                        break;
                    case (true, false):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType,
                            table => { table.Index($"idx_{tableName}_{softDeleteName}", softDeleteName); });
                    }
                        break;
                    case (false, true):
                    {
                        freeSql.CodeFirst.ConfigEntity(args.EntityType,
                            table => { table.Index($"idx_{tableName}_{tenantName}", tenantName); });
                    }
                        break;
                }
            };
        }

        private static IFreeSql Build<TAudit, TTenant>(IServiceProvider provider, FreeSqlConfig<TAudit, TTenant> config)
            where TAudit : IEquatable<TAudit> where TTenant : IEquatable<TTenant>
        {
            var env = provider.GetService<IHostEnvironment>();
            var builder = new FreeSqlBuilder()
                .UseConnectionString(config.DataType, config.ConnectionString)
                .UseLazyLoading(true);

            if (env.IsDevelopment())
            {
                var logger = provider.GetRequiredService<ILogger<FreeSqlConfig>>();
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