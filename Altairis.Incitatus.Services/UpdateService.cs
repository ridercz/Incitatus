using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Altairis.Incitatus.Data;
using Altairis.Services.DateProvider;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altairis.Incitatus.Services;

public partial class UpdateService : BackgroundService {
    private readonly ILogger<UpdateService> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IDateProvider dateProvider;
    private readonly UpdateServiceOptions options;
    private readonly SocketsHttpHandler socketsHttpHandler;
    private readonly HttpClient httpClient;

    private record SitemapItem(string Url, DateTime Date);

    // Constructor

    public UpdateService(IOptions<UpdateServiceOptions> optionsAccessor, ILogger<UpdateService> logger, IServiceScopeFactory scopeFactory, IDateProvider dateProvider) {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.dateProvider = dateProvider;
        this.options = optionsAccessor?.Value ?? new UpdateServiceOptions();
        this.socketsHttpHandler = new SocketsHttpHandler { PooledConnectionLifetime = this.options.PooledCollectionLifetime };
        this.httpClient = new HttpClient(this.socketsHttpHandler) { Timeout = this.options.ConnectionTimeout };
    }

    // Public methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            using (var scope = scopeFactory.CreateScope()) {
                using var dc = scope.ServiceProvider.GetRequiredService<IncitatusDbContext>();

                // Process sites that need to be updated
                var sitesToUpdate = await dc.Sites.Where(x => x.UpdateRequired).ToListAsync(stoppingToken);
                if (sitesToUpdate.Any()) foreach (var site in sitesToUpdate) try {
                            await this.UpdateSingleSitemapAsync(site, dc, stoppingToken);
                        } catch (Exception ex) {
                            this.logger.LogError(ex, "Error while processing sitemap for site {siteId} ({siteName}).", site.Id, site.Name);
                        }
                else this.logger.LogDebug("No sites need to be updated.");

