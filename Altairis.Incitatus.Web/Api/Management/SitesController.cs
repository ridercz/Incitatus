using System.Net.Mime;
using Altairis.Services.DateProvider;
using Microsoft.EntityFrameworkCore;

namespace Altairis.Incitatus.Web.Api.Management;

[ApiController, Route("/api/sites")]
[Produces(MediaTypeNames.Application.Json), Consumes(MediaTypeNames.Application.Json)]
public class SitesController : ControllerBase {
    private readonly IncitatusDbContext dc;
    private readonly IDateProvider dateProvider;
    private readonly ILogger<SitesController> logger;

    // Constructor

    public SitesController(IncitatusDbContext dc, IDateProvider dateProvider, ILogger<SitesController> logger) {
        this.dc = dc;
        this.dateProvider = dateProvider;
        this.logger = logger;
    }

    // Action methods
    /// <summary>
    /// Gets the list of sites.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SiteIndexModel>>> GetIndex() {
        var q = from s in this.dc.Sites
                orderby s.Name
                select new SiteIndexModel(s.Id, s.Name, s.Url, s.DateCreated, s.DateLastUpdated, s.UpdateRequired);
        return await q.ToListAsync();
    }

    /// <summary>
    /// Gets the specified site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns></returns>
    [HttpGet("{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDetailModel>> Get(Guid siteId) {
        var q = from s in this.dc.Sites
                where s.Id == siteId
                select new SiteDetailModel(s.Id, s.Name, s.Url, s.DateCreated, s.DateLastUpdated, s.UpdateRequired, s.SitemapUrl, s.ContentXPath, s.UpdateKey, s.Pages.Count, s.Pages.Count(p => p.UpdateRequired));
        var site = await q.FirstOrDefaultAsync();
        return site == null ? this.NotFound() : this.Ok(site);
    }

    /// <summary>
    /// Creates new site.
    /// </summary>
    /// <param name="model">The site model.</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SiteDetailModel>> Post(NewSiteModel model) {
        if (!this.ModelState.IsValid) return this.BadRequest();

        var newSite = new Site {
            ContentXPath = model.ContentXPath,
            DateCreated = this.dateProvider.Now,
            Name = model.Name,
            SitemapUrl = model.SitemapUrl,
            UpdateKey = Site.CreateRandomUpdateKey(),
            UpdateRequired = true,
            Url = model.Url
        };
        this.dc.Add(newSite);
        await this.dc.SaveChangesAsync();
        var resultModel = new SiteDetailModel(newSite.Id, newSite.Name, newSite.Url, newSite.DateCreated, newSite.DateLastUpdated, newSite.UpdateRequired, newSite.SitemapUrl, newSite.ContentXPath, newSite.UpdateKey, 0, 0);
        return this.CreatedAtAction(nameof(Get), new { siteId = newSite.Id }, resultModel);
    }

    /// <summary>
    /// Updates the specified site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="model">The site model.</param>
    /// <returns></returns>
    [HttpPut("{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(Guid siteId, UpdateSiteModel model) {
        if (!this.ModelState.IsValid) return this.BadRequest();
        var site = await this.dc.FindAsync<Site>(siteId);
        if (site == null) return this.NotFound();

        site.Name = model.Name;
        site.Url = model.Url;
        site.SitemapUrl = model.SitemapUrl;
        site.ContentXPath = model.ContentXPath;
        if (model.ResetUpdateKey) {
            site.UpdateKey = Site.CreateRandomUpdateKey();
            this.logger.LogInformation("UpdateKey was reset for site {siteId} ({siteName}).", site.Id, site.Name);
        }
        await this.dc.SaveChangesAsync();

        return this.NoContent();
    }

    /// <summary>
    /// Deletes the specified site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <returns></returns>
    [HttpDelete("{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid siteId) {
        var site = await this.dc.FindAsync<Site>(siteId);
        if (site != null) {
            this.dc.Remove(site);
            await this.dc.SaveChangesAsync();
        }
        return this.NoContent();
    }

    // Action models

    public record NewSiteModel {
        [Required, MaxLength(1000)]
        public required string Name { get; set; }

        [Required, MaxLength(1000), Url]
        public required string Url { get; set; }

        [Required, MaxLength(1000), Url]
        public required string SitemapUrl { get; set; }

        [Required, MaxLength(1000)]
        public required string ContentXPath { get; set; }

    }

    public record UpdateSiteModel {
        [Required, MaxLength(1000)]
        public required string Name { get; set; }

        [Required, MaxLength(1000), Url]
        public required string Url { get; set; }

        [Required, MaxLength(1000), Url]
        public required string SitemapUrl { get; set; }

        [Required, MaxLength(1000)]
        public required string ContentXPath { get; set; }

        public bool ResetUpdateKey { get; set; } = false;

    }

    public record SiteIndexModel(Guid Id, string Name, string Url, DateTime DateCreated, DateTime? DateLastUpdated, bool UpdateRequired);

    public record SiteDetailModel(Guid Id, string Name, string Url, DateTime DateCreated, DateTime? DateLastUpdated, bool UpdateRequired, string SitemapUrl, string ContentXPath, string UpdateKey, int TotalPages, int PagesRequiringUpdate);

}

