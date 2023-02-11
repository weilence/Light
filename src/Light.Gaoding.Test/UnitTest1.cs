using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.Gaoding.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var accessKey = "";
            var secretKey = "";
            var appId = "";

            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Gaoding:AccessKey"] = accessKey,
                        ["Gaoding:SecretKey"] = secretKey,
                        ["Gaoding:AppId"] = appId,
                    })
                .Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddGaoding();
            var provider = services.BuildServiceProvider();

            var options = provider.GetService<IOptions<GaodingConfig>>();
            var config = options.Value;
            Assert.Equal(accessKey, config.AccessKey);
            Assert.Equal(secretKey, config.SecretKey);

            var gaodingService = provider.GetRequiredService<GaodingService>();
            var exception = Assert.Throws<Exception>(() =>
                gaodingService.Matting("https://foco-clip.dancf.com/static/350487.jpg"));
            Assert.StartsWith("抠图失败", exception.Message);
        }
    }
}