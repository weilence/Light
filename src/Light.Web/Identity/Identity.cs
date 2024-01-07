using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Light.Web.Identity;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddJwtIdentity<T>(this IServiceCollection services, string jwtSecret,
        Func<ClaimsPrincipal, T> func) where T : new()
    {
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = false,
            };
        });

        services.AddHttpContextAccessor();
        return AddIdentity(services, provider =>
        {
            var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            return new HttpContextIdentityStore<T>(httpContextAccessor, func);
        });
    }

    public static IServiceCollection AddIdentity<T>(this IServiceCollection services,
        Func<IServiceProvider, IIdentityStore<T>>? func = null) where T : new()
    {
        services.AddSingleton<IdentityService<T>>();
        if (func != null)
        {
            services.AddSingleton(func);
        }

        return services;
    }
}