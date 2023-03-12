using System.Collections.Generic;
using System.Linq;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Light.Summary
{
    public class OpenAiSummary : ISummary
    {
        private readonly IOpenAIService _openAiService;
        private readonly string _language;
        private readonly string _split;

        private readonly Dictionary<string, Dictionary<string, string>> _questionMap =
            new()
            {
                ["zh"] = new Dictionary<string, string>
                {
                    ["summary"] = "生成摘要，不超过{0}个字",
                    ["keywords"] = "生成{0}个关键词，每个关键词不超过{1}个字，用、分隔"
                }
            };

        public OpenAiSummary(string apiKey, string language = "zh", string split = "、")
        {
            _openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });
            _language = language;
            _split = split;
        }

        public string GetSummary(string content, int length)
        {
            var response = _openAiService.ChatCompletion.Create(new ChatCompletionCreateRequest()
            {
                Messages = new List<ChatMessage>()
                {
                    ChatMessage.FromSystem(content),
                    ChatMessage.FromUser(string.Format(_questionMap[_language]["summary"], length)),
                }
            }, Models.Model.ChatGpt3_5Turbo).Result;

            var summary = response.Choices[0].Message.Content;
            return summary;
        }

        public List<string> GetKeywords(string content, int count, int length)
        {
            var response = _openAiService.ChatCompletion.Create(new ChatCompletionCreateRequest()
            {
                Messages = new List<ChatMessage>()
                {
                    ChatMessage.FromSystem(content),
                    ChatMessage.FromUser(string.Format(_questionMap[_language]["keywords"], count, length)),
                }
            }, Models.Model.ChatGpt3_5Turbo).Result;

            var keywords = response.Choices[0].Message.Content;
            return keywords.Split(_split).ToList();
        }
    }
}