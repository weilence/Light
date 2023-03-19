using System.Text.Json.Serialization;

namespace Light.Gaoding
{
    class MattingProductResponse
    {
        [JsonPropertyName("code")] public int Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }

        [JsonPropertyName("result")] public string Result { get; set; }
    }

    class AuthorizedCodeResponse
    {
        [JsonPropertyName("code")] public string Code { get; set; }
    }
}