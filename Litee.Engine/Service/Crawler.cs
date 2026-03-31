using System.Diagnostics.CodeAnalysis;
using HtmlAgilityPack;
using Litee.Indexer.Model;

namespace Litee.Indexer.Service;

public class Crawler
{
    private readonly PageDownloader _pageDownloader;
    
    public Crawler(PageDownloader pageDownloader)
    {
        _pageDownloader = pageDownloader;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    private static List<string> ExtractLinks(string html, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = new List<string>();
        var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
        
        foreach (var node in nodes)
        {
            var href = node.GetAttributeValue("href", "");

            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            try
            {
                var absolute = new Uri(new Uri(baseUrl), href).ToString();
                links.Add(absolute);
            }
            catch
            {
                Console.WriteLine($"No links in {href}");
            }
        }

        return links;
    }
}