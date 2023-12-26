using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Rougamo.Context;

namespace Light.EntityFrameworkCore
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionalAttribute : Rougamo.MoAttribute
    {
        private static readonly AsyncLocal<DbContext> _dbContext = new();
        private static readonly AsyncLocal<DbContextTransactionCounter> _trans = new();
        public static void SetServiceProvider(DbContext dbContext) => _dbContext.Value = dbContext;

        public override void OnEntry(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_dbContext.Value);

            if (_trans.Value != null)
            {
                _trans.Value.Count++;
                return;
            }

            _trans.Value = new DbContextTransactionCounter()
            {
                Count = 1, Transaction = _dbContext.Value.Database.BeginTransaction(),
            };
        }

        public override void OnException(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_trans.Value);

            _trans.Value.Count--;
            if (_trans.Value.Count == 0)
            {
                _trans.Value.Transaction.Rollback();
            }
        }

        public override void OnSuccess(MethodContext context)
        {
            ArgumentNullException.ThrowIfNull(_trans.Value);

            _trans.Value.Count--;
            if (_trans.Value.Count == 0)
            {
                _trans.Value.Transaction.Commit();
            }
        }
    }

    public class DbContextTransactionCounter
    {
        public IDbContextTransaction Transaction { get; set; }
        public uint Count { get; set; }
    }
}