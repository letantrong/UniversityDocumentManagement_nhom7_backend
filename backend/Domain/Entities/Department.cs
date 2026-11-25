using Domain.Common;

namespace Domain.Entities;

public class Department : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }

    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public ICollection<Station> Stations { get; set; } = new List<Station>();
}
