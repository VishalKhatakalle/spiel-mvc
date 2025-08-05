using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;

namespace BlogSite.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MetadataController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MetadataController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("fetch-title")]
    public async Task<IActionResult> FetchTitle([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return BadRequest("URL is required");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(title))
                return NotFound("No <title> tag found");

            return Ok(new { title });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching title: {ex.Message}");
        }
    }
}