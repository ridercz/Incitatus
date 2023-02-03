using System.Globalization;
using System.Xml;
using Altairis.Incitatus.Data;
using Altairis.Services.DateProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Altairis.Incitatus.Core;

public class SitemapUpdater : ISitemapUpdater {
    private readonly IncitatusDbContext dc;
    private readonly IDateProvider dateProvider;
    private readonly ILogger<SitemapUpdater> logger;
    private readonly HttpClient httpClient;

    private record SitemapItem(string Url, DateTime Date);

    public SitemapUpdater(IncitatusDbContext dc, IDateProvider dateProvider, ILogger<SitemapUpdater> logger) {
        this.dc = dc;
        this.dateProvider = dateProvider;
        this.logger = logger;
        this.httpClient = new HttpClient();
    }

    public async Task UpdateSitemap(Guid siteId, CancellationToken cancellationToken) {
        // Get site info
        var site = await this.dc.Sites.FindAsync(new object?[] { siteId }, cancellationToken: cancellationToken);
        if (site == null) throw new ArgumentException("Site not found.", nameof(siteId));

        // Get items from sitemap
        var sitemapItems = this.GetSitemapItems(site).ToList();
        if (!sitemapItems.Any()) {
            this.logger.LogWarning("Sitemap for site {siteId} ({siteName}) does not contain any valid elements.", site.Id, site.Name);
            return;
        }

        // Get current pages and update their status
        var currentPages = await this.dc.Pages.Where(x => x.SiteId == siteId).Select(x => new { x.Id, x.Url, x.DateLastUpdated, x.UpdateRequired }).ToListAsync(cancellationToken);
        foreach (var item in currentPages) {
            var correspondingSitemapItem = sitemapItems.FirstOrDefault(x => x.Url == item.Url);
            if (correspondingSitemapItem == null) {
                // Existing page was not found in sitemap, therefore it was deleted
                this.logger.LogInformation("Deleting {pageId} ({pageUrl}) because it's no longer present in sitemap.", item.Id, item.Url);
                await this.dc.Pages.Where(x => x.Id == item.Id).ExecuteDeleteAsync(cancellationToken);
            } else if (!item.DateLastUpdated.HasValue || item.DateLastUpdated < correspondingSitemapItem.Date) {
                // Existing page was updated
                if (!item.UpdateRequired) {
                    this.logger.LogInformation("Page {pageId} ({pageUrl}) is newer, setting for update.", item.Id, item.Url);
                    await this.dc.Pages.Where(x => x.Id == item.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.UpdateRequired, true), cancellationToken);
                }
                sitemapItems.Remove(correspondingSitemapItem);
            } else {
                // Existing page is current
                this.logger.LogDebug("Page {pageId} ({pageUrl}) is current.", item.Id, item.Url);
                sitemapItems.Remove(correspondingSitemapItem);
            }
        }

        // Add new sitemap items
        foreach (var item in sitemapItems) {
            this.logger.LogDebug("Adding new page with url {pageUrl}.", item.Url);
            var newPage = new Page {
                Title = string.Empty,
                Text = string.Empty,
                DateCreated = this.dateProvider.Now,
                SiteId = site.Id,
                Url = item.Url,
                UpdateRequired = true
            };
            await this.dc.Pages.AddAsync(newPage, cancellationToken);
        }

        // Save changes
        site.UpdateRequired = false;
        await this.dc.SaveChangesAsync(cancellationToken);
    }

    private IEnumerable<SitemapItem> GetSitemapItems(Site site) {
        // Load XML document from stream
        var doc = new XmlDocument();
        var mgr = new XmlNamespaceManager(doc.NameTable);
        mgr.AddNamespace("sm", "http://www.sitemaps.org/schemas/sitemap/0.9");
        try {
            using var stream = this.httpClient.GetStreamAsync(site.SitemapUrl).Result;
            doc.Load(stream);
            this.logger.LogDebug("Loaded sitemap for site {siteId} ({siteName}) from URL {siteUrl}.", site.Id, site.Name, site.SitemapUrl);
        } catch (Exception ex) {
            this.logger.LogError(ex, "Cannot load sitemap for site {siteId} ({siteName}) from URL {siteUrl}.", site.Id, site.Name, site.SitemapUrl);
            yield break;
        }

        // Get all items
        var urlElements = doc.SelectNodes("/sm:urlset/sm:url[sm:loc and sm:lastmod]", mgr)?.Cast<XmlElement>();
        if (urlElements is null || !urlElements.Any()) {
            yield break;
        }

        // Parse all urls in sitemap
        foreach (var item in urlElements) {
            var loc = item.SelectSingleNode("sm:loc", mgr)?.InnerText ?? string.Empty;
            var lastModString = item.SelectSingleNode("sm:lastmod", mgr)?.InnerText ?? string.Empty;
            var isValid = !string.IsNullOrEmpty(loc)
                && !string.IsNullOrEmpty(lastModString)
                && DateTime.TryParse(lastModString, out _)
                && Uri.TryCreate(loc, UriKind.Absolute, out _);
            if (!isValid) {
                this.logger.LogWarning("Sitemap item {loc} for site {siteId} ({siteName}) is invalid.", loc, site.Id, site.Name);
                continue;
            }
            yield return new SitemapItem(loc, DateTime.Parse(lastModString));
        }
    }

}
