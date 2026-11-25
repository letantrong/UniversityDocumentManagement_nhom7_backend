using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Approval : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public Guid ApproverId { get; set; }
    public User Approver { get; set; } = null!;

    public int StepOrder { get; set; } = 1;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? Comments { get; set; }
    public DateTime? DecisionAt { get; set; }
}
