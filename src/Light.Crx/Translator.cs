using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Light.Crx;

internal class Translator
{
    private Dictionary<string, Dictionary<string, string>> _locales;

    private string _defaultLocale = "";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static Translator Default = new Translator();

    private Translator()
    {
        _locales = new();
    }

    public Translator(string defaultLocale, ZipArchive zip)
    {
        _defaultLocale = defaultLocale;
        _locales = new Dictionary<string, Dictionary<string, string>>();
        foreach (var entry in zip.Entries.Where(m => m.Name == "messages.json" && m.FullName.StartsWith("_locales/")))
        {
            var lang = entry.FullName.Split('/')[1];

            using var messageStream = entry.Open();
            var messages = JsonSerializer.Deserialize<Dictionary<string, Messages>>(messageStream, ManifestJsonOptions) ?? throw new Exception("messages.json parse failed");
            _locales[lang] = messages.ToDictionary(m => m.Key, m => m.Value.Message);
        }
    }

    private static readonly Regex _messageRegex = new("^__MSG_(?<key>.+)__$");

    public string Message(string lang, string msg)
    {
        if (!_locales.TryGetValue(lang, out var messages) && !_locales.TryGetValue(_defaultLocale, out messages))
        {
            return msg;
        }

        var group = _messageRegex.Match(msg);
        if (!group.Success)
        {
            return msg;
        }

        var key = group.Groups["key"].Value;
        return messages[key] ?? key;
    }
}
