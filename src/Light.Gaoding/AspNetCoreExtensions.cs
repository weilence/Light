using Microsoft.Extensions.DependencyInjection;

namespace Light.Gaoding
{
    public static class AspNetCoreExtensions
    {
        public static IServiceCollection AddGaoding(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddOptions<GaodingConfig>().BindConfiguration("Gaoding");
            services.AddSingleton<GaodingService>();
            return services;
        }
    }
}