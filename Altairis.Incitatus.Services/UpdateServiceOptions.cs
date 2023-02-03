namespace Altairis.Incitatus.Services;

public record UpdateServiceOptions {

    public TimeSpan PooledCollectionLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan DelayBetweenPageRequests { get; set; } = TimeSpan.Zero;

    public ICollection<string> TitleMetaFields { get; set; } = new[] {
        "/html/head/meta[@name='title']",
        "/html/head/meta[@name='dc:title']",
        "/html/head/meta[@name='dcterms:title']",
        "/html/head/meta[@name='twitter:title']",
        "/html/head/meta[@property='og:title']"
    };

    public ICollection<string> DescriptionMetaFields { get; set; } = new[] {
        "/html/head/meta[@name='description']",
        "/html/head/meta[@name='dc:abstract']",
        "/html/head/meta[@name='dcterms:abstract']",
        "/html/head/meta[@name='twitter:description']",
        "/html/head/meta[@property='og:description']"
    };

}