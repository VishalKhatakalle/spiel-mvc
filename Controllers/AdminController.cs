using BlogSite.Data;
using BlogSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogSite.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }
    
    private async Task SaveRevisionAsync(Blog blog)
    {
        var revision = new BlogRevision
        {
            BlogId = blog.Id,
            Content = blog.Content,
            CreatedAt = DateTime.UtcNow,
            EditedByUserId = User.Identity?.Name
        };
        _context.BlogRevisions.Add(revision);
        await _context.SaveChangesAsync();
    }

    public IActionResult Index(string? search)
    {
        var blogs = _context.Blogs
            .Where(b => search == null || b.Title!.ToLower().Contains(search.ToLower()))
            .ToList();
        return View(blogs);
    }

    public IActionResult Create() => View(new Blog());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Blog blog, IFormFile? CoverImage)
    {
        var user = await _userManager.GetUserAsync(User);
        blog.UserId = user.Id;
        blog.PublishedAt = DateTime.UtcNow;
        blog.Slug = blog.Title?.ToLower().Replace(" ", "-");

        if (CoverImage != null)
        {
            var filename = Guid.NewGuid() + Path.GetExtension(CoverImage.FileName);
            var path = Path.Combine(_env.WebRootPath, "uploads", filename);
            await using var stream = new FileStream(path, FileMode.Create);
            await CoverImage.CopyToAsync(stream);
            blog.CoverImageUrl = "/uploads/" + filename;
        }

        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(Guid id)
    {
        var blog = _context.Blogs.FirstOrDefault(b => b.Id == id);
        return blog == null ? NotFound() : View(blog);
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    Guid id,
    [FromForm] string Title,
    [FromForm] string? Description,
    [FromForm] string? Content,
    [FromForm] string? Category,
    [FromForm] string? Tags,
    [FromForm] IFormFile? CoverImage)
{
    var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
    if (blog == null) return NotFound();

    // ✅ Update fields first
    blog.Title = Title;
    blog.Description = Description;
    blog.Content = Content;
    blog.Category = Category;
    blog.Tags = Tags;
    blog.Slug = Title?.ToLower().Replace(" ", "-");

    if (CoverImage != null)
    {
        var filename = Guid.NewGuid() + Path.GetExtension(CoverImage.FileName);
        var path = Path.Combine(_env.WebRootPath, "uploads", filename);
        await using var stream = new FileStream(path, FileMode.Create);
        await CoverImage.CopyToAsync(stream);
        blog.CoverImageUrl = "/uploads/" + filename;
    }

    // 🧹 Remove old references
    var existingRefs = _context.BlogReferences.Where(r => r.BlogId == blog.Id).ToList();
    _context.BlogReferences.RemoveRange(existingRefs);

    // ✅ Parse submitted references
    var references = new List<BlogReference>();
    int i = 0;
    while (true)
    {
        var urlKey = $"References[{i}].Url";
        if (!Request.Form.ContainsKey(urlKey)) break;

        var url = Request.Form[urlKey];
        if (string.IsNullOrWhiteSpace(url)) { i++; continue; }

        var title = Request.Form[$"References[{i}].Title"];
        var description = Request.Form[$"References[{i}].Description"];

        references.Add(new BlogReference
        {
            BlogId = blog.Id,
            Url = url,
            Title = title,
            Description = description
        });

        i++;
    }
    _context.BlogReferences.AddRange(references);

    // ✅ Save all changes (includes the updated content)
    await _context.SaveChangesAsync();

    // ✅ THEN create revision from updated blog
    await SaveRevisionAsync(blog);

    return RedirectToAction(nameof(Index));
}

    public IActionResult Delete(Guid id)
    {
        var blog = _context.Blogs.FirstOrDefault(b => b.Id == id);
        return blog == null ? NotFound() : View(blog);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var blog = await _context.Blogs.FindAsync(id);
        if (blog != null)
        {
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
} 
