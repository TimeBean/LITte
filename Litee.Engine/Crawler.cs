using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Litee.Engine.Dto;
using Litee.Engine.Model;
using Litee.Engine.Service;

namespace Litee.Engine;

public class Crawler
{
    private readonly PageDownloader _pageDownloader;
    private readonly IPageRepository _pageRepository;

    public Crawler(PageDownloader pageDownloader, IPageRepository pageRepository)
    {
        _pageDownloader = pageDownloader;
        _pageRepository = pageRepository;
    }

    [SuppressMessage("ReSharper", "ComplexConditionExpression")]
    public async Task Run(Url startUrl, CancellationToken cancellationToken = default)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(startUrl.Value);

        while (queue.Count > 0)
        {
            var url = queue.Dequeue();

            if (visited.Contains(url))
            {
                continue;
            }

            visited.Add(url);

            Console.WriteLine($"Visiting: {url}");

            try
            {
                var page = await _pageDownloader.GetPage(new Url(url), cancellationToken);
                var links = ExtractLinks(page.Content, url);
                foreach (var link in links)
                {
                    if (!visited.Contains(link))
                    {
                        queue.Enqueue(link);
                    }
                }

                try
                {
                    var pageDto = new PageDto(null, page.Url, page.Content, page.Keywords);
                    var result = _pageRepository.AddPage(pageDto);
                    Console.WriteLine($"[Crawler] Try add Page {url} IsOk: {result.Success}; {result.Error}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB] Error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Crawler] Error: {ex.Message}");
            }
        }
    }

    private static List<string> ExtractLinks(string html, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = new HashSet<string>();

        var urlAttributes = new Dictionary<string, string[]>
        {
            { "//a[@href]", new[] { "href" } },
            { "//link[@href]", new[] { "href" } },
            { "//area[@href]", new[] { "href" } },
            { "//base[@href]", new[] { "href" } },

            { "//img[@src]", new[] { "src", "data-src", "data-lazy-src", "data-original" } },
            { "//script[@src]", new[] { "src" } },
            { "//iframe[@src]", new[] { "src", "data-src" } },
            { "//frame[@src]", new[] { "src" } },
            { "//embed[@src]", new[] { "src" } },
            { "//audio[@src]", new[] { "src" } },
            { "//video[@src]", new[] { "src" } },
            { "//source[@src]", new[] { "src", "srcset" } },
            { "//track[@src]", new[] { "src" } },
            { "//input[@src]", new[] { "src" } },

            { "//form[@action]", new[] { "action" } },
            { "//button[@formaction]", new[] { "formaction" } },
            { "//input[@formaction]", new[] { "formaction" } },

            { "//object[@data]", new[] { "data" } },
            { "//blockquote[@cite]", new[] { "cite" } },
            { "//q[@cite]", new[] { "cite" } },
            { "//ins[@cite]", new[] { "cite" } },
            { "//del[@cite]", new[] { "cite" } },
        };

        var baseUri = new Uri(baseUrl);

        foreach (var (xpath, attrs) in urlAttributes)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null) continue;

            foreach (var node in nodes)
            {
                foreach (var attr in attrs)
                {
                    ProcessAttribute(node, attr, baseUri, links);
                }

                var srcset = node.GetAttributeValue("srcset", "");
                if (!string.IsNullOrWhiteSpace(srcset))
                {
                    foreach (var part in srcset.Split(','))
                    {
                        var url = part.Trim().Split(' ')[0];
                        TryAddLink(url, baseUri, links);
                    }
                }
            }
        }

        var dataNodes = doc.DocumentNode.SelectNodes("//*[@*]");
        if (dataNodes != null)
        {
            var dataUrlAttrs = new[]
            {
                "data-href", "data-url", "data-link", "data-target",
                "data-src", "data-lazy", "data-original", "data-redirect"
            };
            foreach (var node in dataNodes)
            {
                foreach (var attr in dataUrlAttrs)
                {
                    ProcessAttribute(node, attr, baseUri, links);
                }
            }
        }

        var styledNodes = doc.DocumentNode.SelectNodes("//*[@style]");
        if (styledNodes != null)
        {
            foreach (var node in styledNodes)
            {
                var style = node.GetAttributeValue("style", "");
                ExtractFromCssUrls(style, baseUri, links);
            }
        }

        var styleTags = doc.DocumentNode.SelectNodes("//style");
        if (styleTags != null)
        {
            foreach (var node in styleTags)
            {
                ExtractFromCssUrls(node.InnerText, baseUri, links);
            }
        }

        var metaNodes = doc.DocumentNode.SelectNodes("//meta[@content]");
        if (metaNodes != null)
        {
            foreach (var node in metaNodes)
            {
                var httpEquiv = node.GetAttributeValue("http-equiv", "").ToLower();
                var name = node.GetAttributeValue("name", "").ToLower();
                var content = node.GetAttributeValue("content", "");

                if (httpEquiv == "refresh")
                {
                    var match = Regex.Match(content, @"url\s*=\s*['""]?([^'"";\s]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                        TryAddLink(match.Groups[1].Value, baseUri, links);
                }
            }
        }

        return links.ToList();
    }

    private static readonly Regex CssUrlRegex = new Regex(
        @"url\s*\(\s*['""]?([^'"")\s]+)['""]?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void ExtractFromCssUrls(string css, Uri baseUri, HashSet<string> links)
    {
        if (string.IsNullOrWhiteSpace(css)) return;

        foreach (Match match in CssUrlRegex.Matches(css))
        {
            TryAddLink(match.Groups[1].Value, baseUri, links);
        }
    }

    private static void ProcessAttribute(HtmlNode node, string attr, Uri baseUri, HashSet<string> links)
    {
        var value = node.GetAttributeValue(attr, "");
        TryAddLink(value, baseUri, links);
    }

    private static void TryAddLink(string href, Uri baseUri, HashSet<string> links)
    {
        if (string.IsNullOrWhiteSpace(href)) return;
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return;
        if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return;
        if (href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return;
        if (href.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;
        if (href == "#") return;

        try
        {
            var decoded = Uri.UnescapeDataString(href);
            var escaped = Uri.EscapeUriString(decoded); 

            var absolute = new Uri(baseUri, escaped).ToString();
            links.Add(absolute);
        }
        catch
        {
            Console.WriteLine($"Skipped invalid URL: {href}");
        }
    }
}