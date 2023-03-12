using System.Collections.Generic;

namespace Light.Summary
{
    public interface ISummary
    {
        string GetSummary(string content, int length);

        List<string> GetKeywords(string content, int count, int length);
    }
}