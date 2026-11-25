using Domain.Common;

namespace Domain.Entities;

public class Station : BaseEntity
{
    public string StationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public ICollection<IntakeRecord> IntakeRecords { get; set; } = new List<IntakeRecord>();
}