                // Process pages that need to be updated
                var pagesToUpdate = await dc.Pages.Include(x => x.Site).Where(x => x.UpdateRequired).ToListAsync(stoppingToken);
                if (pagesToUpdate.Any()) foreach (var page in pagesToUpdate) {
                        try {
                            var changed = await UpdateSinglePageAsync(page, stoppingToken);
                            if (changed) await dc.SaveChangesAsync(stoppingToken);
                        } catch (Exception ex) {
                            logger.LogError(ex, "Error while processing page {pageId} ({pageUrl}).", page.Id, page.Url);
                        }
                        // Wait between requests if configyred
                        if (this.options.DelayBetweenPageRequests > TimeSpan.Zero) await Task.Delay(this.options.DelayBetweenPageRequests);
                    }
                else this.logger.LogDebug("No pages need to be updated.");
            }

            // Wait for a while
            await Task.Delay(this.options.PollInterval, stoppingToken);
        }
    }

    // Helper methods

    private async Task UpdateSingleSitemapAsync(Site site, IncitatusDbContext dc, CancellationToken cancellationToken) {
        // Get items from sitemap
        var sitemapItems = await this.GetSitemapItems(site);
        if (!sitemapItems.Any()) {
            this.logger.LogWarning("Sitemap for site {siteId} ({siteName}) does not contain any valid elements.", site.Id, site.Name);
            return;
        }

        // Get current pages and update their status
        var currentPages = await dc.Pages.Where(x => x.SiteId == site.Id).Select(x => new { x.Id, x.Url, x.DateLastUpdated, x.UpdateRequired }).ToListAsync(cancellationToken);
        foreach (var item in currentPages) {
            var correspondingSitemapItem = sitemapItems.FirstOrDefault(x => x.Url == item.Url);
            if (correspondingSitemapItem == null) {
                // Existing page was not found in sitemap, therefore it was deleted
                this.logger.LogInformation("Deleting {pageId} ({pageUrl}) because it's no longer present in sitemap.", item.Id, item.Url);
                await dc.Pages.Where(x => x.Id == item.Id).ExecuteDeleteAsync(cancellationToken);
            } else if (!item.DateLastUpdated.HasValue || item.DateLastUpdated < correspondingSitemapItem.Date) {
                // Existing page was updated
                if (!item.UpdateRequired) {
                    this.logger.LogInformation("Page {pageId} ({pageUrl}) is newer, setting for update.", item.Id, item.Url);
                    await dc.Pages.Where(x => x.Id == item.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.UpdateRequired, true), cancellationToken);
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
                Description = string.Empty,
                Text = string.Empty,
                DateCreated = this.dateProvider.Now,
                SiteId = site.Id,
                Url = item.Url,
                UpdateRequired = true
            };
            await dc.Pages.AddAsync(newPage, cancellationToken);
        }

        // Save changes
        site.DateLastUpdated = this.dateProvider.Now;
        site.UpdateRequired = false;
        await dc.SaveChangesAsync(cancellationToken);
    }

    private async Task<IList<SitemapItem>> GetSitemapItems(Site site) {
        // Load XML document from stream
        var doc = new XmlDocument();
        using var stream = await this.httpClient.GetStreamAsync(site.SitemapUrl);
        doc.Load(stream);
        this.logger.LogDebug("Loaded sitemap for site {siteId} ({siteName}) from URL {siteUrl}.", site.Id, site.Name, site.SitemapUrl);

        // Get all items
        var mgr = new XmlNamespaceManager(doc.NameTable);
        mgr.AddNamespace("sm", "http://www.sitemaps.org/schemas/sitemap/0.9");
        var urlElements = doc.SelectNodes("/sm:urlset/sm:url[sm:loc and sm:lastmod]", mgr)?.Cast<XmlElement>();
        if (urlElements is null || !urlElements.Any()) return new List<SitemapItem>();

        // Parse all urls in sitemap
        var r = new List<SitemapItem>();
        foreach (var item in urlElements) {
            var loc = item.SelectSingleNode("sm:loc", mgr)?.InnerText ?? string.Empty;
            var lastModString = item.SelectSingleNode("sm:lastmod", mgr)?.InnerText ?? string.Empty;
            var lastModDate = DateTime.MinValue;
            var isValid = !string.IsNullOrEmpty(loc)
                && !string.IsNullOrEmpty(lastModString)
                && DateTime.TryParse(lastModString, out lastModDate)
                && Uri.TryCreate(loc, UriKind.Absolute, out _);
            if (!isValid) {
                this.logger.LogWarning("Sitemap item {loc} for site {siteId} ({siteName}) is invalid.", loc, site.Id, site.Name);
                continue;
            }
            r.Add(new SitemapItem(loc, lastModDate));
        }
        return r;
    }

    private async Task<bool> UpdateSinglePageAsync(Page page, CancellationToken stoppingToken) {
        // Download HTML
        string html;
        try {
            html = await this.httpClient.GetStringAsync(page.Url, stoppingToken);
        } catch (Exception ex) {
            this.logger.LogWarning(ex, "Cannot download HTML from page {pageId} ({pageUrl}).", page.Id, page.Url);
            return false;
        }

        // Parse using HTML Agility pack
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Declare helper functions
        static string? getStringFromMetadata(HtmlDocument doc, params string[] selectors) {
            foreach (var selector in selectors) {
                var value = doc.DocumentNode.SelectSingleNode(selector)?.GetAttributeValue("content", null);
                if (value != null) return value;
            }
            return null;
        }
        static string limitMaxLength(string s, int maxLength) => s.Length > maxLength ? s[..maxLength] : s;
        static string normalizeWhitespace(string text, bool multiLine = true) {
            var sb = new StringBuilder();

            // Normalize lines and remove extra whitespace
            using var r = new StringReader(text);
            while (r.Peek() > -1) {
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                var line = r.ReadLine();
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
                if (line is null) continue;

                // Replace all whitespace with regular spaces
                line = NormalizeWhitespaceRegex().Replace(line, " ");

                // Remove internal double spaces
                while (line.Contains("  ")) line = line.Replace("  ", " ");

                // Remove whitespace at beginning and end
                line = line.Trim();

                // Ignore empty lines
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Write normalized line
                sb.Append(line);
                if (multiLine) sb.Append("\r\n");
            }

            return sb.ToString();
        }

        // Update values
        page.Text = normalizeWhitespace(doc.DocumentNode.SelectSingleNode(page.Site?.ContentXPath ?? "//main").InnerText);
        page.Title = normalizeWhitespace(getStringFromMetadata(doc, this.options.TitleMetaFields.ToArray()) ?? page.Url, multiLine: false);
        page.Description = normalizeWhitespace(getStringFromMetadata(doc, this.options.DescriptionMetaFields.ToArray()) ?? page.Title, multiLine: false);
        page.Title = limitMaxLength(page.Title, 1000);
        page.Description = limitMaxLength(page.Description, 1000);
        page.DateLastUpdated = this.dateProvider.Now;
        page.UpdateRequired = false;

        if (string.IsNullOrEmpty(page.Text)) this.logger.LogWarning("The page {pageId} ({pageUrl}) has null or empty content node.", page.Id, page.Url);
        this.logger.LogInformation("Updated text of page {pageId} ({pageUrl}).", page.Id, page.Url);

        return true;
    }

    [GeneratedRegex("\\s")]
    private static partial Regex NormalizeWhitespaceRegex();
}
