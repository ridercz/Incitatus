namespace Altairis.Incitatus.Web;

public record AppSettings {

    public string HomepageRedirectUrl { get; set; } = "/docs";

    public ApiSettings Api { get; set; } = new ApiSettings();

}

public record ApiSettings {
    public Uri? TermsOfServiceUrl { get; set; }
    public Uri? ContactUrl { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
}