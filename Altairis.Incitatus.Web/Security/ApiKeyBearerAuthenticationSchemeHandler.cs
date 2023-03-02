using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Altairis.Incitatus.Web.Security;

public class ApiKeyBearerAuthenticationSchemeHandler : AuthenticationHandler<ApiKeyBearerAuthenticationSchemeOptions> {
    private const string BearerSchemePrefix = "Bearer ";

    private readonly IBearerTokenValidator tokenValidator;

    public ApiKeyBearerAuthenticationSchemeHandler(IOptionsMonitor<ApiKeyBearerAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IBearerTokenValidator tokenValidator) : base(options, logger, encoder, clock) {
        this.tokenValidator = tokenValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
        // Get the Authorization header value
        var headerValue = this.Context.Request.Headers.Authorization.FirstOrDefault(x => x != null && x.StartsWith(BearerSchemePrefix, StringComparison.OrdinalIgnoreCase) && x.Length > BearerSchemePrefix.Length);
        if (headerValue == null) return AuthenticateResult.Fail("Authorization header is missing or invalid.");

        // Get the bearer token
        var token = headerValue[BearerSchemePrefix.Length..];
        if (!await this.tokenValidator.ValidateToken(token)) return AuthenticateResult.Fail("Bearer token is invalid.");

        // Token is valid
        var principal = new ClaimsPrincipal(new ClaimsIdentity(this.Scheme.Name));
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

}

public class ApiKeyBearerAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

public static class ApiKeyBearerDefaults {
    public const string Scheme = "ApiKeyBearer";
}