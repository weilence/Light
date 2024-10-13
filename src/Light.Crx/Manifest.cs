namespace Light.Crx;

public class Manifest
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Icons { get; set; } = new();
    public string DefaultLocale { get; set; } = "";
}

public class Messages
{
    public string Message { get; set; } = "";
}