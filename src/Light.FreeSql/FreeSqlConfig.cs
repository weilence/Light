using System;
using FreeSql;

namespace Light.FreeSql
{
    public class FreeSqlConfig
    {
        public DataType DataType { get; set; }

        public string ConnectionString { get; set; }

        public Action<IServiceProvider, FreeSqlBuilder> BuilderSetup { get; set; }

        public Action<IServiceProvider, IFreeSql> FreeSqlSetup { get; set; }
    }

    public class FreeSqlConfig<TAudit, TTenant> : FreeSqlConfig
        where TAudit : IEquatable<TAudit>
        where TTenant : IEquatable<TTenant>
    {
        public Func<IServiceProvider, TAudit> ResolveAudit { get; set; }
        public Func<IServiceProvider, TTenant> ResolveTenant { get; set; }
    }
}