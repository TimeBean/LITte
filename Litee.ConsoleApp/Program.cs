using Litee.Engine;
using Litee.Engine.Model;
using Litee.Engine.Service;

namespace Litee.ConsoleApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        private static async Task Main()
        {
            var httpClient = new HttpClient();
            var pageDownloader = new PageDownloader(httpClient);
            var crawler = new Crawler(pageDownloader);
            await crawler.Run(new Url("https://google.com"));
        }
    }
}