using System;

namespace Light.FreeSql
{
    public interface ISoftDelete
    {
        bool IsDelete { get; set; }
    }

    public interface ICreateAt
    {
        DateTime CreateAt { get; set; }
    }

    public interface ICreateBy<T>
    {
        T CreateBy { get; set; }
    }

    public interface IUpdateAt
    {
        DateTime UpdateAt { get; set; }
    }

    public interface IUpdateBy<T>
    {
        T UpdateBy { get; set; }
    }

    public interface IId<T>
    {
        T Id { get; set; }
    }

    public interface ITenant<T>
    {
        T Tenant { get; set; }
    }
}