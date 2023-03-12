using System;
using System.Diagnostics;
using System.Threading;
using FreeSql;
using FreeSql.Aop;
using FreeSql.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Light.FreeSql
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddFreeSql(this IServiceCollection services, FreeSqlConfig config)
        {
            services.AddSingleton<IFreeSql>(provider =>
            {
                var freeSql = Build(provider, config);
                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return new AsyncFreeSql(freeSql);
            });

            return services;
        }

        public static IServiceCollection AddFreeSql<T>(this IServiceCollection services, FreeSqlConfig<T> config)
            where T : IEquatable<T>
        {
            services.AddSingleton<IFreeSql>(provider =>
            {
                var freeSql = Build(provider, config);

                if (config.ResolveTenant == null)
                {
                    throw new ArgumentNullException(nameof(config.ResolveTenant));
                }

                freeSql.Aop.AuditValue += (s, e) =>
                {
                    if (e.AuditValueType != AuditValueType.Insert) return;

                    switch (e.Column.CsName)
                    {
                        case nameof(ITenant<T>.Tenant) when e.Value == null:
                            e.Value = config.ResolveTenant.Invoke(provider);
                            break;
                    }
                };
                freeSql.GlobalFilter.Apply<ITenant<T>>(nameof(ITenant<object>.Tenant),
                    m => m.Tenant.Equals(config.ResolveTenant.Invoke(provider)));

                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return new AsyncFreeSql(freeSql);
            });

            return services;
        }

        private static IFreeSql Build(IServiceProvider provider, FreeSqlConfig config)
        {
            var env = provider.GetService<IHostEnvironment>();
            var builder = new FreeSqlBuilder()
                .UseConnectionString(config.DataType, config.ConnectionString)
                .UseLazyLoading(true);

            if (env.IsDevelopment())
            {
                builder = builder.UseAutoSyncStructure(true);
                builder = builder.UseNoneCommandParameter(true)
                    .UseMonitorCommand(m => Trace.WriteLine(m.CommandText));
            }

            config.BuilderSetup?.Invoke(provider, builder);

            var freeSql = builder.Build();

            freeSql.Aop.ConfigEntity += (sender, args) =>
            {
                short index = -1;

                if (typeof(ISoftDelete).IsAssignableFrom(args.EntityType))
                {
                    var position = index;
                    index--;
                    freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                    {
                        table.Index("idx_" + nameof(ISoftDelete.IsDelete), nameof(ISoftDelete.IsDelete));
                        table.Property(nameof(ISoftDelete.IsDelete)).Position(position);
                    });
                }

                if (args.EntityType.GetInterface(typeof(IId<>).Name) != null)
                {
                    var position = index;
                    index--;
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(IId<object>.Id)).IsPrimary(true).IsIdentity(true).Position(1)
                    );
                }

                if (args.EntityType.GetInterface(nameof(ICreateAt)) != null)
                {
                    var position = index;
                    index--;
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table =>
                        {
                            table.Property(nameof(ICreateAt.CreateAt)).CanUpdate(false).Position(position)
                                .ServerTime(DateTimeKind.Local);
                        }
                    );
                }

                if (args.EntityType.GetInterface(nameof(IUpdateAt)) != null)
                {
                    var position = index;
                    index--;
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table =>
                        {
                            table.Property(nameof(IUpdateAt.UpdateAt)).Position(position)
                                .ServerTime(DateTimeKind.Local);
                        }
                    );
                }

                if (args.EntityType.GetInterface(typeof(ITenant<>).Name) != null)
                {
                    var position = index;
                    index--;
                    freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                    {
                        table.Index("idx_" + nameof(ITenant<object>.Tenant), nameof(ITenant<object>.Tenant));
                        table.Property(nameof(ITenant<object>.Tenant)).Position(position);
                    });
                }
            };

            freeSql.UseJsonMap();
            freeSql.GlobalFilter.Apply<ISoftDelete>(nameof(ISoftDelete.IsDelete), m => m.IsDelete == false);

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