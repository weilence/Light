namespace Light.Summary.Test;

public class OpenAiTest
{
    [Fact]
    public void Test()
    {
        var apiKey = "";
        var openAi = new OpenAiSummary(apiKey);
        var text = """

""";
        var summary = openAi.GetSummary(text, 100);
        Assert.True(summary.Length <= 100);

        var keywords = openAi.GetKeywords(text, 3, 5);
        Assert.True(keywords.Count <= 3);
        foreach (var keyword in keywords)
        {
            Assert.True(keyword.Length < 5);
        }
    }
}