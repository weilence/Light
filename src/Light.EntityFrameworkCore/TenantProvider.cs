namespace Light.EntityFrameworkCore;

public interface ITenantProvider<out T>
{
    T Tenant { get; }
}