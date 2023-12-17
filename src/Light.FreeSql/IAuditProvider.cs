namespace Light.FreeSql
{
    public interface IAuditProvider
    {
        object UserId { get; }
    }

    public interface ITenantProvider
    {
        object Tenant { get; }
    }
}