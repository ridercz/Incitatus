using System.ComponentModel.DataAnnotations.Schema;

namespace Altairis.Incitatus.Data;

public class EventLogItem {

    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Site))]
    public Guid SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    [ForeignKey(nameof(Page))]
    public Guid? PageId { get; set; }

    [ForeignKey(nameof(PageId))]
    public Page? Page { get; set; }

    public DateTime DateCreated { get; set; }

    public required EventLogItemSeverity Severity { get; set; }

    [MaxLength(int.MaxValue)]
    public required string Message { get; set; }

}

public enum EventLogItemSeverity {
    Information = 1,
    Warning = 2,
    Error = 3,
}