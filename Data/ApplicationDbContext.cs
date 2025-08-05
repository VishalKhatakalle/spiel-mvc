using BlogSite.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogSite.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Blog> Blogs { get; set; } = default!;
    public DbSet<BlogImages> BlogImages { get; set; } = default!;
    
    public DbSet<BlogReference> BlogReferences { get; set; }
    public DbSet<BlogRevision> BlogRevisions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Blog>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BlogImages>()
            .HasOne(i => i.Blog)
            .WithMany(b => b.Images)
            .HasForeignKey(i => i.BlogPostId);

        builder.Entity<BlogImages>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}