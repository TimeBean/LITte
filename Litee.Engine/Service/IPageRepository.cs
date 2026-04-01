using Litee.Engine.Common;
using Litee.Engine.Dto;
using Litee.Engine.Model;

namespace Litee.Engine.Service;

public interface IPageRepository
{
    public Result AddPage(PageDto dto);
    public Result UpdatePage(UpdatePageDto dto);
    public Result RemovePageById(Guid guid);
    public Result<Page> GetPageById(Guid guid);
    public Result<Page[]> FindPages(string content);
}
