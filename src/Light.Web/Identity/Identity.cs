using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Light.Web.Identity;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddJwtIdentity<T>(this IServiceCollection services, string jwtSecret,
        Func<ClaimsPrincipal, T> parser)
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
        services.AddSingleton(new IdentityUserParser<T>(parser));
        services.AddSingleton<IIdentityGetter, HttpContextIdentityGetter>();
        services.AddSingleton<IdentityService<T>>();

        return services;
    }
}