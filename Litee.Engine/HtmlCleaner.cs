using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Litee.Engine;

public static class HtmlCleaner
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "и", "в", "на", "с", "по", "для", "от", "до", "из", "за", "к", "о", "об",
        "при", "под", "над", "без", "через", "между", "после", "перед", "или", "но",
        "что", "как", "так", "это", "не", "же", "бы", "ли", "если", "то", "все",
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
        "it", "its", "this", "that", "as", "at", "into", "up", "out"
    };
    
    public static string CleanHtml(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        RemoveNodes(doc, "//script");
        RemoveNodes(doc, "//style");
        RemoveNodes(doc, "//comment()");

        var priorityContent = new StringBuilder();

        AppendInnerText(priorityContent, doc, "//title");
        AppendMetaContent(priorityContent, doc, "keywords");
        AppendMetaContent(priorityContent, doc, "description");
        AppendInnerText(priorityContent, doc, "//h1");
        AppendInnerText(priorityContent, doc, "//h2");
        AppendInnerText(priorityContent, doc, "//h3");

        var bodyText = doc.DocumentNode.InnerText;

        var combined = priorityContent + " " + bodyText;

        combined = System.Net.WebUtility.HtmlDecode(combined);

        combined = CleanSymbolsRegex.Replace(combined, " ");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keywords = new List<string>();

        foreach (var raw in combined.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var word = raw.Trim('-').ToLowerInvariant();

            if (word.Length <= 2) continue; 
            if (StopWords.Contains(word)) continue;
            if (PureDigitsRegex.IsMatch(word)) continue; 
            if (!seen.Add(word)) continue; 

            keywords.Add(word);
        }

        return string.Join(" ", keywords);
    }

    private static void RemoveNodes(HtmlDocument doc, string xpath)
    {
        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes == null) return;
        foreach (var node in nodes.ToList())
            node.Remove();
    }

    private static void AppendInnerText(StringBuilder sb, HtmlDocument doc, string xpath)
    {
        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            var text = node.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
                sb.Append(' ').Append(text);
        }
    }

    private static void AppendMetaContent(StringBuilder sb, HtmlDocument doc, string metaName)
    {
        var xpath = $"//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{metaName}']";
        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            var content = node.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(content))
                sb.Append(' ').Append(content);
        }
    }

    private static readonly Regex CleanSymbolsRegex = new Regex(
        @"[^\w\s\-]",
        RegexOptions.Compiled);

    private static readonly Regex PureDigitsRegex = new Regex(
        @"^\d+$",
        RegexOptions.Compiled);

    private static readonly char[] Separators = { ' ', '\t', '\r', '\n' };
}