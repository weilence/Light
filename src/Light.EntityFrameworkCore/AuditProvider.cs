namespace Light.EntityFrameworkCore;

public interface IAuditProvider<out T>
{
    T Audit { get; }
}