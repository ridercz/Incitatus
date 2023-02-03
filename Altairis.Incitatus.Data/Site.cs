namespace Altairis.Incitatus.Data;

public class Site {

    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(1000)]
    public required string Name { get; set; }

    [Required, MaxLength(1000), Url]
    public required string Url { get; set; }

    [Required, MaxLength(1000), Url]
    public required string SitemapUrl { get; set; }

    [Required, MaxLength(1000)]
    public required string ContentXPath { get; set; }

    [Required, StringLength(UpdateKeyLength, MinimumLength = UpdateKeyLength)]
    public required string UpdateKey { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    public DateTime
        ? DateLastUpdated { get; set; }

    public bool UpdateRequired { get; set; }

    public ICollection<Page> Pages { get; set; } = new HashSet<Page>();

    // Helper methods

    private const string UpdateKeyAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int UpdateKeyLength = 30;

    public static string CreateRandomUpdateKey() {
        var updateKeyChars = new char[UpdateKeyLength];
        for (var i = 0; i < UpdateKeyLength; i++) {
            var randomCharIndex = System.Security.Cryptography.RandomNumberGenerator.GetInt32(UpdateKeyAlphabet.Length);
            updateKeyChars[i] = UpdateKeyAlphabet[randomCharIndex];
        }
        return new(updateKeyChars);
    }

}
