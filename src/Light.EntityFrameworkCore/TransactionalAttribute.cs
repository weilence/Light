using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Rougamo.Context;

namespace Light.EntityFrameworkCore
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionalAttribute : Rougamo.MoAttribute
    {
        private static readonly AsyncLocal<IServiceProvider> _serviceProvider = new();
        private static readonly AsyncLocal<IDbContextTransaction> _trans = new();
        public static void SetServiceProvider(IServiceProvider serviceProvider) => _serviceProvider.Value = serviceProvider;

        public override void OnEntry(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_serviceProvider.Value);

            var dbContext = _serviceProvider.Value.GetRequiredService<DbContext>();
            _trans.Value = dbContext.Database.BeginTransaction();
        }

        public override void OnException(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_trans.Value);

            _trans.Value.Rollback();
        }

        public override void OnSuccess(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_trans.Value);

            _trans.Value.Commit();
        }
    }
}