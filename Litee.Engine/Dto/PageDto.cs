using Litee.Engine.Model;

namespace Litee.Engine.Dto;

public class PageDto
{
    public Guid? Id { get; private set; }
    public string? Content { get; set; }
    public string? Keywords { get; set; }
    public string? Url { get; set; }
    
    public PageDto(Guid? id = null, Url? url = null, string? content = null, string? keywords = null)
    {
        Id = id;
        Url = url?.Value;
        Content = content;
        Keywords = keywords;
    }
}