// Controllers/BlogController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogSite.Data;
using HtmlAgilityPack;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace BlogSite.Controllers;

public class BlogController : Controller
{
    private readonly ApplicationDbContext _context;

    public BlogController(ApplicationDbContext context)
    {
        _context = context;
    }

    private List<(int Level, string Text, string Id)> ExtractHeadings(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var headings = new List<(int, string, string)>();
        foreach (var node in doc.DocumentNode.Descendants()
                     .Where(n => n.Name is "h1" or "h2" or "h3" or "h4" or "h5" or "h6"))
        {
            var level = int.Parse(node.Name.Substring(1));
            var id = node.GetAttributeValue("id", "");
            var text = node.InnerText.Trim();
            headings.Add((level, text, id));
        }

        return headings;
    }

    public IActionResult Index()
    {
        var blogs = _context.Blogs
            .Include(b => b.Images)
            .OrderByDescending(b => b.PublishedAt)
            .ToList();
        return View(blogs);
    }

    [Route("blog/{slug}")]
    public IActionResult Detail(string slug)
    {
        var blog = _context.Blogs
            .Include(b => b.Images)
            .Include(b => b.User)
            .Include(b => b.References)
            .Include(b => b.Revisions.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefault(b => b.Slug == slug);

        if (blog == null)
            return NotFound();

        var headings = ExtractHeadings(blog.ContentHtml ?? "");
        ViewData["Headings"] = headings;

        return View(blog);
    }

    [HttpGet("/blog/diff")]
    public async Task<IActionResult> Diff(int rev1, int rev2)
    {
        var currentRev = await _context.BlogRevisions.FindAsync(rev1);
        var selectedRev = await _context.BlogRevisions.FindAsync(rev2);

        if (currentRev == null || selectedRev == null)
            return NotFound(new { error = "Invalid revision(s)" });

        var builder = new SideBySideDiffBuilder(new Differ());
        var diff = builder.BuildDiffModel(currentRev.Content ?? "", selectedRev.Content ?? "");

        int additions = 0, deletions = 0;

        string RenderBlock(IEnumerable<DiffPiece> lines, bool isLeft)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<pre class='p-3 rounded whitespace-pre-wrap'>");

            foreach (var line in lines)
            {
                string css = "";

                if (isLeft)
                {
                    // CURRENT version: shows deleted lines
                    if (line.Type == ChangeType.Deleted)
                    {
                        deletions++;
                        css = "bg-red-100 text-red-800 line-through";
                    }
                    else if (line.Type == ChangeType.Modified)
                    {
                        css = "bg-yellow-100 text-yellow-800";
                    }
                }
                else
                {
                    // REVISION version: shows inserted lines
                    if (line.Type == ChangeType.Inserted)
                    {
                        additions++;
                        css = "bg-green-100 text-green-800";
                    }
                    else if (line.Type == ChangeType.Modified)
                    {
                        css = "bg-yellow-100 text-yellow-800";
                    }
                }

                sb.AppendLine($"<div class='{css}'>{System.Net.WebUtility.HtmlEncode(line.Text)}</div>");
            }

            sb.AppendLine("</pre>");
            return sb.ToString();
        }

        // Render current blog on left, revision on right
        var leftHtml = RenderBlock(diff.OldText.Lines, isLeft: true);
        var rightHtml = RenderBlock(diff.NewText.Lines, isLeft: false);
        var summary = $"🟢 +{additions} additions, 🔴 -{deletions} deletions";

        return Json(new { leftHtml, rightHtml, summary });
    }
}