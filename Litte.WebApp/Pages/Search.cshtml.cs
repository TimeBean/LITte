using Litee.Engine.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Page = Litee.Engine.Model.Page;

namespace Litte.WebApp.Pages;

public class Search : PageModel
{
    [BindProperty(SupportsGet = true)] public string Query { get; set; }
    public Page[]? Pages { get; set; }

    private readonly IPageRepository _pageRepository;

    public Search(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public void OnGet()
    {
        if (!string.IsNullOrEmpty(Query))
        {
            /*Console.WriteLine(Query);*/

            var result = _pageRepository.FindPages(Query);

            if (result.Success)
            {
                Pages = result.Value;
            }
            else
            {
                Console.WriteLine(result.Error);
            }            
        }
    }
}