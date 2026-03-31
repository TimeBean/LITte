namespace Litee.Indexer.Model;

public class Page
{
    public Guid Id { get; private set; }
    public string Content { get; set; }
    public Url Url { get; set; }

    public Page(Url url, string content)
    {
        Id = Guid.NewGuid();
        Url = url;
        Content = content;
    }

    public Page(Guid id, Url url, string content)
    {
        Id = id;
        Url = url;
        Content = content;
    }
}