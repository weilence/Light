namespace Light.EntityFrameworkCore;

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

public interface ISoftDelete
{
    bool IsDelete { get; set; }
}

public interface ITenant<T>
{
    T Tenant { get; set; }
}

public interface IId<T>
{
    T Id { get; set; }
}

public class BaseEntity<T> : IId<T>, ICreateAt, ICreateBy<T>, IUpdateAt, IUpdateBy<T>, ISoftDelete
{
    public T Id { get; set; } = default!;
    public DateTime CreateAt { get; set; }
    public T CreateBy { get; set; } = default!;
    public DateTime UpdateAt { get; set; }
    public T UpdateBy { get; set; } = default!;
    public bool IsDelete { get; set; }
}

public class BaseTenantEntity<T> : BaseEntity<T>, ITenant<T>
{
    public T Tenant { get; set; } = default!;
}