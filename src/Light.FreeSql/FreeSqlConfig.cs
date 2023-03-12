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

    public class FreeSqlConfig<T> : FreeSqlConfig where T : IEquatable<T>
    {
        public Func<IServiceProvider, T> ResolveTenant { get; set; }
    }
}