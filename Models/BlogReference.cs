namespace BlogSite.Models;

public class BlogReference
{
    public int Id { get; set; }
    public Guid BlogId { get; set; }
    public Blog Blog { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
}