using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.Gaoding.Test
{
    public class UnitTest1
    {
        private IServiceProvider _provider;

        public UnitTest1()
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
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void TestMatting()
        {
            var gaodingService = _provider.GetRequiredService<GaodingService>();
            var exception = Assert.Throws<Exception>(() =>
                gaodingService.Matting("https://foco-clip.dancf.com/static/350487.jpg"));
            Assert.StartsWith("抠图失败", exception.Message);
        }

        [Fact]
        public void TestAuthorizedCode()
        {
            var gaodingService = _provider.GetRequiredService<GaodingService>();
            var code = gaodingService.AuthorizedCode("test");
            Assert.NotEmpty(code);
        }
    }
}