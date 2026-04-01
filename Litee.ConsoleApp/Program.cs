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
            const string connectionString = "";
            var databaseRepository = new DatabasePageRepository(connectionString);
            
            var crawler = new Crawler(pageDownloader, databaseRepository);
            await crawler.Run(new Url("https://github.com/"));
            
            //SearchPage("about", databaseRepository);
        }
        
        private static void SearchPage(string keywords, DatabasePageRepository repository)
        {
            var result = repository.FindPages(keywords);

            if (result.Success)
            {
                var a = result.Value;
                
                {
                    if (a is not null)
                    {
                        foreach (var p in a)
                        {
                            Console.WriteLine(p.Url);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Не мае");
                    }
                }
            }
            else
            {
                Console.WriteLine(result.Error);
            }
        }
    }
}