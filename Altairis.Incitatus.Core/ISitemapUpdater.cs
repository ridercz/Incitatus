namespace Altairis.Incitatus.Core;

public interface ISitemapUpdater {
    Task UpdateSitemap(Guid siteId, CancellationToken cancellationToken);
}