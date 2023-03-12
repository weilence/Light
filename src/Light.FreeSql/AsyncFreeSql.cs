using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using FreeSql;
using FreeSql.Internal;
using FreeSql.Internal.ObjectPool;

namespace Light.FreeSql
{
    internal class AsyncFreeSql : IFreeSql
    {
        private static AsyncLocal<TransactionContext> _local = new AsyncLocal<TransactionContext>();

        private static TransactionContext Context => _local.Value ?? (_local.Value = new TransactionContext());

        private IFreeSql _originalFsql;

        public AsyncFreeSql(IFreeSql fsql)
        {
            _originalFsql = fsql;
        }

        public IAdo Ado => _originalFsql.Ado;
        public IAop Aop => _originalFsql.Aop;
        public ICodeFirst CodeFirst => _originalFsql.CodeFirst;
        public IDbFirst DbFirst => _originalFsql.DbFirst;
        public GlobalFilter GlobalFilter => _originalFsql.GlobalFilter;

        public ISelect<T1> Select<T1>() where T1 : class =>
            Context.Transaction == null
                ? _originalFsql.Select<T1>()
                : _originalFsql.Select<T1>().WithTransaction(Context.Transaction);

        public ISelect<T1> Select<T1>(object dywhere) where T1 : class => Select<T1>().WhereDynamic(dywhere);

        public IDelete<T1> Delete<T1>() where T1 : class =>
            Context.Transaction == null
                ? _originalFsql.Delete<T1>()
                : _originalFsql.Delete<T1>().WithTransaction(Context.Transaction);

        public IDelete<T1> Delete<T1>(object dywhere) where T1 : class => Delete<T1>().WhereDynamic(dywhere);

        public IUpdate<T1> Update<T1>() where T1 : class =>
            Context.Transaction == null
                ? _originalFsql.Update<T1>()
                : _originalFsql.Update<T1>().WithTransaction(Context.Transaction);

        public IUpdate<T1> Update<T1>(object dywhere) where T1 : class => Update<T1>().WhereDynamic(dywhere);

        public IInsert<T1> Insert<T1>() where T1 : class =>
            Context.Transaction == null
                ? _originalFsql.Insert<T1>()
                : _originalFsql.Insert<T1>().WithTransaction(Context.Transaction);

        public IInsert<T1> Insert<T1>(T1 source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(T1[] source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(List<T1> source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(IEnumerable<T1> source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsertOrUpdate<T1> InsertOrUpdate<T1>() where T1 : class =>
            Context.Transaction == null
                ? _originalFsql.InsertOrUpdate<T1>()
                : _originalFsql.InsertOrUpdate<T1>().WithTransaction(Context.Transaction);

        public void Dispose() => TransactionCommit(true);

        public void Transaction(Action handler) => Transaction(IsolationLevel.Unspecified, handler);

        public void Transaction(IsolationLevel isolationLevel, Action handler)
        {
            Begin(isolationLevel);
            try
            {
                handler();
            }
            catch
            {
                Rollback();
                throw;
            }

            Commit();
        }

        public void Begin(IsolationLevel? isolationLevel)
        {
            if (Context.Transaction != null)
            {
                Context.TransactionCount++;
                return; //事务已开启
            }

            try
            {
                if (Context.Connection == null) Context.Connection = _originalFsql.Ado.MasterPool.Get();
                Context.Transaction = isolationLevel == null
                    ? Context.Connection.Value.BeginTransaction()
                    : Context.Connection.Value.BeginTransaction(isolationLevel.Value);
                Context.TransactionCount = 1;
            }
            catch
            {
                TransactionCommit(false);
                throw;
            }
        }

        public void Commit() => TransactionCommit(true);
        public void Rollback() => TransactionCommit(false);

        private void TransactionCommit(bool iscommit)
        {
            if (Context.Transaction == null) return;
            Context.TransactionCount--;
            try
            {
                if (iscommit == false) Context.Transaction.Rollback();
                else if (Context.TransactionCount <= 0) Context.Transaction.Commit();
            }
            finally
            {
                if (iscommit == false || Context.TransactionCount <= 0)
                {
                    _originalFsql.Ado.MasterPool.Return(Context.Connection);
                    Context.Connection = null;
                    Context.Transaction = null;
                }
            }
        }
    }

    class TransactionContext
    {
        public int TransactionCount;
        public DbTransaction Transaction;
        public Object<DbConnection> Connection;
    }
}