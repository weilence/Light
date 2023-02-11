using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Light.Gaoding
{
    public class GaodingService
    {
        private readonly GaodingConfig _gaodingConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public GaodingService(IHttpClientFactory httpClientFactory, IOptions<GaodingConfig> gaodingConfig)
        {
            _httpClientFactory = httpClientFactory;
            _gaodingConfig = gaodingConfig.Value;
        }

        public string Matting(string url)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://open-api.gaoding.com");

            var method = "POST";
            var path = "/api/call/mattingproduct";
            var queryString = "";

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payLoad = JsonSerializer.Serialize(new { url });
            var signature = sha1(method + "@" + path + "/" + "@" + queryString + "@" + timestamp + "@" + payLoad,
                _gaodingConfig.SecretKey);

            var content = new StringContent(payLoad);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            content.Headers.Add("X-Timestamp", timestamp.ToString());
            content.Headers.Add("X-AccessKey", _gaodingConfig.AccessKey);
            content.Headers.Add("X-Signature", signature);
            content.Headers.Add("app_id", _gaodingConfig.AppId);
            var res = httpClient.PostAsync(path, content).Result;

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"抠图失败");
            }
            
            var resContent = res.Content.ReadAsStringAsync().Result;
            var data = JsonSerializer.Deserialize<MattingProductResponse>(resContent);

            if (data.Code != 0)
            {
                throw new Exception($"抠图失败: {data.Message}");
            }

            return data.Result;
        }

        static string sha1(string text, string key)
        {
            var hmacsha1 = new HMACSHA1();
            hmacsha1.Key = Encoding.UTF8.GetBytes(key);

            var dataBuffer = Encoding.UTF8.GetBytes(text);
            var hashBytes = hmacsha1.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
    }
}