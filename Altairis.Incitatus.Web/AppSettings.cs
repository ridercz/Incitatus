namespace Altairis.Incitatus.Web;

public record AppSettings {

    public string HomepageRedirectUrl { get; set; } = "/docs";

    public ApiSettings Api { get; set; } = new();

    public UpdateServiceSettings UpdateService { get; set; } = new();

}

public record ApiSettings {
    public Uri? TermsOfServiceUrl { get; set; }
    public Uri? ContactUrl { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
}

public record UpdateServiceSettings {
    public bool Enabled { get; set; } = true;
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