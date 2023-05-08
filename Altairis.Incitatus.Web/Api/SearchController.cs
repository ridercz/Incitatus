using System.Globalization;
using System.Net.Mime;
using System.Text;
using Altairis.Services.DateProvider;
using Microsoft.AspNetCore.Cors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Altairis.Incitatus.Web.Api;

[ApiController, Route("/api/search"), ApiExplorerSettings(GroupName = "search")]
[Produces(MediaTypeNames.Application.Json), Consumes(MediaTypeNames.Application.Json)]
public class SearchController : ControllerBase {
    private readonly IncitatusDbContext dc;
    private readonly IDateProvider dateProvider;
    private readonly ILogger<SitesController> logger;

    // Constructor

    public SearchController(IncitatusDbContext dc, IDateProvider dateProvider, ILogger<SitesController> logger) {
        this.dc = dc;
        this.dateProvider = dateProvider;
        this.logger = logger;
    }

    // Action methods

    /// <summary>
    /// Gets search results for specified site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query</param>
    /// <returns></returns>
    [HttpGet("{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableCors("public")]
    public async Task<ActionResult<IEnumerable<SearchResultModel>>> Get(Guid siteId, string query) {
        try {
            var result = await this.dc.SearchPages(siteId, query).Select(x => new SearchResultModel(x.Url, x.Title, x.Description, x.DateLastUpdated)).ToListAsync();
            return this.Ok(result);
        } catch (SqlException sex) when (sex.Number == 7645) { // Null or empty full-text predicate.
            return this.BadRequest();
        }
    }

    public record SearchResultModel(string Url, string Title, string Description, DateTime? LastUpdated);

    /// <summary>
    /// Gets search results for specified site as HTML fragment.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query.</param>
    /// <param name="locale">The locale code, ie. <c>en-US</c> or <c>cs-CZ</c>. If missing or invalid, server culture is used.</param>
    /// <param name="dateFormat">The last update date format string in .NET format. If missing, default date format for selected culture is used.</param>
    /// <returns></returns>
    [HttpGet("{siteId:guid}.html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/html")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableCors("public")]
    public async Task<ActionResult<string>> GetHtml(Guid siteId, string query, string? locale = null, string dateFormat = "d") {
        try {
            var result = await this.dc.SearchPages(siteId, query).Select(x => new SearchResultModel(x.Url, x.Title, x.Description, x.DateLastUpdated)).ToListAsync();
            if(!result.Any()) return this.NotFound();
            var culture = string.IsNullOrEmpty(locale) ? CultureInfo.CurrentCulture : new CultureInfo(locale);
            var sb = new StringBuilder();
            foreach (var item in result) {
                sb.AppendLine($"""
                    <article>
                        <header><a href="{item.Url}">{item.Title}</a></header>
                        <aside><time datetime="{item.LastUpdated:s}">{item.LastUpdated?.ToString(dateFormat, culture)}</time></aside>
                        <div>{item.Description}</div>
                    </article>
                    """);
            }
            return this.Content(sb.ToString(), "text/html");
        } catch (SqlException sex) when (sex.Number == 7645) { // Null or empty full-text predicate.
            return this.BadRequest();
        }
    }

    /// <summary>
    /// Marks the specified site as in need of update.
    /// </summary>
    /// <param name="updateKey">The update key.</param>
    /// <returns></returns>
    [HttpGet("/api/update/{updateKey}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult> Update(string updateKey) {
        var site = await this.dc.Sites.Where(x => x.UpdateKey == updateKey).FirstOrDefaultAsync();
        if (site != null) {
            site.UpdateRequired = true;
            await dc.SaveChangesAsync();
        }
        return this.Accepted();
    }

}