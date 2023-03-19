using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;

namespace Light.Gaoding
{
    public class GaodingService
    {
        private readonly GaodingConfig _gaodingConfig;
        private readonly HttpClient _httpClient;

        public GaodingService(IHttpClientFactory httpClientFactory, IOptions<GaodingConfig> gaodingConfig)
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://open-api.gaoding.com");
            httpClient.DefaultRequestHeaders.Add("X-AccessKey", gaodingConfig.Value.AccessKey);

            _httpClient = httpClient;
            _gaodingConfig = gaodingConfig.Value;
        }

        public string Matting(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/call/mattingproduct");
            req.Content = new StringContent(JsonSerializer.Serialize(new { url }));
            req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            req.Headers.Add("app_id", _gaodingConfig.AppId);

            SetSignature(req);

            var res = _httpClient.SendAsync(req).Result;
            var resContent = res.Content.ReadAsStringAsync().Result;
            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"抠图失败: {resContent}");
            }

            var data = JsonSerializer.Deserialize<MattingProductResponse>(resContent);

            if (data.Code != 0)
            {
                throw new Exception($"抠图失败: {data.Message}");
            }

            return data.Result;
        }

        public string AuthorizedCode(string uid)
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"/api/authorized/code?app_id={_gaodingConfig.AppId}&uid={uid}");

            SetSignature(req);

            var res = _httpClient.SendAsync(req).Result;
            var resContent = res.Content.ReadAsStringAsync().Result;
            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"获取授权码失败: {resContent}");
            }

            var data = JsonSerializer.Deserialize<AuthorizedCodeResponse>(resContent);
            return data.Code;
        }

        private void SetSignature(HttpRequestMessage req)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var strs = req.RequestUri.OriginalString.Split('?');
            var path = strs[0] + "/";
            var queryString = strs.Length > 1 ? strs[1] : "";

            var text = string.Join("@", req.Method, path, queryString, timestamp);
            if (req.Content != null)
            {
                var data = req.Content.ReadAsStringAsync().Result;
                text = text + "@" + data;
            }

            var signature = HmacSha1(text, _gaodingConfig.SecretKey);

            req.Headers.Add("X-Timestamp", timestamp.ToString());
            req.Headers.Add("X-Signature", signature);
        }

        static string HmacSha1(string text, string key)
        {
            var dataBuffer = Encoding.UTF8.GetBytes(text);

            var hmacsha1 = new HMACSHA1();
            hmacsha1.Key = Encoding.UTF8.GetBytes(key);
            var hashBytes = hmacsha1.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }
    }
}