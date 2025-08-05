namespace BlogSite.Models;

public class BlogRevision
{
    public int Id { get; set; }
    public Guid BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? EditedByUserId { get; set; }
}