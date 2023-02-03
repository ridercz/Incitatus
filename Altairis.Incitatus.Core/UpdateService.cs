using Altairis.Incitatus.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altairis.Incitatus.Core;

public class UpdateService : BackgroundService {
    private readonly ILogger<UpdateService> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly UpdateServiceOptions options;

    public UpdateService(IOptions<UpdateServiceOptions> optionsAccessor, ILogger<UpdateService> logger, IServiceScopeFactory scopeFactory) {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.options = optionsAccessor?.Value ?? new UpdateServiceOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            using (var scope = scopeFactory.CreateScope()) {
                using var dc = scope.ServiceProvider.GetRequiredService<IncitatusDbContext>();
                var sitemapUpdater = scope.ServiceProvider.GetRequiredService<ISitemapUpdater>();

                // Process sites that need to be updated
                var sitesToUpdate = await dc.Sites.Where(x => x.UpdateRequired).Select(x => x.Id).ToListAsync(cancellationToken: stoppingToken);
                if (sitesToUpdate.Any()) {
                    foreach (var site in sitesToUpdate) {
                        await sitemapUpdater.UpdateSitemap(site, stoppingToken);
                    }
                } else {
                    this.logger.LogDebug("No sites need to be updated.");
                }
            }

            // Wait for a while
            await Task.Delay(this.options.PollInterval, stoppingToken);
        }
    }
}

public record UpdateServiceOptions {

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

}