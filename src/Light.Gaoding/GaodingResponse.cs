using System.Text.Json.Serialization;

namespace Light.Gaoding
{
    class GaodingResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    class MattingProductResponse : GaodingResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }
    }
}