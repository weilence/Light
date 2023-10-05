using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Web.Identity;

public class IdentityService<T> where T : new()
{
    private readonly IIdentityStore<T>? _store;
    private static readonly AsyncLocal<T> Local = new();

    public IdentityService(IServiceProvider provider)
    {
        _store = provider.GetService<IIdentityStore<T>>();
    }

    public T User
    {
        get
        {
            var user = Local.Value;
            if (user != null)
            {
                return user;
            }

            if (_store != null)
            {
                user = _store.Get();
            }

            user ??= new T();
            Local.Value = user;
            return user;
        }
        set => Local.Value = value;
    }
}

public interface IIdentityStore<out T>
{
    T? Get();
}

public class HttpContextIdentityStore<T>: IIdentityStore<T>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Func<ClaimsPrincipal, T> _func;

    public HttpContextIdentityStore(IHttpContextAccessor httpContextAccessor, Func<ClaimsPrincipal, T> func)
    {
        _httpContextAccessor = httpContextAccessor;
        _func = func;
    }

    public T? Get()
    {
        var httpContextUser = _httpContextAccessor.HttpContext?.User;
        return httpContextUser == null ? default : _func(httpContextUser);
    }
}