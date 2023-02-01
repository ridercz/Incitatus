using System.ComponentModel.DataAnnotations.Schema;

namespace Altairis.Incitatus.Data;

public class Page {

    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Site))]
    public Guid SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    [Required, MaxLength(1000)]
    public required string Title { get; set; }

    [Required, MaxLength(1000), Url]
    public required string Url { get; set; }

    [Required, MaxLength(int.MaxValue)]
    public string? Text { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    public DateTime? DateLastUpdated { get; set; }

    public bool UpdateRequired { get; set; }

    public ICollection<EventLogItem> Events { get; set; } = new HashSet<EventLogItem>();

}
