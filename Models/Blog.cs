namespace BlogSite.Models;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
public class Blog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    
    public string? CoverImageUrl { get; set; }
    
    public string[] TagList => Tags?.Split(' ') ?? Array.Empty<string>();
    public DateTime PublishedAt { get; set; }

    // FK + Navigation to ApplicationUser
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = null!;

    public ICollection<BlogImages> Images { get; set; } = new List<BlogImages>();

    public string? ContentHtml => Content != null
        ? Markdown.ToHtml(Content, new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // adds id="..."
            .Build())
        : null;

    public string PublishedAgo => (DateTime.UtcNow - PublishedAt).TotalDays switch
    {
        < 1 => "Today",
        < 7 => $"{(int)(DateTime.UtcNow - PublishedAt).TotalDays} days ago",
        < 30 => $"{(int)((DateTime.UtcNow - PublishedAt).TotalDays / 7)} weeks ago",
        _ => PublishedAt.ToShortDateString()
    };
    
    public ICollection<BlogReference> References { get; set; } = new List<BlogReference>();
    public ICollection<BlogRevision> Revisions { get; set; } = new List<BlogRevision>();
}