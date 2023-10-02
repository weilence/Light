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
            services.AddSingleton(provider =>
            {
                var freeSql = Build(provider, config);
                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return freeSql;
            });

            return services;
        }

        public static IServiceCollection AddFreeSql<TAudit, TTenant>(this IServiceCollection services,
            FreeSqlConfig<TAudit, TTenant> config)
            where TAudit : IEquatable<TAudit>
            where TTenant : IEquatable<TTenant>
        {
            services.AddSingleton(provider =>
            {
                var freeSql = Build(provider, config);

                if (config.ResolveTenant == null)
                {
                    throw new ArgumentNullException(nameof(config.ResolveTenant));
                }

                freeSql.Aop.AuditValue += (s, e) =>
                {
                    if (e.AuditValueType == AuditValueType.Insert)
                        switch (e.Column.CsName)
                        {
                            case nameof(ITenant<TTenant>.Tenant):
                                e.Value = config.ResolveTenant.Invoke(provider);
                                break;
                            case nameof(ICreateBy<TAudit>.CreateBy):
                                e.Value = config.ResolveAudit.Invoke(provider);
                                break;
                            case nameof(IUpdateBy<TAudit>.UpdateBy):
                                e.Value = config.ResolveAudit.Invoke(provider);
                                break;
                        }
                };
                freeSql.GlobalFilter.Apply<ITenant<TTenant>>(nameof(ITenant<object>.Tenant),
                    m => m.Tenant.Equals(config.ResolveTenant.Invoke(provider)));

                config.FreeSqlSetup?.Invoke(provider, freeSql);
                return freeSql;
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
                if (args.EntityType.GetInterface(typeof(IId<>).Name) != null)
                {
                    freeSql.CodeFirst.ConfigEntity(args.EntityType,
                        table => table.Property(nameof(IId<object>.Id)).IsPrimary(true).IsIdentity(true).Position(1)
                    );
                }

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

                if (args.EntityType.GetInterface(typeof(ITenant<>).Name) != null)
                {
                    var position = index;
                    index--;
                    const string name = nameof(ITenant<object>.Tenant);
                    freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                    {
                        table.Index("idx_" + name, name);
                        table.Property(name).Position(position);
                    });
                }

                if (args.EntityType.GetInterface(typeof(IUpdateBy<>).Name) != null)
                {
                    var position = index;
                    index--;
                    const string name = nameof(IUpdateBy<object>.UpdateBy);
                    freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                    {
                        table.Index("idx_" + name, name);
                        table.Property(name).Position(position);
                    });
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

                if (args.EntityType.GetInterface(typeof(ICreateBy<>).Name) != null)
                {
                    var position = index;
                    index--;
                    const string name = nameof(ICreateBy<object>.CreateBy);
                    freeSql.CodeFirst.ConfigEntity(args.EntityType, table =>
                    {
                        table.Index("idx_" + name, name);
                        table.Property(name).Position(position);
                    });
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