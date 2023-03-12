using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Web.Identity;

public class IdentityService<T>
{
    private readonly IIdentityGetter _getter;
    private readonly IdentityUserParser<T> _parser;
    private readonly AsyncLocal<T?> _user = new();

    public IdentityService(IIdentityGetter getter, IdentityUserParser<T> parser)
    {
        _getter = getter;
        _parser = parser;
    }

    public T? User
    {
        get
        {
            var user = _user.Value;
            if (user != null)
            {
                return user;
            }

            var principal = _getter.Get();
            if (principal != null)
            {
                user = _parser.Parse(principal);
            }

            _user.Value = user;
            return user;
        }
    }
}

public interface IIdentityGetter
{
    ClaimsPrincipal? Get();
}

public class HttpContextIdentityGetter : IIdentityGetter
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextIdentityGetter(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public ClaimsPrincipal? Get() => _accessor.HttpContext?.User;
}

public class IdentityUserParser<T>
{
    private readonly Func<ClaimsPrincipal, T> _parser;

    public IdentityUserParser(Func<ClaimsPrincipal, T> parser)
    {
        _parser = parser;
    }

    public T Parse(ClaimsPrincipal principal) => _parser(principal);
}