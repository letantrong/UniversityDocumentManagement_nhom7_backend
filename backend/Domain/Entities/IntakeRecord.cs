using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class IntakeRecord : BaseEntity
{
    public string IntakeNumber { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderOrganization { get; set; }
    public string? SenderContact { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public IntakeStatus Status { get; set; } = IntakeStatus.Received;
    public string? Notes { get; set; }

    public Guid StationId { get; set; }
    public Station Station { get; set; } = null!;

    public Guid? HandlerId { get; set; }
    public User? Handler { get; set; }

    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
}
