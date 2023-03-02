using Microsoft.Extensions.Options;

namespace Altairis.Incitatus.Web.Security;

public interface IBearerTokenValidator {

    public Task<bool> ValidateToken(string token);

}

public class ConfigurationBearerTokenValidator : IBearerTokenValidator {
    private string validToken;

    public ConfigurationBearerTokenValidator(IConfiguration configuration, IOptions<ConfigurationBearerTokenValidatorOptions>? options = null) {
        var keyName = (options?.Value ?? new ConfigurationBearerTokenValidatorOptions()).ConfigurationKeyName;
        this.validToken = configuration[keyName] ?? throw new FormatException($"The {keyName} configuration key is missing or invalid.");
    }

    public Task<bool> ValidateToken(string token) => Task.FromResult(this.validToken.Equals(token, StringComparison.Ordinal));
}

public class ConfigurationBearerTokenValidatorOptions {

    public string ConfigurationKeyName { get; set; } = "AuthorizationToken";

}
