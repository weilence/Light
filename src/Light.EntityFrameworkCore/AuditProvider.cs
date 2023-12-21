namespace Light.EntityFrameworkCore;

public interface IAuditProvider<out T>
{
    public T Audit { get; }
}