namespace Light.EntityFrameworkCore;

public interface ITenantProvider<out T>
{
    public T Tenant { get; }
}