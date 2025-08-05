namespace BlogSite.Models;

public class BlogImages
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;

    // FK to Blog
    public Guid BlogPostId { get; set; }
    public Blog Blog { get; set; } = null!;

    // Optional: FK to user who uploaded this image
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = null!;
}